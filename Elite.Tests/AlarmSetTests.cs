using System;
using Elite.Generic;
using EliteJournalReader;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Several alarms on one Alarm key (Elite/Generic/AlarmSet.cs, v3.6, issue #1).
    /// </summary>
    [TestFixture]
    public class AlarmSetTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

        private static readonly JObject Settings = JObject.Parse(@"{
            ""event"":""UnderAttack"", ""filterField"":""Target"", ""filterOp"":""equals"", ""filterValue"":""You"", ""duration"":""10"",
            ""idleImage"":""idle.png"", ""activeImage"":""attack.png"", ""pressCommand"":""FireChaffLauncher"",
            ""event2"":""HeatWarning"", ""duration2"":""0"", ""activeImage2"":""heat.png"", ""pressCommand2"":""DeployHeatSink"",
            ""event4"":""FuelScoop"" }");

        private static RawJournalEventArgs Event(string json, bool live = true)
        {
            var evt = JObject.Parse(json);
            return new RawJournalEventArgs((string)evt["event"], evt, live);
        }

        [Test]
        public void Settings_Alarm1AlwaysAndTheOthersWhenSet()
        {
            var set = AlarmSet.FromSettings(Settings);

            Assert.That(set.Count, Is.EqualTo(3), "alarm 3 has no event");
            Assert.That(set.Config(0).Event, Is.EqualTo("UnderAttack"));
            Assert.That(set.Config(1).Event, Is.EqualTo("HeatWarning"));
            Assert.That(set.Config(1).DurationSeconds, Is.EqualTo(0));
            Assert.That(set.Config(1).Command, Is.EqualTo("DeployHeatSink"));
            Assert.That(set.Config(1).IdleImage, Is.EqualTo("idle.png"), "idle image common");
            Assert.That(set.Config(2).Event, Is.EqualTo("FuelScoop"), "alarm 4, third of the set");

            // a key of L8 (one alarm): unchanged
            var old = AlarmSet.FromSettings(JObject.Parse(@"{ ""event"":""UnderAttack"", ""duration"":""5"" }"));
            Assert.That(old.Count, Is.EqualTo(1));
            Assert.That(AlarmSet.FromSettings(null).Count, Is.EqualTo(1));
        }

        [Test]
        public void TheMostRecentActiveAlarm_IsShown_AndAcknowledged()
        {
            var set = AlarmSet.FromSettings(Settings);
            Assert.That(set.Active(T0), Is.Null);

            Assert.That(set.Observe(Event(@"{ ""event"":""UnderAttack"", ""Target"":""You"" }"), T0), Is.EqualTo(new[] { 0 }));
            Assert.That(set.Active(T0), Is.EqualTo(0));

            Assert.That(set.Observe(Event(@"{ ""event"":""HeatWarning"" }"), T0.AddSeconds(2)), Is.EqualTo(new[] { 1 }));
            Assert.That(set.Active(T0.AddSeconds(3)), Is.EqualTo(1), "the most recent");

            // a press acknowledges the displayed alarm only: the attack comes back
            Assert.That(set.Acknowledge(T0.AddSeconds(4)), Is.EqualTo(1));
            Assert.That(set.Active(T0.AddSeconds(4)), Is.EqualTo(0));

            // the attack ends after its 10 s: rest, a press acknowledges nothing (command of alarm 1)
            Assert.That(set.Active(T0.AddSeconds(11)), Is.Null);
            Assert.That(set.Acknowledge(T0.AddSeconds(11)), Is.Null);
        }

        [Test]
        public void EachAlarm_KeepsItsFilterAndTheLiveGuard()
        {
            var set = AlarmSet.FromSettings(Settings);

            Assert.That(set.Observe(Event(@"{ ""event"":""UnderAttack"", ""Target"":""Fighter"" }"), T0), Is.Empty, "filter of alarm 1");
            Assert.That(set.Observe(Event(@"{ ""event"":""HeatWarning"" }", live: false), T0), Is.Empty, "replayed at startup");
            Assert.That(set.Observe(Event(@"{ ""event"":""FuelScoop"", ""Scooped"":1.5 }"), T0), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void AlertsInProgress_SurviveASettingsChange()
        {
            var set = AlarmSet.FromSettings(Settings);
            set.Trigger(1, T0);

            var changed = AlarmSet.FromSettings(Settings);
            changed.KeepStatesOf(set);
            Assert.That(changed.Active(T0.AddSeconds(30)), Is.EqualTo(1), "duration 0: until acknowledged");
        }
    }
}
