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
    /// "Donnée" action (UUID kept from L3: com.mhwlng.elite.value), see docs/L4-donnee.md:
    /// - text: the value of the displayed view (up to 4 views), optional;
    /// - image: first matching rule on the main value (view 1), otherwise the default image;
    /// - key press: next view and/or a keyboard command (EliteKeys.SendKeypress) and/or a sound.
    /// Redrawn only when one of its keys changes, and only what changed is sent (§4.5).
    /// </summary>
    [PluginActionId("com.mhwlng.elite.value")]
    public class ValueAction : EliteKeypadBase
    {
        // property names = JSON names of Generic.html; defaults apply to keys created in L3 (missing settings)
        protected class PluginSettings
        {
            [JsonProperty(PropertyName = "source")] public string Source { get; set; } = "";
            [JsonProperty(PropertyName = "prefix")] public string Prefix { get; set; } = "";
            [JsonProperty(PropertyName = "suffix")] public string Suffix { get; set; } = "";
            [JsonProperty(PropertyName = "decimals")] public string Decimals { get; set; } = "0";
            [JsonProperty(PropertyName = "scale")] public string Scale { get; set; } = "1";
            [JsonProperty(PropertyName = "offset")] public string Offset { get; set; } = "0";
            [JsonProperty(PropertyName = "compact")] public bool Compact { get; set; }

            [JsonProperty(PropertyName = "source2")] public string Source2 { get; set; } = "";
            [JsonProperty(PropertyName = "prefix2")] public string Prefix2 { get; set; } = "";
            [JsonProperty(PropertyName = "suffix2")] public string Suffix2 { get; set; } = "";
            [JsonProperty(PropertyName = "decimals2")] public string Decimals2 { get; set; } = "0";
            [JsonProperty(PropertyName = "scale2")] public string Scale2 { get; set; } = "1";
            [JsonProperty(PropertyName = "offset2")] public string Offset2 { get; set; } = "0";
            [JsonProperty(PropertyName = "compact2")] public bool Compact2 { get; set; }

            [JsonProperty(PropertyName = "source3")] public string Source3 { get; set; } = "";
            [JsonProperty(PropertyName = "prefix3")] public string Prefix3 { get; set; } = "";
            [JsonProperty(PropertyName = "suffix3")] public string Suffix3 { get; set; } = "";
            [JsonProperty(PropertyName = "decimals3")] public string Decimals3 { get; set; } = "0";
            [JsonProperty(PropertyName = "scale3")] public string Scale3 { get; set; } = "1";
            [JsonProperty(PropertyName = "offset3")] public string Offset3 { get; set; } = "0";
            [JsonProperty(PropertyName = "compact3")] public bool Compact3 { get; set; }

            [JsonProperty(PropertyName = "source4")] public string Source4 { get; set; } = "";
            [JsonProperty(PropertyName = "prefix4")] public string Prefix4 { get; set; } = "";
            [JsonProperty(PropertyName = "suffix4")] public string Suffix4 { get; set; } = "";
            [JsonProperty(PropertyName = "decimals4")] public string Decimals4 { get; set; } = "0";
            [JsonProperty(PropertyName = "scale4")] public string Scale4 { get; set; } = "1";
            [JsonProperty(PropertyName = "offset4")] public string Offset4 { get; set; } = "0";
            [JsonProperty(PropertyName = "compact4")] public bool Compact4 { get; set; }

            [JsonProperty(PropertyName = "emptyText")] public string EmptyText { get; set; } = ValueSettings.DefaultEmptyText;
            [JsonProperty(PropertyName = "showText")] public bool ShowText { get; set; } = true;

            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage")] public string BackgroundImage { get; set; } = "";

            [JsonProperty(PropertyName = "rule1Op")] public string Rule1Op { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Value")] public string Rule1Value { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule1Image")] public string Rule1Image { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Op")] public string Rule2Op { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Value")] public string Rule2Value { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule2Image")] public string Rule2Image { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Op")] public string Rule3Op { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Value")] public string Rule3Value { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule3Image")] public string Rule3Image { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Op")] public string Rule4Op { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Value")] public string Rule4Value { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule4Image")] public string Rule4Image { get; set; } = "";

            [JsonProperty(PropertyName = "pressCycle")] public bool PressCycle { get; set; }
            [JsonProperty(PropertyName = "pressCommand")] public string PressCommand { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "clickSound")] public string ClickSound { get; set; } = "";
        }

        private const string NoTitle = "\u0000no title";

        private readonly object renderLock = new object();
        private readonly Dictionary<string, string> imageCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private PluginSettings settings;
        private DataKeyConfig config;
        private List<ImageRule> rules = new List<ImageRule>();
        private int viewIndex;
        private string lastTitle;
        private string lastImage;
        private bool imageSent;
        private string soundPath;
        private CachedSound sound;
        private bool disposed;

        public ValueAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            try
            {
                settings = payload.Settings == null || payload.Settings.Count == 0
                    ? new PluginSettings()
                    : payload.Settings.ToObject<PluginSettings>();

                // saves the defaults of the settings added since the key was created (L3 keys)
                Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();

                ApplySettings();
                EliteStore.DataChanged += OnDataChanged;
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction: constructor failed: {ex}");
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            try
            {
                if (config.PressCycle && config.Views.Count > 1)
                {
                    lock (renderLock)
                        viewIndex = (viewIndex + 1) % config.Views.Count;
                    Render(false);
                }

                if (!string.IsNullOrEmpty(config.PressCommand))
                    EliteKeys.SendKeypress(config.PressCommand);

                if (sound != null)
                    AudioPlaybackEngine.Instance.PlaySound(sound);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction[{config?.MainView.Source}]: key press failed: {ex}");
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
                var current = config.Views[Math.Min(viewIndex, config.Views.Count - 1)].Source;
                var main = config.MainView.Source;
                if ((current.Length > 0 && changedKeys.Contains(current, StoreKeys.Comparer))
                    || (main.Length > 0 && changedKeys.Contains(main, StoreKeys.Comparer)))
                    Render(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction[{config?.MainView.Source}]: refresh failed: {ex}");
            }
        }

        private void ApplySettings()
        {
            var newConfig = DataKeyConfig.FromSettings(JObject.FromObject(settings),
                warning => Logger.Instance.LogMessage(TracingLevel.WARN, $"ValueAction[{settings.Source}]: {warning}"));

            // a rule whose image file does not exist is ignored
            var newRules = newConfig.Rules.Select(r => new ImageRule(r.Operator, r.Operand, ExistingFile(r.Image))).ToList();

            lock (renderLock)
            {
                config = newConfig;
                rules = newRules;
                if (viewIndex >= config.Views.Count)
                    viewIndex = 0;
            }

            LoadSound(config.ClickSound);
        }

        /// <summary>
        /// Sends the title and the image only when they changed (force: after creation or a settings change).
        /// Nothing is awaited, so that the watcher threads are never blocked.
        /// </summary>
        private void Render(bool force)
        {
            lock (renderLock)
            {
                if (disposed || config == null)
                    return;

                var view = config.Views[Math.Min(viewIndex, config.Views.Count - 1)];
                var title = config.ShowText ? ValueFormatter.Format(Read(view.Source), view.Display, DateTime.Now) : NoTitle;
                if (force || title != lastTitle)
                {
                    lastTitle = title;
                    if (title == NoTitle)
                        Watch(Connection.SetTitleAsync(null), "SetTitle"); // the title typed in the Stream Deck software is shown
                    else
                    {
                        Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Value[{view.Source}] = {title.Replace("\n", "\\n")}");
                        Watch(Connection.SetTitleAsync(title), "SetTitle");
                    }
                }

                var image = ImageRules.Choose(Read(config.MainView.Source), config.MainView.Display, rules) ?? ExistingFile(config.DefaultImage);
                if (force || !imageSent || image != lastImage)
                {
                    lastImage = image;
                    imageSent = true;
                    var base64 = image == null ? null : ImageBase64(image);
                    if (base64 != null)
                        Watch(Connection.SetImageAsync(base64), "SetImage");
                    else
                        Watch(Connection.SetDefaultImageAsync(), "SetDefaultImage");
                }
            }
        }

        private static JToken Read(string key)
        {
            JToken value = null;
            if (!string.IsNullOrEmpty(key))
                EliteStore.TryGet(key, out value);
            return value;
        }

        private static string ExistingFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
        }

        private string ImageBase64(string path)
        {
            string base64;
            if (!imageCache.TryGetValue(path, out base64))
            {
                try
                {
                    base64 = Tools.FileToBase64(path, true);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"ValueAction: cannot read image {path}: {ex.Message}");
                    base64 = null;
                }

                imageCache[path] = base64;
            }

            return base64;
        }

        private void LoadSound(string path)
        {
            if (path == soundPath)
                return;

            soundPath = path;
            sound = null;
            if (ExistingFile(path) == null)
                return;

            try
            {
                sound = new CachedSound(path);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"ValueAction: cannot load sound {path}: {ex.Message}");
            }
        }

        private void Watch(Task task, string what)
        {
            task.ContinueWith(t => Logger.Instance.LogMessage(TracingLevel.ERROR, $"ValueAction[{config?.MainView.Source}]: {what} failed: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
