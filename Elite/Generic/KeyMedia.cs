using System;
using System.Collections.Generic;
using System.IO;
using BarRaider.SdTools;

namespace Elite.Generic
{
    /// <summary>
    /// Images (base64) and sounds of a key, read once then cached. A missing or unreadable file gives null and a WARN.
    /// Used by the Alarm key (ValueAction keeps its own copy, unchanged since L7).
    /// </summary>
    internal class KeyMedia
    {
        private readonly string owner;
        private readonly Dictionary<string, string> images = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CachedSound> sounds = new Dictionary<string, CachedSound>(StringComparer.OrdinalIgnoreCase);

        public KeyMedia(string owner)
        {
            this.owner = owner;
        }

        public static string ExistingFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
        }

        public string ImageBase64(string path)
        {
            lock (images)
            {
                string base64;
                if (!images.TryGetValue(path, out base64))
                {
                    try
                    {
                        base64 = Tools.FileToBase64(path, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"{owner}: cannot read image {path}: {ex.Message}");
                        base64 = null;
                    }

                    images[path] = base64;
                }

                return base64;
            }
        }

        /// <summary>Plays the sound file, if any (nothing for an empty or missing file).</summary>
        public void Play(string path)
        {
            if (ExistingFile(path) == null)
                return;

            CachedSound sound;
            lock (sounds)
            {
                if (!sounds.TryGetValue(path, out sound))
                {
                    try
                    {
                        sound = new CachedSound(path);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"{owner}: cannot load sound {path}: {ex.Message}");
                        sound = null;
                    }

                    sounds[path] = sound;
                }
            }

            if (sound != null)
                AudioPlaybackEngine.Instance.PlaySound(sound);
        }
    }
}
