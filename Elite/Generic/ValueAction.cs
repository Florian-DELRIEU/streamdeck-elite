using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Elite.Buttons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Value action (cahier des charges §5.1): shows the value of one EliteStore key as the key title.
    /// Redrawn only when its key changes and only when the text changes (§4.5). Pressing the key does nothing.
    /// </summary>
    [PluginActionId("com.mhwlng.elite.value")]
    public class ValueAction : EliteKeypadBase
    {
        protected class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                return new PluginSettings
                {
                    Source = string.Empty,
                    Prefix = string.Empty,
                    Suffix = string.Empty,
                    Decimals = "0",
                    Scale = "1",
                    Offset = "0",
                    Compact = false,
                    EmptyText = ValueSettings.DefaultEmptyText,
                    BackgroundImage = string.Empty,
                };
            }

            [JsonProperty(PropertyName = "source")]
            public string Source { get; set; }

            [JsonProperty(PropertyName = "prefix")]
            public string Prefix { get; set; }

            [JsonProperty(PropertyName = "suffix")]
            public string Suffix { get; set; }

            [JsonProperty(PropertyName = "decimals")]
            public string Decimals { get; set; }

            [JsonProperty(PropertyName = "scale")]
            public string Scale { get; set; }

            [JsonProperty(PropertyName = "offset")]
            public string Offset { get; set; }

            [JsonProperty(PropertyName = "compact")]
            public bool Compact { get; set; }

            [JsonProperty(PropertyName = "emptyText")]
            public string EmptyText { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage")]
            public string BackgroundImage { get; set; }
        }

        private readonly object renderLock = new object();
        private PluginSettings settings;
        private ValueSettings display;
        private string lastTitle;
        private string lastImage;
        private bool disposed;

        public ValueAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            try
            {
                if (payload.Settings == null || payload.Settings.Count == 0)
                {
                    settings = PluginSettings.CreateDefaultSettings();
                    Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();
                }
                else
                {
                    settings = payload.Settings.ToObject<PluginSettings>();
                }

                ApplySettings();
                EliteStore.DataChanged += OnDataChanged;
                UpdateImage();
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction: constructor failed: {ex}");
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            // display only (§5.1)
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            try
            {
                BarRaider.SdTools.Tools.AutoPopulateSettings(settings, payload.Settings);
                ApplySettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();
                UpdateImage();
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction: settings update failed: {ex}");
            }
        }

        public override void Dispose()
        {
            lock (renderLock)
                disposed = true;

            EliteStore.DataChanged -= OnDataChanged;
            base.Dispose();
        }

        // background thread (status or journal watcher)
        private void OnDataChanged(IReadOnlyCollection<string> changedKeys)
        {
            try
            {
                var source = settings.Source;
                if (!string.IsNullOrEmpty(source) && changedKeys.Contains(source, StoreKeys.Comparer))
                    Render(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction: refresh failed for {settings.Source}: {ex}");
            }
        }

        private void ApplySettings()
        {
            display = ValueSettings.Parse(settings.Prefix, settings.Suffix, settings.Decimals, settings.Scale, settings.Offset,
                settings.Compact, settings.EmptyText,
                warning => Logger.Instance.LogMessage(TracingLevel.WARN, $"ValueAction[{settings.Source}]: {warning}"));
        }

        /// <summary>
        /// Sends the title only when it changed (force: after creation or a settings change).
        /// The title is sent without waiting, so that the watcher threads are never blocked.
        /// </summary>
        private void Render(bool force)
        {
            lock (renderLock)
            {
                if (disposed)
                    return;

                JToken value = null;
                if (!string.IsNullOrEmpty(settings.Source))
                    EliteStore.TryGet(settings.Source, out value);

                var title = ValueFormatter.Format(value, display, DateTime.Now);
                if (!force && title == lastTitle)
                    return;

                lastTitle = title;
                Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Value[{settings.Source}] = {title.Replace("\n", "\\n")}");
                Watch(Connection.SetTitleAsync(title), "SetTitle");
            }
        }

        private void UpdateImage()
        {
            var path = settings.BackgroundImage;
            if (path == lastImage)
                return;

            lastImage = path;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                Watch(Connection.SetImageAsync(Tools.FileToBase64(path, true)), "SetImage");
            else
                Watch(Connection.SetDefaultImageAsync(), "SetDefaultImage");
        }

        private void Watch(Task task, string what)
        {
            task.ContinueWith(t => Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction[{settings.Source}]: {what} failed: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
