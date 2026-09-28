using System;
using System.Threading;
using BarRaider.SdTools;
using Elite.Buttons;

namespace Elite.Generic
{
    /// <summary>
    /// "FireGroup-A"..."FireGroup-H" of the Data and Alarm keys (docs/L10-v3.md, issue #3): the same cycle as
    /// EliteKeys.HandleFireGroup (target - current fire group of status.json, 70 ms between two presses), without its
    /// blocking conditions (docked, landing gear down, SRV...). In the SRV, the fire group keys of the SRV are used.
    /// EliteKeys and the historic Firegroup action are not modified.
    /// </summary>
    public static class FireGroupSender
    {
        public const string Prefix = "FireGroup-";
        public const int Groups = 8;
        private const int DelayMilliseconds = 70;

        /// <summary>"FireGroup-A" -> 0 ... "FireGroup-H" -> 7.</summary>
        public static bool TryParse(string command, out int group)
        {
            group = -1;
            if (command == null || !command.StartsWith(Prefix, StringComparison.Ordinal) || command.Length != Prefix.Length + 1)
                return false;

            group = command[Prefix.Length] - 'A';
            return group >= 0 && group < Groups;
        }

        /// <summary>Number of presses: positive = next fire group, negative = previous (as EliteKeys).</summary>
        public static int Presses(int target, int current)
        {
            return target - current;
        }

        public static void Send(int target, string owner)
        {
            // same guard as EliteKeys.SendKeypress: a macro still being typed is stopped
            if (StreamDeckCommon.InputRunning || Program.Binding == null)
            {
                StreamDeckCommon.ForceStop = true;
                return;
            }

            var status = EliteData.StatusData;
            int presses = Presses(target, status.Firegroup);
            if (presses == 0)
                return;

            StandardBindingInfo binding;
            if (status.InSRV)
            {
                UserBindings srv;
                Program.Binding.TryGetValue(BindingType.Srv, out srv);
                binding = srv == null ? null : presses > 0 ? srv.BuggyCycleFireGroupNext : srv.BuggyCycleFireGroupPrevious;
            }
            else
            {
                UserBindings ship;
                Program.Binding.TryGetValue(BindingType.Ship, out ship);
                binding = ship == null ? null : presses > 0 ? ship.CycleFireGroupNext : ship.CycleFireGroupPrevious;
            }

            if (binding == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{owner}: {Prefix}{(char)('A' + target)} ignored (no {(status.InSRV ? "SRV" : "ship")} fire group binding)");
                return;
            }

            for (int i = 0; i < Math.Abs(presses); i++)
            {
                StreamDeckCommon.SendKeypress(binding);
                Thread.Sleep(DelayMilliseconds);
            }
        }
    }
}
