using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Elite.Buttons;
using WindowsInput;
using WindowsInput.Native;

namespace Elite.Generic
{
    /// <summary>
    /// Free keyboard shortcut of a view of the "Donnee" action (docs/L7-retours-d6.md). The property inspector records
    /// the physical keys (KeyboardEvent.code, e.g. "ControlLeft+ShiftLeft+F5": independent of the AZERTY / QWERTY layout)
    /// and the plugin sends them as DirectInput scan codes, with the engine of the game commands (CommandTools.HandleMacro).
    /// </summary>
    public static class Hotkey
    {
        public const char Separator = '+';

        private const int KeyDelayMilliseconds = 40;
        private const int WaitForCommandMilliseconds = 1000;

        private static readonly Dictionary<string, DirectInputKeyCode> Codes = BuildCodes();

        private static readonly HashSet<DirectInputKeyCode> ModifierKeys = new HashSet<DirectInputKeyCode>
        {
            DirectInputKeyCode.DikLcontrol, DirectInputKeyCode.DikRcontrol,
            DirectInputKeyCode.DikLshift, DirectInputKeyCode.DikRshift,
            DirectInputKeyCode.DikLmenu, DirectInputKeyCode.DikRmenu,
            DirectInputKeyCode.DikLwin, DirectInputKeyCode.DikRwin,
        };

        /// <summary>
        /// "ControlLeft+ShiftLeft+F5" -> modifiers [Lcontrol, Lshift], key F5. The last key is the one pressed, the others
        /// are held (a modifier alone is a key). False for an empty text or an unknown key code.
        /// </summary>
        public static bool TryParse(string text, out List<DirectInputKeyCode> modifiers, out DirectInputKeyCode key)
        {
            modifiers = new List<DirectInputKeyCode>();
            key = default(DirectInputKeyCode);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var keys = new List<DirectInputKeyCode>();
            foreach (var part in text.Split(Separator).Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                DirectInputKeyCode code;
                if (!Codes.TryGetValue(part, out code))
                    return false;
                if (!keys.Contains(code))
                    keys.Add(code);
            }

            if (keys.Count == 0)
                return false;

            // the modifiers are held, whatever the order of the recording
            var others = keys.Where(k => !ModifierKeys.Contains(k)).ToList();
            key = others.Count > 0 ? others.Last() : keys.Last();
            var pressed = key;
            modifiers = keys.Where(k => k != pressed).ToList();
            return true;
        }

        public static bool IsKnownCode(string code)
        {
            return code != null && Codes.ContainsKey(code);
        }

        /// <summary>
        /// Sends the shortcut on a background task, after the keystrokes of a command sent by the same press.
        /// </summary>
        public static void Send(string text, string label)
        {
            List<DirectInputKeyCode> modifiers;
            DirectInputKeyCode key;
            if (!TryParse(text, out modifiers, out key))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Hotkey: '{text}' ignored (unknown key)");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    // the command of EliteKeys is typed on its own task (StreamDeckCommon.SendInput)
                    var waited = Stopwatch.StartNew();
                    while (StreamDeckCommon.InputRunning && waited.ElapsedMilliseconds < WaitForCommandMilliseconds)
                        Thread.Sleep(20);

                    var keyboard = new InputSimulator().Keyboard;
                    if (modifiers.Count > 0)
                        keyboard.DelayedModifiedKeyStroke(modifiers, key, KeyDelayMilliseconds);
                    else
                        keyboard.DelayedKeyPress(key, KeyDelayMilliseconds);

                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Hotkey: {label} ({text}) sent");
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Hotkey: '{text}' failed: {ex}");
                }
            });
        }

        // KeyboardEvent.code (https://www.w3.org/TR/uievents-code/) -> DirectInput scan code
        private static Dictionary<string, DirectInputKeyCode> BuildCodes()
        {
            var codes = new Dictionary<string, DirectInputKeyCode>(StringComparer.OrdinalIgnoreCase);

            for (char c = 'A'; c <= 'Z'; c++)
                codes["Key" + c] = (DirectInputKeyCode)Enum.Parse(typeof(DirectInputKeyCode), "Dik" + c);
            for (int d = 0; d <= 9; d++)
            {
                codes["Digit" + d] = (DirectInputKeyCode)Enum.Parse(typeof(DirectInputKeyCode), "Dik" + d);
                codes["Numpad" + d] = (DirectInputKeyCode)Enum.Parse(typeof(DirectInputKeyCode), "DikNumpad" + d);
            }
            for (int f = 1; f <= 24; f++)
                codes["F" + f] = (DirectInputKeyCode)Enum.Parse(typeof(DirectInputKeyCode), "DikF" + f);

            var named = new Dictionary<string, DirectInputKeyCode>
            {
                { "Escape", DirectInputKeyCode.DikEscape },
                { "Tab", DirectInputKeyCode.DikTab },
                { "CapsLock", DirectInputKeyCode.DikCapital },
                { "Space", DirectInputKeyCode.DikSpace },
                { "Enter", DirectInputKeyCode.DikReturn },
                { "Backspace", DirectInputKeyCode.DikBack },
                { "Minus", DirectInputKeyCode.DikMinus },
                { "Equal", DirectInputKeyCode.DikEquals },
                { "BracketLeft", DirectInputKeyCode.DikLbracket },
                { "BracketRight", DirectInputKeyCode.DikRbracket },
                { "Backslash", DirectInputKeyCode.DikBackslash },
                { "Semicolon", DirectInputKeyCode.DikSemicolon },
                { "Quote", DirectInputKeyCode.DikApostrophe },
                { "Backquote", DirectInputKeyCode.DikGrave },
                { "Comma", DirectInputKeyCode.DikComma },
                { "Period", DirectInputKeyCode.DikPeriod },
                { "Slash", DirectInputKeyCode.DikSlash },
                { "IntlBackslash", DirectInputKeyCode.DikOem102 }, // "<>" key of the AZERTY keyboards
                { "ArrowUp", DirectInputKeyCode.DikUp },
                { "ArrowDown", DirectInputKeyCode.DikDown },
                { "ArrowLeft", DirectInputKeyCode.DikLeft },
                { "ArrowRight", DirectInputKeyCode.DikRight },
                { "Insert", DirectInputKeyCode.DikInsert },
                { "Delete", DirectInputKeyCode.DikDelete },
                { "Home", DirectInputKeyCode.DikHome },
                { "End", DirectInputKeyCode.DikEnd },
                { "PageUp", DirectInputKeyCode.DikPrior },
                { "PageDown", DirectInputKeyCode.DikNext },
                { "PrintScreen", DirectInputKeyCode.DikSysrq },
                { "ScrollLock", DirectInputKeyCode.DikScroll },
                { "Pause", DirectInputKeyCode.DikPause },
                { "NumLock", DirectInputKeyCode.DikNumlock },
                { "NumpadAdd", DirectInputKeyCode.DikAdd },
                { "NumpadSubtract", DirectInputKeyCode.DikSubtract },
                { "NumpadMultiply", DirectInputKeyCode.DikMultiply },
                { "NumpadDivide", DirectInputKeyCode.DikDivide },
                { "NumpadDecimal", DirectInputKeyCode.DikDecimal },
                { "NumpadEnter", DirectInputKeyCode.DikNumpadenter },
                { "NumpadEqual", DirectInputKeyCode.DikNumpadequals },
                { "NumpadComma", DirectInputKeyCode.DikNumpadcomma },
                { "ControlLeft", DirectInputKeyCode.DikLcontrol },
                { "ControlRight", DirectInputKeyCode.DikRcontrol },
                { "ShiftLeft", DirectInputKeyCode.DikLshift },
                { "ShiftRight", DirectInputKeyCode.DikRshift },
                { "AltLeft", DirectInputKeyCode.DikLmenu },
                { "AltRight", DirectInputKeyCode.DikRmenu },
                { "MetaLeft", DirectInputKeyCode.DikLwin },
                { "MetaRight", DirectInputKeyCode.DikRwin },
                { "ContextMenu", DirectInputKeyCode.DikApps },
                { "AudioVolumeMute", DirectInputKeyCode.DikMute },
                { "AudioVolumeDown", DirectInputKeyCode.DikVolumedown },
                { "AudioVolumeUp", DirectInputKeyCode.DikVolumeup },
                { "MediaPlayPause", DirectInputKeyCode.DikPlaypause },
                { "MediaStop", DirectInputKeyCode.DikMediastop },
                { "MediaTrackNext", DirectInputKeyCode.DikNexttrack },
                { "MediaTrackPrevious", DirectInputKeyCode.DikPrevtrack },
            };
            foreach (var pair in named)
                codes[pair.Key] = pair.Value;

            return codes;
        }
    }
}
