using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Elite.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// The settings sent by a property inspector page (its sdProperty elements) are exactly the JSON settings of the C#
    /// action. The names are definitive once a key uses them: a field renamed on one side only would be lost.
    /// </summary>
    [TestFixture]
    public class InspectorFieldsTests
    {
        private static string[] PageFields(string page)
        {
            var plugin = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                @"..\..\..\Elite\bin\Debug\com.mhwlng.elite.sdPlugin"));
            var html = File.ReadAllText(Path.Combine(plugin, "PropertyInspector", "Elite", page));

            return Regex.Matches(html, @"<(?:input|select)\b[^>]*>")
                .Cast<Match>()
                .Where(m => Regex.IsMatch(m.Value, @"class=""[^""]*\bsdProperty\b"))
                .Select(m => Regex.Match(m.Value, @"\bid=""([^""]+)""").Groups[1].Value)
                .OrderBy(id => id)
                .ToArray();
        }

        private static string[] ActionSettings(System.Type action)
        {
            var settings = action.GetNestedType("PluginSettings", BindingFlags.NonPublic | BindingFlags.Public);
            return settings.GetProperties()
                .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>().PropertyName)
                .OrderBy(name => name)
                .ToArray();
        }

        [Test]
        public void AlarmPage_SendsExactlyTheSettingsOfTheAlarmAction()
        {
            var fields = PageFields("EventAlarm.html");

            Assert.That(fields, Is.EqualTo(ActionSettings(typeof(EventAlarmAction))));
            var alarm1 = new[]
            {
                "event", "filterField", "filterOp", "filterValue", "duration", "idleImage", "activeImage", "alarmSound",
                "pressCommand", "pressHotkey", "pressHotkeyText", "clickSound",
            };
            Assert.That(fields, Is.SupersetOf(alarm1), "names of docs/L8-alarme.md, kept by alarm 1");

            // v3.6: alarms 2 to 4 = the same names + their number, idleImage common
            var others = Enumerable.Range(2, 3).SelectMany(a => alarm1.Where(n => n != "idleImage").Select(n => n + a)).ToArray();
            Assert.That(fields, Is.EquivalentTo(alarm1.Concat(others)));
        }

        [Test]
        public void GraphPage_SendsTheSettingsOfTheGraphAction_ExceptTheDisplayedView()
        {
            var settings = ActionSettings(typeof(GraphAction)).Where(name => name != "currentView").ToArray();

            Assert.That(PageFields("Graph.html"), Is.EqualTo(settings));
            Assert.That(settings, Does.Contain("graphType").And.Contain("colorRule4Color4").And.Contain("curveInterval3"));
            Assert.That(settings, Does.Not.Contain("rule1Image"), "no image rules on a Graph key");
        }

        [Test]
        public void DataPage_SendsTheSettingsOfTheDataAction_ExceptTheDisplayedView()
        {
            // currentView is saved by the plugin only (docs/L7-retours-d6.md)
            var settings = ActionSettings(typeof(ValueAction)).Where(name => name != "currentView").ToArray();

            Assert.That(PageFields("Generic.html"), Is.EqualTo(settings));
        }
    }
}
