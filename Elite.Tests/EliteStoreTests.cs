using System;
using System.Collections.Generic;
using System.Linq;
using Elite.Generic;
using EliteJournalReader;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class EliteStoreTests
    {
        private List<IReadOnlyCollection<string>> changes;

        [SetUp]
        public void SetUp()
        {
            EliteStore.Reset();
            changes = new List<IReadOnlyCollection<string>>();
            EliteStore.DataChanged += keys => changes.Add(keys);
        }

        [TearDown]
        public void TearDown()
        {
            EliteStore.Reset();
        }

        private static void Journal(string line, bool isLive = false)
        {
            var evt = JObject.Parse(line);
            EliteStore.HandleRawJournal(null, new RawJournalEventArgs(evt.Value<string>("event"), evt, isLive));
        }

        private static void Status(string file)
        {
            EliteStore.HandleRawStatus(null, new RawStatusEventArgs(TestData.Status(file)));
        }

        [Test]
        public void TryGet_UnknownKey_ReturnsFalse()
        {
            JToken value;
            Assert.That(EliteStore.TryGet("journal.Nothing.Here", out value), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(EliteStore.TryGet(null, out value), Is.False);
        }

        [Test]
        public void Journal_NewEventReplacesPrevious_DisappearedKeysRemovedAndSignalled()
        {
            Journal(TestData.JournalLine("Cargo", 1)); // Inventory: [ ]
            JToken value;
            Assert.That(EliteStore.TryGet("journal.Cargo.Inventory.#count", out value), Is.True);
            Assert.That((int)value, Is.EqualTo(0));

            Journal(TestData.JournalLine("Cargo", 2)); // Count 1, no Inventory

            Assert.That(EliteStore.TryGet("journal.Cargo.Inventory.#count", out value), Is.False);
            Assert.That(EliteStore.TryGet("journal.Cargo.Count", out value), Is.True);
            Assert.That((int)value, Is.EqualTo(1));
            Assert.That(changes.Last(), Does.Contain("journal.Cargo.Inventory.#count"));
            Assert.That(changes.Last(), Does.Contain("journal.Cargo.Count"));
        }

        [Test]
        public void DataChanged_OnlyChangedKeys_AndNothingWhenIdentical()
        {
            Journal(TestData.JournalLine("ShieldState", 1)); // 16:28:02 ShieldsUp false
            Journal(TestData.JournalLine("ShieldState", 2)); // 16:29:27 ShieldsUp true

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(changes[1], Is.EquivalentTo(new[] { "journal.ShieldState.timestamp", "journal.ShieldState.ShieldsUp" }));

            Journal(TestData.JournalLine("ShieldState", 2));
            Assert.That(changes.Count, Is.EqualTo(2), "an identical event changes nothing");
        }

        [Test]
        public void Status_ShipThenMenu_RemovesAbsentFields()
        {
            Status("status-vaisseau.json");
            JToken value;
            Assert.That(EliteStore.TryGet("status.Fuel.FuelMain", out value), Is.True);

            Status("status-menu.json");

            Assert.That(EliteStore.TryGet("status.Fuel.FuelMain", out value), Is.False);
            Assert.That(EliteStore.TryGet("status.Flags.LandingGearDown", out value), Is.True);
            Assert.That((bool)value, Is.False);
            Assert.That(changes.Last(), Does.Contain("status.Fuel.FuelMain"));
            Assert.That(changes.Last(), Does.Contain("status.Flags.LandingGearDown"));
            Assert.That(changes.Last(), Does.Not.Contain("status.Flags.Docked"), "Docked was already false");
        }

        [Test]
        public void Keys_AreCaseInsensitive()
        {
            Journal(TestData.JournalLine("DropshipDeploy"));

            JToken value;
            Assert.That(EliteStore.TryGet("journal.DropShipDeploy.Body", out value), Is.True);
            Assert.That((string)value, Is.EqualTo("Beta-3 Tucani 2 a"));
            Assert.That(changes.Last().Contains("JOURNAL.dropshipdeploy.BODY"), Is.True);
        }

        [Test]
        public void TryGet_ReturnsACopy()
        {
            Journal(TestData.JournalLine("UnderAttack", 1));
            JToken value;
            EliteStore.TryGet("journal.UnderAttack.Target", out value);
            ((JValue)value).Value = "Changed";

            EliteStore.TryGet("journal.UnderAttack.Target", out value);
            Assert.That((string)value, Is.EqualTo("You"));
        }

        [Test]
        public void FailingSubscriber_DoesNotBlockOthers()
        {
            EliteStore.Reset();
            int called = 0;
            EliteStore.DataChanged += keys => { throw new InvalidOperationException("test"); };
            EliteStore.DataChanged += keys => called++;
            EliteStore.JournalEventReceived += (s, e) => { throw new InvalidOperationException("test"); };
            EliteStore.JournalEventReceived += (s, e) => called++;

            Assert.DoesNotThrow(() => Journal(TestData.JournalLine("UnderAttack", 1)));
            Assert.That(called, Is.EqualTo(2));
        }

        [Test]
        public void JournalEventReceived_RelaysIsLive_LiveCountOnlyForLiveEvents()
        {
            var received = new List<RawJournalEventArgs>();
            EliteStore.JournalEventReceived += (s, e) => received.Add(e);
            var line = TestData.JournalLine("UnderAttack", 1);

            Journal(line, isLive: false);
            Assert.That(received.Single().IsLive, Is.False);
            Assert.That(EliteStore.GetLiveCount("UnderAttack"), Is.EqualTo(0));

            Journal(line, isLive: true);
            Journal(line, isLive: true);
            Assert.That(received.Count, Is.EqualTo(3), "every occurrence is relayed, even identical ones");
            Assert.That(received[1].IsLive, Is.True);
            Assert.That(EliteStore.GetLiveCount("UnderAttack"), Is.EqualTo(2));
        }
    }
}
