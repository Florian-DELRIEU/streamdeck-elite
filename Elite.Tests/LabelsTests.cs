using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// French names of the menus (v3.8, docs/L11-v3.8.md): Elite.CatalogGen/labels-fr.json, merged into catalog.js
    /// ("labels"). Only what is displayed: the settings keep the technical key.
    /// </summary>
    [TestFixture]
    public class LabelsTests
    {
        private const int MaxLength = 40;

        private JObject catalog;
        private JObject french;
        private List<JObject> groups;
        private HashSet<string> keys;
        private HashSet<string> groupIds;
        private JObject labels;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = TestData.GeneratedJs("catalog.js", "ELITE_CATALOG");
            french = JObject.Parse(File.ReadAllText(TestData.PathOf("labels-fr.json")));
            groups = catalog["groups"].Cast<JObject>().ToList();
            keys = new HashSet<string>(groups.SelectMany(g => g["fields"].Select(f => (string)g["prefix"] + "." + (string)f[0])), StringComparer.OrdinalIgnoreCase);
            groupIds = new HashSet<string>(groups.Select(g => (string)g["id"]), StringComparer.OrdinalIgnoreCase);
            labels = (JObject)catalog["labels"];
        }

        [Test]
        public void EveryLabel_NamesAnExistingKeyOrGroup()
        {
            var orphans = french.Properties()
                .Select(p => p.Name)
                .Where(n => !n.StartsWith("#") && !keys.Contains(n) && !groupIds.Contains(n))
                .ToList();

            Assert.That(orphans, Is.Empty);
        }

        [Test]
        public void EveryStatusKey_PluginKey_AndEvent_HasAFrenchName()
        {
            var named = new HashSet<string>(labels.Properties().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var prefixes = new[] { "status.", "calc.", "ship.", "system.", "target." };

            var dataKeys = keys.Where(k => prefixes.Any(p => k.StartsWith(p, StringComparison.OrdinalIgnoreCase))).ToList();
            Assert.That(dataKeys.Count, Is.GreaterThanOrEqualTo(150));
            Assert.That(dataKeys.Where(k => !named.Contains(k)), Is.Empty, "keys without a French name");

            var events = groupIds.Where(id => id.StartsWith("journal.", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.That(events.Count, Is.GreaterThanOrEqualTo(249));
            Assert.That(events.Where(e => !named.Contains(e)), Is.Empty, "events without a French name");
        }

        [Test]
        public void Names_AreShort_AndUniqueInTheirMenu()
        {
            var tooLong = labels.Properties().Where(p => ((string)p.Value).Length > MaxLength).Select(p => p.Name + " = " + (string)p.Value).ToList();
            Assert.That(tooLong, Is.Empty);

            // fields of a group (menu of the data)
            foreach (var group in groups)
            {
                var names = group["fields"].Select(f => Name((string)group["prefix"] + "." + (string)f[0]) ?? (string)f[0]).ToList();
                var twice = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                Assert.That(twice, Is.Empty, "same name twice in " + (string)group["id"]);
            }

            // groups of a category (menu of the events)
            foreach (var category in groups.GroupBy(g => (string)g["category"]))
            {
                var names = category.Select(g => (Name((string)g["id"]) ?? (string)g["label"]) + ((bool)g["odyssey"] ? " (Odyssey)" : "")).ToList();
                var twice = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                Assert.That(twice, Is.Empty, "same group name twice in " + category.Key);
            }
        }

        [Test]
        public void Names_AreMergedIntoTheCatalog()
        {
            Assert.That((string)labels["calc.Fuel.Percent"], Is.EqualTo("Carburant principal (%)"));
            Assert.That((string)labels["status.Flags.LandingGearDown"], Is.EqualTo("Train d'atterrissage sorti"));
            Assert.That((string)labels["system.Star.Scoopable"], Is.EqualTo("Étoile écopable"));
            Assert.That((string)labels["journal.FSDJump"], Is.EqualTo("Saut FSD terminé"));
            Assert.That((string)labels["status.Flags"], Is.EqualTo("Indicateurs du vaisseau"), "group id");
            Assert.That(labels["journal.FSDJump.JumpDist"], Is.Null, "fields of the events keep their name in the game");
        }

        private string Name(string key)
        {
            var property = labels.Properties().FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
            return property == null ? null : (string)property.Value;
        }
    }
}
