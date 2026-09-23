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
    }

    /// <summary>
    /// Image of a Data key: the rules are tested in order on the main value (after scale and offset, i.e. in the
    /// displayed unit); the first true rule gives the image. Pure function.
    /// </summary>
    public static class ImageRules
    {
        /// <summary>
        /// Returns the image of the first matching rule, or null (= default image) when none matches or the value is absent.
        /// Rules without a test or without an image are ignored.
        /// </summary>
        public static string Choose(JToken mainValue, ValueSettings mainDisplay, IEnumerable<ImageRule> rules)
        {
            var tested = Displayed(mainValue, mainDisplay);
            foreach (var rule in rules)
            {
                if (rule.Operator == ConditionOperator.None || string.IsNullOrWhiteSpace(rule.Image))
                    continue;

                if (Condition.Evaluate(tested, rule.Operator, rule.Operand))
                    return rule.Image;
            }

            return null;
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
