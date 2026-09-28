using System;
using BarRaider.SdTools;
using Elite.Buttons;

namespace Elite.Generic
{
    /// <summary>
    /// Command and free shortcut of a press on a Data, Graph or Alarm key: the fire groups are sent by FireGroupSender
    /// (without the blocking of EliteKeys), the other commands by EliteKeys.SendKeypress, then the shortcut.
    /// </summary>
    public static class KeyCommands
    {
        public static void Send(string command, string hotkey, string hotkeyText, string owner)
        {
            try
            {
                int group;
                if (FireGroupSender.TryParse(command, out group))
                    FireGroupSender.Send(group, owner);
                else if (!string.IsNullOrEmpty(command))
                    EliteKeys.SendKeypress(command);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{owner}: command {command} failed: {ex}");
            }

            if (!string.IsNullOrEmpty(hotkey))
                Hotkey.Send(hotkey, hotkeyText);
        }
    }
}
