using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Display settings of the Value action (cahier des charges §5.1), parsed from the property inspector strings.
    /// </summary>
    public class ValueSettings
    {
        public const string DefaultEmptyText = "—";

        public string Prefix { get; set; } = "";
        public string Suffix { get; set; } = "";
        public int Decimals { get; set; }
        public double Scale { get; set; } = 1;
        public double Offset { get; set; }
        public bool Compact { get; set; }
        public string EmptyText { get; set; } = DefaultEmptyText;

        /// <summary>
        /// The property inspector sends text: decimals, scale and offset accept "0,5" and "0.5".
        /// An invalid value keeps its default and is reported through warn.
        /// </summary>
        public static ValueSettings Parse(string prefix, string suffix, string decimals, string scale, string offset,
            bool compact, string emptyText, Action<string> warn = null)
        {
            return new ValueSettings
            {
                Prefix = prefix ?? "",
                Suffix = suffix ?? "",
                Decimals = Math.Max(0, Math.Min(6, (int)ParseNumber(decimals, 0, "decimals", warn))),
                Scale = ParseNumber(scale, 1, "scale", warn),
                Offset = ParseNumber(offset, 0, "offset", warn),
                Compact = compact,
                EmptyText = emptyText ?? DefaultEmptyText,
            };
        }

        private static double ParseNumber(string text, double defaultValue, string name, Action<string> warn)
        {
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            double result;
            if (double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                && !double.IsNaN(result) && !double.IsInfinity(result))
                return result;

            warn?.Invoke("invalid " + name + " '" + text + "', using " + defaultValue.ToString(CultureInfo.InvariantCulture));
            return defaultValue;
        }
    }

    /// <summary>
    /// Text shown on a Value key. Pure function: no state, no Stream Deck call.
    /// </summary>
    public static class ValueFormatter
    {
        // French formatting with a plain space as thousands separator (the fr-FR separator of Windows is U+202F,
        // not guaranteed in the Stream Deck font)
        private static readonly NumberFormatInfo Numbers = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = " ",
            NegativeSign = "-",
        };

        private static readonly string[] CompactUnits = { "k", "M", "G", "T" };

        public static string Format(JToken value, ValueSettings settings, DateTime now)
        {
            return Unescape(settings.Prefix) + FormatValue(value, settings, now) + Unescape(settings.Suffix);
        }

        public static string FormatValue(JToken value, ValueSettings settings, DateTime now)
        {
            if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                return settings.EmptyText;

            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return (bool)value ? "Oui" : "Non";
                case JTokenType.Integer:
                case JTokenType.Float:
                    return FormatNumber((double)value * settings.Scale + settings.Offset, settings);
                case JTokenType.Date:
                    return FormatDate((DateTime)value, now);
                case JTokenType.String:
                    return (string)value;
                default:
                    return value.ToString();
            }
        }

        public static string FormatNumber(double number, ValueSettings settings)
        {
            if (!settings.Compact || Math.Abs(number) < 1000)
                return number.ToString("N" + settings.Decimals, Numbers);

            // 3 significant digits: 1,23 k / 12,3 M / 123 G
            int unit = -1;
            double scaled = number;
            while (Math.Abs(scaled) >= 1000 && unit + 1 < CompactUnits.Length)
            {
                scaled /= 1000;
                unit++;
            }

            int decimals = Math.Abs(scaled) < 10 ? 2 : Math.Abs(scaled) < 100 ? 1 : 0;
            scaled = Math.Round(scaled, decimals, MidpointRounding.AwayFromZero);
            if (Math.Abs(scaled) >= 1000 && unit + 1 < CompactUnits.Length)
            {
                // 999,95 k rounds to 1000 k: show 1 M
                scaled /= 1000;
                unit++;
                decimals = 2;
            }

            return scaled.ToString("0." + new string('#', decimals), Numbers) + " " + CompactUnits[unit];
        }

        public static string FormatDate(DateTime date, DateTime now)
        {
            // the game writes UTC timestamps
            var local = date.Kind == DateTimeKind.Local ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();
            return local.Date == now.Date
                ? local.ToString("HH:mm", CultureInfo.InvariantCulture)
                : local.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// "\n" typed in the property inspector becomes a line break.
        /// </summary>
        public static string Unescape(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : text.Replace("\\n", "\n");
        }
    }
}
