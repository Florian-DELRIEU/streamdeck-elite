using System;
using System.Reflection;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Pages of the "ZV Elite" profile delivered with the plugin, instead of folders (v3.7, issue #1, trial,
    /// docs/L10-v3.md). The Stream Deck SDK cannot open a folder, nor a profile made by the user: a plugin can only
    /// switch to a profile it declares in manifest.json (Profiles), at a given page. Commands:
    /// ZV-Page-1 ... ZV-Page-5 (page of the ZV Elite profile) and ZV-ProfileBack (back to the previous profile).
    /// The page is not supported by the C# library: the switchToProfile message is sent as raw JSON; without it, the
    /// profile is opened without page (WARN).
    /// </summary>
    public static class ProfilePages
    {
        public const string ProfileName = "ZV Elite";
        public const string PagePrefix = "ZV-Page-";
        public const string Back = "ZV-ProfileBack";
        public const int Pages = 5;

        /// <summary>ZV-Page-n: page 1 to Pages; ZV-ProfileBack: page 0 (previous profile).</summary>
        public static bool TryParse(string command, out int page)
        {
            page = -1;
            if (string.Equals(command, Back, StringComparison.Ordinal))
            {
                page = 0;
                return true;
            }

            return command != null && command.StartsWith(PagePrefix, StringComparison.Ordinal)
                && int.TryParse(command.Substring(PagePrefix.Length), out page) && page >= 1 && page <= Pages;
        }

        /// <summary>
        /// switchToProfile message of the SDK (page indexed from 0; without profile = previous profile).
        /// context = the plugin UUID of the registration, device = the Stream Deck of the key.
        /// </summary>
        public static JObject Message(string pluginUuid, string device, int page)
        {
            var payload = new JObject();
            if (page >= 1)
            {
                payload["profile"] = ProfileName;
                payload["page"] = page - 1;
            }

            return new JObject
            {
                { "event", "switchToProfile" },
                { "context", pluginUuid },
                { "device", device },
                { "payload", payload },
            };
        }

        public static void Send(object connection, int page, string owner)
        {
            try
            {
                string pluginUuid, device;
                object client;
                Func<string, Task> send = Sender(connection, out pluginUuid, out device, out client);
                if (send != null)
                {
                    var json = Message(pluginUuid, device, page).ToString(Formatting.None);
                    Watch(send(json), owner);
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{owner}: {(page >= 1 ? "page " + page + " of " + ProfileName : "previous profile")}");
                    return;
                }

                // without the raw message: the profile, at its current page (the SDK of the library has no page)
                Logger.Instance.LogMessage(TracingLevel.WARN, $"{owner}: raw switchToProfile unavailable, profile opened without page");
                if (page >= 1 && connection is SDConnection sd)
                    Watch(sd.SwitchProfileAsync(ProfileName), owner);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{owner}: profile switch failed: {ex}");
            }
        }

        // SDConnection (BarRaider) -> its StreamDeckConnection (streamdeck-client-csharp) and its private SendAsync(string)
        private static Func<string, Task> Sender(object connection, out string pluginUuid, out string device, out object client)
        {
            pluginUuid = null;
            device = null;
            client = null;
            if (connection == null)
                return null;

            var type = connection.GetType();
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            pluginUuid = type.GetField("pluginUUID", any)?.GetValue(connection) as string;
            device = type.GetProperty("DeviceId", any)?.GetValue(connection) as string;
            client = type.GetProperty("StreamDeckConnection", any)?.GetValue(connection);
            var method = client?.GetType().GetMethod("SendAsync", any, null, new[] { typeof(string) }, null);
            if (method == null || string.IsNullOrEmpty(pluginUuid) || string.IsNullOrEmpty(device))
                return null;

            var target = client;
            return text => method.Invoke(target, new object[] { text }) as Task ?? Task.FromResult(0);
        }

        private static void Watch(Task task, string owner)
        {
            task?.ContinueWith(t => Logger.Instance.LogMessage(TracingLevel.ERROR, $"{owner}: profile switch failed: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
