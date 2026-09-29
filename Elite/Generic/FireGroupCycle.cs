using System;

namespace Elite.Generic
{
    public enum FireGroupOutcome
    {
        /// <summary>The requested group is active.</summary>
        Reached,
        /// <summary>The requested group was already active: nothing sent.</summary>
        AlreadyActive,
        /// <summary>Back to the starting group without meeting the requested one: it does not exist.</summary>
        NotFound,
        /// <summary>A press did not change the group of status.json (key not working, no wrap after the last group...).</summary>
        NoResponse,
        /// <summary>Neither "next" nor "previous" has a key in the game.</summary>
        NoBinding
    }

    public sealed class FireGroupResult
    {
        public FireGroupResult(FireGroupOutcome outcome, int presses, int lastGroup)
        {
            Outcome = outcome;
            Presses = presses;
            LastGroup = lastGroup;
        }

        public FireGroupOutcome Outcome { get; }
        public int Presses { get; }
        public int LastGroup { get; }
    }

    /// <summary>
    /// Closed loop of the fire group commands (v3.8, D10): one press, then wait until status.json shows another group,
    /// until the requested group is active. The game cannot select a group directly, and a blind count of presses fails
    /// when "previous" has no key (SRV of D10) or when the group does not exist. Pure: the game is injected (tests).
    /// </summary>
    public static class FireGroupCycle
    {
        public const int MaxPresses = FireGroupSender.Groups;

        /// <param name="target">requested group, 0 = A</param>
        /// <param name="current">group of status.json</param>
        /// <param name="nextBound">"next fire group" has a key</param>
        /// <param name="previousBound">"previous fire group" has a key</param>
        /// <param name="press">sends one press: true = next, false = previous</param>
        /// <param name="waitForChange">group shown after a press (argument: group before it), null if it did not change in time</param>
        public static FireGroupResult Run(int target, Func<int> current, bool nextBound, bool previousBound,
            Action<bool> press, Func<int, int?> waitForChange)
        {
            int start = current();
            if (start == target)
                return new FireGroupResult(FireGroupOutcome.AlreadyActive, 0, start);
            if (!nextBound && !previousBound)
                return new FireGroupResult(FireGroupOutcome.NoBinding, 0, start);

            // "previous" only when it goes the short way and has a key, or when "next" has none
            bool next = !nextBound ? false : !(target < start && previousBound);

            int group = start;
            for (int presses = 1; presses <= MaxPresses; presses++)
            {
                press(next);
                int? shown = waitForChange(group);
                if (!shown.HasValue)
                    return new FireGroupResult(FireGroupOutcome.NoResponse, presses, group);

                group = shown.Value;
                if (group == target)
                    return new FireGroupResult(FireGroupOutcome.Reached, presses, group);
                if (group == start)
                    return new FireGroupResult(FireGroupOutcome.NotFound, presses, group);
            }

            return new FireGroupResult(FireGroupOutcome.NotFound, MaxPresses, group);
        }
    }
}
