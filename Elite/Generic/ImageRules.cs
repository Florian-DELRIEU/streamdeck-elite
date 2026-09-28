using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    public class ImageRule
    {
        public ImageRule(ConditionOperator op, string operand, string image)
        {
            Operator = op;
            Operand = operand;
            Image = image;
        }

        public ConditionOperator Operator { get; }
        public string Operand { get; }
        public string Image { get; }

        /// <summary>Data tested by the rule (v3.5); empty = the data of the view, after its factor and offset.</summary>
        public string Key { get; set; } = "";

        /// <summary>Second condition, linked by AND (v3.5); None = no second condition.</summary>
        public ConditionOperator AndOperator { get; set; }
        public string AndOperand { get; set; } = "";
        /// <summary>Data of the second condition; empty = the same data as the first one.</summary>
        public string AndKey { get; set; } = "";

        public IEnumerable<string> Keys
        {
            get
            {
                if (!string.IsNullOrEmpty(Key))
                    yield return Key;
                if (!string.IsNullOrEmpty(AndKey))
                    yield return AndKey;
            }
        }
    }

    /// <summary>
    /// Image of a Data key: the rules are tested in order; the first true rule gives the image. Pure function.
    /// A rule tests the data of the view (after scale and offset, i.e. in the displayed unit) or its own data (raw
    /// value), and optionally a second condition (AND). OR = several rules with the same image.
    /// </summary>
    public static class ImageRules
    {
        /// <summary>
        /// Returns the image of the first matching rule, or null (= default image) when none matches or the value is absent.
        /// Rules without a test or without an image are ignored.
        /// </summary>
        public static string Choose(JToken mainValue, ValueSettings mainDisplay, IEnumerable<ImageRule> rules)
        {
            return Choose(key => null, mainValue, mainDisplay, rules);
        }

        /// <summary>Same, with the reader of the store for the rules that test their own data.</summary>
        public static string Choose(Func<string, JToken> read, JToken mainValue, ValueSettings mainDisplay, IEnumerable<ImageRule> rules)
        {
            var tested = Displayed(mainValue, mainDisplay);
            foreach (var rule in rules)
            {
                if (rule.Operator == ConditionOperator.None || string.IsNullOrWhiteSpace(rule.Image))
                    continue;

                if (IsTrue(read, tested, rule.Operator, rule.Operand, rule.Key, rule.AndOperator, rule.AndOperand, rule.AndKey))
                    return rule.Image;
            }

            return null;
        }

        /// <summary>
        /// One rule: first condition on its data (its own key, raw, or the tested value of the view), then the AND
        /// condition on its own data (or the same data).
        /// </summary>
        public static bool IsTrue(Func<string, JToken> read, JToken viewValue, ConditionOperator op, string operand, string key,
            ConditionOperator andOp, string andOperand, string andKey)
        {
            var first = string.IsNullOrWhiteSpace(key) ? viewValue : read(key.Trim());
            if (!Condition.Evaluate(first, op, operand))
                return false;

            if (andOp == ConditionOperator.None)
                return true;

            var second = string.IsNullOrWhiteSpace(andKey) ? first : read(andKey.Trim());
            return Condition.Evaluate(second, andOp, andOperand);
        }

        /// <summary>
        /// Numbers are tested in the displayed unit (value x scale + offset); other values as they are.
        /// </summary>
        public static JToken Displayed(JToken value, ValueSettings display)
        {
            if (value == null || display == null || (value.Type != JTokenType.Integer && value.Type != JTokenType.Float))
                return value;

            return new JValue((double)value * display.Scale + display.Offset);
        }
    }
}
