using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// calc.* keys: values computed by the plugin from the other keys of the store (docs/L10-v3.md). Pure functions:
    /// the store passes a reader of its current values and replaces the "calc" source with the result.
    /// </summary>
    public static class DerivedKeys
    {
        public const string RemainingJumps = "calc.Route.RemainingJumps";

        // events giving the current star system (the most recent one wins)
        private static readonly string[] SystemEvents = { "FSDJump", "Location", "CarrierJump" };

        public static Dictionary<string, JToken> Compute(Func<string, JToken> read)
        {
            var result = new Dictionary<string, JToken>(StoreKeys.Comparer);
            result[RemainingJumps] = new JValue(ComputeRemainingJumps(read));
            return result;
        }

        /// <summary>
        /// Remaining jumps of the route, as the historic Route button: the game writes no FSDTarget when the last jump
        /// starts (FSDTarget 1, NavRouteClear, FSDJump), so the last RemainingJumpsInRoute stays at 1 (issue #3).
        /// 0 when the current system is the FSD target, or when the route was cleared after the last FSDTarget;
        /// otherwise FSDTarget.RemainingJumpsInRoute (0 without FSDTarget).
        /// </summary>
        public static int ComputeRemainingJumps(Func<string, JToken> read)
        {
            var targetTime = Time(read("journal.FSDTarget.timestamp"));
            if (targetTime == null)
                return 0;

            var target = Text(read("journal.FSDTarget.Name"));
            var current = CurrentSystem(read);
            if (target.Length > 0 && string.Equals(target, current, StringComparison.OrdinalIgnoreCase))
                return 0;

            var cleared = Time(read("journal.NavRouteClear.timestamp"));
            if (cleared != null && cleared.Value >= targetTime.Value)
                return 0;

            var remaining = read("journal.FSDTarget.RemainingJumpsInRoute");
            double jumps;
            return remaining != null && Condition.TryParseNumber(remaining.ToString(), out jumps) ? Math.Max(0, (int)jumps) : 0;
        }

        private static string CurrentSystem(Func<string, JToken> read)
        {
            string system = "";
            DateTime? latest = null;
            foreach (var evt in SystemEvents)
            {
                var time = Time(read("journal." + evt + ".timestamp"));
                if (time != null && (latest == null || time.Value > latest.Value))
                {
                    latest = time;
                    system = Text(read("journal." + evt + ".StarSystem"));
                }
            }

            return system;
        }

        internal static DateTime? Time(JToken token)
        {
            if (token == null)
                return null;
            if (token.Type == JTokenType.Date)
                return ((DateTime)token).ToUniversalTime();

            DateTime parsed;
            return token.Type == JTokenType.String && DateTime.TryParse((string)token, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out parsed)
                ? parsed
                : (DateTime?)null;
        }

        internal static string Text(JToken token)
        {
            return token == null || token.Type == JTokenType.Null ? "" : token.Type == JTokenType.String ? (string)token : token.ToString();
        }
    }
}
