using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// One view ("drawer") of a Data key: its data and text, its icon, its command (docs/L5-tiroir.md).
    /// </summary>
    public class DataView
    {
        public DataView(int number, string source, ValueSettings display)
        {
            Number = number;
            Source = source;
            Display = display;
        }

        /// <summary>1 to 4.</summary>
        public int Number { get; }
        public string Source { get; }
        public ValueSettings Display { get; }
        public bool ShowText { get; internal set; } = true;
        public string DefaultImage { get; internal set; } = "";
        public List<ImageRule> Rules { get; } = new List<ImageRule>();
        public string Command { get; internal set; } = "";
        /// <summary>Free keyboard shortcut (physical key codes, see Hotkey), sent after the command.</summary>
        public string Hotkey { get; internal set; } = "";
        public string HotkeyText { get; internal set; } = "";
        public string Sound { get; internal set; } = "";

        /// <summary>A view without default image and without rule uses the icon of view 1.</summary>
        public bool HasOwnIcon
        {
            get { return DefaultImage.Length > 0 || Rules.Count > 0; }
        }
    }

    /// <summary>
    /// Settings of a Data key (action com.mhwlng.elite.value), read from the settings JSON.
    /// View 1 uses the names of L3/L4 (source, prefix, ..., showText, backgroundImage, rule1Op..., pressCommand, clickSound);
    /// views 2 to 4 the same names followed by their number (source2, rule1Op2, pressCommand2...). Added in L7:
    /// pressHotkey / pressHotkeyText (per view) and currentView (displayed view, saved by the plugin).
    /// Inheritance: absent showText of a view = the one of view 1; a view without its own icon uses the icon of view 1;
    /// commands, shortcuts and sounds are never inherited.
    /// </summary>
    public class DataKeyConfig
    {
        public const int MaxViews = 4;
        public const int MaxRules = 4;
        private const string NoFile = "No file...";

        /// <summary>View 1 is always present; views 2 to 4 only when they have a key, a command, a shortcut or their own icon.</summary>
        public List<DataView> Views { get; } = new List<DataView>();
        public PressMode PressMode { get; private set; }

        /// <summary>Number (1 to 4) of the view displayed when the key was last seen; 1 when absent.</summary>
        public int CurrentViewNumber { get; private set; } = 1;

        /// <summary>Index in Views of the view CurrentViewNumber, 0 when it no longer exists.</summary>
        public int CurrentViewIndex
        {
            get
            {
                int index = Views.FindIndex(v => v.Number == CurrentViewNumber);
                return index < 0 ? 0 : index;
            }
        }

        public DataView MainView
        {
            get { return Views[0]; }
        }

        /// <summary>View whose icon is shown while the given view is displayed.</summary>
        public DataView IconViewOf(DataView view)
        {
            return view.HasOwnIcon ? view : MainView;
        }

        public static DataKeyConfig FromSettings(JObject settings, Action<string> warn = null)
        {
            settings = settings ?? new JObject();
            var config = new DataKeyConfig();
            var emptyText = Text(settings, "emptyText", null);
            var mainShowText = Flag(settings, "showText", true);

            for (int i = 1; i <= MaxViews; i++)
            {
                var suffix = i == 1 ? "" : i.ToString();
                int number = i;
                var display = ValueSettings.Parse(
                    Text(settings, "prefix" + suffix, ""), Text(settings, "suffix" + suffix, ""),
                    Text(settings, "decimals" + suffix, ""), Text(settings, "scale" + suffix, ""), Text(settings, "offset" + suffix, ""),
                    Flag(settings, "compact" + suffix, false), emptyText,
                    warn == null ? (Action<string>)null : message => warn("view " + number + ": " + message));

                var view = new DataView(i, Text(settings, "source" + suffix, "").Trim(), display)
                {
                    ShowText = i == 1 ? mainShowText : Flag(settings, "showText" + suffix, mainShowText),
                    DefaultImage = FileName(settings, "backgroundImage" + suffix),
                    Command = Text(settings, "pressCommand" + suffix, "").Trim(),
                    Hotkey = Text(settings, "pressHotkey" + suffix, "").Trim(),
                    HotkeyText = Text(settings, "pressHotkeyText" + suffix, "").Trim(),
                    Sound = FileName(settings, "clickSound" + suffix),
                };

                for (int r = 1; r <= MaxRules; r++)
                {
                    var op = Condition.ParseOperator(Text(settings, "rule" + r + "Op" + suffix, ""));
                    if (op != ConditionOperator.None)
                        view.Rules.Add(new ImageRule(op, Text(settings, "rule" + r + "Value" + suffix, ""), FileName(settings, "rule" + r + "Image" + suffix)));
                }

                if (i == 1 || view.Source.Length > 0 || view.Command.Length > 0 || view.Hotkey.Length > 0 || view.HasOwnIcon)
                    config.Views.Add(view);
            }

            config.PressMode = PressGesture.ParseMode(Text(settings, "pressMode", ""));

            int current;
            if (int.TryParse(Text(settings, "currentView", "1"), out current) && current >= 1 && current <= MaxViews)
                config.CurrentViewNumber = current;
            return config;
        }

        private static string Text(JObject settings, string name, string defaultValue)
        {
            var token = settings[name];
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;
            return token.Type == JTokenType.String ? (string)token : token.ToString();
        }

        // the property inspector may send "No file..." for an empty file field
        private static string FileName(JObject settings, string name)
        {
            var value = Text(settings, name, "").Trim();
            return value == NoFile ? "" : value;
        }

        private static bool Flag(JObject settings, string name, bool defaultValue)
        {
            var token = settings[name];
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;
            if (token.Type == JTokenType.Boolean)
                return (bool)token;

            bool parsed;
            return bool.TryParse(token.ToString(), out parsed) ? parsed : defaultValue;
        }
    }
}
