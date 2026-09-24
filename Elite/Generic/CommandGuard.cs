using System;

namespace Elite.Generic
{
    /// <summary>
    /// Says why EliteKeys will ignore a state-dependent command, so that the "Donnee" key shows an alert instead of
    /// silently doing nothing (docs/L7-retours-d6.md). The conditions are those of EliteKeys.HandleFireGroup, which is
    /// not modified.
    /// </summary>
    public static class CommandGuard
    {
        private const string FireGroupPrefix = "FireGroup-";

        /// <summary>
        /// Null when the command is sent, otherwise the reason (for the log).
        /// </summary>
        public static string BlockReason(string command, EliteData.Status status)
        {
            if (command == null || status == null || !command.StartsWith(FireGroupPrefix, StringComparison.Ordinal))
                return null;

            return FireGroupBlockReason(status.OnFoot, status.InSRV, status.Docked, status.Landed, status.LandingGearDown, status.FsdJump);
        }

        public static string FireGroupBlockReason(bool onFoot, bool inSrv, bool docked, bool landed, bool landingGearDown, bool fsdJump)
        {
            if (onFoot)
                return "on foot";
            if (inSrv)
                return "in the SRV";
            if (docked)
                return "docked";
            if (landed)
                return "landed";
            if (landingGearDown)
                return "landing gear down";
            if (fsdJump)
                return "FSD jump";
            return null;
        }
    }
}
