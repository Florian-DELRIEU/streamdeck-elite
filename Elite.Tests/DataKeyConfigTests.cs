using System.Linq;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class DataKeyConfigTests
    {
        [Test]
        public void SettingsOfAnL3Key_GiveOneViewWithText()
        {
            // settings saved by the Value action of L3 (before views, rules and key press existed)
            var l3 = JObject.Parse("{ \"source\":\"status.Fuel.FuelMain\", \"prefix\":\"Fuel\\\\n\", \"suffix\":\"\", \"decimals\":\"1\","
                + " \"scale\":\"1\", \"offset\":\"0\", \"compact\":false, \"emptyText\":\"\u2014\", \"backgroundImage\":\"No file...\" }");

            var config = DataKeyConfig.FromSettings(l3);

            Assert.That(config.Views.Count, Is.EqualTo(1));
            Assert.That(config.MainView.Source, Is.EqualTo("status.Fuel.FuelMain"));
            Assert.That(config.MainView.Display.Decimals, Is.EqualTo(1));
            Assert.That(config.MainView.Display.Prefix, Is.EqualTo("Fuel\\n"));
            Assert.That(config.ShowText, Is.True, "absent -> text shown");
            Assert.That(config.PressCycle, Is.False);
            Assert.That(config.PressCommand, Is.Empty);
            Assert.That(config.Rules, Is.Empty);
        }

        [Test]
        public void FullSettings_ViewsRulesAndPress()
        {
            var settings = new JObject
            {
                { "source", "status.Fuel.FuelMain" }, { "decimals", "1" },
                { "source2", "status.Fuel.FuelReservoir" }, { "decimals2", "2" }, { "prefix2", "R\u00e9s.\\n" },
                { "source3", "" },
                { "source4", "status.Balance" }, { "compact4", true },
                { "showText", false },
                { "rule1Op", "lt" }, { "rule1Value", "25" }, { "rule1Image", "C:\\img\\red.png" },
                { "rule2Op", "" }, { "rule2Value", "50" },
                { "rule3Op", "gte" }, { "rule3Value", "50" }, { "rule3Image", "C:\\img\\green.png" },
                { "pressCycle", true }, { "pressCommand", " GalaxyMapOpen " }, { "clickSound", "C:\\snd\\click.wav" },
            };

            var config = DataKeyConfig.FromSettings(settings);

            Assert.That(config.Views.Select(v => v.Source), Is.EqualTo(new[] { "status.Fuel.FuelMain", "status.Fuel.FuelReservoir", "status.Balance" }),
                "empty view 3 skipped");
            Assert.That(config.Views[1].Display.Decimals, Is.EqualTo(2));
            Assert.That(config.Views[2].Display.Compact, Is.True);
            Assert.That(config.ShowText, Is.False);
            Assert.That(config.Rules.Select(r => r.Operator), Is.EqualTo(new[] { ConditionOperator.LessThan, ConditionOperator.GreaterOrEqual }),
                "rule without test skipped");
            Assert.That(config.Rules[1].Image, Is.EqualTo("C:\\img\\green.png"));
            Assert.That(config.PressCycle, Is.True);
            Assert.That(config.PressCommand, Is.EqualTo("GalaxyMapOpen"));
            Assert.That(config.ClickSound, Is.EqualTo("C:\\snd\\click.wav"));
        }

        [Test]
        public void EmptySettings_GiveAnEmptyMainView()
        {
            var config = DataKeyConfig.FromSettings(null);

            Assert.That(config.Views.Count, Is.EqualTo(1));
            Assert.That(config.MainView.Source, Is.Empty);
            Assert.That(config.ShowText, Is.True);
        }
    }
}
