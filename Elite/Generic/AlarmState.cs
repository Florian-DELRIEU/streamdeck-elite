using System;

namespace Elite.Generic
{
    /// <summary>
    /// Alert state of an Alarm key (docs/L8-alarme.md). Pure: the time is given by the caller.
    /// A new trigger during the alert restarts the duration; a duration of 0 lasts until Acknowledge.
    /// </summary>
    public class AlarmState
    {
        private DateTime? activeUntil;

        /// <summary>Starts (or restarts) the alert at now, for durationSeconds (0 or less = until acknowledged).</summary>
        public void Trigger(DateTime now, double durationSeconds)
        {
            activeUntil = durationSeconds > 0 ? now.AddSeconds(durationSeconds) : DateTime.MaxValue;
        }

        public bool IsActive(DateTime now)
        {
            return activeUntil.HasValue && now < activeUntil.Value;
        }

        /// <summary>Ends the alert; true when it was still active at now.</summary>
        public bool Acknowledge(DateTime now)
        {
            bool wasActive = IsActive(now);
            activeUntil = null;
            return wasActive;
        }
    }
}
