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
    /// v3.3 (docs/L10-v3.md): remembered ships (ShipMemory, PluginMemory), calc.* of the ship, backpack, history.
    /// </summary>
    [TestFixture]
    public class ShipMemoryTests
    {
        private string folder;

        [SetUp]
        public void Clear()
        {
            EliteStore.Reset();
            folder = Path.Combine(Path.GetTempPath(), "zv-memory-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Remove()
        {
            EliteStore.Reset();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        // Loadout of a real ship (numbers only), without engineering
        private static JObject Loadout(int shipId = 6, double hull = 1.0)
        {
            return JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:18:11Z"", ""event"":""Loadout"", ""Ship"":""mandalay"", ""ShipID"":" + shipId + @",
                ""ShipName"":""Test"", ""ShipIdent"":""ZV-01"", ""HullHealth"":" + hull.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",
                ""UnladenMass"":456.400024, ""CargoCapacity"":32, ""MaxJumpRange"":26.69397,
                ""FuelCapacity"":{ ""Main"":32.0, ""Reserve"":0.5 },
                ""Modules"":[ { ""Slot"":""FrameShiftDrive"", ""Item"":""int_hyperdrive_overcharge_size5_class4"" },
                              { ""Slot"":""Slot01_Size5"", ""Item"":""int_cargorack_size5_class1"" } ] }");
        }

        private static void Journal(JObject evt, bool live = true)
        {
            EliteStore.UpdateJournal(new EliteJournalReader.RawJournalEventArgs((string)evt["event"], evt, live));
        }

        private static JToken Read(string key)
        {
            JToken value;
            return EliteStore.TryGet(key, out value) ? value : null;
        }

        [Test]
        public void JumpRangeFormula_GivesTheMaxJumpRangeOfTheGame_ForSixRealShips()
        {
            // UnladenMass, FSD, MaxJumpRange of 6 Loadout events (2026-09): mass = unladen + maximum fuel per jump
            var ships = new[]
            {
                Tuple.Create(79.5, "int_hyperdrive_size3_class4", 15.869396),
                Tuple.Create(232.5, "int_hyperdrive_size4_class1", 11.467434),
                Tuple.Create(456.400024, "int_hyperdrive_overcharge_size5_class4", 26.69397),
                Tuple.Create(1121.300049, "int_hyperdrive_overcharge_size6_class5", 21.232605),
                Tuple.Create(261.600006, "int_hyperdrive_overcharge_size4_class5", 24.204325),
                Tuple.Create(472.0, "int_hyperdrive_size5_class2", 14.136359),
            };

            foreach (var ship in ships)
            {
                var fsd = FsdTables.Drives[ship.Item2];
                var range = fsd.OptimalMass / (ship.Item1 + fsd.MaxFuelPerJump) * Math.Pow(fsd.MaxFuelPerJump / fsd.FuelMultiplier, 1 / fsd.FuelPower);
                Assert.That(range, Is.EqualTo(ship.Item3).Within(0.001), ship.Item2);
            }
        }

        [Test]
        public void Loadout_IsRemembered_AndGivesTheShipKeys()
        {
            Journal(Loadout());

            Assert.That((string)Read("ship.Ship"), Is.EqualTo("mandalay"));
            Assert.That((int)Read("ship.CargoCapacity"), Is.EqualTo(32));
            Assert.That((double)Read("ship.FuelCapacity.Main"), Is.EqualTo(32.0));
            Assert.That((bool)Read("ship.FsdOvercharge"), Is.True, "SCO drive");
            Assert.That((double)Read("ship.Fsd.OptimalMass"), Is.EqualTo(1050));
            Assert.That((double)Read("ship.GuardianBoost"), Is.EqualTo(0));
        }

        [Test]
        public void JumpRange_WithTheCurrentMass()
        {
            Journal(Loadout());

            // one jump of fuel, empty hold: the MaxJumpRange of the game
            EliteStore.UpdateStatus(JObject.Parse(@"{ ""event"":""Status"", ""Flags"":16777224, ""Fuel"":{ ""FuelMain"":5.0, ""FuelReservoir"":0.0 }, ""Cargo"":0.0 }"));
            Assert.That((double)Read(DerivedKeys.JumpRange), Is.EqualTo(26.69).Within(0.01));

            // full tank and full hold: shorter
            EliteStore.UpdateStatus(JObject.Parse(@"{ ""event"":""Status"", ""Flags"":16777224, ""Fuel"":{ ""FuelMain"":32.0, ""FuelReservoir"":0.5 }, ""Cargo"":32.0 }"));
            var loaded = (double)Read(DerivedKeys.JumpRange);
            Assert.That(loaded, Is.LessThan(26.69).And.GreaterThan(20));
            Assert.That((double)Read(DerivedKeys.FuelPercent), Is.EqualTo(100));
            Assert.That((double)Read(DerivedKeys.CargoPercent), Is.EqualTo(100));

            // less fuel than one jump: even shorter than with the full tank's mass
            EliteStore.UpdateStatus(JObject.Parse(@"{ ""event"":""Status"", ""Flags"":16777224, ""Fuel"":{ ""FuelMain"":2.0, ""FuelReservoir"":0.5 }, ""Cargo"":0.0 }"));
            Assert.That((double)Read(DerivedKeys.JumpRange), Is.LessThan(26.69));
            Assert.That((double)Read(DerivedKeys.FuelPercent), Is.EqualTo(6.3));
        }

        [Test]
        public void JumpRange_IsCalibratedOnTheGame_ForAnEngineeringTheTableDoesNotKnow()
        {
            // same ship, game range 10 % higher (e.g. engineering without modifiers in the event)
            var loadout = Loadout();
            loadout["MaxJumpRange"] = 26.69397 * 1.1;
            Journal(loadout);
            EliteStore.UpdateStatus(JObject.Parse(@"{ ""event"":""Status"", ""Flags"":16777224, ""Fuel"":{ ""FuelMain"":5.0 }, ""Cargo"":0.0 }"));

            Assert.That((double)Read(DerivedKeys.JumpRange), Is.EqualTo(26.69397 * 1.1).Within(0.01));
        }

        [Test]
        public void CurrentShip_FollowsLoadGameAndShipyardSwap()
        {
            Journal(Loadout(6));
            var swap = JObject.Parse(@"{ ""timestamp"":""2026-09-28T16:00:00Z"", ""event"":""ShipyardSwap"", ""ShipType"":""viper"", ""ShipID"":2 }");
            Journal(swap);
            Assert.That((string)Read("ship.Ship"), Is.EqualTo("viper"));
            Assert.That(Read("ship.CargoCapacity"), Is.Null, "never seen in a Loadout");

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T17:00:00Z"", ""event"":""LoadGame"", ""Ship"":""mandalay"", ""ShipID"":6 }"));
            Assert.That((int)Read("ship.CargoCapacity"), Is.EqualTo(32), "remembered");
        }

        [Test]
        public void Memory_IsSavedInAFile_AndReadAgain()
        {
            var file = Path.Combine(folder, "memoire.json");
            PluginMemory.Configure(file);
            Journal(Loadout(6));
            History.Track("status.Fuel.FuelMain", 5, DateTime.UtcNow);
            PluginMemory.SaveNow();
            Assert.That(File.Exists(file), Is.True);

            EliteStore.Reset();
            Assert.That(Read("ship.Ship"), Is.Null);

            PluginMemory.Configure(file);
            EliteStore.RefreshShipMemory();
            Assert.That((string)Read("ship.Ship"), Is.EqualTo("mandalay"), "after a restart, before any Loadout");
            Assert.That(History.SeriesCount, Is.EqualTo(1));
        }

        [Test]
        public void UnreadableFile_IsIgnored()
        {
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "memoire.json");
            File.WriteAllText(file, "{ not json");

            Assert.DoesNotThrow(() => PluginMemory.Configure(file));
            Assert.That(ShipMemory.ShipCount, Is.EqualTo(0));
        }

        [Test]
        public void Hull_TheMostRecentValue()
        {
            Journal(Loadout(6, 0.9));
            Assert.That((double)Read(DerivedKeys.HullPercent), Is.EqualTo(90));

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:20:00Z"", ""event"":""HullDamage"", ""Health"":0.42, ""PlayerPilot"":true, ""Fighter"":false }"));
            Assert.That((double)Read(DerivedKeys.HullPercent), Is.EqualTo(42));

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:21:00Z"", ""event"":""HullDamage"", ""Health"":0.10, ""PlayerPilot"":true, ""Fighter"":true }"));
            Assert.That((double)Read(DerivedKeys.HullPercent), Is.EqualTo(42), "damage of the fighter");

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:30:00Z"", ""event"":""RepairAll"", ""Cost"":1000 }"));
            Assert.That((double)Read(DerivedKeys.HullPercent), Is.EqualTo(100));
        }

        [Test]
        public void Supercharged_UntilTheNextJump()
        {
            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:00:00Z"", ""event"":""FSDJump"", ""StarSystem"":""Sol"" }"));
            Assert.That((bool)Read(DerivedKeys.FsdSupercharged), Is.False);

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:05:00Z"", ""event"":""JetConeBoost"", ""BoostValue"":4.0 }"));
            Assert.That((bool)Read(DerivedKeys.FsdSupercharged), Is.True);

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T15:06:00Z"", ""event"":""FSDJump"", ""StarSystem"":""Alpha"" }"));
            Assert.That((bool)Read(DerivedKeys.FsdSupercharged), Is.False);
        }

        [Test]
        public void Backpack_ContentAndChanges()
        {
            // real sequence of 2026-09-28 (consumables only)
            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T05:30:02Z"", ""event"":""Backpack"", ""Items"":[], ""Components"":[], ""Data"":[],
                ""Consumables"":[ { ""Name"":""healthpack"", ""Count"":1 }, { ""Name"":""energycell"", ""Count"":2 },
                                  { ""Name"":""amm_grenade_frag"", ""Count"":2 } ] }"));
            Assert.That((int)Read("calc.Backpack.amm_grenade_frag"), Is.EqualTo(2));

            // UseConsumable + BackpackChange: counted once
            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T05:39:31Z"", ""event"":""UseConsumable"", ""Name"":""energycell"", ""Type"":""Consumable"" }"));
            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T05:39:31Z"", ""event"":""BackpackChange"", ""Removed"":[ { ""Name"":""energycell"", ""Count"":1, ""Type"":""Consumable"" } ] }"));
            Assert.That((int)Read("calc.Backpack.energycell"), Is.EqualTo(1));

            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T05:45:00Z"", ""event"":""BackpackChange"", ""Added"":[ { ""Name"":""bypass"", ""Count"":1, ""Type"":""Consumable"" } ] }"));
            Assert.That((int)Read("calc.Backpack.bypass"), Is.EqualTo(1));

            // the next Backpack event replaces everything
            Journal(JObject.Parse(@"{ ""timestamp"":""2026-09-28T05:52:22Z"", ""event"":""Backpack"", ""Items"":[], ""Components"":[], ""Data"":[],
                ""Consumables"":[ { ""Name"":""healthpack"", ""Count"":1 } ] }"));
            Assert.That(Read("calc.Backpack.bypass"), Is.Null);
        }

        [Test]
        public void History_SamplesEveryInterval_AndKeepsTheLastPoints()
        {
            var values = new Dictionary<string, JToken> { { "status.Fuel.FuelMain", new JValue(10.0) } };
            var start = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
            History.Track("status.Fuel.FuelMain", 5, start);

            for (int s = 0; s <= 20; s++)
            {
                values["status.Fuel.FuelMain"] = new JValue(10.0 - s * 0.1);
                History.Tick(start.AddSeconds(s), key => values.ContainsKey(key) ? values[key] : null);
            }

            var points = History.Points("status.Fuel.FuelMain", 5);
            Assert.That(points.Count, Is.EqualTo(5), "0, 5, 10, 15, 20 s");
            Assert.That(points.Last(), Is.EqualTo(8.0).Within(0.0001));

            for (int s = 25; s < 5 * (History.MaxPoints + 20); s += 5)
                History.Tick(start.AddSeconds(s), key => new JValue(1));
            Assert.That(History.Points("status.Fuel.FuelMain", 5).Count, Is.EqualTo(History.MaxPoints));
        }
    }
}
