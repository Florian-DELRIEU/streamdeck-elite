using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    public enum GraphType
    {
        HorizontalBar,
        VerticalBar,
        Dial,
        Curve
    }

    /// <summary>Colour of the graph when its condition is true (the first true rule wins).</summary>
    public class ColorRule
    {
        public ConditionOperator Operator { get; set; }
        public string Operand { get; set; } = "";
        public string Color { get; set; } = GraphConfig.DefaultRuleColor;
    }

    /// <summary>Graph of one view of a Graph key (v3.4, docs/L10-v3.md).</summary>
    public class GraphView
    {
        public GraphType Type { get; set; } = GraphType.HorizontalBar;
        /// <summary>Minimum and maximum: a number, or a key of the store (e.g. ship.FuelCapacity.Main); empty = automatic for a curve.</summary>
        public string Min { get; set; } = "0";
        public string Max { get; set; } = "100";
        public string Color { get; set; } = GraphConfig.DefaultColor;
        public List<ColorRule> Rules { get; } = new List<ColorRule>();
        public int CurvePoints { get; set; } = GraphConfig.DefaultCurvePoints;
        public int CurveInterval { get; set; } = GraphConfig.DefaultCurveInterval;

        public string Signature
        {
            get
            {
                return string.Join("|", Type, Min, Max, Color, CurvePoints, CurveInterval,
                    string.Join(";", Rules.Select(r => r.Operator + "," + r.Operand + "," + r.Color)));
            }
        }
    }

    /// <summary>
    /// Reading of the graph settings of a view: graphType, graphMin, graphMax, graphColor, colorRule{r}Op / Value /
    /// Color, curvePoints, curveInterval, followed by the view number for views 2 to 4 (as the other settings).
    /// </summary>
    public static class GraphConfig
    {
        public const int MaxRules = 4;
        public const string DefaultColor = "#a7d7d6";      // light blue of the icon pack: ON / ready
        public const string DefaultRuleColor = "#ff4646";  // red of the icon pack: alert
        public const string TrackColor = "#3a3a3a";
        public const int DefaultCurvePoints = 60;
        public const int DefaultCurveInterval = 5;
        public const int MinCurvePoints = 5;

        public static GraphView Parse(JObject settings, string suffix)
        {
            settings = settings ?? new JObject();
            var view = new GraphView
            {
                Type = ParseType(Text(settings, "graphType" + suffix)),
                Min = Text(settings, "graphMin" + suffix, "0").Trim(),
                Max = Text(settings, "graphMax" + suffix, "100").Trim(),
                Color = NonEmpty(Text(settings, "graphColor" + suffix), DefaultColor),
                CurvePoints = Clamp(Text(settings, "curvePoints" + suffix), DefaultCurvePoints, MinCurvePoints, History.MaxPoints),
                CurveInterval = Clamp(Text(settings, "curveInterval" + suffix), DefaultCurveInterval, History.MinInterval, 3600),
            };

            for (int r = 1; r <= MaxRules; r++)
            {
                var op = Condition.ParseOperator(Text(settings, "colorRule" + r + "Op" + suffix));
                if (op == ConditionOperator.None)
                    continue;
                view.Rules.Add(new ColorRule
                {
                    Operator = op,
                    Operand = Text(settings, "colorRule" + r + "Value" + suffix),
                    Color = NonEmpty(Text(settings, "colorRule" + r + "Color" + suffix), DefaultRuleColor),
                });
            }

            return view;
        }

        public static GraphType ParseType(string text)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "vbar": return GraphType.VerticalBar;
                case "dial": return GraphType.Dial;
                case "curve": return GraphType.Curve;
                default: return GraphType.HorizontalBar;
            }
        }

        /// <summary>
        /// A bound: a number (in the displayed unit), or a key whose value goes through the factor and offset of the view,
        /// like the value itself. Null when empty or unknown.
        /// </summary>
        public static double? Bound(string text, Func<string, JToken> read, ValueSettings display)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            double number;
            if (Condition.TryParseNumber(text, out number))
                return number;

            return History.Number(ImageRules.Displayed(read(text.Trim()), display));
        }

        /// <summary>Keys used as bounds (to redraw when they change).</summary>
        public static IEnumerable<string> BoundKeys(GraphView view)
        {
            double number;
            foreach (var bound in new[] { view.Min, view.Max })
            {
                if (!string.IsNullOrWhiteSpace(bound) && !Condition.TryParseNumber(bound, out number))
                    yield return bound.Trim();
            }
        }

        /// <summary>Colour of the first true rule on the tested (displayed) value, otherwise the colour of the view.</summary>
        public static string ColorOf(GraphView view, JToken tested)
        {
            foreach (var rule in view.Rules)
            {
                if (Condition.Evaluate(tested, rule.Operator, rule.Operand))
                    return rule.Color;
            }
            return view.Color;
        }

        /// <summary>Position (0 to 1) of a value between the bounds; 0 without value or with wrong bounds.</summary>
        public static double Fraction(double? value, double? min, double? max)
        {
            if (!value.HasValue || !min.HasValue || !max.HasValue || max.Value <= min.Value)
                return 0;
            return Math.Max(0, Math.Min(1, (value.Value - min.Value) / (max.Value - min.Value)));
        }

        private static string Text(JObject settings, string name, string defaultValue = "")
        {
            var token = settings[name];
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;
            return token.Type == JTokenType.String ? (string)token : token.ToString();
        }

        private static string NonEmpty(string text, string fallback)
        {
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        private static int Clamp(string text, int fallback, int min, int max)
        {
            double number;
            if (!Condition.TryParseNumber(text, out number))
                return fallback;
            return (int)Math.Max(min, Math.Min(max, Math.Round(number, MidpointRounding.AwayFromZero)));
        }

        internal static string Format(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";
        }
    }
}
