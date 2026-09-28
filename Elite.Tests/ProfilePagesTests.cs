using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Pages of the ZV Elite profile delivered with the plugin (v3.7, trial, docs/L10-v3.md).
    /// </summary>
    [TestFixture]
    public class ProfilePagesTests
    {
        private static string Plugin
        {
            get
            {
                return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\Elite\bin\Debug\com.mhwlng.elite.sdPlugin"));
            }
        }

        [Test]
        public void Commands()
        {
            int page;
            Assert.That(ProfilePages.TryParse("ZV-Page-1", out page) && page == 1, Is.True);
            Assert.That(ProfilePages.TryParse("ZV-Page-5", out page) && page == 5, Is.True);
            Assert.That(ProfilePages.TryParse("ZV-ProfileBack", out page) && page == 0, Is.True);
            Assert.That(ProfilePages.TryParse("ZV-Page-6", out page), Is.False);
            Assert.That(ProfilePages.TryParse("ZV-Page-0", out page), Is.False);
            Assert.That(ProfilePages.TryParse("LandingGearToggle", out page), Is.False);
            Assert.That(ProfilePages.TryParse(null, out page), Is.False);
        }

        [Test]
        public void SwitchToProfileMessage_OfTheSdk()
        {
            var page2 = ProfilePages.Message("plugin-uuid", "device-id", 2);
            Assert.That((string)page2["event"], Is.EqualTo("switchToProfile"));
            Assert.That((string)page2["context"], Is.EqualTo("plugin-uuid"), "the plugin UUID of the registration");
            Assert.That((string)page2["device"], Is.EqualTo("device-id"));
            Assert.That((string)page2["payload"]["profile"], Is.EqualTo("ZV Elite"));
            Assert.That((int)page2["payload"]["page"], Is.EqualTo(1), "pages are indexed from 0 in the SDK");

            var back = ProfilePages.Message("plugin-uuid", "device-id", 0);
            Assert.That(((JObject)back["payload"]).Count, Is.EqualTo(0), "without profile: the previous profile");
        }

        [Test]
        public void TheProfile_IsDeclared_AndDelivered()
        {
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(Plugin, "manifest.json")));
            var profile = (JObject)manifest["Profiles"].Single();
            Assert.That((string)profile["Name"], Is.EqualTo(ProfilePages.ProfileName));
            Assert.That((int)profile["DeviceType"], Is.EqualTo(0), "Stream Deck MK.1 / MK.2");
            Assert.That((bool)profile["ReadOnly"], Is.False, "Florian must be able to fill it");
            Assert.That((bool)profile["DontAutoSwitchWhenInstalled"], Is.True);

            var file = Path.Combine(Plugin, ProfilePages.ProfileName + ".streamDeckProfile");
            Assert.That(File.Exists(file), Is.True, file);

            using (var zip = ZipFile.OpenRead(file))
            {
                var root = zip.Entries.Single(e => e.FullName.EndsWith(".sdProfile/manifest.json") && e.FullName.Count(c => c == '/') == 1);
                JObject content;
                using (var reader = new StreamReader(root.Open()))
                    content = JObject.Parse(reader.ReadToEnd());

                Assert.That((string)content["Name"], Is.EqualTo("ZV Elite"));
                Assert.That((string)content["Device"]["Model"], Is.EqualTo("20GBA9901"), "Stream Deck MK.2");
                var pages = content["Pages"]["Pages"].Values<string>().ToList();
                Assert.That(pages.Count, Is.EqualTo(2));

                // every page has its folder (id in base 32 + "Z", as in the profiles of the Stream Deck software)
                var folders = zip.Entries.Where(e => e.FullName.Contains("/Profiles/") && e.Name == "manifest.json")
                    .Select(e => e.FullName.Split('/')[2]).ToList();
                foreach (var page in pages.Concat(new[] { (string)content["Pages"]["Default"] }))
                    Assert.That(folders, Does.Contain(PageFolder(Guid.Parse(page))), page);
            }
        }

        [Test]
        public void TheProfileName_IsNotTakenByTheAutomaticSwitchOfTheHistoricCode()
        {
            // Profile.GetProfileTypes (mhwlng): a profile whose name holds a keyword (main, docked...) is switched to
            // automatically according to the game state; ZV Elite must never be
            var method = typeof(EliteData).Assembly.GetType("Elite.Profile").GetMethod("GetProfileTypes", BindingFlags.NonPublic | BindingFlags.Static);
            var types = (System.Collections.IList)method.Invoke(null, new object[] { ProfilePages.ProfileName });
            Assert.That(types.Count, Is.EqualTo(0));
        }

        // base 32 (alphabet 0-9 A-T V W) of the 16 bytes of the id, big endian, then "Z"
        private static string PageFolder(Guid id)
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTVW";
            var bytes = id.ToByteArray();
            var bigEndian = new byte[16];
            Array.Copy(bytes, bigEndian, 16);
            Array.Reverse(bigEndian, 0, 4);
            Array.Reverse(bigEndian, 4, 2);
            Array.Reverse(bigEndian, 6, 2);

            var bits = string.Concat(bigEndian.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))) + "00";
            var text = new List<char>();
            for (int i = 0; i < bits.Length; i += 5)
                text.Add(alphabet[Convert.ToInt32(bits.Substring(i, 5), 2)]);
            return new string(text.ToArray()) + "Z";
        }
    }
}
