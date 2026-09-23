using System.Collections.Generic;
using System.Linq;
using Elite.Generic;
using EliteJournalReader;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Acceptance criterion of the specification (§8, "Magasin"): a real journal extract and a status.json,
    /// replayed as at plugin startup, give the right values for 10 reference keys.
    /// </summary>
    [TestFixture]
    public class ReferenceKeysTests
    {
        private List<RawJournalEventArgs> received;

        [OneTimeSetUp]
        public void ReplayJournalAndStatus()
        {
            EliteStore.Reset();
            received = new List<RawJournalEventArgs>();
            EliteStore.JournalEventReceived += (s, e) => received.Add(e);

            using (var watcher = new TestJournalWatcher())
            {
                watcher.RawEventHandler += EliteStore.HandleRawJournal;
                watcher.SetLive(false); // startup replay
                watcher.ParseText(string.Join("\r\n", TestData.JournalLines()));
            }

            EliteStore.HandleRawStatus(null, new RawStatusEventArgs(TestData.Status("status-vaisseau.json")));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            EliteStore.Reset();
        }

        private static JToken Get(string key)
        {
            JToken value;
            Assert.That(EliteStore.TryGet(key, out value), Is.True, "missing key " + key);
            return value;
        }

        [Test]
        public void TenReferenceKeys()
        {
            Assert.That((bool)Get("status.Flags.LandingGearDown"), Is.True);
            Assert.That((double)Get("status.Fuel.FuelMain"), Is.EqualTo(27.5));
            Assert.That((string)Get("status.GuiFocus"), Is.EqualTo("GalaxyMap"));
            Assert.That((int)Get("status.Pips.Engine"), Is.EqualTo(8));
            Assert.That((long)Get("status.Balance"), Is.EqualTo(12345678));

            Assert.That((string)Get("journal.FSDJump.StarSystem"), Is.EqualTo("Two Ladies"));
            Assert.That((double)Get("journal.FSDJump.JumpDist"), Is.EqualTo(19.106).Within(1e-9));
            Assert.That((string)Get("journal.UnderAttack.Target"), Is.EqualTo("You"));
            Assert.That((int)Get("journal.Cargo.Count"), Is.EqualTo(1));
            Assert.That((string)Get("journal.SupercruiseDestinationDrop.Type"), Is.EqualTo("$USS_Type_PowerEmissions;"));
        }

        [Test]
        public void LastEventWins_OlderFieldsDoNotSurvive()
        {
            JToken value;
            // the last Cargo event has no Inventory: the one of an older Cargo event must not survive
            Assert.That(EliteStore.TryGet("journal.Cargo.Inventory.#count", out value), Is.False);
            Assert.That((int)Get("journal.ScanBaryCentre.BodyID"), Is.EqualTo(20));
        }

        [Test]
        public void Replay_IsNeverLive()
        {
            Assert.That(received.Count, Is.EqualTo(TestData.JournalLines().Length));
            Assert.That(received.Any(e => e.IsLive), Is.False);
            Assert.That(EliteStore.GetLiveCount("UnderAttack"), Is.EqualTo(0));
        }
    }
}
