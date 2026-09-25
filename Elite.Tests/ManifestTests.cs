using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Elite\manifest.json: the historic actions are untouched, the new actions follow §5 (1 State, Keypad),
    /// and every action has its C# class and its property inspector in the built plugin.
    /// </summary>
    [TestFixture]
    public class ManifestTests
    {
        private static readonly string[] HistoricActions =
        {
            "com.mhwlng.elite.alarm", "com.mhwlng.elite.fss", "com.mhwlng.elite.hyperspace", "com.mhwlng.elite.route",
            "com.mhwlng.elite.limpet", "com.mhwlng.elite.power", "com.mhwlng.elite.static", "com.mhwlng.elite.repeatingstatic",
            "com.mhwlng.elite", "com.mhwlng.elite.firegroup", "com.mhwlng.elite.dial", "com.mhwlng.elite.firegroupdial",
        };

        private JObject manifest;

        [OneTimeSetUp]
        public void Load()
        {
            manifest = JObject.Parse(File.ReadAllText(TestData.PathOf("manifest.json")));
        }

        private JObject Action(string uuid)
        {
            return manifest["Actions"].Cast<JObject>().Single(a => (string)a["UUID"] == uuid);
        }

        [Test]
        public void HistoricActions_AndPluginIdentity_Unchanged()
        {
            var uuids = manifest["Actions"].Select(a => (string)a["UUID"]).ToList();

            Assert.That(uuids.Take(HistoricActions.Length), Is.EqualTo(HistoricActions), "12 historic actions, same order");
            Assert.That(uuids, Is.Unique);
            Assert.That((string)manifest["Version"], Is.EqualTo("2.7.4"), "changes in L5");
            Assert.That((string)manifest["CodePath"], Is.EqualTo("com.mhwlng.elite"));
        }

        [Test]
        public void ValueAction_OneStateKeypadOnly()
        {
            var value = Action("com.mhwlng.elite.value");

            Assert.That((string)value["Name"], Is.EqualTo("Donnée"), "universal Data action (decision D4)");
            Assert.That((string)value["States"][0]["FontSize"], Is.EqualTo("14"));
            Assert.That(manifest["Actions"].Any(a => (string)a["UUID"] == "com.mhwlng.elite.state"), Is.False, "reserved, never declared");
            Assert.That(value["States"].Count(), Is.EqualTo(1));
            Assert.That(value["Controllers"].Values<string>(), Is.EqualTo(new[] { "Keypad" }));
            Assert.That((bool)value["SupportedInMultiActions"], Is.False);
            Assert.That((string)value["PropertyInspectorPath"], Is.EqualTo("PropertyInspector/Elite/Generic.html"));
        }

        [Test]
        public void AlarmAction_OneStateKeypadOnly()
        {
            var alarm = Action("com.mhwlng.elite.eventalarm");

            Assert.That((string)alarm["Name"], Is.EqualTo("Alarme"));
            Assert.That(alarm["States"].Count(), Is.EqualTo(1));
            Assert.That(alarm["Controllers"].Values<string>(), Is.EqualTo(new[] { "Keypad" }));
            Assert.That((bool)alarm["SupportedInMultiActions"], Is.False);
            Assert.That((string)alarm["PropertyInspectorPath"], Is.EqualTo("PropertyInspector/Elite/EventAlarm.html"));

            var uuids = manifest["Actions"].Select(a => (string)a["UUID"]).ToList();
            Assert.That(uuids.Count, Is.EqualTo(HistoricActions.Length + 2), "12 historic actions + Donnee + Alarme");
            Assert.That(uuids, Has.No.Member("com.mhwlng.elite.counter"), "reserved, never declared");
        }

        [Test]
        public void EveryAction_HasItsClassAndItsInspectorInThePlugin()
        {
            var actionIds = typeof(EliteData).Assembly.GetTypes()
                .SelectMany(t => t.GetCustomAttributesData())
                .Where(a => a.AttributeType.Name == "PluginActionIdAttribute")
                .Select(a => (string)a.ConstructorArguments[0].Value)
                .ToList();

            var plugin = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                @"..\..\..\Elite\bin\Debug\com.mhwlng.elite.sdPlugin"));

            foreach (JObject action in manifest["Actions"])
            {
                var uuid = (string)action["UUID"];
                Assert.That(actionIds, Does.Contain(uuid), "no [PluginActionId] class for " + uuid);
                Assert.That(File.Exists(Path.Combine(plugin, (string)action["PropertyInspectorPath"])), Is.True, "inspector of " + uuid);
            }

            foreach (var file in new[] { "catalog.js", "commands.js", "generic.js", "alarm.js", "sdtools.common.js" })
                Assert.That(File.Exists(Path.Combine(plugin, "PropertyInspector", file)), Is.True, file);

            // generic.js and alarm.js are loaded without charset: ASCII only (non-ASCII written as \u escapes)
            foreach (var file in new[] { "generic.js", "alarm.js" })
            {
                var script = File.ReadAllBytes(Path.Combine(plugin, "PropertyInspector", file));
                Assert.That(script.All(b => b < 0x80), Is.True, file + " must be ASCII-only");
            }
        }
    }
}
