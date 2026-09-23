using System.Collections.Generic;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class ImageRulesTests
    {
        // fuel: red below 25, orange below 50, otherwise default
        private static readonly List<ImageRule> FuelRules = new List<ImageRule>
        {
            new ImageRule(ConditionOperator.LessThan, "25", "red.png"),
            new ImageRule(ConditionOperator.LessThan, "50", "orange.png"),
        };

        [TestCase(10.0, "red.png")]
        [TestCase(24.9, "red.png")]
        [TestCase(30.0, "orange.png")]
        [TestCase(60.0, null)]
        public void FirstMatchingRule_OtherwiseDefault(double fuel, string expected)
        {
            Assert.That(ImageRules.Choose(new JValue(fuel), new ValueSettings(), FuelRules), Is.EqualTo(expected));
        }

        [Test]
        public void AbsentValue_GivesDefault()
        {
            var rules = new List<ImageRule> { new ImageRule(ConditionOperator.IsFalse, "", "off.png") };
            Assert.That(ImageRules.Choose(null, new ValueSettings(), rules), Is.Null);
        }

        [Test]
        public void RulesWithoutTestOrImage_AreIgnored()
        {
            var rules = new List<ImageRule>
            {
                new ImageRule(ConditionOperator.None, "", "never.png"),
                new ImageRule(ConditionOperator.IsTrue, "", null),
                new ImageRule(ConditionOperator.IsTrue, "", "on.png"),
            };
            Assert.That(ImageRules.Choose(new JValue(true), new ValueSettings(), rules), Is.EqualTo("on.png"));
        }

        [Test]
        public void Thresholds_UseTheDisplayedUnit()
        {
            // body temperature 310 K shown in degrees C (offset -273.15): 36.85 degrees C
            var display = new ValueSettings { Offset = -273.15 };
            var rules = new List<ImageRule> { new ImageRule(ConditionOperator.GreaterThan, "35", "hot.png") };
            Assert.That(ImageRules.Choose(new JValue(310.0), display, rules), Is.EqualTo("hot.png"));
            Assert.That(ImageRules.Choose(new JValue(300.0), display, rules), Is.Null);
        }

        [Test]
        public void EnumValue_Equality()
        {
            var rules = new List<ImageRule>
            {
                new ImageRule(ConditionOperator.EqualTo, "GalaxyMap", "galaxy.png"),
                new ImageRule(ConditionOperator.EqualTo, "SystemMap", "system.png"),
            };
            Assert.That(ImageRules.Choose(new JValue("SystemMap"), new ValueSettings(), rules), Is.EqualTo("system.png"));
            Assert.That(ImageRules.Choose(new JValue("NoFocus"), new ValueSettings(), rules), Is.Null);
        }
    }
}
