using System.Collections.Generic;
using System.Linq;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// "Donnée" action (UUID kept from L3: com.mhwlng.elite.value), see docs/L5-tiroir.md and docs/L10-v3.md.
    /// Up to 4 views ("drawer"); each view has its data and text, its icon (rules), its command and its sound. The views,
    /// gestures, press and title are in DataKeyActionBase; this class chooses the image: the first true rule, or the
    /// default image, with the value drawn over it when the view asks for it (v3.2).
    /// </summary>
    [PluginActionId("com.mhwlng.elite.value")]
    public class ValueAction : DataKeyActionBase<ValueAction.PluginSettings>
    {
        // property names = JSON names of Generic.html; defaults apply to keys created before the setting existed
        public class PluginSettings : IDataKeySettings
        {
            // ---- view 1 (names of L3 / L4)
            [JsonProperty(PropertyName = "source")] public string Source { get; set; } = "";
            [JsonProperty(PropertyName = "prefix")] public string Prefix { get; set; } = "";
            [JsonProperty(PropertyName = "suffix")] public string Suffix { get; set; } = "";
            [JsonProperty(PropertyName = "decimals")] public string Decimals { get; set; } = "0";
            [JsonProperty(PropertyName = "scale")] public string Scale { get; set; } = "1";
            [JsonProperty(PropertyName = "offset")] public string Offset { get; set; } = "0";
            [JsonProperty(PropertyName = "compact")] public bool Compact { get; set; }
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
            // own data of a rule and AND condition (v3.5)
            [JsonProperty(PropertyName = "rule1Key")] public string Rule1Key { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndOp")] public string Rule1AndOp { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndValue")] public string Rule1AndValue { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndKey")] public string Rule1AndKey { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Key")] public string Rule2Key { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndOp")] public string Rule2AndOp { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndValue")] public string Rule2AndValue { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndKey")] public string Rule2AndKey { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Key")] public string Rule3Key { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndOp")] public string Rule3AndOp { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndValue")] public string Rule3AndValue { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndKey")] public string Rule3AndKey { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Key")] public string Rule4Key { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndOp")] public string Rule4AndOp { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndValue")] public string Rule4AndValue { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndKey")] public string Rule4AndKey { get; set; } = "";
            [JsonProperty(PropertyName = "pressCommand")] public string PressCommand { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkey")] public string PressHotkey { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkeyText")] public string PressHotkeyText { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "clickSound")] public string ClickSound { get; set; } = "";
            // text drawn into the image (v3.2)
            [JsonProperty(PropertyName = "drawText")] public bool DrawText { get; set; }
            [JsonProperty(PropertyName = "fontSize")] public string FontSize { get; set; } = "18";
            [JsonProperty(PropertyName = "fontName")] public string FontName { get; set; } = TextStyle.DefaultFont;
            [JsonProperty(PropertyName = "fontColor")] public string FontColor { get; set; } = TextStyle.DefaultColor;
            [JsonProperty(PropertyName = "fontBold")] public bool FontBold { get; set; }
            [JsonProperty(PropertyName = "textPosition")] public string TextPosition { get; set; } = "middle";
            [JsonProperty(PropertyName = "textWrap")] public bool TextWrap { get; set; } = true;
            [JsonProperty(PropertyName = "textFit")] public bool TextFit { get; set; } = true;

            // ---- views 2 to 4 (same names + view number): generated by tools/make-generic-html.py, do not edit by hand
            // <generated-views-2-4>
            [JsonProperty(PropertyName = "source2")] public string Source2 { get; set; } = "";
            [JsonProperty(PropertyName = "prefix2")] public string Prefix2 { get; set; } = "";
            [JsonProperty(PropertyName = "suffix2")] public string Suffix2 { get; set; } = "";
            [JsonProperty(PropertyName = "decimals2")] public string Decimals2 { get; set; } = "0";
            [JsonProperty(PropertyName = "scale2")] public string Scale2 { get; set; } = "1";
            [JsonProperty(PropertyName = "offset2")] public string Offset2 { get; set; } = "0";
            [JsonProperty(PropertyName = "compact2")] public bool Compact2 { get; set; }
            [JsonProperty(PropertyName = "showText2")] public bool ShowText2 { get; set; } = true;
            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage2")] public string BackgroundImage2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Op2")] public string Rule1Op2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Value2")] public string Rule1Value2 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule1Image2")] public string Rule1Image2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Key2")] public string Rule1Key2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndOp2")] public string Rule1AndOp2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndValue2")] public string Rule1AndValue2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndKey2")] public string Rule1AndKey2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Op2")] public string Rule2Op2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Value2")] public string Rule2Value2 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule2Image2")] public string Rule2Image2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Key2")] public string Rule2Key2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndOp2")] public string Rule2AndOp2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndValue2")] public string Rule2AndValue2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndKey2")] public string Rule2AndKey2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Op2")] public string Rule3Op2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Value2")] public string Rule3Value2 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule3Image2")] public string Rule3Image2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Key2")] public string Rule3Key2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndOp2")] public string Rule3AndOp2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndValue2")] public string Rule3AndValue2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndKey2")] public string Rule3AndKey2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Op2")] public string Rule4Op2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Value2")] public string Rule4Value2 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule4Image2")] public string Rule4Image2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Key2")] public string Rule4Key2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndOp2")] public string Rule4AndOp2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndValue2")] public string Rule4AndValue2 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndKey2")] public string Rule4AndKey2 { get; set; } = "";
            [JsonProperty(PropertyName = "pressCommand2")] public string PressCommand2 { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkey2")] public string PressHotkey2 { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkeyText2")] public string PressHotkeyText2 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "clickSound2")] public string ClickSound2 { get; set; } = "";
            [JsonProperty(PropertyName = "drawText2")] public bool DrawText2 { get; set; }
            [JsonProperty(PropertyName = "fontSize2")] public string FontSize2 { get; set; } = "18";
            [JsonProperty(PropertyName = "fontName2")] public string FontName2 { get; set; } = TextStyle.DefaultFont;
            [JsonProperty(PropertyName = "fontColor2")] public string FontColor2 { get; set; } = TextStyle.DefaultColor;
            [JsonProperty(PropertyName = "fontBold2")] public bool FontBold2 { get; set; }
            [JsonProperty(PropertyName = "textPosition2")] public string TextPosition2 { get; set; } = "middle";
            [JsonProperty(PropertyName = "textWrap2")] public bool TextWrap2 { get; set; } = true;
            [JsonProperty(PropertyName = "textFit2")] public bool TextFit2 { get; set; } = true;
            [JsonProperty(PropertyName = "source3")] public string Source3 { get; set; } = "";
            [JsonProperty(PropertyName = "prefix3")] public string Prefix3 { get; set; } = "";
            [JsonProperty(PropertyName = "suffix3")] public string Suffix3 { get; set; } = "";
            [JsonProperty(PropertyName = "decimals3")] public string Decimals3 { get; set; } = "0";
            [JsonProperty(PropertyName = "scale3")] public string Scale3 { get; set; } = "1";
            [JsonProperty(PropertyName = "offset3")] public string Offset3 { get; set; } = "0";
            [JsonProperty(PropertyName = "compact3")] public bool Compact3 { get; set; }
            [JsonProperty(PropertyName = "showText3")] public bool ShowText3 { get; set; } = true;
            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage3")] public string BackgroundImage3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Op3")] public string Rule1Op3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Value3")] public string Rule1Value3 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule1Image3")] public string Rule1Image3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Key3")] public string Rule1Key3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndOp3")] public string Rule1AndOp3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndValue3")] public string Rule1AndValue3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndKey3")] public string Rule1AndKey3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Op3")] public string Rule2Op3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Value3")] public string Rule2Value3 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule2Image3")] public string Rule2Image3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Key3")] public string Rule2Key3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndOp3")] public string Rule2AndOp3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndValue3")] public string Rule2AndValue3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndKey3")] public string Rule2AndKey3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Op3")] public string Rule3Op3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Value3")] public string Rule3Value3 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule3Image3")] public string Rule3Image3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Key3")] public string Rule3Key3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndOp3")] public string Rule3AndOp3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndValue3")] public string Rule3AndValue3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndKey3")] public string Rule3AndKey3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Op3")] public string Rule4Op3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Value3")] public string Rule4Value3 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule4Image3")] public string Rule4Image3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Key3")] public string Rule4Key3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndOp3")] public string Rule4AndOp3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndValue3")] public string Rule4AndValue3 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndKey3")] public string Rule4AndKey3 { get; set; } = "";
            [JsonProperty(PropertyName = "pressCommand3")] public string PressCommand3 { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkey3")] public string PressHotkey3 { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkeyText3")] public string PressHotkeyText3 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "clickSound3")] public string ClickSound3 { get; set; } = "";
            [JsonProperty(PropertyName = "drawText3")] public bool DrawText3 { get; set; }
            [JsonProperty(PropertyName = "fontSize3")] public string FontSize3 { get; set; } = "18";
            [JsonProperty(PropertyName = "fontName3")] public string FontName3 { get; set; } = TextStyle.DefaultFont;
            [JsonProperty(PropertyName = "fontColor3")] public string FontColor3 { get; set; } = TextStyle.DefaultColor;
            [JsonProperty(PropertyName = "fontBold3")] public bool FontBold3 { get; set; }
            [JsonProperty(PropertyName = "textPosition3")] public string TextPosition3 { get; set; } = "middle";
            [JsonProperty(PropertyName = "textWrap3")] public bool TextWrap3 { get; set; } = true;
            [JsonProperty(PropertyName = "textFit3")] public bool TextFit3 { get; set; } = true;
            [JsonProperty(PropertyName = "source4")] public string Source4 { get; set; } = "";
            [JsonProperty(PropertyName = "prefix4")] public string Prefix4 { get; set; } = "";
            [JsonProperty(PropertyName = "suffix4")] public string Suffix4 { get; set; } = "";
            [JsonProperty(PropertyName = "decimals4")] public string Decimals4 { get; set; } = "0";
            [JsonProperty(PropertyName = "scale4")] public string Scale4 { get; set; } = "1";
            [JsonProperty(PropertyName = "offset4")] public string Offset4 { get; set; } = "0";
            [JsonProperty(PropertyName = "compact4")] public bool Compact4 { get; set; }
            [JsonProperty(PropertyName = "showText4")] public bool ShowText4 { get; set; } = true;
            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundImage4")] public string BackgroundImage4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Op4")] public string Rule1Op4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Value4")] public string Rule1Value4 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule1Image4")] public string Rule1Image4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1Key4")] public string Rule1Key4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndOp4")] public string Rule1AndOp4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndValue4")] public string Rule1AndValue4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule1AndKey4")] public string Rule1AndKey4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Op4")] public string Rule2Op4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Value4")] public string Rule2Value4 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule2Image4")] public string Rule2Image4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2Key4")] public string Rule2Key4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndOp4")] public string Rule2AndOp4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndValue4")] public string Rule2AndValue4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule2AndKey4")] public string Rule2AndKey4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Op4")] public string Rule3Op4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Value4")] public string Rule3Value4 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule3Image4")] public string Rule3Image4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3Key4")] public string Rule3Key4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndOp4")] public string Rule3AndOp4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndValue4")] public string Rule3AndValue4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule3AndKey4")] public string Rule3AndKey4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Op4")] public string Rule4Op4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Value4")] public string Rule4Value4 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "rule4Image4")] public string Rule4Image4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4Key4")] public string Rule4Key4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndOp4")] public string Rule4AndOp4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndValue4")] public string Rule4AndValue4 { get; set; } = "";
            [JsonProperty(PropertyName = "rule4AndKey4")] public string Rule4AndKey4 { get; set; } = "";
            [JsonProperty(PropertyName = "pressCommand4")] public string PressCommand4 { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkey4")] public string PressHotkey4 { get; set; } = "";
            [JsonProperty(PropertyName = "pressHotkeyText4")] public string PressHotkeyText4 { get; set; } = "";
            [FilenameProperty]
            [JsonProperty(PropertyName = "clickSound4")] public string ClickSound4 { get; set; } = "";
            [JsonProperty(PropertyName = "drawText4")] public bool DrawText4 { get; set; }
            [JsonProperty(PropertyName = "fontSize4")] public string FontSize4 { get; set; } = "18";
            [JsonProperty(PropertyName = "fontName4")] public string FontName4 { get; set; } = TextStyle.DefaultFont;
            [JsonProperty(PropertyName = "fontColor4")] public string FontColor4 { get; set; } = TextStyle.DefaultColor;
            [JsonProperty(PropertyName = "fontBold4")] public bool FontBold4 { get; set; }
            [JsonProperty(PropertyName = "textPosition4")] public string TextPosition4 { get; set; } = "middle";
            [JsonProperty(PropertyName = "textWrap4")] public bool TextWrap4 { get; set; } = true;
            [JsonProperty(PropertyName = "textFit4")] public bool TextFit4 { get; set; } = true;
            // </generated-views-2-4>

            // ---- whole key
            [JsonProperty(PropertyName = "emptyText")] public string EmptyText { get; set; } = ValueSettings.DefaultEmptyText;
            [JsonProperty(PropertyName = "pressMode")] public string PressMode { get; set; } = PressGesture.ShortActLongViewName;

            // ---- saved by the plugin, not by the page: displayed view (1 to 4), kept when the key disappears (folder...)
            [JsonProperty(PropertyName = "currentView")] public int CurrentView { get; set; } = 1;
        }

        private Dictionary<int, List<ImageRule>> validRules = new Dictionary<int, List<ImageRule>>();

        public ValueAction(SDConnection connection, InitialPayload payload) : base(connection, payload, "ValueAction")
        {
        }

        /// <summary>
        /// Keys created before L5 have no showText2..4: those views keep the choice of view 1 (as in L4).
        /// </summary>
        protected override void PrepareSettings(JObject raw)
        {
            if (raw == null)
                return;

            var main = raw["showText"];
            for (int i = 2; i <= DataKeyConfig.MaxViews; i++)
            {
                var name = "showText" + i;
                if (raw[name] == null || raw[name].Type == JTokenType.Null)
                    raw[name] = main == null || main.Type == JTokenType.Null ? new JValue(true) : main.DeepClone();
            }
        }

        // a rule whose image file does not exist is ignored
        protected override void SettingsApplied(JObject json)
        {
            var newConfig = DataKeyConfig.FromSettings(json);
            var newRules = newConfig.Views.ToDictionary(
                v => v.Number,
                v => v.Rules.Select(r => new ImageRule(r.Operator, r.Operand, KeyMedia.ExistingFile(r.Image))
                {
                    Key = r.Key,
                    AndOperator = r.AndOperator,
                    AndOperand = r.AndOperand,
                    AndKey = r.AndKey,
                }).ToList());

            lock (renderLock)
                validRules = newRules;
        }

        protected override IEnumerable<string> WatchedKeys(DataView view)
        {
            yield return view.Source;
            var iconView = config.IconViewOf(view);
            yield return iconView.Source;
            // v3.5: data tested by the rules themselves
            foreach (var key in iconView.Rules.SelectMany(r => r.Keys))
                yield return key;
        }

        // caller holds renderLock
        protected override KeyImage BuildImage(DataView view, string drawnText)
        {
            var iconView = config.IconViewOf(view);
            List<ImageRule> rules;
            if (!validRules.TryGetValue(iconView.Number, out rules))
                rules = new List<ImageRule>();
            var image = ImageRules.Choose(Read, Read(iconView.Source), iconView.Display, rules) ?? KeyMedia.ExistingFile(iconView.DefaultImage);

            if (drawnText == null)
                return new KeyImage("file|" + image, () => image == null ? null : media.ImageBase64(image));

            var style = view.Text;
            return new KeyImage("text|" + image + "|" + drawnText + "|" + style.Signature,
                () => KeyRenderer.RenderBase64(image == null ? null : media.ImageBytes(image), null, drawnText, style));
        }
    }
}
