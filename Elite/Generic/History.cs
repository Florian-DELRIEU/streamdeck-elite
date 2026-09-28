using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// History of the keys shown as a curve by a Graph key (issue #2, v3.3/v3.4): one series per key and interval, up
    /// to MaxPoints values (oldest first), sampled even while the Graph key is not displayed, and saved in PluginMemory.
    /// A series not requested for UnusedDays is dropped when the file is read.
    /// </summary>
    public static class History
    {
        public const int MaxPoints = 240;
        public const int MinInterval = 1;
        public const int UnusedDays = 30;

        private class Series
        {
            public string Key;
            public int Interval;
            public DateTime LastUsed;
            public DateTime LastSample = DateTime.MinValue;
            public readonly List<double?> Points = new List<double?>();
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Series> All = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
        private static Timer timer;

        /// <summary>Series sampled (id = key@interval), from the timer thread.</summary>
        public static event Action<string> Changed;

        public static int SeriesCount
        {
            get
            {
                lock (Sync)
                    return All.Count;
            }
        }

        public static string Id(string key, int interval)
        {
            return key + "@" + Math.Max(MinInterval, interval);
        }

        /// <summary>Starts (or keeps) the series of a key, sampled every interval seconds.</summary>
        public static void Track(string key, int interval, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            interval = Math.Max(MinInterval, interval);
            lock (Sync)
            {
                Series series;
                var id = Id(key, interval);
                if (!All.TryGetValue(id, out series))
                {
                    series = new Series { Key = key.Trim(), Interval = interval };
                    All[id] = series;
                }
                series.LastUsed = now;
            }
        }

        /// <summary>Values of a series, oldest first (null = no value at that time); empty if not tracked.</summary>
        public static List<double?> Points(string key, int interval)
        {
            lock (Sync)
            {
                Series series;
                return All.TryGetValue(Id(key, interval), out series) ? new List<double?>(series.Points) : new List<double?>();
            }
        }

        /// <summary>Samples every series whose interval elapsed; read gives the current value of a key.</summary>
        internal static List<string> Tick(DateTime now, Func<string, JToken> read)
        {
            var sampled = new List<string>();
            lock (Sync)
            {
                foreach (var pair in All)
                {
                    var series = pair.Value;
                    if ((now - series.LastSample).TotalSeconds < series.Interval)
                        continue;

                    series.LastSample = now;
                    series.Points.Add(Number(read(series.Key)));
                    if (series.Points.Count > MaxPoints)
                        series.Points.RemoveRange(0, series.Points.Count - MaxPoints);
                    sampled.Add(pair.Key);
                }
            }

            if (sampled.Count > 0)
            {
                PluginMemory.RequestSave();
                var handler = Changed;
                foreach (var id in sampled)
                {
                    try
                    {
                        handler?.Invoke(id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"History: subscriber failed: {ex}");
                    }
                }
            }

            return sampled;
        }

        /// <summary>Samples every second, from the plugin start.</summary>
        public static void Start()
        {
            lock (Sync)
            {
                if (timer != null)
                    return;
                timer = new Timer(_ =>
                {
                    try
                    {
                        Tick(DateTime.UtcNow, key =>
                        {
                            JToken value;
                            return EliteStore.TryGet(key, out value) ? value : null;
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"History: sampling failed: {ex}");
                    }
                }, null, 1000, 1000);
            }
        }

        public static double? Number(JToken value)
        {
            if (value == null)
                return null;
            switch (value.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    return (double)value;
                case JTokenType.Boolean:
                    return (bool)value ? 1 : 0;
                default:
                    double parsed;
                    return value.Type == JTokenType.String && Condition.TryParseNumber((string)value, out parsed) ? parsed : (double?)null;
            }
        }

        internal static void Load(JObject json, DateTime now)
        {
            lock (Sync)
            {
                All.Clear();
                if (json == null)
                    return;

                foreach (var property in json.Properties())
                {
                    try
                    {
                        var item = property.Value as JObject;
                        var lastUsed = item?["lastUsed"] != null ? (DateTime)item["lastUsed"] : now;
                        if (item == null || (now - lastUsed).TotalDays > UnusedDays)
                            continue;

                        var series = new Series { Key = (string)item["key"], Interval = Math.Max(MinInterval, (int?)item["interval"] ?? 5), LastUsed = lastUsed };
                        foreach (var point in item["points"] as JArray ?? new JArray())
                            series.Points.Add(point.Type == JTokenType.Null ? (double?)null : (double)point);
                        if (!string.IsNullOrEmpty(series.Key))
                            All[Id(series.Key, series.Interval)] = series;
                    }
                    catch (Exception)
                    {
                        // one unreadable series does not prevent the others
                    }
                }
            }
        }

        internal static JObject Snapshot()
        {
            lock (Sync)
            {
                var json = new JObject();
                foreach (var pair in All.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    json[pair.Key] = new JObject
                    {
                        { "key", pair.Value.Key },
                        { "interval", pair.Value.Interval },
                        { "lastUsed", pair.Value.LastUsed },
                        { "points", new JArray(pair.Value.Points.Select(p => p.HasValue ? (JToken)new JValue(Math.Round(p.Value, 4)) : JValue.CreateNull())) },
                    };
                }
                return json;
            }
        }

        internal static void Reset()
        {
            lock (Sync)
            {
                All.Clear();
                timer?.Dispose();
                timer = null;
            }
            Changed = null;
        }
    }
}
