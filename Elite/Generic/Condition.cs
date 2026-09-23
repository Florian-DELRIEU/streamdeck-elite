using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    public enum ConditionOperator
    {
        None,
        IsTrue,
        IsFalse,
        EqualTo,
        NotEqualTo,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual,
    }

    /// <summary>
    /// Tests of the image rules (cahier des charges §5.2 operators). Pure functions.
    /// An absent value never satisfies a test (the default image is shown).
    /// </summary>
    public static class Condition
    {
        /// <summary>
        /// Property inspector values: isTrue, isFalse, equals, notEquals, lt, lte, gt, gte (anything else: no test).
        /// </summary>
        public static ConditionOperator ParseOperator(string text)
        {
            switch ((text ?? "").Trim())
            {
                case "isTrue": return ConditionOperator.IsTrue;
                case "isFalse": return ConditionOperator.IsFalse;
                case "equals": return ConditionOperator.EqualTo;
                case "notEquals": return ConditionOperator.NotEqualTo;
                case "lt": return ConditionOperator.LessThan;
                case "lte": return ConditionOperator.LessOrEqual;
                case "gt": return ConditionOperator.GreaterThan;
                case "gte": return ConditionOperator.GreaterOrEqual;
                default: return ConditionOperator.None;
            }
        }

        public static bool Evaluate(JToken value, ConditionOperator op, string operand)
        {
            if (op == ConditionOperator.None || value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                return false;

            switch (op)
            {
                case ConditionOperator.IsTrue:
                    return IsTruthy(value);
                case ConditionOperator.IsFalse:
                    return !IsTruthy(value);
                case ConditionOperator.EqualTo:
                    return AreEqual(value, operand);
                case ConditionOperator.NotEqualTo:
                    return !AreEqual(value, operand);
            }

            // ordering: numbers only
            double number, threshold;
            if (!TryGetNumber(value, out number) || !TryParseNumber(operand, out threshold))
                return false;

            switch (op)
            {
                case ConditionOperator.LessThan: return number < threshold;
                case ConditionOperator.LessOrEqual: return number <= threshold;
                case ConditionOperator.GreaterThan: return number > threshold;
                case ConditionOperator.GreaterOrEqual: return number >= threshold;
                default: return false;
            }
        }

        public static bool IsTruthy(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return (bool)value;
                case JTokenType.Integer:
                case JTokenType.Float:
                    return (double)value != 0;
                case JTokenType.String:
                    var text = (string)value;
                    bool parsed;
                    return TryParseBoolean(text, out parsed) ? parsed : !string.IsNullOrWhiteSpace(text);
                default:
                    return true;
            }
        }

        public static bool TryParseNumber(string text, out double number)
        {
            number = 0;
            return !string.IsNullOrWhiteSpace(text)
                && double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private static bool AreEqual(JToken value, string operand)
        {
            var expected = (operand ?? "").Trim();

            if (value.Type == JTokenType.Boolean)
            {
                bool wanted;
                return TryParseBoolean(expected, out wanted) && wanted == (bool)value;
            }

            double number, other;
            if (TryGetNumber(value, out number) && TryParseNumber(expected, out other))
                return Math.Abs(number - other) < 1e-9;

            var text = value.Type == JTokenType.String ? (string)value : value.ToString();
            return string.Equals(text.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetNumber(JToken value, out double number)
        {
            number = 0;
            if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)
                return false;

            number = (double)value;
            return true;
        }

        private static bool TryParseBoolean(string text, out bool value)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "true":
                case "oui":
                case "vrai":
                case "yes":
                case "1":
                    value = true;
                    return true;
                case "false":
                case "non":
                case "faux":
                case "no":
                case "0":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
        }
    }
}
