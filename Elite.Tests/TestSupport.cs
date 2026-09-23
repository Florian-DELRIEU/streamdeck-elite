using System.IO;
using EliteJournalReader;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Test data (Data folder):
    /// - journal-extrait.txt: real journal lines of the author, without CMDR name nor FID;
    /// - status-menu.json: real status.json (main menu);
    /// - status-vaisseau.json, status-a-pied.json: synthetic, written from the official status.json format.
    /// </summary>
    internal static class TestData
    {
        public static string PathOf(string name)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", name);
        }

        public static string[] JournalLines()
        {
            return File.ReadAllLines(PathOf("journal-extrait.txt"));
        }

        /// <summary>
        /// First line of the extract for this event, optionally the n-th one (0-based).
        /// </summary>
        public static string JournalLine(string eventName, int occurrence = 0)
        {
            foreach (var line in JournalLines())
            {
                if (line.Contains("\"event\":\"" + eventName + "\"") && occurrence-- == 0)
                    return line;
            }

            Assert.Fail("No " + eventName + " line in journal-extrait.txt");
            return null;
        }

        public static JObject Status(string name)
        {
            return JObject.Parse(File.ReadAllText(PathOf(name)));
        }
    }

    /// <summary>
    /// Real JournalWatcher (real Parse), without file system: lines are pushed with ParseText.
    /// </summary>
    internal class TestJournalWatcher : JournalWatcher
    {
        public void SetLive(bool live)
        {
            IsLive = live;
        }
    }

    /// <summary>
    /// Real StatusWatcher reading a given file.
    /// </summary>
    internal class TestStatusWatcher : StatusWatcher
    {
        public void Read(string fullPath)
        {
            UpdateStatus(fullPath, 0);
        }
    }
}
