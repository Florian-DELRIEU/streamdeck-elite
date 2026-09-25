using System;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Elite.Buttons;
using EliteJournalReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// "Alarme" action (com.mhwlng.elite.eventalarm, cahier des charges §5.3, docs/L8-alarme.md): shows its alert image
    /// and plays its alert sound when a journal event of the chosen type (optionally filtered on one of its fields) is
    /// written live. Events replayed at startup never trigger it (IsLive guard). The alert ends after its duration, or at
    /// a key press when the duration is 0. A press acknowledges the alert, then sends the command, the shortcut and the
    /// click sound. A key that is not displayed (other page, closed folder) does not exist: it misses the events.
    /// </summary>
    [PluginActionId("com.mhwlng.elite.eventalarm")]
    public class EventAlarmAction : EliteKeypadBase
    {
        // property names = JSON names of EventAlarm.html; defaults apply to keys created before the setting existed
        protected class PluginSettings
        {
            [JsonProperty(PropertyName = "event")] public string Event { get; set; } = "";
            [JsonProperty(PropertyName = "filterField")] public string FilterField { get; set; } = "";
            [JsonProperty(PropertyName = "filterOp")] public string FilterOp { get; set; } = "";
            [JsonProperty(PropertyName = "filterValue")] public string FilterValue { get; set; } = "";
            [JsonProperty(PropertyName = "duration")] public string Duration { get; set; } = "5";
            [FilenameProperty]
            [JsonProperty(PropertyName = "idleImage")] public string IdleImage { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "activeImage")] public string ActiveImage { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "alarmSound")] public string AlarmSound { get; set; } = "";
            [JsonProperty(PropertyName = "pressCommand")] public string PressCommand { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkey")] public string PressHotkey { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkeyText")] public string PressHotkeyText { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "clickSound")] public string ClickSound { get; set; } = "";
        }

        private readonly object sync = new object();
        private readonly KeyMedia media = new KeyMedia("EventAlarm");
        private readonly AlarmState state = new AlarmState();
        private PluginSettings settings = new PluginSettings();
        private AlarmConfig config = AlarmConfig.FromSettings(null);
        private string lastImage;
        private bool lastActive;
        private bool imageSent;
        private bool disposed;

        public EventAlarmAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            try
            {
                var raw = payload.Settings ?? new JObject();
                settings = raw.Count == 0 ? new PluginSettings() : raw.ToObject<PluginSettings>();

                // saves the defaults of the settings added since the key was created
                Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();

                ApplySettings();
                EliteStore.JournalEventReceived += OnJournalEvent;
                Connection.OnSendToPlugin += OnSendToPlugin;
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm: constructor failed: {ex}");
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            try
            {
                AlarmConfig current;
                bool acknowledged;
                lock (sync)
                {
                    current = config;
                    acknowledged = state.Acknowledge(DateTime.UtcNow);
                }

                // the alert is acknowledged before the command is sent (§5.3)
                if (acknowledged)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"EventAlarm[{current.Event}]: acknowledged");
                    Render(false);
                }

                if (current.Command.Length > 0)
                {
                    var blocked = CommandGuard.BlockReason(current.Command, EliteData.StatusData);
                    if (blocked != null)
                    {
                        // EliteKeys ignores it silently: the Stream Deck warning triangle says so (as on a Data key)
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"EventAlarm[{current.Event}]: {current.Command} ignored ({blocked})");
                        Watch(Connection.ShowAlert(), "ShowAlert");
                    }
                    else
                        EliteKeys.SendKeypress(current.Command);
                }

                if (current.Hotkey.Length > 0)
                    Hotkey.Send(current.Hotkey, current.HotkeyText);

                media.Play(current.ClickSound);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm[{config?.Event}]: key press failed: {ex}");
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            try
            {
                BarRaider.SdTools.Tools.AutoPopulateSettings(settings, payload.Settings);
                ApplySettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm: settings update failed: {ex}");
            }
        }

        /// <summary>
        /// About every second: the alert ends when its duration is over (redrawn only when the alert state changed).
        /// </summary>
        public override void OnTick()
        {
            base.OnTick();
            try
            {
                bool changed;
                lock (sync)
                    changed = imageSent && state.IsActive(DateTime.UtcNow) != lastActive;
                if (changed)
                    Render(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm[{config?.Event}]: tick failed: {ex}");
            }
        }

        public override void Dispose()
        {
            lock (sync)
                disposed = true;

            EliteStore.JournalEventReceived -= OnJournalEvent;
            Connection.OnSendToPlugin -= OnSendToPlugin;
            base.Dispose();
        }

        // background thread (journal watcher), for every journal line, replayed (IsLive false) or live
        private void OnJournalEvent(object sender, RawJournalEventArgs e)
        {
            try
            {
                AlarmConfig current;
                lock (sync)
                {
                    if (disposed || !config.ShouldTrigger(e))
                        return;
                    current = config;
                    state.Trigger(DateTime.UtcNow, current.DurationSeconds);
                }

                Logger.Instance.LogMessage(TracingLevel.INFO, $"EventAlarm[{current.Event}]: triggered");
                Alert(current);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm[{config?.Event}]: event handling failed: {ex}");
            }
        }

        /// <summary>
        /// Property inspector: { alarmTest: true } triggers the alert as a live event would ("Tester l'alarme" button);
        /// { genericValueRequest: key } asks for the current value of a key (the "i" panel: last event received).
        /// </summary>
        private void OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            try
            {
                var payload = e?.Event?.Payload;
                if (payload == null)
                    return;

                if (payload["alarmTest"] != null)
                {
                    AlarmConfig current;
                    lock (sync)
                    {
                        if (disposed)
                            return;
                        current = config;
                        state.Trigger(DateTime.UtcNow, current.DurationSeconds);
                    }

                    Logger.Instance.LogMessage(TracingLevel.INFO, $"EventAlarm[{current.Event}]: test from the property inspector");
                    Alert(current);
                    return;
                }

                var key = payload["genericValueRequest"];
                if (key != null)
                {
                    JToken value;
                    EliteStore.TryGet((string)key, out value);
                    var reply = ValueFormatter.DescribeCurrentValue((string)key, value, new ValueSettings(), DateTime.Now);
                    Watch(Connection.SendToPropertyInspectorAsync(reply), "SendToPropertyInspector");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm: property inspector request failed: {ex}");
            }
        }

        private void Alert(AlarmConfig current)
        {
            Render(false);

            // without an alert image the key would not change: the Stream Deck warning triangle shows the alert
            if (KeyMedia.ExistingFile(current.ActiveImage) == null)
                Watch(Connection.ShowAlert(), "ShowAlert");

            media.Play(current.AlarmSound);
        }

        private void ApplySettings()
        {
            var newConfig = AlarmConfig.FromSettings(JObject.FromObject(settings),
                warning => Logger.Instance.LogMessage(TracingLevel.WARN, $"EventAlarm[{settings.Event}]: {warning}"));

            lock (sync)
                config = newConfig;
        }

        /// <summary>
        /// Sends the image only when it changed (force: after creation or a settings change): alert image while the
        /// alert is active, idle image otherwise; the default image of the action when the file is missing.
        /// Nothing is awaited, so that the watcher threads are never blocked.
        /// </summary>
        private void Render(bool force)
        {
            lock (sync)
            {
                if (disposed)
                    return;

                bool active = state.IsActive(DateTime.UtcNow);
                var image = KeyMedia.ExistingFile(active ? config.ActiveImage : config.IdleImage);
                lastActive = active;
                if (!force && imageSent && image == lastImage)
                    return;

                lastImage = image;
                imageSent = true;
                var base64 = image == null ? null : media.ImageBase64(image);
                if (base64 != null)
                    Watch(Connection.SetImageAsync(base64), "SetImage");
                else
                    Watch(Connection.SetDefaultImageAsync(), "SetDefaultImage");
            }
        }

        private void Watch(Task task, string what)
        {
            task.ContinueWith(t => Logger.Instance.LogMessage(TracingLevel.ERROR, $"EventAlarm[{config?.Event}]: {what} failed: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
