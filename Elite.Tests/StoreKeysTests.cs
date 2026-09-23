using System;
using System.Linq;
using Elite.Generic;
using EliteJournalReader.Events;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class StoreKeysTests
    {
        [Test]
        public void Journal_FlattensObjects_CountsArrays_SkipsNullAndEventName()
        {
            var evt = JObject.Parse("{ \"timestamp\":\"2026-09-23T10:00:00Z\", \"event\":\"Test\", \"A\":{ \"B\":{ \"C\":5 } }, \"L\":[1,2,3], \"N\":null, \"S\":\"x\" }");

            var keys = StoreKeys.FromJournalEvent("Test", evt);

            Assert.That((int)keys["journal.Test.A.B.C"], Is.EqualTo(5));
            Assert.That((int)keys["journal.Test.L.#count"], Is.EqualTo(3));
            Assert.That((string)keys["journal.Test.S"], Is.EqualTo("x"));
            Assert.That(keys.ContainsKey("journal.Test.timestamp"), Is.True);
            Assert.That(keys.ContainsKey("journal.Test.L"), Is.False, "arrays only give #count");
            Assert.That(keys.ContainsKey("journal.Test.N"), Is.False, "null values are skipped");
            Assert.That(keys.ContainsKey("journal.Test.event"), Is.False);
        }

        [Test]
        public void Journal_RealDockedLine_NestedFaction()
        {
            var keys = StoreKeys.FromJournalEvent("Docked", JObject.Parse(TestData.JournalLine("Docked")));

            Assert.That((string)keys["journal.Docked.StationName"], Is.EqualTo("Bakewell Platform"));
            Assert.That(keys.ContainsKey("journal.Docked.StationFaction.Name"), Is.True);
        }

        [Test]
        public void Status_Flags_DecomposedWithoutNone_IncludingBit31()
        {
            // LandingGearDown (0x4) + SrvHighBeam (0x80000000)
            var keys = StoreKeys.FromStatus(JObject.Parse("{ \"event\":\"Status\", \"Flags\":2147483652 }"));

            Assert.That((bool)keys["status.Flags.LandingGearDown"], Is.True);
            Assert.That((bool)keys["status.Flags.SrvHighBeam"], Is.True);
            Assert.That((bool)keys["status.Flags.Docked"], Is.False);
            Assert.That(keys.ContainsKey("status.Flags.None"), Is.False);

            int namedFlags = Enum.GetValues(typeof(StatusFlags)).Length - 1;
            Assert.That(keys.Keys.Count(k => k.StartsWith("status.Flags.")), Is.EqualTo(namedFlags));
        }

        [Test]
        public void Status_Flags2Absent_NoFlags2Keys()
        {
            var keys = StoreKeys.FromStatus(TestData.Status("status-menu.json"));

            Assert.That(keys.Keys.Any(k => k.StartsWith("status.Flags2.")), Is.False);
            Assert.That((bool)keys["status.Flags.Docked"], Is.False);
        }

        [Test]
        public void BreathableAtmosphere_IsBit16Only()
        {
            Assert.That((long)MoreStatusFlags.BreathableAtmosphere, Is.EqualTo(0x10000));

            var breathableOnly = StoreKeys.FromStatus(JObject.Parse("{ \"Flags2\":65536 }"));
            Assert.That((bool)breathableOnly["status.Flags2.BreathableAtmosphere"], Is.True);
            Assert.That((bool)breathableOnly["status.Flags2.OnFoot"], Is.False);

            var onFootOnly = StoreKeys.FromStatus(JObject.Parse("{ \"Flags2\":1 }"));
            Assert.That((bool)onFootOnly["status.Flags2.OnFoot"], Is.True);
            Assert.That((bool)onFootOnly["status.Flags2.BreathableAtmosphere"], Is.False);

            // same test as EliteData.HandleStatusEvents (historic Toggle button)
            Assert.That((MoreStatusFlags.OnFoot & MoreStatusFlags.BreathableAtmosphere) != 0, Is.False);
        }

        [Test]
        public void Status_Pips_GuiFocus_NestedFields()
        {
            var keys = StoreKeys.FromStatus(TestData.Status("status-vaisseau.json"));

            Assert.That((int)keys["status.Pips.System"], Is.EqualTo(4));
            Assert.That((int)keys["status.Pips.Engine"], Is.EqualTo(8));
            Assert.That((int)keys["status.Pips.Weapons"], Is.EqualTo(0));
            Assert.That((string)keys["status.GuiFocus"], Is.EqualTo("GalaxyMap"));
            Assert.That((double)keys["status.Fuel.FuelMain"], Is.EqualTo(27.5));
            Assert.That((string)keys["status.Destination.Name"], Is.EqualTo("Bakewell Platform"));
            Assert.That(keys.ContainsKey("status.event"), Is.False);
            Assert.That(keys.ContainsKey("status.Oxygen"), Is.False, "Oxygen is absent from the file");
        }

        [Test]
        public void Status_UnknownGuiFocus_KeepsNumber()
        {
            var keys = StoreKeys.FromStatus(JObject.Parse("{ \"GuiFocus\":99 }"));

            Assert.That((int)keys["status.GuiFocus"], Is.EqualTo(99));
        }

        [Test]
        public void Status_OnFoot_OdysseyFields()
        {
            var keys = StoreKeys.FromStatus(TestData.Status("status-a-pied.json"));

            Assert.That((bool)keys["status.Flags2.OnFoot"], Is.True);
            Assert.That((bool)keys["status.Flags2.BreathableAtmosphere"], Is.True);
            Assert.That((bool)keys["status.Flags2.InTaxi"], Is.False);
            Assert.That((double)keys["status.Health"], Is.EqualTo(0.85));
            Assert.That((string)keys["status.SelectedWeapon_Localised"], Is.EqualTo("Poings"));
        }
    }
}
