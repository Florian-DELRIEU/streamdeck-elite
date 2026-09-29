using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Elite.Buttons;

namespace Elite.Generic
{
    /// <summary>
    /// "FireGroup-A"..."FireGroup-H" of the Data, Graph and Alarm keys (docs/L10-v3.md, issue #3), without the blocking
    /// conditions of EliteKeys.HandleFireGroup (docked, landing gear down, SRV...). In the SRV, the fire group keys of
    /// the SRV are used. v3.8 (docs/L11-v3.8.md): closed loop (FireGroupCycle) instead of a blind count of presses, on
    /// a background task; a group that does not exist or a game that does not answer shows the alert of the key.
    /// EliteKeys and the historic Firegroup action are not modified.
    /// </summary>
    public static class FireGroupSender
    {
        public const string Prefix = "FireGroup-";
        public const int Groups = 8;
        private const int PollMilliseconds = 20;
        private const int ChangeTimeoutMilliseconds = 1000;
        private const int InputTimeoutMilliseconds = 1000;

        private static int busy;

        /// <summary>"FireGroup-A" -> 0 ... "FireGroup-H" -> 7.</summary>
        public static bool TryParse(string command, out int group)
        {
            group = -1;
            if (command == null || !command.StartsWith(Prefix, StringComparison.Ordinal) || command.Length != Prefix.Length + 1)
                return false;

            group = command[Prefix.Length] - 'A';
            return group >= 0 && group < Groups;
        }

        public static string Name(int group)
        {
            return Prefix + (char)('A' + group);
        }

        /// <summary>alert = triangle of the key (group not found, no answer from the game, no key in the game).</summary>
        public static void Send(int target, string owner, Action alert)
        {
            // same guard as EliteKeys.SendKeypress: a macro still being typed is stopped
            if (StreamDeckCommon.InputRunning || Program.Binding == null)
            {
                StreamDeckCommon.ForceStop = true;
                return;
            }

            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, $"{owner}: {Name(target)} ignored (fire group change in progress)");
                return;
            }

            bool srv = EliteData.StatusData.InSRV;
            string vehicle = srv ? "SRV" : "ship";
            UserBindings bindings;
            Program.Binding.TryGetValue(srv ? BindingType.Srv : BindingType.Ship, out bindings);
            var next = bindings == null ? null : srv ? bindings.BuggyCycleFireGroupNext : bindings.CycleFireGroupNext;
            var previous = bindings == null ? null : srv ? bindings.BuggyCycleFireGroupPrevious : bindings.CycleFireGroupPrevious;

            StreamDeckCommon.ForceStop = false;
            Task.Run(() =>
            {
                try
                {
                    var result = FireGroupCycle.Run(target, () => EliteData.StatusData.Firegroup, IsBound(next), IsBound(previous),
                        isNext => Press(isNext ? next : previous), WaitForChange);
                    Report(result, target, vehicle, owner, IsBound(previous), alert);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{owner}: {Name(target)} failed: {ex}");
                }
                finally
                {
                    Interlocked.Exchange(ref busy, 0);
                }
            });
        }

        private static bool IsBound(StandardBindingInfo binding)
        {
            return binding != null && !string.IsNullOrEmpty(CommandTools.BuildInputText(binding));
        }

        private static void Press(StandardBindingInfo binding)
        {
            StreamDeckCommon.SendKeypress(binding);

            // the keystroke is typed on its own task (StreamDeckCommon.SendInput)
            var waited = Stopwatch.StartNew();
            while (StreamDeckCommon.InputRunning && waited.ElapsedMilliseconds < InputTimeoutMilliseconds)
                Thread.Sleep(PollMilliseconds);
        }

        // group of status.json once it differs from 'before', null after the timeout
        private static int? WaitForChange(int before)
        {
            var waited = Stopwatch.StartNew();
            while (waited.ElapsedMilliseconds < ChangeTimeoutMilliseconds)
            {
                int now = EliteData.StatusData.Firegroup;
                if (now != before)
                    return now;
                Thread.Sleep(PollMilliseconds);
            }
            return null;
        }

        private static void Report(FireGroupResult result, int target, string vehicle, string owner, bool previousBound, Action alert)
        {
            string what = $"{owner}: {Name(target)} ({vehicle})";
            switch (result.Outcome)
            {
                case FireGroupOutcome.Reached:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"{what}: reached after {result.Presses} press(es)");
                    return;
                case FireGroupOutcome.AlreadyActive:
                    return;
                case FireGroupOutcome.NotFound:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"{what}: this fire group does not exist (back to group {(char)('A' + result.LastGroup)} after {result.Presses} press(es))");
                    break;
                case FireGroupOutcome.NoResponse:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"{what}: the game did not change the fire group after {result.Presses} press(es), still group {(char)('A' + result.LastGroup)}"
                        + (previousBound ? "" : " (no key for 'previous fire group' in the game: the last group may not wrap to A)"));
                    break;
                case FireGroupOutcome.NoBinding:
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"{what}: no key for the {vehicle} fire groups in the game");
                    break;
            }

            try
            {
                alert?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{owner}: alert failed: {ex}");
            }
        }
    }
}
