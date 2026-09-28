using System;
using BarRaider.SdTools;
using Elite.Buttons;

namespace Elite.Generic
{
    /// <summary>
    /// Command and free shortcut of a press on a Data, Graph or Alarm key: the fire groups are sent by FireGroupSender
    /// (without the blocking of EliteKeys), the pages of the ZV Elite profile by ProfilePages (v3.7), the other commands
    /// by EliteKeys.SendKeypress, then the shortcut. connection = the SDConnection of the key.
    /// </summary>
    public static class KeyCommands
    {
        public static void Send(object connection, string command, string hotkey, string hotkeyText, string owner)
        {
            try
            {
                int group, page;
                if (FireGroupSender.TryParse(command, out group))
                    FireGroupSender.Send(group, owner);
                else if (ProfilePages.TryParse(command, out page))
                    ProfilePages.Send(connection, page, owner);
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
