using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// French descriptions of the "i" button (Elite.CatalogGen/descriptions-fr.json, merged into catalog.js "info").
    /// </summary>
    [TestFixture]
    public class DescriptionsTests
    {
        private JObject catalog;
        private JObject french;
        private HashSet<string> keys;
        private HashSet<string> prefixes;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = TestData.GeneratedJs("catalog.js", "ELITE_CATALOG");
            french = JObject.Parse(File.ReadAllText(TestData.PathOf("descriptions-fr.json")));
            var groups = catalog["groups"].Cast<JObject>().ToList();
            keys = new HashSet<string>(groups.SelectMany(g => g["fields"].Select(f => (string)g["prefix"] + "." + (string)f[0])), StringComparer.OrdinalIgnoreCase);
            prefixes = new HashSet<string>(groups.Select(g => (string)g["prefix"]), StringComparer.OrdinalIgnoreCase);
        }

        [Test]
        public void EveryDescription_DescribesAnExistingEventOrKey()
        {
            var orphans = french.Properties()
                .Select(p => p.Name)
                .Where(n => !n.StartsWith("#") && !keys.Contains(n) && !prefixes.Contains(n))
                .ToList();

            Assert.That(orphans, Is.Empty);
        }

        [Test]
        public void EveryEventAndEveryStatusKey_IsDescribed()
        {
            var info = (JObject)catalog["info"];
            var described = new HashSet<string>(info.Properties().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            var events = prefixes.Where(p => p.StartsWith("journal.")).ToList();
            Assert.That(events.Count, Is.GreaterThanOrEqualTo(249));
            Assert.That(events.Where(e => !described.Contains(e)), Is.Empty, "events without description");

            var statusKeys = keys.Where(k => k.StartsWith("status.")).ToList();
            Assert.That(statusKeys.Count, Is.GreaterThanOrEqualTo(78));
            Assert.That(statusKeys.Where(k => !described.Contains(k)), Is.Empty, "status keys without description");
        }

        [Test]
        public void Descriptions_AreFrenchAndSearchable()
        {
            var info = (JObject)catalog["info"];
            Assert.That((string)info["status.Fuel.FuelMain"], Does.Contain("Carburant"));
            Assert.That((string)info["status.Flags.LandingGearDown"], Does.Contain("Train d'atterrissage"));
            Assert.That((string)info["journal.UnderAttack"], Does.StartWith("Écrit quand"));
            Assert.That((string)info["journal.UnderAttack.Target"], Does.Contain("Mothership"));
            Assert.That((string)info["journal.CarrierJump"], Does.Not.Contain("reset"), "copy-paste error of the library corrected");
        }

        [Test]
        public void CurrentValueReply_ForThePanel()
        {
            var settings = new ValueSettings { Prefix = "Fuel\\n", Decimals = 1 };
            var present = ValueFormatter.DescribeCurrentValue("status.Fuel.FuelMain", new JValue(27.5), settings, DateTime.Now);
            Assert.That((string)present["genericValueKey"], Is.EqualTo("status.Fuel.FuelMain"));
            Assert.That((bool)present["genericValuePresent"], Is.True);
            Assert.That((string)present["genericValueRaw"], Is.EqualTo("27.5"));
            Assert.That((string)present["genericValueText"], Is.EqualTo("Fuel\n27,5"));

            var text = ValueFormatter.DescribeCurrentValue("journal.FSDJump.StarSystem", new JValue("Two Ladies"), new ValueSettings(), DateTime.Now);
            Assert.That((string)text["genericValueRaw"], Is.EqualTo("Two Ladies"));

            var absent = ValueFormatter.DescribeCurrentValue("status.Oxygen", null, settings, DateTime.Now);
            Assert.That((bool)absent["genericValuePresent"], Is.False);
            Assert.That((string)absent["genericValueText"], Is.Empty);
        }
    }
}
