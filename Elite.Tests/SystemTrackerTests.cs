using System;
using System.IO;
using System.Linq;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// v3.8 (issue #1, docs/L11-v3.8.md): current system (system.*), target system (target.*), visited systems.
    /// Data/journal-systeme.txt: real lines of 2026-09-22 (reduced to the travel events): Location in LHS 3167, then a
    /// route of 3 jumps Pladset (Y) -> Col 285 Sector NE-R a34-2 (T) -> Beta-3 Tucani (A).
    /// </summary>
    [TestFixture]
    public class SystemTrackerTests
    {
        private const string LhsAddress = "3932210336474";
        private string folder;

        [SetUp]
        public void Clear()
        {
            EliteStore.Reset();
            folder = Path.Combine(Path.GetTempPath(), "zv-systems-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Remove()
        {
            EliteStore.Reset();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private static string[] Lines()
        {
            return File.ReadAllLines(TestData.PathOf("journal-systeme.txt")).Where(l => l.Trim().Length > 0).ToArray();
        }

        private static void Journal(string line, bool live = false)
        {
            var evt = JObject.Parse(line);
            EliteStore.UpdateJournal(new EliteJournalReader.RawJournalEventArgs((string)evt["event"], evt, live));
        }

        // lines up to (and including) the n-th one
        private static void Replay(int count)
        {
            foreach (var line in Lines().Take(count))
                Journal(line);
        }

        private static JToken Read(string key)
        {
            JToken value;
            return EliteStore.TryGet(key, out value) ? value : null;
        }

        [Test]
        public void Location_GivesTheSystem_ItsControllingFaction_AndItsConflict()
        {
            Replay(1);
            Assert.That((string)Read("system.Name"), Is.EqualTo("LHS 3167"));
            Assert.That((long)Read("system.Population"), Is.EqualTo(8667714761L));
            Assert.That((string)Read("system.Security"), Is.EqualTo("Haute sécurité"), "text of the game in French");
            Assert.That((string)Read("system.Power"), Is.EqualTo("Aisling Duval"));
            Assert.That((bool)Read("system.Wanted"), Is.False);

            Assert.That((string)Read("system.Faction.Name"), Is.EqualTo("Arbor Caelum Internal Defense"));
            Assert.That((string)Read("system.Faction.State"), Is.EqualTo("Aucun"));
            Assert.That((double)Read("system.Faction.Influence"), Is.EqualTo(47.0), "0.46994 -> %");
            Assert.That((double)Read("system.Faction.Reputation"), Is.EqualTo(30.0));
            Assert.That((string)Read("system.Faction.Relation"), Is.EqualTo("Cordial"));
            Assert.That((string)Read("system.Faction.Happiness"), Is.EqualTo("Heureuse"));

            Assert.That((int)Read("system.Conflicts"), Is.EqualTo(1));
            Assert.That((string)Read("system.Conflict.Type"), Is.EqualTo("Guerre civile"));
            Assert.That((string)Read("system.Conflict.Status"), Is.EqualTo("En attente"));
            Assert.That((string)Read("system.Conflict.Faction"), Is.EqualTo("LHS 3167 Values Party"), "not the controlling faction: Faction1");
            Assert.That((string)Read("system.Conflict.Opponent"), Is.EqualTo("LHS 3167 Purple Bridge Company"));

            Assert.That(Read("system.Star.Class"), Is.Null, "a Location does not give the star");
            Assert.That(Read("journal.Location.Factions.#count"), Is.Not.Null, "journal.* unchanged");
        }

        [Test]
        public void Route_TargetThenArrival_WithTheStarOfStartJump()
        {
            Replay(2); // FSDTarget Pladset
            Assert.That((string)Read("target.Name"), Is.EqualTo("Pladset"));
            Assert.That((string)Read("target.Star.Class"), Is.EqualTo("Y"));
            Assert.That((bool)Read("target.Star.Scoopable"), Is.False, "brown dwarf");
            Assert.That((bool)Read("target.Known"), Is.False);
            Assert.That((int)Read("target.RemainingJumps"), Is.EqualTo(3));
            Assert.That(Read("target.Population"), Is.Null, "the game says nothing before the arrival");

            Journal(Lines()[2]); // StartJump Pladset (Y)
            Journal(Lines()[3]); // FSDTarget Col 285... (T): written during the jump
            Journal(Lines()[4]); // FSDJump Pladset
            Assert.That((string)Read("system.Name"), Is.EqualTo("Pladset"));
            Assert.That((string)Read("system.Star.Class"), Is.EqualTo("Y"));
            Assert.That((bool)Read("system.Star.Scoopable"), Is.False);
            Assert.That((string)Read("system.Faction.State"), Is.EqualTo("Boom"));
            Assert.That((string)Read("system.Faction.Relation"), Is.EqualTo("Neutre"));
            Assert.That((string)Read("target.Name"), Is.EqualTo("Col 285 Sector NE-R a34-2"));

            foreach (var line in Lines().Skip(5).Take(5)) // Scan, StartJump, FSDTarget Beta-3, FSDJump Col 285, StartJump
                Journal(line);
            Assert.That((string)Read("system.Star.Class"), Is.EqualTo("T"));
            Assert.That((string)Read("target.Name"), Is.EqualTo("Beta-3 Tucani"));
            Assert.That((bool)Read("target.Star.Scoopable"), Is.True, "A");

            Journal(Lines()[10]); // NavRouteClear, written before the last arrival
            Assert.That(Read("target.Name"), Is.Null);
            Assert.That(Read("target.Known"), Is.Null);

            Journal(Lines()[11]); // FSDJump Beta-3 Tucani
            Assert.That((string)Read("system.Name"), Is.EqualTo("Beta-3 Tucani"));
            Assert.That((string)Read("system.Star.Class"), Is.EqualTo("A"));
            Assert.That((bool)Read("system.Star.Scoopable"), Is.True);
            Assert.That((string)Read("system.Faction.State"), Is.EqualTo("Jour férié"));
            Assert.That((string)Read("system.Faction.Relation"), Is.EqualTo("Ami"), "51.0");
            Assert.That((int)Read("system.Conflicts"), Is.EqualTo(2));
            Assert.That((string)Read("system.Conflict.Type"), Is.EqualTo("Guerre"));
            Assert.That((string)Read("system.Conflict.Status"), Is.EqualTo("En cours"));
        }

        [Test]
        public void TargetAlreadyVisited_GivesItsLastVisit()
        {
            Replay(Lines().Length);
            Journal(@"{ ""timestamp"":""2026-09-22T00:40:00Z"", ""event"":""FSDTarget"", ""Name"":""LHS 3167"", ""SystemAddress"":" + LhsAddress + @", ""StarClass"":""M"", ""RemainingJumpsInRoute"":1 }");

            Assert.That((bool)Read("target.Known"), Is.True);
            Assert.That((long)Read("target.Population"), Is.EqualTo(8667714761L));
            Assert.That((string)Read("target.Faction.Name"), Is.EqualTo("Arbor Caelum Internal Defense"));
            Assert.That((string)Read("target.Faction.Relation"), Is.EqualTo("Cordial"));
            Assert.That((int)Read("target.Conflicts"), Is.EqualTo(1));
            Assert.That(Read("target.LastVisit"), Is.Not.Null);
            Assert.That((string)Read("target.Star.Class"), Is.EqualTo("M"), "class written by the game with the target");
            Assert.That((bool)Read("target.Star.Scoopable"), Is.True);
        }

        [Test]
        public void LocationInAKnownSystem_TakesTheStarFromTheMemory()
        {
            Replay(Lines().Length);
            Journal(@"{ ""timestamp"":""2026-09-22T11:04:03Z"", ""event"":""Location"", ""StarSystem"":""Beta-3 Tucani"", ""SystemAddress"":2827992680811, ""Population"":256427 }");
            Assert.That((string)Read("system.Star.Class"), Is.EqualTo("A"));
            Assert.That(Read("system.Faction.Name"), Is.Null, "what the new event says, not the memory");
        }

        [Test]
        public void Memory_IsSavedAndReadAgain()
        {
            var file = Path.Combine(folder, "memoire.json");
            PluginMemory.Configure(file);
            Replay(Lines().Length);
            PluginMemory.SaveNow();
            Assert.That(SystemMemory.Count, Is.EqualTo(4));

            SystemMemory.Reset();
            PluginMemory.Configure(file);
            Assert.That(SystemMemory.Count, Is.EqualTo(4));
            var lhs = SystemMemory.Find(LhsAddress);
            Assert.That((string)lhs["Faction"]["Relation"], Is.EqualTo("Cordial"));
            Assert.That(DerivedKeys.Time(lhs[SystemTracker.VisitedField]), Is.EqualTo(new DateTime(2026, 9, 21, 23, 22, 36, DateTimeKind.Utc)));
        }

        [Test]
        public void Memory_KeepsTheLastThousandSystems()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 1; i <= SystemMemory.MaxSystems + 5; i++)
            {
                SystemMemory.Remember(new JObject
                {
                    { "Name", "S" + i },
                    { "Address", i },
                    { SystemTracker.VisitedField, start.AddMinutes(i).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") },
                });
            }

            Assert.That(SystemMemory.Count, Is.EqualTo(SystemMemory.MaxSystems));
            Assert.That(SystemMemory.Find("1"), Is.Null, "oldest visit removed");
            Assert.That(SystemMemory.Find("5"), Is.Null);
            Assert.That(SystemMemory.Find("6"), Is.Not.Null);
            Assert.That(SystemMemory.Find((SystemMemory.MaxSystems + 5).ToString()), Is.Not.Null);
        }

        [TestCase("O", true)]
        [TestCase("B", true)]
        [TestCase("A", true)]
        [TestCase("F", true)]
        [TestCase("G", true)]
        [TestCase("K", true)]
        [TestCase("M", true)]
        [TestCase("K_OrangeGiant", true)]
        [TestCase("M_RedSuperGiant", true)]
        [TestCase("A_BlueWhiteSuperGiant", true)]
        [TestCase("MS", false)]
        [TestCase("S", false)]
        [TestCase("N", false)]
        [TestCase("H", false)]
        [TestCase("DA", false)]
        [TestCase("DC", false)]
        [TestCase("W", false)]
        [TestCase("WC", false)]
        [TestCase("TTS", false)]
        [TestCase("AeBe", false)]
        [TestCase("Y", false)]
        [TestCase("L", false)]
        [TestCase("T", false)]
        [TestCase("C", false)]
        [TestCase("CN", false)]
        [TestCase("SupermassiveBlackHole", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Scoopable(string starClass, bool expected)
        {
            Assert.That(SystemTracker.IsScoopable(starClass), Is.EqualTo(expected));
        }

        [TestCase(-100, "Hostile")]
        [TestCase(-90, "Hostile")]
        [TestCase(-89.9, "Inamical")]
        [TestCase(-35, "Inamical")]
        [TestCase(-34.9, "Neutre")]
        [TestCase(0, "Neutre")]
        [TestCase(3.9, "Neutre")]
        [TestCase(4, "Cordial")]
        [TestCase(34.9, "Cordial")]
        [TestCase(35, "Ami")]
        [TestCase(89.9, "Ami")]
        [TestCase(90, "Allié")]
        [TestCase(100, "Allié")]
        public void Relation(double reputation, string expected)
        {
            Assert.That(SystemTracker.Relation(reputation), Is.EqualTo(expected));
        }
    }
}
