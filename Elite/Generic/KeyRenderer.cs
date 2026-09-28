using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using BarRaider.SdTools;

namespace Elite.Generic
{
    /// <summary>How a word longer than the line is cut.</summary>
    public enum WordBreak
    {
        None,
        Punctuation,
        Anywhere
    }

    public enum TextPosition
    {
        Top,
        Middle,
        Bottom
    }

    /// <summary>
    /// Text drawn into the key image by the plugin (v3.2, issue #1): free size, font, colour, position, wrap and fit.
    /// Sizes are in pixels of the 72-pixel key (the image is drawn at double density, 144 x 144).
    /// </summary>
    public class TextStyle
    {
        public const float DefaultSize = 18;
        public const float MinSize = 6;
        public const float MaxSize = 72;
        public const string DefaultFont = "Rubik";
        public const string DefaultColor = "#d18105"; // amber of the icon pack

        public bool Draw { get; set; }
        public float Size { get; set; } = DefaultSize;
        public string FontName { get; set; } = DefaultFont;
        public string Color { get; set; } = DefaultColor;
        public bool Bold { get; set; }
        public TextPosition Position { get; set; } = TextPosition.Middle;
        public bool Wrap { get; set; } = true;
        public bool Fit { get; set; } = true;

        public string Signature
        {
            get { return string.Join("|", Size.ToString(CultureInfo.InvariantCulture), FontName, Color, Bold, Position, Wrap, Fit); }
        }

        public static TextPosition ParsePosition(string text)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "top": return TextPosition.Top;
                case "bottom": return TextPosition.Bottom;
                default: return TextPosition.Middle;
            }
        }
    }

    public class TextLayout
    {
        public List<string> Lines { get; } = new List<string>();
        public float Size { get; set; }
        public float LineHeight { get; set; }

        public float Height
        {
            get { return Lines.Count * LineHeight; }
        }
    }

    /// <summary>
    /// Layout of a text on a key (pure, the measure is injected so that it can be tested) and drawing with System.Drawing.
    /// Every drawing is done under one global lock and with objects created for it: GDI+ objects shared between threads
    /// give "object is currently in use elsewhere" (the error of the historic Hyperspace button).
    /// </summary>
    public static class KeyRenderer
    {
        public const int KeyPixels = 72;
        public const int ImagePixels = 144;
        public const float Scale = ImagePixels / (float)KeyPixels;
        public const float Margin = 3;

        // a long word without space is cut after these characters (coordinates, names...)
        private static readonly char[] BreakAfter = { ',', '.', '-', '/', ';', ':', '_' };

        private static readonly object GdiLock = new object();
        private static PrivateFontCollection privateFonts;

        public static float AvailableWidth
        {
            get { return KeyPixels - 2 * Margin; }
        }

        /// <summary>
        /// Cuts one paragraph (no "\n") into lines of at most maxWidth, at the spaces; a word longer than the line is cut
        /// according to mode (not at all, after , . - / ; : _ , or anywhere). width(text) is the width at the current size.
        /// </summary>
        public static List<string> Wrap(string paragraph, float maxWidth, Func<string, float> width, WordBreak mode = WordBreak.Anywhere)
        {
            var lines = new List<string>();
            var current = "";
            foreach (var word in (paragraph ?? "").Split(' ').Where(w => w.Length > 0))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (width(candidate) <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                    lines.Add(current);
                current = "";

                if (width(word) <= maxWidth || mode == WordBreak.None)
                {
                    current = word;
                    continue;
                }

                var pieces = BreakWord(word, maxWidth, width, mode == WordBreak.Anywhere);
                for (int i = 0; i < pieces.Count - 1; i++)
                    lines.Add(pieces[i]);
                current = pieces[pieces.Count - 1];
            }

            if (current.Length > 0 || lines.Count == 0)
                lines.Add(current);
            return lines;
        }

        private static List<string> BreakWord(string word, float maxWidth, Func<string, float> width, bool anywhere)
        {
            // segments ending after a break character: "43.5678,-12.345" -> "43." "5678," "-" "12." "345"
            var segments = new List<string>();
            int start = 0;
            for (int i = 0; i < word.Length; i++)
            {
                if (Array.IndexOf(BreakAfter, word[i]) >= 0 || i == word.Length - 1)
                {
                    segments.Add(word.Substring(start, i - start + 1));
                    start = i + 1;
                }
            }

            var pieces = new List<string>();
            var current = "";
            foreach (var segment in segments)
            {
                if (width(current + segment) <= maxWidth)
                {
                    current += segment;
                    continue;
                }

                if (current.Length > 0)
                    pieces.Add(current);
                current = "";

                if (!anywhere)
                {
                    current = segment; // still too long: the layout will reduce the size
                    continue;
                }

                // a segment still too long: cut anywhere
                foreach (var c in segment)
                {
                    if (current.Length > 0 && width(current + c) > maxWidth)
                    {
                        pieces.Add(current);
                        current = "";
                    }
                    current += c;
                }
            }

            if (current.Length > 0 || pieces.Count == 0)
                pieces.Add(current);
            return pieces;
        }

        /// <summary>
        /// Lines and size of a text: "\n" always starts a line; with Wrap the paragraphs are cut to the key width.
        /// With Fit, the first that fits: smaller size without cutting a word (down to 60 % of the size), then words cut
        /// after , . - / ; : _ (coordinates), then cut anywhere, each time from the requested size down to MinSize.
        /// width(text, size) and lineHeight(size) are in key pixels.
        /// </summary>
        public static TextLayout Layout(string text, TextStyle style, Func<string, float, float> width, Func<float, float> lineHeight)
        {
            var paragraphs = (text ?? "").Replace("\r", "").Split('\n');
            float size = Math.Max(TextStyle.MinSize, Math.Min(TextStyle.MaxSize, style.Size));

            if (!style.Fit)
                return LayoutAt(paragraphs, size, style.Wrap, WordBreak.Anywhere, width, lineHeight);

            var passes = new[]
            {
                new { Mode = WordBreak.None, Floor = Math.Max(TextStyle.MinSize, (float)Math.Floor(size * 0.6f)) },
                new { Mode = WordBreak.Punctuation, Floor = TextStyle.MinSize },
                new { Mode = WordBreak.Anywhere, Floor = TextStyle.MinSize },
            };

            TextLayout last = null;
            foreach (var pass in passes)
            {
                for (float current = size; ; current = Math.Max(pass.Floor, current - 1))
                {
                    last = LayoutAt(paragraphs, current, style.Wrap, pass.Mode, width, lineHeight);
                    if (Fits(last, width))
                        return last;
                    if (current <= pass.Floor)
                        break;
                }

                if (!style.Wrap)
                    break; // without wrapping, the word breaks change nothing
            }

            return last;
        }

        private static TextLayout LayoutAt(string[] paragraphs, float size, bool wrap, WordBreak mode,
            Func<string, float, float> width, Func<float, float> lineHeight)
        {
            var layout = new TextLayout { Size = size, LineHeight = lineHeight(size) };
            foreach (var paragraph in paragraphs)
            {
                if (wrap)
                    layout.Lines.AddRange(Wrap(paragraph, AvailableWidth, t => width(t, size), mode));
                else
                    layout.Lines.Add(paragraph);
            }

            return layout;
        }

        private static bool Fits(TextLayout layout, Func<string, float, float> width)
        {
            return layout.Height <= KeyPixels - 2 * Margin && layout.Lines.All(l => width(l, layout.Size) <= AvailableWidth);
        }

        /// <summary>
        /// Key image: background (image file bytes, or black), an optional drawing (graph), then the text. PNG in base64
        /// with its data-URI header, as SetImageAsync expects.
        /// </summary>
        public static string RenderBase64(byte[] background, Action<Graphics> drawing, string text, TextStyle style)
        {
            lock (GdiLock)
            {
                using (var bitmap = new Bitmap(ImagePixels, ImagePixels, PixelFormat.Format32bppArgb))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.Clear(Color.Black);

                    if (background != null)
                        DrawBackground(graphics, background);

                    drawing?.Invoke(graphics);

                    if (!string.IsNullOrEmpty(text) && style != null)
                        DrawText(graphics, text, style);

                    return BarRaider.SdTools.Tools.ImageToBase64(bitmap, true);
                }
            }
        }

        public static Color ParseColor(string text, Color fallback)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                    return ColorTranslator.FromHtml(text.Trim());
            }
            catch (Exception)
            {
            }

            return fallback;
        }

        private static void DrawBackground(Graphics graphics, byte[] background)
        {
            try
            {
                using (var stream = new MemoryStream(background))
                using (var image = Image.FromStream(stream))
                    graphics.DrawImage(image, 0, 0, ImagePixels, ImagePixels);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"KeyRenderer: unreadable background image: {ex.Message}");
            }
        }

        private static void DrawText(Graphics graphics, string text, TextStyle style)
        {
            var family = Family(style.FontName);
            bool fakeBold = style.Bold && !family.IsStyleAvailable(FontStyle.Bold);
            var fontStyle = style.Bold && !fakeBold ? FontStyle.Bold : RegularStyle(family);
            var fonts = new Dictionary<float, Font>();
            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            try
            {
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                Func<float, Font> font = size =>
                {
                    Font f;
                    if (!fonts.TryGetValue(size, out f))
                    {
                        f = new Font(family, size * Scale, fontStyle, GraphicsUnit.Pixel);
                        fonts[size] = f;
                    }
                    return f;
                };

                var layout = Layout(text, style,
                    (t, size) => graphics.MeasureString(t, font(size), PointF.Empty, format).Width / Scale,
                    size => font(size).GetHeight(graphics) / Scale);

                float top;
                switch (style.Position)
                {
                    case TextPosition.Top: top = Margin; break;
                    case TextPosition.Bottom: top = KeyPixels - Margin - layout.Height; break;
                    default: top = (KeyPixels - layout.Height) / 2; break;
                }

                var drawFont = font(layout.Size);
                using (var brush = new SolidBrush(ParseColor(style.Color, ParseColor(TextStyle.DefaultColor, Color.Orange))))
                {
                    for (int i = 0; i < layout.Lines.Count; i++)
                    {
                        var line = layout.Lines[i];
                        float lineWidth = graphics.MeasureString(line, drawFont, PointF.Empty, format).Width / Scale;
                        float x = (KeyPixels - lineWidth) / 2 * Scale;
                        float y = (top + i * layout.LineHeight) * Scale;
                        graphics.DrawString(line, drawFont, brush, x, y, format);
                        if (fakeBold)
                            graphics.DrawString(line, drawFont, brush, x + 1, y, format);
                    }
                }
            }
            finally
            {
                foreach (var f in fonts.Values)
                    f.Dispose();
            }
        }

        private static FontStyle RegularStyle(FontFamily family)
        {
            return family.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;
        }

        /// <summary>
        /// Font family: a font delivered with the plugin (Fonts\*.ttf), else an installed font, else Tahoma (the closest
        /// installed font to Rubik according to the icon pack), else the generic sans serif. Caller holds GdiLock.
        /// </summary>
        private static FontFamily Family(string name)
        {
            if (privateFonts == null)
            {
                privateFonts = new PrivateFontCollection();
                try
                {
                    var folder = Path.Combine(Path.GetDirectoryName(typeof(KeyRenderer).Assembly.Location) ?? "", "Fonts");
                    if (Directory.Exists(folder))
                    {
                        foreach (var file in Directory.GetFiles(folder, "*.ttf").Concat(Directory.GetFiles(folder, "*.otf")))
                            privateFonts.AddFontFile(file);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"KeyRenderer: fonts of the plugin not loaded: {ex.Message}");
                }
            }

            var wanted = string.IsNullOrWhiteSpace(name) ? TextStyle.DefaultFont : name.Trim();
            var own = privateFonts.Families.FirstOrDefault(f => string.Equals(f.Name, wanted, StringComparison.OrdinalIgnoreCase));
            if (own != null)
                return own;

            foreach (var candidate in new[] { wanted, "Tahoma" })
            {
                try
                {
                    return new FontFamily(candidate);
                }
                catch (ArgumentException)
                {
                }
            }

            return FontFamily.GenericSansSerif;
        }
    }
}
