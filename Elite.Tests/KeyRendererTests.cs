using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Text drawn into the key image (Elite/Generic/KeyRenderer.cs, v3.2, issue #1).
    /// </summary>
    [TestFixture]
    public class KeyRendererTests
    {
        // monospace measure: every character is 0.6 x the size wide, lines are 1.2 x the size high
        private static float Width(string text, float size)
        {
            return text.Length * 0.6f * size;
        }

        private static float LineHeight(float size)
        {
            return 1.2f * size;
        }

        [Test]
        public void Wrap_AtTheSpaces()
        {
            // 66 key pixels at size 10: 11 characters per line
            var lines = KeyRenderer.Wrap("Fuel tank level", 66, t => Width(t, 10));
            Assert.That(lines, Is.EqualTo(new[] { "Fuel tank", "level" }));
        }

        [Test]
        public void Wrap_ALongValueWithoutSpace_AfterThePunctuation()
        {
            var lines = KeyRenderer.Wrap("-12.345678,43.987654", 66, t => Width(t, 10));
            Assert.That(lines, Is.EqualTo(new[] { "-12.345678,", "43.987654" }));
            Assert.That(lines.All(l => Width(l, 10) <= 66), Is.True);
            Assert.That(string.Concat(lines), Is.EqualTo("-12.345678,43.987654"), "nothing lost");
        }

        [Test]
        public void Wrap_AWordWithoutBreakCharacter_Anywhere()
        {
            var lines = KeyRenderer.Wrap("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 66, t => Width(t, 10));
            Assert.That(lines, Is.EqualTo(new[] { "ABCDEFGHIJK", "LMNOPQRSTUV", "WXYZ" }));
        }

        [Test]
        public void Layout_KeepsTheExplicitLines_AndReducesTheSizeToFit()
        {
            var style = new TextStyle { Size = 30, Wrap = true, Fit = true };
            var layout = KeyRenderer.Layout("Fuel\n27.5 t", style, Width, LineHeight);

            Assert.That(layout.Lines.First(), Is.EqualTo("Fuel"));
            Assert.That(layout.Size, Is.LessThan(30), "30 px does not fit 2 lines in 66 px");
            Assert.That(layout.Height, Is.LessThanOrEqualTo(66));
            Assert.That(layout.Lines.All(l => Width(l, layout.Size) <= KeyRenderer.AvailableWidth), Is.True);
        }

        [Test]
        public void Layout_AWord_IsReducedRatherThanCut()
        {
            var layout = KeyRenderer.Layout("Supercruise", new TextStyle { Size = 30 }, Width, LineHeight);
            Assert.That(layout.Lines, Is.EqualTo(new[] { "Supercruise" }));
            Assert.That(layout.Size, Is.EqualTo(10));
        }

        [Test]
        public void Layout_ACoordinate_IsCutAfterThePunctuation()
        {
            // 20 characters without space: whole, it would need a size of 5 (below the minimum)
            var layout = KeyRenderer.Layout("-12.345678,43.987654", new TextStyle { Size = 30 }, Width, LineHeight);
            Assert.That(layout.Size, Is.EqualTo(13));
            Assert.That(layout.Lines, Is.EqualTo(new[] { "-12.", "345678,", "43.", "987654" }));
        }

        [Test]
        public void Layout_WithoutFit_KeepsTheSize_AndWithoutWrap_KeepsTheParagraphs()
        {
            var noFit = KeyRenderer.Layout("Very long text for a small key", new TextStyle { Size = 30, Fit = false }, Width, LineHeight);
            Assert.That(noFit.Size, Is.EqualTo(30));

            var noWrap = KeyRenderer.Layout("A B C\nD", new TextStyle { Size = 10, Wrap = false, Fit = false }, Width, LineHeight);
            Assert.That(noWrap.Lines, Is.EqualTo(new[] { "A B C", "D" }));
        }

        [Test]
        public void Layout_NeverBelowTheMinimumSize()
        {
            var text = string.Concat(Enumerable.Repeat("word ", 80));
            var layout = KeyRenderer.Layout(text, new TextStyle { Size = 20 }, Width, LineHeight);
            Assert.That(layout.Size, Is.EqualTo(TextStyle.MinSize));
        }

        [Test]
        public void Render_GivesA144PixelPng_WithTheRealFonts()
        {
            var style = new TextStyle { Size = 30, Color = "#a7d7d6" };
            var base64 = KeyRenderer.RenderBase64(null, null, "-12.345678, 43.987654", style);

            Assert.That(base64, Does.StartWith("data:image/png;base64,"));
            using (var image = Decode(base64))
            {
                Assert.That(image.Width, Is.EqualTo(KeyRenderer.ImagePixels));
                Assert.That(image.Height, Is.EqualTo(KeyRenderer.ImagePixels));
                Assert.That(CountColored(image), Is.GreaterThan(100), "some text is drawn");
            }
        }

        [Test]
        public void Render_IsThreadSafe()
        {
            // the historic buttons crash with "object is currently in use elsewhere" when GDI+ objects are shared
            var style = new TextStyle { Size = 24 };
            Assert.DoesNotThrow(() => System.Threading.Tasks.Parallel.For(0, 40,
                i => KeyRenderer.RenderBase64(null, null, "Value " + i, style)));
        }

        [Test]
        public void TextSettings_OfAView_AndOldKeysUnchanged()
        {
            var old = DataKeyConfig.FromSettings(JObject.Parse("{ \"source\":\"status.Fuel.FuelMain\" }"));
            Assert.That(old.MainView.Text.Draw, Is.False, "keys created before v3.2: title of the software");
            Assert.That(old.MainView.Text.Size, Is.EqualTo(TextStyle.DefaultSize));
            Assert.That(old.MainView.Text.Wrap && old.MainView.Text.Fit, Is.True);

            var drawn = DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""a.b"", ""source2"":""c.d"",
                ""drawText2"":true, ""fontSize2"":""40"", ""fontName2"":""Tahoma"", ""fontColor2"":""#ff4646"",
                ""fontBold2"":true, ""textPosition2"":""bottom"", ""textWrap2"":false, ""textFit2"":false, ""fontSize"":""500"" }"));
            var view2 = drawn.Views[1].Text;
            Assert.That(view2.Draw, Is.True);
            Assert.That(view2.Size, Is.EqualTo(40));
            Assert.That(view2.FontName, Is.EqualTo("Tahoma"));
            Assert.That(view2.Color, Is.EqualTo("#ff4646"));
            Assert.That(view2.Bold, Is.True);
            Assert.That(view2.Position, Is.EqualTo(TextPosition.Bottom));
            Assert.That(view2.Wrap || view2.Fit, Is.False);
            Assert.That(drawn.MainView.Text.Draw, Is.False, "never inherited from another view");
            Assert.That(drawn.MainView.Text.Size, Is.EqualTo(TextStyle.MaxSize), "500 is clamped");
        }

        /// <summary>
        /// Visual check of the rendering: writes PNG files into the folder of the ZV_RENDER_PREVIEW variable.
        /// Run on demand only: nunit3-console Elite.Tests.dll --where "test =~ WritePreviews".
        /// </summary>
        [Test, Explicit]
        public void WritePreviews()
        {
            var folder = Environment.GetEnvironmentVariable("ZV_RENDER_PREVIEW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty, "set ZV_RENDER_PREVIEW");
            Directory.CreateDirectory(folder);

            Save(folder, "1-coordonnees", KeyRenderer.RenderBase64(null, null, "Lat -12.345678\nLon 43.987654", new TextStyle { Size = 30 }));
            Save(folder, "2-grand", KeyRenderer.RenderBase64(null, null, "27", new TextStyle { Size = 40, Color = "#a7d7d6" }));
            Save(folder, "3-long", KeyRenderer.RenderBase64(null, null, "Wregoe TC-X b29-0 Lagrange", new TextStyle { Size = 22 }));
            Save(folder, "4-gras-bas", KeyRenderer.RenderBase64(null, null, "FUEL\n31.5 t", new TextStyle { Size = 20, Bold = true, Position = TextPosition.Bottom }));
            Save(folder, "5-tahoma", KeyRenderer.RenderBase64(null, null, "Tahoma 18", new TextStyle { Size = 18, FontName = "Tahoma" }));
        }

        private static void Save(string folder, string name, string base64)
        {
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), Convert.FromBase64String(base64.Substring(base64.IndexOf(',') + 1)));
        }

        private static Bitmap Decode(string base64)
        {
            var bytes = Convert.FromBase64String(base64.Substring(base64.IndexOf(',') + 1));
            return new Bitmap(new MemoryStream(bytes));
        }

        private static int CountColored(Bitmap image)
        {
            int count = 0;
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    var c = image.GetPixel(x, y);
                    if (c.R > 60 || c.G > 60 || c.B > 60)
                        count++;
                }
            return count;
        }
    }
}
