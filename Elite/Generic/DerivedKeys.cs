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
        public const string FuelPercent = "calc.Fuel.Percent";
        public const string CargoPercent = "calc.Cargo.Percent";
        public const string HullPercent = HullTracker.Key; // stateful: HullTracker
        public const string JumpRange = "calc.Jump.Range";
        public const string FsdSupercharged = "calc.Fsd.Supercharged";

        // events giving the current star system (the most recent one wins)
        private static readonly string[] SystemEvents = { "FSDJump", "Location", "CarrierJump" };

        public static Dictionary<string, JToken> Compute(Func<string, JToken> read)
        {
            var result = new Dictionary<string, JToken>(StoreKeys.Comparer);
            result[RemainingJumps] = new JValue(ComputeRemainingJumps(read));
            Put(result, FuelPercent, Percent(Number(read("status.Fuel.FuelMain")), Number(read("ship.FuelCapacity.Main"))));
            Put(result, CargoPercent, Percent(Number(read("status.Cargo")), Number(read("ship.CargoCapacity"))));
            Put(result, JumpRange, ComputeJumpRange(read));
            result[FsdSupercharged] = new JValue(ComputeSupercharged(read));
            return result;
        }

        private static void Put(Dictionary<string, JToken> result, string key, double? value)
        {
            if (value.HasValue)
                result[key] = new JValue(value.Value);
        }

        /// <summary>value / capacity x 100, 1 decimal; absent without value or capacity.</summary>
        public static double? Percent(double? value, double? capacity)
        {
            if (!value.HasValue || !capacity.HasValue || capacity.Value <= 0)
                return null;
            return Math.Round(Math.Max(0, value.Value) / capacity.Value * 100, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Jump range (ly) with the current mass: unladen mass + fuel (main + reservoir) + cargo, and the fuel of one
        /// jump (at most the maximum fuel per jump). FSD formula: optimal mass / mass x (fuel / multiplier)^(1 / power),
        /// plus the Guardian booster. Calibrated on the MaxJumpRange of the game (same formula at unladen mass + maximum
        /// fuel per jump: exact for 6 real ships), which absorbs an engineering the table does not know.
        /// </summary>
        public static double? ComputeJumpRange(Func<string, JToken> read)
        {
            var unladen = Number(read("ship.UnladenMass"));
            var maxJump = Number(read("ship.MaxJumpRange"));
            var optimal = Number(read("ship.Fsd.OptimalMass"));
            var maxFuel = Number(read("ship.Fsd.MaxFuelPerJump"));
            var multiplier = Number(read("ship.Fsd.FuelMultiplier"));
            var power = Number(read("ship.Fsd.FuelPower"));
            var fuel = Number(read("status.Fuel.FuelMain"));
            if (!unladen.HasValue || !optimal.HasValue || !maxFuel.HasValue || !multiplier.HasValue || !power.HasValue || !fuel.HasValue
                || multiplier.Value <= 0 || power.Value <= 0 || maxFuel.Value <= 0)
                return null;

            double boost = Number(read("ship.GuardianBoost")) ?? 0;
            double reservoir = Number(read("status.Fuel.FuelReservoir")) ?? 0;
            double cargo = Number(read("status.Cargo")) ?? 0;

            Func<double, double, double> range = (mass, jumpFuel) =>
                optimal.Value / mass * Math.Pow(jumpFuel / multiplier.Value, 1 / power.Value);

            double calibration = 1;
            if (maxJump.HasValue)
            {
                var theory = range(unladen.Value + maxFuel.Value, maxFuel.Value);
                var factor = (maxJump.Value - boost) / theory;
                if (!double.IsNaN(factor) && !double.IsInfinity(factor) && factor > 0.8 && factor < 1.25)
                    calibration = factor;
            }

            double used = Math.Min(maxFuel.Value, Math.Max(0, fuel.Value));
            if (used <= 0)
                return 0;

            double currentMass = unladen.Value + Math.Max(0, fuel.Value) + reservoir + Math.Max(0, cargo);
            return Math.Round(calibration * range(currentMass, used) + boost, 2);
        }

        /// <summary>FSD supercharged by a neutron star or white dwarf cone: JetConeBoost since the last jump.</summary>
        public static bool ComputeSupercharged(Func<string, JToken> read)
        {
            var boost = Time(read("journal.JetConeBoost.timestamp"));
            if (boost == null)
                return false;

            foreach (var evt in new[] { "FSDJump", "CarrierJump" })
            {
                var jump = Time(read("journal." + evt + ".timestamp"));
                if (jump != null && jump.Value > boost.Value)
                    return false;
            }
            return true;
        }

        internal static double? Number(JToken value)
        {
            return History.Number(value);
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
