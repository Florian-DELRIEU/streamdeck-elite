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

        [Test]
        public void AndRules_OwnDataAndSecondCondition()
        {
            // v3.5: landing gear down AND speed... here: gear down AND in supercruise = warning image; OR = two rules
            var store = new Dictionary<string, JToken>
            {
                { "status.Flags.LandingGearDown", new JValue(true) },
                { "status.Flags.Supercruise", new JValue(false) },
                { "status.Fuel.FuelMain", new JValue(3.0) },
            };
            System.Func<string, JToken> read = k => store.ContainsKey(k) ? store[k] : null;
            var rules = new List<ImageRule>
            {
                new ImageRule(ConditionOperator.IsTrue, "", "warning.png")
                    { AndOperator = ConditionOperator.IsTrue, AndKey = "status.Flags.Supercruise" },
                new ImageRule(ConditionOperator.LessThan, "5", "fuel.png") { Key = "status.Fuel.FuelMain" },
                new ImageRule(ConditionOperator.IsTrue, "", "gear.png"),
            };
            var gear = read("status.Flags.LandingGearDown");

            Assert.That(ImageRules.Choose(read, gear, new ValueSettings(), rules), Is.EqualTo("fuel.png"), "AND false, then the rule on its own data");

            store["status.Flags.Supercruise"] = new JValue(true);
            Assert.That(ImageRules.Choose(read, gear, new ValueSettings(), rules), Is.EqualTo("warning.png"));

            store["status.Flags.Supercruise"] = new JValue(false);
            store["status.Fuel.FuelMain"] = new JValue(20.0);
            Assert.That(ImageRules.Choose(read, gear, new ValueSettings(), rules), Is.EqualTo("gear.png"));

            // AND on the same data: between 25 and 50
            var between = new List<ImageRule>
            {
                new ImageRule(ConditionOperator.GreaterOrEqual, "25", "mid.png") { AndOperator = ConditionOperator.LessThan, AndOperand = "50" },
            };
            Assert.That(ImageRules.Choose(read, new JValue(30), new ValueSettings(), between), Is.EqualTo("mid.png"));
            Assert.That(ImageRules.Choose(read, new JValue(60), new ValueSettings(), between), Is.Null);
        }

        [Test]
        public void AndRules_AreReadFromTheSettings_AndOldKeysUnchanged()
        {
            var config = DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""status.Flags.LandingGearDown"",
                ""rule1Op"":""isTrue"", ""rule1Image"":""a.png"", ""rule1AndOp"":""isTrue"", ""rule1AndKey"":""status.Flags.Supercruise"",
                ""source2"":""a.b"", ""rule2Op2"":""lt"", ""rule2Value2"":""5"", ""rule2Image2"":""b.png"", ""rule2Key2"":""status.Fuel.FuelMain"" }"));

            var rule = config.MainView.Rules[0];
            Assert.That(rule.AndOperator, Is.EqualTo(ConditionOperator.IsTrue));
            Assert.That(rule.AndKey, Is.EqualTo("status.Flags.Supercruise"));
            Assert.That(rule.Key, Is.Empty);
            Assert.That(config.Views[1].Rules[0].Key, Is.EqualTo("status.Fuel.FuelMain"));
            Assert.That(config.Views[1].Rules[0].Keys, Is.EqualTo(new[] { "status.Fuel.FuelMain" }));

            var old = DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""x.y"", ""rule1Op"":""lt"", ""rule1Value"":""5"", ""rule1Image"":""c.png"" }"));
            Assert.That(old.MainView.Rules[0].AndOperator, Is.EqualTo(ConditionOperator.None), "keys created before v3.5");
            Assert.That(old.MainView.Rules[0].Key, Is.Empty);
        }

        [Test]
        public void ColourRules_OfAGraph_WithAnd()
        {
            var view = GraphConfig.Parse(JObject.Parse(@"{ ""colorRule1Op"":""lt"", ""colorRule1Value"":""25"", ""colorRule1Color"":""#ff4646"",
                ""colorRule1AndOp"":""isTrue"", ""colorRule1AndKey"":""status.Flags.Supercruise"" }"), "");
            var store = new Dictionary<string, JToken> { { "status.Flags.Supercruise", new JValue(false) } };
            System.Func<string, JToken> read = k => store.ContainsKey(k) ? store[k] : null;

            Assert.That(GraphConfig.ColorOf(view, new JValue(10), read), Is.EqualTo(GraphConfig.DefaultColor), "not in supercruise");
            store["status.Flags.Supercruise"] = new JValue(true);
            Assert.That(GraphConfig.ColorOf(view, new JValue(10), read), Is.EqualTo("#ff4646"));
            Assert.That(GraphConfig.BoundKeys(view), Does.Contain("status.Flags.Supercruise"), "redrawn when it changes");
        }
    }
}
