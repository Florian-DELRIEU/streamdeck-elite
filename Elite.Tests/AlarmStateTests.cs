using System;
using System.Collections.Generic;
using System.Linq;
using Elite.Generic;
using EliteJournalReader;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Alert duration and acknowledgement of the Alarm key (docs/L8-alarme.md).
    /// </summary>
    [TestFixture]
    public class AlarmStateTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 25, 20, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Idle_UntilTriggered()
        {
            Assert.That(new AlarmState().IsActive(T0), Is.False);
        }

        [Test]
        public void Duration_EndsTheAlert()
        {
            var state = new AlarmState();
            state.Trigger(T0, 5);

            Assert.That(state.IsActive(T0), Is.True);
            Assert.That(state.IsActive(T0.AddSeconds(4.9)), Is.True);
            Assert.That(state.IsActive(T0.AddSeconds(5)), Is.False);
        }

        [Test]
        public void NewEventDuringTheAlert_RestartsTheDuration()
        {
            var state = new AlarmState();
            state.Trigger(T0, 5);
            state.Trigger(T0.AddSeconds(4), 5);

            Assert.That(state.IsActive(T0.AddSeconds(8)), Is.True);
            Assert.That(state.IsActive(T0.AddSeconds(9)), Is.False);
        }

        [Test]
        public void DurationZero_UntilAcknowledged()
        {
            var state = new AlarmState();
            state.Trigger(T0, 0);

            Assert.That(state.IsActive(T0.AddHours(3)), Is.True);
            Assert.That(state.Acknowledge(T0.AddHours(3)), Is.True, "was active");
            Assert.That(state.IsActive(T0.AddHours(3)), Is.False);
            Assert.That(state.Acknowledge(T0.AddHours(3)), Is.False, "nothing left to acknowledge");
        }

        [Test]
        public void Acknowledge_EndsATimedAlertToo()
        {
            var state = new AlarmState();
            state.Trigger(T0, 30);

            Assert.That(state.Acknowledge(T0.AddSeconds(1)), Is.True);
            Assert.That(state.IsActive(T0.AddSeconds(2)), Is.False);
            Assert.That(state.Acknowledge(T0.AddSeconds(31)), Is.False);
        }
    }

    /// <summary>
    /// Acceptance test of §8 (cahier des charges): an UnderAttack alarm filtered on You is triggered in combat, but not at
    /// startup by a journal that already contains the attacks. The lines go through the real JournalWatcher and EliteStore,
    /// as in the plugin (Program.cs).
    /// </summary>
    [TestFixture]
    public class AlarmLiveGuardTests
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
        public void ReplayedEvents_NeverTrigger_LiveOnesDo()
        {
            var config = AlarmConfig.FromSettings(JObject.Parse(
                @"{ ""event"":""UnderAttack"", ""filterField"":""Target"", ""filterOp"":""equals"", ""filterValue"":""You"" }"));
            var triggers = new List<string>();
            EliteStore.JournalEventReceived += (s, e) =>
            {
                if (config.ShouldTrigger(e))
                    triggers.Add((e.IsLive ? "live " : "replay ") + e.Event["Target"]);
            };

            using (var watcher = new TestJournalWatcher())
            {
                watcher.RawEventHandler += EliteStore.HandleRawJournal;

                // startup: the whole journal extract is replayed, attacks included
                watcher.SetLive(false);
                foreach (var line in TestData.JournalLines())
                    watcher.ParseText(line);
                Assert.That(triggers, Is.Empty, "no alarm at startup");

                // in combat
                watcher.SetLive(true);
                watcher.ParseText(TestData.JournalLine("UnderAttack", 0)); // Mothership: filtered out
                watcher.ParseText(TestData.JournalLine("UnderAttack", 1)); // You
                watcher.ParseText(TestData.JournalLine("HullDamage"));     // another event
            }

            Assert.That(triggers, Is.EqualTo(new[] { "live You" }));
        }

        [Test]
        public void ShouldTrigger_NullOrReplayed_False()
        {
            var config = AlarmConfig.FromSettings(JObject.Parse(@"{ ""event"":""UnderAttack"" }"));
            var evt = JObject.Parse(TestData.JournalLine("UnderAttack", 1));

            Assert.That(config.ShouldTrigger(null), Is.False);
            Assert.That(config.ShouldTrigger(new RawJournalEventArgs("UnderAttack", evt, false)), Is.False);
            Assert.That(config.ShouldTrigger(new RawJournalEventArgs("UnderAttack", evt, true)), Is.True);
        }
    }
}
