using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elite.Generic;
using EliteJournalReader;
using EliteJournalReader.Events;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// The raw events go through the real JournalWatcher.Parse / StatusWatcher.UpdateStatus code.
    /// </summary>
    [TestFixture]
    public class RawEventTests
    {
        [SetUp]
        public void SetUp()
        {
            EliteStore.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            EliteStore.Reset();
        }

        [Test]
        public void Journal_ReplayThenLive_IsLiveRelayed()
        {
            var received = new List<RawJournalEventArgs>();
            EliteStore.JournalEventReceived += (s, e) => received.Add(e);
            var line = TestData.JournalLine("UnderAttack", 1);

            using (var watcher = new TestJournalWatcher())
            {
                watcher.RawEventHandler += EliteStore.HandleRawJournal;

                watcher.SetLive(false);
                watcher.ParseText(line);
                watcher.SetLive(true);
                watcher.ParseText(line);
            }

            Assert.That(received.Select(e => e.IsLive), Is.EqualTo(new[] { false, true }));
            Assert.That(received[0].EventName, Is.EqualTo("UnderAttack"));
            Assert.That(EliteStore.GetLiveCount("UnderAttack"), Is.EqualTo(1));
        }

        [Test]
        public void Journal_EventsUnknownToTheLibrary_ReachTheStore()
        {
            var typed = new List<string>();

            using (var watcher = new TestJournalWatcher())
            {
                watcher.RawEventHandler += EliteStore.HandleRawJournal;
                watcher.AllEventHandler += (s, e) => typed.Add(e.OriginalEvent.Value<string>("event"));

                foreach (var name in new[] { "GameModeChange", "SupercruiseDestinationDrop", "ScanBaryCentre", "DropshipDeploy" })
                    watcher.ParseText(TestData.JournalLine(name));
            }

            JToken value;
            Assert.That(EliteStore.TryGet("journal.GameModeChange.GameMode", out value), Is.True);
            Assert.That((string)value, Is.EqualTo("MainGame"));
            Assert.That(EliteStore.TryGet("journal.SupercruiseDestinationDrop.Threat", out value), Is.True);
            Assert.That(EliteStore.TryGet("journal.ScanBaryCentre.BodyID", out value), Is.True);
            Assert.That(EliteStore.TryGet("journal.DropshipDeploy.OnPlanet", out value), Is.True);
            Assert.That(typed, Is.Empty, "the typed pipeline does not know these events");
        }

        [Test]
        public void Journal_FailingRawSubscriber_DoesNotBlockTypedEvents()
        {
            int typed = 0;

            using (var watcher = new TestJournalWatcher())
            {
                watcher.RawEventHandler += (s, e) => { throw new InvalidOperationException("test"); };
                watcher.AllEventHandler += (s, e) => typed++;

                watcher.ParseText(TestData.JournalLine("FSDJump"));
            }

            Assert.That(typed, Is.EqualTo(1));
        }

        [Test]
        public void Status_RawAndTypedEventsBothFired()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), "Status-" + Guid.NewGuid().ToString("N") + ".json");
            File.Copy(TestData.PathOf("status-vaisseau.json"), tempFile);
            try
            {
                JObject raw = null;
                StatusFileEvent typed = null;

                using (var watcher = new TestStatusWatcher())
                {
                    watcher.RawStatusUpdated += (s, e) => raw = e.Status;
                    watcher.RawStatusUpdated += EliteStore.HandleRawStatus;
                    watcher.StatusUpdated += (s, e) => typed = e;

                    watcher.Read(tempFile);
                }

                Assert.That(raw, Is.Not.Null);
                Assert.That(raw.Value<long>("Flags"), Is.EqualTo(16842764));
                Assert.That(typed, Is.Not.Null);
                Assert.That(typed.Flags.HasFlag(StatusFlags.LandingGearDown), Is.True);

                JToken value;
                Assert.That(EliteStore.TryGet("status.Pips.Engine", out value), Is.True);
                Assert.That((int)value, Is.EqualTo(8));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
