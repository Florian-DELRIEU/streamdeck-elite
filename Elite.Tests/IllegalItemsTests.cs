using System;
using System.IO;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// v3.8 (docs/L11-v3.8.md, D10 point 9): illegal items of the backpack, because status.LegalState only follows the
    /// ship. Items of the editable list (IllegalItems, Omnipasse = bypass by default) and stolen items (CollectItems).
    /// </summary>
    [TestFixture]
    public class IllegalItemsTests
    {
        private string folder;

        [SetUp]
        public void Clear()
        {
            EliteStore.Reset();
            folder = Path.Combine(Path.GetTempPath(), "zv-illegal-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Remove()
        {
            EliteStore.Reset();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private static void Journal(string line, bool live = true)
        {
            var evt = JObject.Parse(line);
            EliteStore.UpdateJournal(new EliteJournalReader.RawJournalEventArgs((string)evt["event"], evt, live));
        }

        private static JToken Read(string key)
        {
            JToken value;
            return EliteStore.TryGet(key, out value) ? value : null;
        }

        // Backpack when disembarking, 2026-09-29 (D10 test): no illegal item yet
        private const string BackpackOfD10 = @"{ ""timestamp"":""2026-09-29T04:49:15Z"", ""event"":""Backpack"", ""Items"":[  ], ""Components"":[  ], ""Consumables"":[ { ""Name"":""healthpack"", ""Name_Localised"":""Médikit"", ""OwnerID"":0, ""Count"":1 }, { ""Name"":""energycell"", ""Name_Localised"":""Cellule d'énergie"", ""OwnerID"":0, ""Count"":2 }, { ""Name"":""amm_grenade_frag"", ""Name_Localised"":""Grenade à fragmentation"", ""OwnerID"":0, ""Count"":2 } ], ""Data"":[  ] }";

        [Test]
        public void OmnipassesTakenFromTheShip_AreIllegal()
        {
            Assert.That(Read(BackpackTracker.IllegalKey), Is.Null, "unknown before the first backpack event");

            Journal(BackpackOfD10);
            Assert.That((bool)Read(BackpackTracker.IllegalKey), Is.False);
            Assert.That((int)Read(BackpackTracker.IllegalCountKey), Is.EqualTo(0));

            Journal(@"{ ""timestamp"":""2026-09-29T04:49:27Z"", ""event"":""BackpackChange"", ""Added"":[ { ""Name"":""bypass"", ""Name_Localised"":""Omnipasse"", ""OwnerID"":0, ""Count"":2, ""Type"":""Consumable"" } ] }");
            Assert.That((bool)Read(BackpackTracker.IllegalKey), Is.True);
            Assert.That((int)Read(BackpackTracker.IllegalCountKey), Is.EqualTo(2));
            Assert.That((int)Read(BackpackTracker.StolenKey), Is.EqualTo(0));

            // both used: legal again
            Journal(@"{ ""timestamp"":""2026-09-29T04:50:00Z"", ""event"":""BackpackChange"", ""Removed"":[ { ""Name"":""bypass"", ""Name_Localised"":""Omnipasse"", ""OwnerID"":0, ""Count"":2, ""Type"":""Consumable"" } ] }");
            Assert.That((bool)Read(BackpackTracker.IllegalKey), Is.False);
        }

        [Test]
        public void StolenItems_AreCountedOnceCarried_AndForgottenWhenRemoved()
        {
            Journal(BackpackOfD10);

            // real sequence of 2025-06-30: CollectItems (Stolen) then its BackpackChange
            Journal(@"{ ""timestamp"":""2025-06-30T18:59:05Z"", ""event"":""CollectItems"", ""Name"":""personaldocuments"", ""Name_Localised"":""Documents personnels"", ""Type"":""Item"", ""OwnerID"":2953707555, ""Count"":1, ""Stolen"":true }");
            Assert.That((int)Read(BackpackTracker.StolenKey), Is.EqualTo(0), "not in the backpack yet");
            Journal(@"{ ""timestamp"":""2025-06-30T18:59:05Z"", ""event"":""BackpackChange"", ""Added"":[ { ""Name"":""personaldocuments"", ""Name_Localised"":""Documents personnels"", ""OwnerID"":2953707555, ""Count"":1, ""Type"":""Item"" } ] }");
            Assert.That((int)Read(BackpackTracker.StolenKey), Is.EqualTo(1));
            Assert.That((bool)Read(BackpackTracker.IllegalKey), Is.True);

            // an item collected legally is not stolen
            Journal(@"{ ""timestamp"":""2025-06-15T09:36:14Z"", ""event"":""CollectItems"", ""Name"":""energycell"", ""Name_Localised"":""Cellule d'énergie"", ""Type"":""Consumable"", ""OwnerID"":0, ""Count"":1, ""Stolen"":false }");
            Journal(@"{ ""timestamp"":""2025-06-15T09:36:14Z"", ""event"":""BackpackChange"", ""Added"":[ { ""Name"":""energycell"", ""OwnerID"":0, ""Count"":1, ""Type"":""Consumable"" } ] }");
            Assert.That((int)Read(BackpackTracker.IllegalCountKey), Is.EqualTo(1));

            Journal(@"{ ""timestamp"":""2025-06-30T19:00:13Z"", ""event"":""BackpackChange"", ""Removed"":[ { ""Name"":""personaldocuments"", ""OwnerID"":2953707555, ""Count"":1, ""Type"":""Item"" } ] }");
            Assert.That((int)Read(BackpackTracker.StolenKey), Is.EqualTo(0));
            Assert.That((bool)Read(BackpackTracker.IllegalKey), Is.False);

            // collected again later: counted again from 0, not from an old count
            Journal(@"{ ""timestamp"":""2025-06-30T19:02:08Z"", ""event"":""BackpackChange"", ""Added"":[ { ""Name"":""personaldocuments"", ""OwnerID"":0, ""Count"":1, ""Type"":""Item"" } ] }");
            Assert.That((int)Read(BackpackTracker.StolenKey), Is.EqualTo(0));
        }

        [Test]
        public void EditableList_ReplacesTheDefault()
        {
            IllegalItems.Set(new[] { "energycell" });
            Journal(BackpackOfD10);
            Assert.That((int)Read(BackpackTracker.IllegalCountKey), Is.EqualTo(2), "2 energy cells");
        }

        [Test]
        public void ListFile_IsCreatedWithTheDefaults_ThenRead()
        {
            var file = Path.Combine(folder, IllegalItems.FileName);
            IllegalItems.Configure(file);
            Assert.That(File.Exists(file), Is.True);
            Assert.That(IllegalItems.Contains("bypass"), Is.True);
            Assert.That(IllegalItems.Parse(File.ReadAllText(file)), Is.EqualTo(new[] { "bypass" }));

            File.WriteAllText(file, @"{ ""objets"": [ ""bypass"", ""personaldocuments"" ] }");
            IllegalItems.Configure(file);
            Assert.That(IllegalItems.Contains("PersonalDocuments"), Is.True, "case ignored");
            Assert.That(IllegalItems.Count, Is.EqualTo(2));

            File.WriteAllText(file, "not json");
            IllegalItems.Configure(file);
            Assert.That(IllegalItems.Count, Is.EqualTo(1), "unreadable file: default list");
            Assert.That(IllegalItems.Contains("bypass"), Is.True);
        }

        [Test]
        public void Parse_AcceptsAnObjectOrAnArray()
        {
            Assert.That(IllegalItems.Parse(@"[ ""a"", "" b "", """", 3 ]"), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(IllegalItems.Parse(@"{ ""objets"": [ ""a"" ] }"), Is.EqualTo(new[] { "a" }));
            Assert.That(IllegalItems.Parse(@"{ ""items"": [ ""a"" ] }"), Is.Null);
            Assert.That(IllegalItems.Parse("{"), Is.Null);
            Assert.That(IllegalItems.Parse(null), Is.Null);
        }

        [Test]
        public void PluginMemory_ConfiguresTheListOfTheSameFolder()
        {
            PluginMemory.Configure(Path.Combine(folder, "memoire.json"));
            Assert.That(File.Exists(Path.Combine(folder, IllegalItems.FileName)), Is.True);
        }
    }
}
