namespace Elite.Generic
{
    public enum PressMode
    {
        /// <summary>Short press: act (command of the displayed view). Long press: next view. Default.</summary>
        ShortActLongView,
        /// <summary>Short press: next view. Long press: act.</summary>
        ShortViewLongAct,
    }

    public enum PressOutcome
    {
        Act,
        NextView,
    }

    /// <summary>
    /// Key press gestures of a Data key ("drawer", docs/L5-tiroir.md). Pure functions.
    /// </summary>
    public static class PressGesture
    {
        /// <summary>A press held at least this long is a long press (the long action fires without waiting for the release).</summary>
        public const int LongPressMilliseconds = 500;

        public const string ShortActLongViewName = "shortActLongView";
        public const string ShortViewLongActName = "shortViewLongAct";

        public static PressMode ParseMode(string text)
        {
            return (text ?? "").Trim() == ShortViewLongActName ? PressMode.ShortViewLongAct : PressMode.ShortActLongView;
        }

        /// <summary>
        /// With a single view, both gestures act.
        /// </summary>
        public static PressOutcome Resolve(bool isLongPress, PressMode mode, int viewCount)
        {
            bool nextView = mode == PressMode.ShortActLongView ? isLongPress : !isLongPress;
            return nextView && viewCount > 1 ? PressOutcome.NextView : PressOutcome.Act;
        }
    }
}
