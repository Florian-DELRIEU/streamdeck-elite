using System.Linq;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class DataKeyConfigTests
    {
        [Test]
        public void SettingsOfAnL3Key_GiveOneViewWithText()
        {
            // settings saved by the Value action of L3 (before views, rules and key press existed)
            var l3 = JObject.Parse("{ \"source\":\"status.Fuel.FuelMain\", \"prefix\":\"Fuel\\\\n\", \"suffix\":\"\", \"decimals\":\"1\","
                + " \"scale\":\"1\", \"offset\":\"0\", \"compact\":false, \"emptyText\":\"\u2014\", \"backgroundImage\":\"No file...\" }");

            var config = DataKeyConfig.FromSettings(l3);

            Assert.That(config.Views.Count, Is.EqualTo(1));
            Assert.That(config.MainView.Source, Is.EqualTo("status.Fuel.FuelMain"));
            Assert.That(config.MainView.Display.Decimals, Is.EqualTo(1));
            Assert.That(config.MainView.Display.Prefix, Is.EqualTo("Fuel\\n"));
            Assert.That(config.MainView.ShowText, Is.True, "absent -> text shown");
            Assert.That(config.MainView.DefaultImage, Is.Empty, "\"No file...\" = no image");
            Assert.That(config.MainView.Command, Is.Empty);
            Assert.That(config.MainView.Rules, Is.Empty);
            Assert.That(config.PressMode, Is.EqualTo(PressMode.ShortActLongView), "default gesture");
        }

        [Test]
        public void Shortcut_PerView_AndAViewWithOnlyAShortcutExists()
        {
            var settings = JObject.Parse(@"{ ""source"":""status.Flags.NightVision"",
                ""pressHotkey"":""ControlLeft+ShiftLeft+F5"", ""pressHotkeyText"":""Ctrl+Maj+F5"",
                ""pressHotkey3"":""KeyQ"", ""pressHotkeyText3"":""A"" }");

            var config = DataKeyConfig.FromSettings(settings);

            Assert.That(config.MainView.Hotkey, Is.EqualTo("ControlLeft+ShiftLeft+F5"));
            Assert.That(config.MainView.HotkeyText, Is.EqualTo("Ctrl+Maj+F5"));
            Assert.That(config.Views.Select(v => v.Number), Is.EqualTo(new[] { 1, 3 }), "view 3 has only a shortcut");
            Assert.That(config.Views[1].Hotkey, Is.EqualTo("KeyQ"));
            Assert.That(config.Views[1].Command, Is.Empty);
        }

        [Test]
        public void CurrentView_IsTheSavedDisplayedView()
        {
            // keys saved before L7: view 1
            var old = DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""a.b"", ""source2"":""c.d"" }"));
            Assert.That(old.CurrentViewNumber, Is.EqualTo(1));
            Assert.That(old.CurrentViewIndex, Is.EqualTo(0));

            // views 1 and 3: view 3 is the second of the list
            var drawer = DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""a.b"", ""source3"":""c.d"", ""currentView"":3 }"));
            Assert.That(drawer.CurrentViewIndex, Is.EqualTo(1));

            // view removed since, or invalid value: view 1
            Assert.That(DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""a.b"", ""currentView"":2 }")).CurrentViewIndex, Is.EqualTo(0));
            Assert.That(DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""a.b"", ""currentView"":""9"" }")).CurrentViewNumber, Is.EqualTo(1));
            Assert.That(DataKeyConfig.FromSettings(JObject.Parse(@"{ ""source"":""a.b"", ""source2"":""c.d"", ""currentView"":""2"" }")).CurrentViewIndex, Is.EqualTo(1));
        }

        [Test]
        public void SettingsOfAnL4Key_View1KeepsEverything_OtherViewsInheritTheIconOnly()
        {
            // night vision key created by Florian in D4 (L4): rules + command on the key, pressCycle, 2 views
            var l4 = JObject.Parse(@"{
                ""source"":""status.Flags.NightVision"", ""source2"":""journal.CrewAssign.Name"", ""showText"":true,
                ""backgroundImage"":""F:/img/chaff on.png"",
                ""rule1Op"":""isTrue"", ""rule1Value"":"""", ""rule1Image"":""F:/img/night vision on.png"",
                ""rule2Op"":""isFalse"", ""rule2Value"":"""", ""rule2Image"":""F:/img/night vision off.png"",
                ""pressCycle"":true, ""pressCommand"":""NightVisionToggle"", ""clickSound"":"""" }");

            var config = DataKeyConfig.FromSettings(l4);

            Assert.That(config.Views.Count, Is.EqualTo(2));
            var main = config.MainView;
            Assert.That(main.Rules.Count, Is.EqualTo(2));
            Assert.That(main.Command, Is.EqualTo("NightVisionToggle"));
            Assert.That(main.HasOwnIcon, Is.True);

            var second = config.Views[1];
            Assert.That(second.Source, Is.EqualTo("journal.CrewAssign.Name"));
            Assert.That(second.ShowText, Is.True, "absent showText2 -> same as view 1");
            Assert.That(second.HasOwnIcon, Is.False);
            Assert.That(config.IconViewOf(second), Is.SameAs(main), "icon of view 1, computed on the data of view 1");
            Assert.That(second.Command, Is.Empty, "commands are never inherited");
            Assert.That(config.PressMode, Is.EqualTo(PressMode.ShortActLongView), "pressCycle is ignored");
        }

        [Test]
        public void Drawer_EveryViewHasItsOwnDataIconAndCommand()
        {
            var settings = new JObject
            {
                { "source", "status.Flags.LightsOn" }, { "showText", false },
                { "rule1Op", "isTrue" }, { "rule1Image", "C:\\img\\lights on.png" }, { "backgroundImage", "C:\\img\\lights off.png" },
                { "pressCommand", "ShipSpotLightToggle" },

                { "source2", "status.Flags.NightVision" }, { "showText2", false },
                { "rule1Op2", "isTrue" }, { "rule1Value2", "" }, { "rule1Image2", "C:\\img\\night on.png" },
                { "pressCommand2", "NightVisionToggle" }, { "clickSound2", "C:\\snd\\click.wav" },

                // view 3: no data, only an icon and a command (e.g. open the galaxy map)
                { "source3", "" }, { "backgroundImage3", "C:\\img\\galaxy map.png" }, { "pressCommand3", "GalaxyMapOpen" },

                // view 4: nothing at all -> not a view
                { "source4", "" }, { "backgroundImage4", "No file..." }, { "pressCommand4", "" },

                { "pressMode", "shortViewLongAct" },
            };

            var config = DataKeyConfig.FromSettings(settings);

            Assert.That(config.Views.Select(v => v.Number), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(config.Views.Select(v => v.Command), Is.EqualTo(new[] { "ShipSpotLightToggle", "NightVisionToggle", "GalaxyMapOpen" }));
            Assert.That(config.Views[1].ShowText, Is.False);
            Assert.That(config.Views[1].Rules.Single().Image, Is.EqualTo("C:\\img\\night on.png"));
            Assert.That(config.Views[1].Sound, Is.EqualTo("C:\\snd\\click.wav"));
            Assert.That(config.IconViewOf(config.Views[1]), Is.SameAs(config.Views[1]), "own icon, own data");
            Assert.That(config.IconViewOf(config.Views[2]), Is.SameAs(config.Views[2]));
            Assert.That(config.Views[2].Source, Is.Empty);
            Assert.That(config.PressMode, Is.EqualTo(PressMode.ShortViewLongAct));
        }

        [Test]
        public void ViewsWithDifferentFormats()
        {
            var settings = new JObject
            {
                { "source", "status.Fuel.FuelMain" }, { "decimals", "1" }, { "scale", "*100/32" },
                { "source2", "status.Fuel.FuelReservoir" }, { "decimals2", "2" }, { "prefix2", "R\u00e9s.\\n" },
                { "source3", "" },
                { "source4", "status.Balance" }, { "compact4", true },
            };

            var config = DataKeyConfig.FromSettings(settings);

            Assert.That(config.Views.Select(v => v.Source), Is.EqualTo(new[] { "status.Fuel.FuelMain", "status.Fuel.FuelReservoir", "status.Balance" }),
                "empty view 3 skipped");
            Assert.That(config.MainView.Display.Scale, Is.EqualTo(3.125));
            Assert.That(config.Views[1].Display.Decimals, Is.EqualTo(2));
            Assert.That(config.Views[2].Display.Compact, Is.True);
            Assert.That(config.Views[2].Number, Is.EqualTo(4));
        }

        [Test]
        public void EmptySettings_GiveAnEmptyMainView()
        {
            var config = DataKeyConfig.FromSettings(null);

            Assert.That(config.Views.Count, Is.EqualTo(1));
            Assert.That(config.MainView.Source, Is.Empty);
            Assert.That(config.MainView.ShowText, Is.True);
        }
    }
}
