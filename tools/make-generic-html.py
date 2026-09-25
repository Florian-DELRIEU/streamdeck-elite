# Generates the repetitive parts of the "Donnee" action (docs/L5-tiroir.md), so that the page and the C# settings
# always have the same fields:
#   - Elite/PropertyInspector/Elite/Generic.html (whole page: 4 views x ~20 fields)
#   - Elite/Generic/ValueAction.cs: settings of views 2 to 4, between the <generated-views-2-4> markers
# Usage (from anywhere): python tools/make-generic-html.py
# Then: run build.ps1 and test.ps1. Keep the JSON names stable: keys already placed in a Stream Deck profile use them.
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HTML = os.path.join(ROOT, "Elite", "PropertyInspector", "Elite", "Generic.html")
ACTION = os.path.join(ROOT, "Elite", "Generic", "ValueAction.cs")
VIEWS = 4
RULES = 4


def sfx(view):
    return "" if view == 1 else str(view)


def file_picker(id_, label, accept):
    return f'''            <div class="sdpi-item">
                <div class="sdpi-item-label" onclick="clearFileName('{id_}');">{label}</div>
                <div class="sdpi-item-group file">
                    <input class="sdpi-item-value sdProperty sdFile" type="file" id="{id_}" accept="{accept}" oninput="setSettings()">
                    <label class="sdpi-file-info " for="{id_}" id="{id_}Filename">No file...</label>
                    <label class="sdpi-file-label" for="{id_}">Choose file...</label>
                </div>
            </div>'''


IMAGES = ".jpg, .jpeg, .png, .ico, .gif, .bmp, .tiff"


def key_block(v):
    s = sfx(v)
    return f'''        <div class="sdpi-item" data-view="{v}">
            <div class="sdpi-item-label">Clé (avancé)</div>
            <input class="sdpi-item-value sdProperty generic-key" id="source{s}" type="text" placeholder="ex. status.Fuel.FuelMain" oninput="genericSourceTyped()">
        </div>'''


def text_block(v):
    s = sfx(v)
    return f'''        <div data-view="{v}">
            <div class="sdpi-item">
                <div class="sdpi-item-label">Afficher</div>
                <div class="sdpi-item-value">
                    <input id="showText{s}" class="sdProperty sdCheckbox" type="checkbox" value="" oninput="setSettings()" checked>
                    <label for="showText{s}" class="sdpi-item-label"><span></span>la valeur (sinon : titre du logiciel)</label>
                </div>
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Préfixe</div>
                <input class="sdpi-item-value sdProperty" id="prefix{s}" type="text" placeholder="ex. Fuel\\n" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Suffixe</div>
                <input class="sdpi-item-value sdProperty" id="suffix{s}" type="text" placeholder="ex.  t" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Décimales</div>
                <input class="sdpi-item-value sdProperty" id="decimals{s}" type="number" min="0" max="6" step="1" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Facteur</div>
                <input class="sdpi-item-value sdProperty" id="scale{s}" type="text" placeholder="1, /32, *4, *100/32" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Décalage</div>
                <input class="sdpi-item-value sdProperty" id="offset{s}" type="text" placeholder="0 (K → °C : -273,15)" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Abrégé</div>
                <div class="sdpi-item-value">
                    <input id="compact{s}" class="sdProperty sdCheckbox" type="checkbox" value="" oninput="setSettings()">
                    <label for="compact{s}" class="sdpi-item-label"><span></span>12 345 678 → 12,3 M</label>
                </div>
            </div>
        </div>'''


def rule_rows(v, r):
    s = sfx(v)
    return f'''            <div class="sdpi-item">
                <div class="sdpi-item-label">Règle {r}</div>
                <div class="sdpi-item-value generic-rule">
                    <select class="select sdProperty generic-rule-op" id="rule{r}Op{s}" onchange="setSettings()">
                        <option value="">— aucune —</option>
                        <option value="isTrue">est vrai / oui</option>
                        <option value="isFalse">est faux / non</option>
                        <option value="equals">=</option>
                        <option value="notEquals">≠</option>
                        <option value="lt">&lt;</option>
                        <option value="lte">≤</option>
                        <option value="gt">&gt;</option>
                        <option value="gte">≥</option>
                    </select>
                    <input class="sdProperty" id="rule{r}Value{s}" type="text" list="genericEnumValues" placeholder="valeur" oninput="setSettings()">
                </div>
            </div>
{file_picker(f"rule{r}Image{s}", f"Image {r}", IMAGES)}'''


def icon_block(v):
    s = sfx(v)
    rules = "\n".join(rule_rows(v, r) for r in range(1, RULES + 1))
    return f'''        <div data-view="{v}">
{file_picker(f"backgroundImage{s}", "Par défaut", IMAGES)}
{rules}
        </div>'''


def command_block(v):
    s = sfx(v)
    return f'''        <div class="sdpi-item" data-view="{v}">
            <div class="sdpi-item-label">Commande</div>
            <select class="sdpi-item-value select sdProperty generic-command" id="pressCommand{s}" onchange="genericCommandChanged()"></select>
        </div>'''


# free shortcut: the text field shows it (pressHotkeyText), the hidden field keeps the physical key codes (pressHotkey)
def press_block(v):
    s = sfx(v)
    return f'''        <div data-view="{v}">
            <div class="sdpi-item">
                <div class="sdpi-item-label generic-clickable" onclick="genericHotkeyClear({v})" title="clic = effacer le raccourci">Raccourci</div>
                <input class="sdpi-item-value sdProperty generic-hotkey" id="pressHotkeyText{s}" type="text" readonly placeholder="clic ici, puis appuie sur les touches" onfocus="genericHotkeyFocus({v})" onblur="genericHotkeyBlur({v})" onkeydown="genericHotkeyKeyDown(event, {v})" onkeyup="genericHotkeyKeyUp(event, {v})">
                <input class="sdProperty" id="pressHotkey{s}" type="hidden">
            </div>
{file_picker(f"clickSound{s}", "Son", ".wav")}
        </div>'''


def page():
    keys = "\n".join(key_block(v) for v in range(1, VIEWS + 1))
    texts = "\n".join(text_block(v) for v in range(1, VIEWS + 1))
    icons = "\n".join(icon_block(v) for v in range(1, VIEWS + 1))
    commands = "\n".join(command_block(v) for v in range(1, VIEWS + 1))
    presses = "\n".join(press_block(v) for v in range(1, VIEWS + 1))
    return f'''<!DOCTYPE html>
<!-- Generated by tools/make-generic-html.py - do not edit by hand (see docs/L5-tiroir.md) -->
<html>
<head>
    <meta charset="utf-8" />
    <meta name=viewport content="width=device-width,initial-scale=1,maximum-scale=1,minimum-scale=1,user-scalable=no,minimal-ui,viewport-fit=cover">
    <meta name=apple-mobile-web-app-capable content=yes>
    <meta name=apple-mobile-web-app-status-bar-style content=black>
    <title>ZV Elite - donnée du jeu</title>
    <link rel="stylesheet" href="../sdpi.css">
    <style>
        .generic-info {{ font-size: 9pt; opacity: 0.85; line-height: 1.3; }}
        .generic-hover {{ height: 3.9em; overflow: hidden; }} /* fixed height: hovering must never move the list (docs/L7-retours-d6.md, D7) */
        .generic-warning {{ color: #f0ad4e; opacity: 1; }}
        .generic-hint {{ font-size: 8pt; opacity: 0.7; line-height: 1.3; }}
        .sdpi-item > select.sdpi-item-value {{ width: 0; min-width: 0; flex: 1 1 auto; }} /* long options must not widen the page */
        .generic-rule {{ display: flex; gap: 4px; min-width: 0; box-sizing: border-box; }}
        .generic-rule select {{ flex: 0 0 45%; width: 45%; min-width: 0; box-sizing: border-box; }}
        .generic-rule input {{ flex: 1 1 0; width: 0; min-width: 0; box-sizing: border-box; }}
        select.generic-results {{ -webkit-appearance: listbox; appearance: listbox; background-image: none; height: auto; width: 0; min-width: 0; flex: 1 1 auto; }}
        .generic-info-button {{ text-align: left; cursor: pointer; }}
        .generic-info-panel {{ font-size: 9pt; line-height: 1.35; white-space: normal; }}
        .generic-info-panel div {{ margin-bottom: 4px; }}
        .generic-info-panel b {{ opacity: 0.8; }}
        .generic-clickable {{ cursor: pointer; }}
        input.generic-hotkey {{ cursor: pointer; }}
        input.generic-hotkey:focus {{ outline: 1px solid #f0ad4e; }}
    </style>
    <script src="../sdtools.common.js"></script>
    <script src="../catalog.js"></script>
    <script src="../commands.js"></script>
    <script src="../generic.js"></script>
</head>
<body>
<div class="sdpi-wrapper">

    <!-- "Donnee" key (com.mhwlng.elite.value): every section below the view selector belongs to the edited view -->
    <div data-actions="value">
        <div class="sdpi-heading">Vue éditée</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Vue</div>
            <select class="sdpi-item-value select" id="genericView" onchange="genericViewChanged()">
                <option value="1">Vue 1 (principale)</option>
                <option value="2">Vue 2</option>
                <option value="3">Vue 3</option>
                <option value="4">Vue 4</option>
            </select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Chaque vue a sa donnée, son texte, son icône et sa commande. Une vue sans icône propre reprend l'icône de la vue 1. Le geste (réglages de la touche, en bas) fait passer d'une vue à l'autre.</div>
        </div>

        <div class="sdpi-heading">Donnée du jeu</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Catégorie</div>
            <select class="sdpi-item-value select" id="genericCategory" onchange="genericCategoryChanged()"></select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Recherche</div>
            <input class="sdpi-item-value" id="genericSearch" type="text" placeholder="ex. carburant, train, fuel" oninput="genericSearchChanged()">
        </div>
        <div class="sdpi-item" id="genericSearchInfoRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint" id="genericSearchInfo"></div>
        </div>
        <div class="sdpi-item" id="genericResultsRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <select class="sdpi-item-value generic-results" id="genericResults" size="8" onchange="genericResultChosen()" onclick="genericResultChosen()"></select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Donnée</div>
            <select class="sdpi-item-value select" id="genericField" onchange="genericFieldChanged()"></select>
        </div>
{keys}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Clé = identifiant de la donnée, remplie par le menu. À taper seulement pour une donnée absente du menu.</div>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="genericKeyInfo"></div>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <button class="sdpi-item-value generic-info-button" id="genericInfoButton" onclick="genericToggleInfo()">ⓘ Que signifie cette donnée ?</button>
        </div>
        <div class="sdpi-item" id="genericInfoRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info-panel" id="genericInfoPanel"></div>
        </div>

        <div class="sdpi-heading">Texte</div>
{texts}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">\\n dans le préfixe ou le suffixe = retour à la ligne. Taille du texte : icône « T » du logiciel Stream Deck.</div>
        </div>

        <div class="sdpi-heading">Icône</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Règles testées dans l'ordre sur la donnée de la vue (après facteur et décalage) : la première vraie choisit l'image. Sinon, ou si la donnée est absente : image par défaut. Clic sur un libellé d'image = l'effacer.</div>
        </div>
{icons}
        <datalist id="genericEnumValues"></datalist>

        <div class="sdpi-heading">Action (appui)</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Recherche</div>
            <input class="sdpi-item-value" id="genericCommandSearch" type="text" placeholder="ex. train, phares, carte, groupe" oninput="genericCommandSearchChanged()">
        </div>
        <div class="sdpi-item" id="genericCommandSearchInfoRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint" id="genericCommandSearchInfo"></div>
        </div>
        <div class="sdpi-item" id="genericCommandResultsRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <select class="sdpi-item-value generic-results" id="genericCommandResults" size="8" onchange="genericCommandResultChosen()" onclick="genericCommandResultChosen()" onmouseover="genericCommandHover(event)" onmouseleave="genericCommandHover(null)"></select>
        </div>
        <div class="sdpi-item" id="genericCommandHoverRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info generic-hover" id="genericCommandHover"></div>
        </div>
{commands}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="genericCommandInfo"></div>
        </div>
{presses}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Commande : utilise ta touche clavier du jeu (liaison clavier nécessaire), en un appui bref. Raccourci : clic dans le champ, puis appuie sur la combinaison (ex. Ctrl+Maj+F5) ; envoyé à la fenêtre active après la commande. Clic sur le libellé « Raccourci » = l'effacer.</div>
        </div>

        <div class="sdpi-heading">Réglages de la touche</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Geste</div>
            <select class="sdpi-item-value select sdProperty" id="pressMode" onchange="setSettings()">
                <option value="shortActLongView">Court : action · long : vue</option>
                <option value="shortViewLongAct">Court : vue · long : action</option>
            </select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Si vide</div>
            <input class="sdpi-item-value sdProperty" id="emptyText" type="text" placeholder="—" oninput="setSettings()">
        </div>
    </div>

</div>
</body>
</html>
'''


def settings_block():
    lines = []

    def prop(json_name, cs_type, default, filename=False):
        name = json_name[0].upper() + json_name[1:]
        if filename:
            lines.append("            [FilenameProperty]")
        init = "" if default is None else f" = {default};"
        lines.append(f'            [JsonProperty(PropertyName = "{json_name}")] public {cs_type} {name} {{ get; set; }}{init}')

    for v in range(2, VIEWS + 1):
        for base, default in (("source", '""'), ("prefix", '""'), ("suffix", '""'), ("decimals", '"0"'), ("scale", '"1"'), ("offset", '"0"')):
            prop(f"{base}{v}", "string", default)
        prop(f"compact{v}", "bool", None)
        prop(f"showText{v}", "bool", "true")
        prop(f"backgroundImage{v}", "string", '""', filename=True)
        for r in range(1, RULES + 1):
            prop(f"rule{r}Op{v}", "string", '""')
            prop(f"rule{r}Value{v}", "string", '""')
            prop(f"rule{r}Image{v}", "string", '""', filename=True)
        prop(f"pressCommand{v}", "string", '""')
        prop(f"pressHotkey{v}", "string", '""')
        prop(f"pressHotkeyText{v}", "string", '""')
        prop(f"clickSound{v}", "string", '""', filename=True)
    return "\n".join(lines)


def main():
    with open(HTML, "w", encoding="utf-8", newline="\n") as f:
        f.write(page())

    with open(ACTION, encoding="utf-8-sig") as f:
        source = f.read()
    start, end = "// <generated-views-2-4>", "// </generated-views-2-4>"
    pattern = re.compile(re.escape(start) + r".*?" + re.escape(end), re.S)
    if not pattern.search(source):
        raise SystemExit("markers not found in " + ACTION)
    source = pattern.sub(lambda m: start + "\n" + settings_block() + "\n            " + end, source)
    with open(ACTION, "w", encoding="utf-8", newline="\n") as f:
        f.write(source)

    print("written:", os.path.relpath(HTML, ROOT), "and", os.path.relpath(ACTION, ROOT))


if __name__ == "__main__":
    main()
