using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    public class DataView
    {
        public DataView(string source, ValueSettings display)
        {
            Source = source;
            Display = display;
        }

        public string Source { get; }
        public ValueSettings Display { get; }
    }

    /// <summary>
    /// Settings of a Data key (action com.mhwlng.elite.value), read from the settings JSON.
    /// View 1 uses the names of L3 (source, prefix, suffix, decimals, scale, offset, compact), views 2 to 4 the same
    /// names followed by their number (source2, prefix2...). Absent settings take their default value,
    /// so that the keys created in L3 keep working.
    /// </summary>
    public class DataKeyConfig
    {
        public const int MaxViews = 4;
        public const int MaxRules = 4;

        /// <summary>View 1 is always present; views 2 to 4 only when they have a key.</summary>
        public List<DataView> Views { get; } = new List<DataView>();
        public List<ImageRule> Rules { get; } = new List<ImageRule>();
        public bool ShowText { get; private set; }
        public bool PressCycle { get; private set; }
        public string PressCommand { get; private set; }
        public string ClickSound { get; private set; }
        public string DefaultImage { get; private set; }

        public DataView MainView
        {
            get { return Views[0]; }
        }

        public static DataKeyConfig FromSettings(JObject settings, Action<string> warn = null)
        {
            settings = settings ?? new JObject();
            var config = new DataKeyConfig();
            var emptyText = Text(settings, "emptyText", null);

            for (int i = 1; i <= MaxViews; i++)
            {
                var suffix = i == 1 ? "" : i.ToString();
                var source = Text(settings, "source" + suffix, "").Trim();
                if (i > 1 && source.Length == 0)
                    continue;

                var display = ValueSettings.Parse(
                    Text(settings, "prefix" + suffix, ""), Text(settings, "suffix" + suffix, ""),
                    Text(settings, "decimals" + suffix, ""), Text(settings, "scale" + suffix, ""), Text(settings, "offset" + suffix, ""),
                    Flag(settings, "compact" + suffix, false), emptyText,
                    warn == null ? (Action<string>)null : message => warn("view " + i + ": " + message));
                config.Views.Add(new DataView(source, display));
            }

            for (int i = 1; i <= MaxRules; i++)
            {
                var op = Condition.ParseOperator(Text(settings, "rule" + i + "Op", ""));
                if (op != ConditionOperator.None)
                    config.Rules.Add(new ImageRule(op, Text(settings, "rule" + i + "Value", ""), Text(settings, "rule" + i + "Image", "")));
            }

            config.ShowText = Flag(settings, "showText", true);
            config.PressCycle = Flag(settings, "pressCycle", false);
            config.PressCommand = Text(settings, "pressCommand", "").Trim();
            config.ClickSound = Text(settings, "clickSound", "");
            config.DefaultImage = Text(settings, "backgroundImage", "");
            return config;
        }

        private static string Text(JObject settings, string name, string defaultValue)
        {
            var token = settings[name];
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;
            return token.Type == JTokenType.String ? (string)token : token.ToString();
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
