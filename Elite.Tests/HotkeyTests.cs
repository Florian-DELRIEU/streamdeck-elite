using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Elite.Generic;
using NUnit.Framework;
using WindowsInput.Native;

namespace Elite.Tests
{
    /// <summary>
    /// Free keyboard shortcut of a "Donnee" view (Elite/Generic/Hotkey.cs, docs/L7-retours-d6.md).
    /// </summary>
    [TestFixture]
    public class HotkeyTests
    {
        private static DirectInputKeyCode Parse(string text, out List<DirectInputKeyCode> modifiers)
        {
            DirectInputKeyCode key;
            Assert.That(Hotkey.TryParse(text, out modifiers, out key), Is.True, text);
            return key;
        }

        [Test]
        public void ModifiersAreHeld_TheLastKeyIsPressed()
        {
            List<DirectInputKeyCode> modifiers;
            var key = Parse("ControlLeft+ShiftLeft+F5", out modifiers);

            Assert.That(key, Is.EqualTo(DirectInputKeyCode.DikF5));
            Assert.That(modifiers, Is.EqualTo(new[] { DirectInputKeyCode.DikLcontrol, DirectInputKeyCode.DikLshift }));

            key = Parse("F5+AltLeft", out modifiers);
            Assert.That(key, Is.EqualTo(DirectInputKeyCode.DikF5), "whatever the order of the recording");
            Assert.That(modifiers, Is.EqualTo(new[] { DirectInputKeyCode.DikLmenu }));
        }

        [Test]
        public void PhysicalKeys_IndependentOfTheLayout()
        {
            List<DirectInputKeyCode> modifiers;
            Assert.That(Parse("KeyQ", out modifiers), Is.EqualTo(DirectInputKeyCode.DikQ), "the A key of an AZERTY keyboard");
            Assert.That(modifiers, Is.Empty);
            Assert.That(Parse("Digit1", out modifiers), Is.EqualTo(DirectInputKeyCode.Dik1));
            Assert.That(Parse("IntlBackslash", out modifiers), Is.EqualTo(DirectInputKeyCode.DikOem102), "<> key");
        }

        [Test]
        public void ModifierAlone_IsTheKey()
        {
            List<DirectInputKeyCode> modifiers;
            Assert.That(Parse("ControlRight", out modifiers), Is.EqualTo(DirectInputKeyCode.DikRcontrol));
            Assert.That(modifiers, Is.Empty);

            // AltGr of a French keyboard = ControlLeft + AltRight
            Assert.That(Parse("ControlLeft+AltRight", out modifiers), Is.EqualTo(DirectInputKeyCode.DikRmenu));
            Assert.That(modifiers, Is.EqualTo(new[] { DirectInputKeyCode.DikLcontrol }));
        }

        [Test]
        public void SpecialKeys()
        {
            List<DirectInputKeyCode> modifiers;
            Assert.That(Parse("ArrowUp", out modifiers), Is.EqualTo(DirectInputKeyCode.DikUp));
            Assert.That(Parse("Numpad7", out modifiers), Is.EqualTo(DirectInputKeyCode.DikNumpad7));
            Assert.That(Parse("NumpadEnter", out modifiers), Is.EqualTo(DirectInputKeyCode.DikNumpadenter));
            Assert.That(Parse("PageDown", out modifiers), Is.EqualTo(DirectInputKeyCode.DikNext));
            Assert.That(Parse("F24", out modifiers), Is.EqualTo(DirectInputKeyCode.DikF24));
            Assert.That(Parse(" ShiftRight + Space ", out modifiers), Is.EqualTo(DirectInputKeyCode.DikSpace));
        }

        [Test]
        public void EmptyOrUnknown_IsRefused()
        {
            List<DirectInputKeyCode> modifiers;
            DirectInputKeyCode key;
            Assert.That(Hotkey.TryParse("", out modifiers, out key), Is.False);
            Assert.That(Hotkey.TryParse(null, out modifiers, out key), Is.False);
            Assert.That(Hotkey.TryParse("+", out modifiers, out key), Is.False);
            Assert.That(Hotkey.TryParse("ControlLeft+Fn", out modifiers, out key), Is.False, "the whole shortcut or nothing");
            Assert.That(Hotkey.TryParse("Ctrl+F5", out modifiers, out key), Is.False, "codes, not labels");
        }

        [Test]
        public void EveryKeyThePageCanRecord_IsKnownByThePlugin()
        {
            var plugin = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\Elite\bin\Debug\com.mhwlng.elite.sdPlugin"));
            var script = File.ReadAllText(Path.Combine(plugin, "PropertyInspector", "generic.js"));

            // names listed in GENERIC_HOTKEY_MODIFIERS and GENERIC_HOTKEY_KEYS, plus the patterns of genericHotkeyKnown
            var tables = Regex.Matches(script, @"var GENERIC_HOTKEY_(MODIFIERS|KEYS) = \{(?<body>[^}]*)\}");
            Assert.That(tables.Count, Is.EqualTo(2));
            var codes = tables.Cast<Match>()
                .SelectMany(m => Regex.Matches(m.Groups["body"].Value, @"'(\w+)'\s*:").Cast<Match>().Select(c => c.Groups[1].Value))
                .ToList();
            Assert.That(codes.Count, Is.GreaterThan(50));

            for (char c = 'A'; c <= 'Z'; c++)
                codes.Add("Key" + c);
            for (int d = 0; d <= 9; d++)
            {
                codes.Add("Digit" + d);
                codes.Add("Numpad" + d);
            }
            for (int f = 1; f <= 24; f++)
                codes.Add("F" + f);

            Assert.That(codes.Where(c => !Hotkey.IsKnownCode(c)), Is.Empty);
        }
    }

    [TestFixture]
    public class CommandGuardTests
    {
        [Test]
        public void FireGroup_BlockedInTheStatesOfEliteKeys()
        {
            Assert.That(CommandGuard.FireGroupBlockReason(false, false, false, false, false, false), Is.Null);
            Assert.That(CommandGuard.FireGroupBlockReason(false, false, true, false, false, false), Is.EqualTo("docked"));
            Assert.That(CommandGuard.FireGroupBlockReason(false, false, false, false, true, false), Is.EqualTo("landing gear down"));
            Assert.That(CommandGuard.FireGroupBlockReason(true, false, false, false, false, false), Is.EqualTo("on foot"));
            Assert.That(CommandGuard.FireGroupBlockReason(false, true, false, false, false, false), Is.EqualTo("in the SRV"));
            Assert.That(CommandGuard.FireGroupBlockReason(false, false, false, true, false, false), Is.EqualTo("landed"));
            Assert.That(CommandGuard.FireGroupBlockReason(false, false, false, false, false, true), Is.EqualTo("FSD jump"));
        }

        [Test]
        public void OnlyFireGroupCommands_AreChecked()
        {
            var docked = new EliteData.Status { Docked = true };
            Assert.That(CommandGuard.BlockReason("FireGroup-B", docked), Is.EqualTo("docked"));
            Assert.That(CommandGuard.BlockReason("FireGroup-B", new EliteData.Status()), Is.Null, "in flight");
            Assert.That(CommandGuard.BlockReason("LandingGearToggle", docked), Is.Null);
            Assert.That(CommandGuard.BlockReason("CycleFireGroupNext", docked), Is.Null, "sent as is by EliteKeys");
            Assert.That(CommandGuard.BlockReason(null, docked), Is.Null);
        }
    }
}
