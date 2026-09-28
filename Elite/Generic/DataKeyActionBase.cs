using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Elite.Buttons;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>Settings of a key with views: the plugin saves the displayed view in them.</summary>
    public interface IDataKeySettings
    {
        int CurrentView { get; set; }
    }

    /// <summary>
    /// Image of a key: Signature says whether it changed (nothing is sent when it did not), Produce gives the PNG/GIF in
    /// base64 with its data-URI header, or null for the default image of the action.
    /// </summary>
    public class KeyImage
    {
        public KeyImage(string signature, Func<string> produce)
        {
            Signature = signature;
            Produce = produce;
        }

        public string Signature { get; }
        public Func<string> Produce { get; }
    }

    /// <summary>
    /// Common part of the keys with views (docs/L5-tiroir.md, docs/L10-v3.md): "Donnée" (ValueAction) and "Graphique"
    /// (GraphAction). Up to 4 views, each with its data, text, command, shortcut and sound; gestures (short / long press);
    /// displayed view saved in the settings; "current value" requests of the "i" panel; title of the value, or text drawn
    /// into the image (v3.2). The derived action only chooses the image of the displayed view (BuildImage).
    /// Redrawn only when one of its keys changes, and only what changed is sent (§4.5).
    /// </summary>
    public abstract class DataKeyActionBase<TSettings> : EliteKeypadBase where TSettings : class, IDataKeySettings, new()
    {
        private const string NoTitle = "\u0000no title";   // the title typed in the Stream Deck software is shown
        private const string DrawnTitle = "";              // the value is drawn into the image: no title

        protected readonly object renderLock = new object();
        protected readonly KeyMedia media;
        protected TSettings settings;
        protected DataKeyConfig config;
        private int viewIndex;
        private string lastTitle;
        private string lastImage;
        private bool imageSent;
        private bool disposed;

        // key press: the long action fires after LongPressMilliseconds, the short one on release
        private Timer longPressTimer;
        private bool keyDown;
        private bool longPressDone;

        protected DataKeyActionBase(SDConnection connection, InitialPayload payload, string owner) : base(connection, payload)
        {
            Owner = owner;
            media = new KeyMedia(owner);
            try
            {
                var raw = payload.Settings ?? new JObject();
                PrepareSettings(raw);
                settings = raw.Count == 0 ? new TSettings() : raw.ToObject<TSettings>();

                // saves the defaults of the settings added since the key was created
                Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();

                ApplySettings();
                EliteStore.DataChanged += OnDataChanged;
                Connection.OnSendToPlugin += OnSendToPlugin;
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}: constructor failed: {ex}");
            }
        }

        protected string Owner { get; }

        /// <summary>Image of the displayed view; drawnText = the value to draw into it (null: title of the software).</summary>
        protected abstract KeyImage BuildImage(DataView view, string drawnText);

        /// <summary>Settings as received (constructor and page), before they are read: compatibility of old keys.</summary>
        protected virtual void PrepareSettings(JObject raw)
        {
        }

        /// <summary>New settings: the derived action reads its own ones (called outside of the render lock).</summary>
        protected virtual void SettingsApplied(JObject json)
        {
        }

        /// <summary>Keys whose change redraws the displayed view (at least its data).</summary>
        protected virtual IEnumerable<string> WatchedKeys(DataView view)
        {
            yield return view.Source;
        }

        public override void KeyPressed(KeyPayload payload)
        {
            try
            {
                lock (renderLock)
                {
                    keyDown = true;
                    longPressDone = false;
                    longPressTimer?.Dispose();
                    longPressTimer = new Timer(OnLongPress, null, PressGesture.LongPressMilliseconds, Timeout.Infinite);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}[{config?.MainView.Source}]: key press failed: {ex}");
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
            try
            {
                bool shortPress;
                lock (renderLock)
                {
                    longPressTimer?.Dispose();
                    longPressTimer = null;
                    shortPress = keyDown && !longPressDone;
                    keyDown = false;
                }

                if (shortPress)
                    Perform(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}[{config?.MainView.Source}]: key release failed: {ex}");
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            try
            {
                PrepareSettings(payload.Settings);
                BarRaider.SdTools.Tools.AutoPopulateSettings(settings, payload.Settings);
                ApplySettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings)).Wait();
                Render(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}: settings update failed: {ex}");
            }
        }

        public override void Dispose()
        {
            lock (renderLock)
            {
                disposed = true;
                longPressTimer?.Dispose();
                longPressTimer = null;
            }

            EliteStore.DataChanged -= OnDataChanged;
            Connection.OnSendToPlugin -= OnSendToPlugin;
            base.Dispose();
        }

        // timer thread
        private void OnLongPress(object state)
        {
            try
            {
                lock (renderLock)
                {
                    if (!keyDown || longPressDone || disposed)
                        return;
                    longPressDone = true;
                }

                Perform(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}[{config?.MainView.Source}]: long press failed: {ex}");
            }
        }

        private void Perform(bool isLongPress)
        {
            DataView view;
            PressOutcome outcome;
            JObject savedSettings = null;
            lock (renderLock)
            {
                if (config == null)
                    return;
                outcome = PressGesture.Resolve(isLongPress, config.PressMode, config.Views.Count);
                if (outcome == PressOutcome.NextView)
                {
                    viewIndex = (viewIndex + 1) % config.Views.Count;
                    // the key is recreated each time it appears again (folder, page...): the view is kept in its settings
                    settings.CurrentView = config.Views[viewIndex].Number;
                    savedSettings = JObject.FromObject(settings);
                }
                view = CurrentView();
            }

            if (outcome == PressOutcome.NextView)
            {
                Render(false);
                Watch(Connection.SetSettingsAsync(savedSettings), "SetSettings");
                return;
            }

            KeyCommands.Send(view.Command, view.Hotkey, view.HotkeyText, $"{Owner}[{view.Source}]");
            media.Play(view.Sound);
        }

        /// <summary>
        /// "i" panel of the property inspector: { genericValueRequest: key, genericView: n } -> current value of the key,
        /// formatted with the settings of view n.
        /// </summary>
        private void OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            try
            {
                var payload = e?.Event?.Payload;
                var key = payload?["genericValueRequest"];
                if (key == null)
                    return;

                int viewNumber = payload["genericView"] != null ? payload["genericView"].Value<int>() : 1;
                ValueSettings display;
                lock (renderLock)
                {
                    var view = config?.Views.FirstOrDefault(v => v.Number == viewNumber) ?? config?.MainView;
                    display = view != null ? view.Display : new ValueSettings();
                }

                var reply = ValueFormatter.DescribeCurrentValue((string)key, Read((string)key), display, DateTime.Now);
                Watch(Connection.SendToPropertyInspectorAsync(reply), "SendToPropertyInspector");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}: current value request failed: {ex}");
            }
        }

        // background thread (status or journal watcher)
        private void OnDataChanged(IReadOnlyCollection<string> changedKeys)
        {
            try
            {
                List<string> watched;
                lock (renderLock)
                {
                    if (config == null)
                        return;
                    watched = WatchedKeys(CurrentView()).Where(k => !string.IsNullOrEmpty(k)).ToList();
                }

                if (watched.Any(k => changedKeys.Contains(k, StoreKeys.Comparer)))
                    Render(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}[{config?.MainView.Source}]: refresh failed: {ex}");
            }
        }

        private void ApplySettings()
        {
            var json = JObject.FromObject(settings);
            var newConfig = DataKeyConfig.FromSettings(json,
                warning => Logger.Instance.LogMessage(TracingLevel.WARN, $"{Owner}[{json["source"]}]: {warning}"));
            SettingsApplied(json);

            lock (renderLock)
            {
                config = newConfig;
                // saved displayed view (view 1 if it no longer exists)
                viewIndex = config.CurrentViewIndex;
                settings.CurrentView = CurrentView().Number;
            }
        }

        // caller holds renderLock
        protected DataView CurrentView()
        {
            return config.Views[Math.Min(viewIndex, config.Views.Count - 1)];
        }

        /// <summary>
        /// Sends the title and the image of the displayed view only when they changed (force: after creation or a
        /// settings change). Nothing is awaited, so that the watcher threads are never blocked.
        /// </summary>
        protected void Render(bool force)
        {
            lock (renderLock)
            {
                if (disposed || config == null)
                    return;

                var view = CurrentView();
                string value = view.ShowText ? ValueFormatter.Format(Read(view.Source), view.Display, DateTime.Now) : null;
                bool drawn = value != null && view.Text.Draw;
                var title = value == null ? NoTitle : drawn ? DrawnTitle : value;
                if (force || title != lastTitle)
                {
                    lastTitle = title;
                    if (title == NoTitle)
                        Watch(Connection.SetTitleAsync(null), "SetTitle");
                    else
                    {
                        if (!drawn)
                            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"{Owner}[{view.Source}] view {view.Number} = {title.Replace("\n", "\\n")}");
                        Watch(Connection.SetTitleAsync(title), "SetTitle");
                    }
                }

                var image = BuildImage(view, drawn ? value : null);
                if (force || !imageSent || image.Signature != lastImage)
                {
                    lastImage = image.Signature;
                    imageSent = true;
                    string base64 = null;
                    try
                    {
                        base64 = image.Produce();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}[{view.Source}]: image failed: {ex}");
                    }

                    if (base64 != null)
                        Watch(Connection.SetImageAsync(base64), "SetImage");
                    else
                        Watch(Connection.SetDefaultImageAsync(), "SetDefaultImage");
                }
            }
        }

        protected static JToken Read(string key)
        {
            JToken value = null;
            if (!string.IsNullOrEmpty(key))
                EliteStore.TryGet(key, out value);
            return value;
        }

        protected void Watch(Task task, string what)
        {
            task.ContinueWith(t => Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Owner}[{config?.MainView.Source}]: {what} failed: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
