using System;
using System.Collections.Generic;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// calc.* keys (Elite/Generic/DerivedKeys.cs, docs/L10-v3.md) and FireGroupSender (issue #3).
    /// </summary>
    [TestFixture]
    public class DerivedKeysTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 28, 11, 49, 0, DateTimeKind.Utc);

        private Dictionary<string, JToken> store;

        [SetUp]
        public void Clear()
        {
            store = new Dictionary<string, JToken>(StoreKeys.Comparer);
        }

        private JToken Read(string key)
        {
            JToken value;
            return store.TryGetValue(key, out value) ? value : null;
        }

        private void Event(string name, int seconds, params object[] fields)
        {
            // the store keeps only the last event of each type
            foreach (var key in new List<string>(store.Keys))
                if (key.StartsWith("journal." + name + ".", StringComparison.OrdinalIgnoreCase))
                    store.Remove(key);

            store["journal." + name + ".timestamp"] = new JValue(T0.AddSeconds(seconds));
            for (int i = 0; i < fields.Length; i += 2)
                store["journal." + name + "." + fields[i]] = new JValue(fields[i + 1]);
        }

        private int Remaining()
        {
            return DerivedKeys.ComputeRemainingJumps(Read);
        }

        [Test]
        public void RouteOfThreeJumps_CountsDown_AndIsZeroAtTheDestination()
        {
            Event("Location", 0, "StarSystem", "Sol");
            Assert.That(Remaining(), Is.EqualTo(0), "no route");

            Event("FSDTarget", 10, "Name", "Alpha", "RemainingJumpsInRoute", 3);
            Assert.That(Remaining(), Is.EqualTo(3));

            Event("FSDJump", 20, "StarSystem", "Alpha");
            Event("FSDTarget", 30, "Name", "Beta", "RemainingJumpsInRoute", 2);
            Assert.That(Remaining(), Is.EqualTo(2));

            Event("FSDJump", 40, "StarSystem", "Beta");
            Event("FSDTarget", 50, "Name", "Gamma", "RemainingJumpsInRoute", 1);
            Assert.That(Remaining(), Is.EqualTo(1));

            // the game writes no FSDTarget 0: FSDTarget.RemainingJumpsInRoute stays at 1
            Event("FSDJump", 60, "StarSystem", "Gamma");
            Assert.That(Read("journal.FSDTarget.RemainingJumpsInRoute").Value<int>(), Is.EqualTo(1));
            Assert.That(Remaining(), Is.EqualTo(0), "current system = FSD target");
        }

        [Test]
        public void RouteCleared_AfterTheLastTarget_IsZero()
        {
            // real sequence of 2026-09-28: FSDTarget 1, NavRouteClear, then the FSDJump
            Event("FSDJump", 0, "StarSystem", "Sol");
            Event("FSDTarget", 10, "Name", "Alpha", "RemainingJumpsInRoute", 1);
            Event("NavRouteClear", 54);
            Assert.That(Remaining(), Is.EqualTo(0));

            // a new route after the clearing counts again
            Event("FSDTarget", 90, "Name", "Beta", "RemainingJumpsInRoute", 4);
            Assert.That(Remaining(), Is.EqualTo(4));
        }

        [Test]
        public void CurrentSystem_IsTheMostRecentOfFsdJumpLocationCarrierJump()
        {
            Event("FSDJump", 0, "StarSystem", "Sol");
            Event("FSDTarget", 10, "Name", "Alpha", "RemainingJumpsInRoute", 2);
            Assert.That(Remaining(), Is.EqualTo(2));

            // relog in the target system: Location is more recent than the FSDJump
            Event("Location", 20, "StarSystem", "alpha");
            Assert.That(Remaining(), Is.EqualTo(0), "compared without case");

            Event("CarrierJump", 30, "StarSystem", "Delta");
            Assert.That(Remaining(), Is.EqualTo(2));
        }

        [Test]
        public void Compute_GivesTheCalcKey()
        {
            Event("FSDTarget", 10, "Name", "Alpha", "RemainingJumpsInRoute", 5);
            var keys = DerivedKeys.Compute(Read);
            Assert.That(keys[DerivedKeys.RemainingJumps].Value<int>(), Is.EqualTo(5));
            Assert.That(DerivedKeys.RemainingJumps, Does.StartWith(StoreKeys.CalcPrefix + "."));
        }

        [Test]
        public void FireGroupCommands()
        {
            int group;
            Assert.That(FireGroupSender.TryParse("FireGroup-A", out group), Is.True);
            Assert.That(group, Is.EqualTo(0));
            Assert.That(FireGroupSender.TryParse("FireGroup-H", out group), Is.True);
            Assert.That(group, Is.EqualTo(7));
            Assert.That(FireGroupSender.TryParse("FireGroup-I", out group), Is.False);
            Assert.That(FireGroupSender.TryParse("CycleFireGroupNext", out group), Is.False);
            Assert.That(FireGroupSender.TryParse(null, out group), Is.False);

            Assert.That(FireGroupSender.Presses(2, 0), Is.EqualTo(2), "C from A: 2 x next");
            Assert.That(FireGroupSender.Presses(0, 3), Is.EqualTo(-3), "A from D: 3 x previous");
            Assert.That(FireGroupSender.Presses(1, 1), Is.EqualTo(0));
        }

        [Test]
        public void SrvFireGroupBindings_AreReadFromTheBindingsFile()
        {
            var xml = "<Root PresetName=\"Custom\" MajorVersion=\"4\" MinorVersion=\"2\">"
                + "<BuggyCycleFireGroupNext><Primary Device=\"Keyboard\" Key=\"Key_N\" /><Secondary Device=\"{NoDevice}\" Key=\"\" /></BuggyCycleFireGroupNext>"
                + "<BuggyCycleFireGroupPrevious><Primary Device=\"Keyboard\" Key=\"Key_F5\" /><Secondary Device=\"{NoDevice}\" Key=\"\" /></BuggyCycleFireGroupPrevious>"
                + "</Root>";
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(UserBindings));
            var bindings = (UserBindings)serializer.Deserialize(new System.IO.StringReader(xml));

            Assert.That(bindings.BuggyCycleFireGroupNext.Primary.Key, Is.EqualTo("Key_N"));
            Assert.That(bindings.BuggyCycleFireGroupPrevious.Primary.Key, Is.EqualTo("Key_F5"));
        }
    }
}
