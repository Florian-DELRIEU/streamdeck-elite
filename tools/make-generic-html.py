# Generates the property inspector pages of the generic actions, so that the pages and the C# settings always have
# the same fields:
#   - Elite/PropertyInspector/Elite/Generic.html: "Donnee" action (docs/L5-tiroir.md, whole page: 4 views x ~20 fields)
#   - Elite/Generic/ValueAction.cs: settings of views 2 to 4, between the <generated-views-2-4> markers
#   - Elite/PropertyInspector/Elite/EventAlarm.html: "Alarme" action (docs/L8-alarme.md); its press section reuses the
#     fields of view 1 of "Donnee" (pressCommand, pressHotkey, pressHotkeyText, clickSound)
# Usage (from anywhere): python tools/make-generic-html.py
# Then: run build.ps1 and test.ps1. Keep the JSON names stable: keys already placed in a Stream Deck profile use them.
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HTML = os.path.join(ROOT, "Elite", "PropertyInspector", "Elite", "Generic.html")
ALARM_HTML = os.path.join(ROOT, "Elite", "PropertyInspector", "Elite", "EventAlarm.html")
ACTION = os.path.join(ROOT, "Elite", "Generic", "ValueAction.cs")
GRAPH_HTML = os.path.join(ROOT, "Elite", "PropertyInspector", "Elite", "Graph.html")
GRAPH_ACTION = os.path.join(ROOT, "Elite", "Generic", "GraphAction.cs")
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


def text_block(v, position="middle"):
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
{drawn_text_rows(v, position)}
        </div>'''


POSITIONS = (("top", "en haut"), ("middle", "au milieu"), ("bottom", "en bas"))


# text drawn into the image by the plugin (v3.2, docs/L10-v3.md): shared by "Donnee" and "Graphique"
def drawn_text_rows(v, position="middle"):
    s = sfx(v)
    positions = "\n".join(f'                    <option value="{value}"{" selected" if value == position else ""}>{text}</option>'
                          for value, text in POSITIONS)
    return f'''            <div class="sdpi-item">
                <div class="sdpi-item-label">Dessiner</div>
                <div class="sdpi-item-value">
                    <input id="drawText{s}" class="sdProperty sdCheckbox" type="checkbox" value="" oninput="setSettings()">
                    <label for="drawText{s}" class="sdpi-item-label"><span></span>le texte dans l'image (réglages ci-dessous)</label>
                </div>
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Taille</div>
                <input class="sdpi-item-value sdProperty" id="fontSize{s}" type="number" min="6" max="72" step="1" value="18" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Police</div>
                <input class="sdpi-item-value sdProperty" id="fontName{s}" type="text" value="Rubik" placeholder="Rubik, Tahoma, Arial..." oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Couleur</div>
                <input class="sdpi-item-value sdProperty generic-color" id="fontColor{s}" type="color" value="#d18105" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Gras</div>
                <div class="sdpi-item-value">
                    <input id="fontBold{s}" class="sdProperty sdCheckbox" type="checkbox" value="" oninput="setSettings()">
                    <label for="fontBold{s}" class="sdpi-item-label"><span></span>texte en gras</label>
                </div>
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Position</div>
                <select class="sdpi-item-value select sdProperty" id="textPosition{s}" onchange="setSettings()">
{positions}
                </select>
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Lignes</div>
                <div class="sdpi-item-value">
                    <input id="textWrap{s}" class="sdProperty sdCheckbox" type="checkbox" value="" oninput="setSettings()" checked>
                    <label for="textWrap{s}" class="sdpi-item-label"><span></span>retour à la ligne automatique</label>
                </div>
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Ajuster</div>
                <div class="sdpi-item-value">
                    <input id="textFit{s}" class="sdProperty sdCheckbox" type="checkbox" value="" oninput="setSettings()" checked>
                    <label for="textFit{s}" class="sdpi-item-label"><span></span>réduire la taille pour que tout tienne</label>
                </div>
            </div>'''


# C# settings of drawn_text_rows (view 1 is written by hand in the action, views 2 to 4 are generated)
def drawn_text_props(prop, v, position="middle"):
    s = "" if v == 1 else str(v)
    prop(f"drawText{s}", "bool", None)
    prop(f"fontSize{s}", "string", '"18"')
    prop(f"fontName{s}", "string", "TextStyle.DefaultFont")
    prop(f"fontColor{s}", "string", "TextStyle.DefaultColor")
    prop(f"fontBold{s}", "bool", None)
    prop(f"textPosition{s}", "string", f'"{position}"')
    prop(f"textWrap{s}", "bool", "true")
    prop(f"textFit{s}", "bool", "true")


# tests of Elite/Generic/Condition.cs (image rules of "Donnee", filter of "Alarme")
OPERATORS = [("", "— aucune —"), ("isTrue", "est vrai / oui"), ("isFalse", "est faux / non"), ("equals", "="),
             ("notEquals", "≠"), ("lt", "&lt;"), ("lte", "≤"), ("gt", "&gt;"), ("gte", "≥")]


def operator_options(indent):
    return "\n".join(f'{indent}<option value="{value}">{text}</option>' for value, text in OPERATORS)


def rule_rows(v, r):
    s = sfx(v)
    return f'''            <div class="sdpi-item">
                <div class="sdpi-item-label">Règle {r}</div>
                <div class="sdpi-item-value generic-rule">
                    <select class="select sdProperty generic-rule-op" id="rule{r}Op{s}" onchange="setSettings()">
{operator_options(" " * 24)}
                    </select>
                    <input class="sdProperty" id="rule{r}Value{s}" type="text" list="genericEnumValues" placeholder="valeur" oninput="setSettings()">
                </div>
            </div>
{and_rows("rule", v, r)}
{file_picker(f"rule{r}Image{s}", f"Image {r}", IMAGES)}'''


# v3.5 (docs/L10-v3.md): own data of a rule and second condition linked by AND, folded by default
def and_rows(prefix, v, r):
    s = sfx(v)
    return f'''            <details class="generic-and">
                <summary>règle {r} : autre donnée, condition ET</summary>
                <div class="sdpi-item">
                    <div class="sdpi-item-label">Donnée</div>
                    <input class="sdpi-item-value sdProperty generic-rule-key" id="{prefix}{r}Key{s}" type="text" placeholder="vide = donnée de la vue" oninput="genericRuleKeyTyped()">
                </div>
                <div class="sdpi-item">
                    <div class="sdpi-item-label">ET</div>
                    <div class="sdpi-item-value generic-rule">
                        <select class="select sdProperty" id="{prefix}{r}AndOp{s}" onchange="setSettings()">
{operator_options(" " * 28)}
                        </select>
                        <input class="sdProperty" id="{prefix}{r}AndValue{s}" type="text" placeholder="valeur" oninput="setSettings()">
                    </div>
                </div>
                <div class="sdpi-item">
                    <div class="sdpi-item-label">Donnée ET</div>
                    <input class="sdpi-item-value sdProperty" id="{prefix}{r}AndKey{s}" type="text" placeholder="vide = même donnée" oninput="setSettings()">
                </div>
            </details>'''


def and_props(prop, prefix, v, r):
    s = "" if v == 1 else str(v)
    prop(f"{prefix}{r}Key{s}", "string", '""')
    prop(f"{prefix}{r}AndOp{s}", "string", '""')
    prop(f"{prefix}{r}AndValue{s}", "string", '""')
    prop(f"{prefix}{r}AndKey{s}", "string", '""')


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
def press_block(v, sound_label="Son"):
    s = sfx(v)
    return f'''        <div data-view="{v}">
            <div class="sdpi-item">
                <div class="sdpi-item-label generic-clickable" onclick="genericHotkeyClear({v})" title="clic = effacer le raccourci">Raccourci</div>
                <input class="sdpi-item-value sdProperty generic-hotkey" id="pressHotkeyText{s}" type="text" readonly placeholder="clic ici, puis appuie sur les touches" onfocus="genericHotkeyFocus({v})" onblur="genericHotkeyBlur({v})" onkeydown="genericHotkeyKeyDown(event, {v})" onkeyup="genericHotkeyKeyUp(event, {v})">
                <input class="sdProperty" id="pressHotkey{s}" type="hidden">
            </div>
{file_picker(f"clickSound{s}", sound_label, ".wav")}
        </div>'''


def head(title, doc, scripts):
    script_tags = "\n".join(f'    <script src="../{name}"></script>' for name in scripts)
    return f'''<!DOCTYPE html>
<!-- Generated by tools/make-generic-html.py - do not edit by hand (see {doc}) -->
<html>
<head>
    <meta charset="utf-8" />
    <meta name=viewport content="width=device-width,initial-scale=1,maximum-scale=1,minimum-scale=1,user-scalable=no,minimal-ui,viewport-fit=cover">
    <meta name=apple-mobile-web-app-capable content=yes>
    <meta name=apple-mobile-web-app-status-bar-style content=black>
    <title>{title}</title>
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
        .generic-rule input.generic-rule-color {{ flex: 0 0 30px; width: 30px; padding: 0; }}
        details.generic-and {{ margin: -2px 0 4px 0; }}
        details.generic-and > summary {{ font-size: 8pt; opacity: 0.7; cursor: pointer; margin-left: 110px; }}
        select.generic-results {{ -webkit-appearance: listbox; appearance: listbox; background-image: none; height: auto; width: 0; min-width: 0; flex: 1 1 auto; }}
        .generic-info-button {{ text-align: left; cursor: pointer; }}
        .generic-info-panel {{ font-size: 9pt; line-height: 1.35; white-space: normal; }}
        .generic-info-panel div {{ margin-bottom: 4px; }}
        .generic-info-panel b {{ opacity: 0.8; }}
        .generic-clickable {{ cursor: pointer; }}
        input.generic-hotkey {{ cursor: pointer; }}
        input.generic-hotkey:focus {{ outline: 1px solid #f0ad4e; }}
    </style>
{script_tags}
</head>
<body>
<div class="sdpi-wrapper">
'''


# command search (commands.js): shared by both pages, it fills pressCommand of the edited view (view 1 on the alarm)
def command_search_block():
    return '''        <div class="sdpi-item">
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
        </div>'''


PRESS_HINT = '''        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Commande : utilise ta touche clavier du jeu (liaison clavier nécessaire), en un appui bref. Raccourci : clic dans le champ, puis appuie sur la combinaison (ex. Ctrl+Maj+F5) ; envoyé à la fenêtre active après la commande. Clic sur le libellé « Raccourci » = l'effacer.</div>
        </div>'''


def icon_section():
    icons = "\n".join(icon_block(v) for v in range(1, VIEWS + 1))
    return f'''        <div class="sdpi-heading">Icône</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Règles testées dans l'ordre sur la donnée de la vue (après facteur et décalage) : la première vraie choisit l'image. Sinon, ou si la donnée est absente : image par défaut. Clic sur un libellé d'image = l'effacer.</div>
        </div>
{icons}'''


GRAPH_TYPES = (("hbar", "barre horizontale"), ("vbar", "barre verticale"), ("dial", "cadran"), ("curve", "courbe (historique)"))


def color_rule_row(v, r):
    s = sfx(v)
    return f'''            <div class="sdpi-item">
                <div class="sdpi-item-label">Couleur {r}</div>
                <div class="sdpi-item-value generic-rule">
                    <select class="select sdProperty generic-rule-op" id="colorRule{r}Op{s}" onchange="setSettings()">
{operator_options(" " * 24)}
                    </select>
                    <input class="sdProperty" id="colorRule{r}Value{s}" type="text" list="genericEnumValues" placeholder="valeur" oninput="setSettings()">
                    <input class="sdProperty generic-rule-color" id="colorRule{r}Color{s}" type="color" value="#ff4646" oninput="setSettings()">
                </div>
            </div>
{and_rows("colorRule", v, r)}'''


# graph of a view of the "Graphique" key (v3.4, docs/L10-v3.md)
def graph_block(v):
    s = sfx(v)
    types = "\n".join(f'                    <option value="{value}"{" selected" if value == "hbar" else ""}>{text}</option>' for value, text in GRAPH_TYPES)
    rules = "\n".join(color_rule_row(v, r) for r in range(1, RULES + 1))
    return f'''        <div data-view="{v}">
            <div class="sdpi-item">
                <div class="sdpi-item-label">Type</div>
                <select class="sdpi-item-value select sdProperty" id="graphType{s}" onchange="setSettings()">
{types}
                </select>
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Minimum</div>
                <input class="sdpi-item-value sdProperty" id="graphMin{s}" type="text" value="0" placeholder="0, ou une clé" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Maximum</div>
                <input class="sdpi-item-value sdProperty" id="graphMax{s}" type="text" value="100" placeholder="100, ou une clé : ship.FuelCapacity.Main" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Couleur</div>
                <input class="sdpi-item-value sdProperty generic-color" id="graphColor{s}" type="color" value="#a7d7d6" oninput="setSettings()">
            </div>
{rules}
            <div class="sdpi-item">
                <div class="sdpi-item-label">Points</div>
                <input class="sdpi-item-value sdProperty" id="curvePoints{s}" type="number" min="5" max="240" step="1" value="60" oninput="setSettings()">
            </div>
            <div class="sdpi-item">
                <div class="sdpi-item-label">Toutes les</div>
                <input class="sdpi-item-value sdProperty" id="curveInterval{s}" type="number" min="1" max="3600" step="1" value="5" oninput="setSettings()">
            </div>
        </div>'''


def graph_section():
    graphs = "\n".join(graph_block(v) for v in range(1, VIEWS + 1))
    return f'''        <div class="sdpi-heading">Graphique</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Le plugin dessine la donnée de la vue (après facteur et décalage) entre le minimum et le maximum : un nombre, ou une clé (même facteur et décalage). Couleurs : la première règle vraie, sinon « Couleur ». Courbe : historique de la donnée, « Points » valeurs relevées « Toutes les » N secondes, même touche non affichée ; minimum ou maximum vide = automatique.</div>
        </div>
{graphs}'''


def page(kind="value"):
    graph = kind == "graph"
    keys = "\n".join(key_block(v) for v in range(1, VIEWS + 1))
    texts = "\n".join(text_block(v, "top" if graph else "middle") for v in range(1, VIEWS + 1))
    visuals = graph_section() if graph else icon_section()
    commands = "\n".join(command_block(v) for v in range(1, VIEWS + 1))
    presses = "\n".join(press_block(v) for v in range(1, VIEWS + 1))
    if graph:
        title, doc = "ZV Elite - graphique", "docs/L10-v3.md"
        comment = '"Graphique" key (com.mhwlng.elite.graph)'
        view_hint = "Chaque vue a sa donnée, son texte, son graphique et sa commande. Le geste (réglages de la touche, en bas) fait passer d'une vue à l'autre."
    else:
        title, doc = "ZV Elite - donnée du jeu", "docs/L5-tiroir.md"
        comment = '"Donnee" key (com.mhwlng.elite.value)'
        view_hint = "Chaque vue a sa donnée, son texte, son icône et sa commande. Une vue sans icône propre reprend l'icône de la vue 1. Le geste (réglages de la touche, en bas) fait passer d'une vue à l'autre."
    return head(title, doc, ["sdtools.common.js", "catalog.js", "commands.js", "generic.js"]) + f'''
    <!-- {comment}: every section below the view selector belongs to the edited view -->
    <div data-actions="{kind}">
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
            <div class="sdpi-item-value generic-hint">{view_hint}</div>
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
            <div class="sdpi-item-value generic-hint">\\n dans le préfixe ou le suffixe = retour à la ligne. Sans « Dessiner » : texte du logiciel Stream Deck (taille : icône « T », 18 au plus). Avec « Dessiner » : le plugin écrit la valeur dans l'image, taille libre en pixels (touche de 72), retour à la ligne et réduction automatiques ; le titre du logiciel est alors vidé.</div>
        </div>

{visuals}
        <datalist id="genericEnumValues"></datalist>

        <div class="sdpi-heading">Action (appui)</div>
{command_search_block()}
{commands}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="genericCommandInfo"></div>
        </div>
{presses}
{PRESS_HINT}

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


def alarm_page():
    return head("ZV Elite - alarme", "docs/L8-alarme.md",
                ["sdtools.common.js", "catalog.js", "commands.js", "generic.js", "alarm.js"]) + f'''
    <!-- "Alarme" key (com.mhwlng.elite.eventalarm): alert when a journal event is written live -->
    <div data-actions="alarm">
        <div class="sdpi-heading">Événement du journal</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Catégorie</div>
            <select class="sdpi-item-value select" id="alarmCategory" onchange="alarmCategoryChanged()"></select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Recherche</div>
            <input class="sdpi-item-value" id="alarmSearch" type="text" placeholder="ex. attaque, interdiction, surchauffe" oninput="alarmSearchChanged()">
        </div>
        <div class="sdpi-item" id="alarmSearchInfoRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint" id="alarmSearchInfo"></div>
        </div>
        <div class="sdpi-item" id="alarmResultsRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <select class="sdpi-item-value generic-results" id="alarmResults" size="8" onchange="alarmResultChosen()" onclick="alarmResultChosen()"></select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Événement</div>
            <select class="sdpi-item-value select" id="alarmEventList" onchange="alarmEventListChanged()"></select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Nom (avancé)</div>
            <input class="sdpi-item-value sdProperty" id="event" type="text" placeholder="ex. UnderAttack" oninput="alarmEventTyped()">
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Nom = identifiant de l'événement, rempli par le menu. À taper seulement pour un événement absent du menu (nom exact écrit par le jeu).</div>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="alarmEventInfo"></div>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <button class="sdpi-item-value generic-info-button" id="alarmInfoButton" onclick="alarmToggleInfo()">ⓘ Quand cet événement arrive-t-il ?</button>
        </div>
        <div class="sdpi-item" id="alarmInfoRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info-panel" id="alarmInfoPanel"></div>
        </div>

        <div class="sdpi-heading">Filtre (optionnel)</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Champ</div>
            <select class="sdpi-item-value select sdProperty" id="filterField" onchange="alarmFilterFieldChanged()"></select>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Test</div>
            <div class="sdpi-item-value generic-rule">
                <select class="select sdProperty generic-rule-op" id="filterOp" onchange="setSettings()">
{operator_options(" " * 20)}
                </select>
                <input class="sdProperty" id="filterValue" type="text" list="alarmEnumValues" placeholder="valeur" oninput="setSettings()">
            </div>
        </div>
        <datalist id="alarmEnumValues"></datalist>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="alarmFilterInfo"></div>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Sans filtre, chaque événement de ce type déclenche l'alarme. Exemple : UnderAttack, champ Target = You : seulement quand c'est toi qui es attaqué.</div>
        </div>

        <div class="sdpi-heading">Alerte</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label">Durée (s)</div>
            <input class="sdpi-item-value sdProperty" id="duration" type="number" min="0" step="1" placeholder="5" oninput="setSettings()">
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Temps d'affichage de l'image d'alerte ; un nouvel événement relance la durée. 0 = jusqu'à un appui sur la touche.</div>
        </div>
{file_picker("idleImage", "Au repos", IMAGES)}
{file_picker("activeImage", "En alerte", IMAGES)}
{file_picker("alarmSound", "Son d'alerte", ".wav")}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Sans image d'alerte, Stream Deck affiche son triangle ⚠. Les événements relus au lancement du plugin ne déclenchent jamais l'alarme. Clic sur un libellé d'image ou de son = l'effacer.</div>
        </div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <button class="sdpi-item-value generic-info-button" id="alarmTestButton" onclick="alarmTest()">▶ Tester l'alarme</button>
        </div>
        <div class="sdpi-item" id="alarmTestRow" style="display: none">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="alarmTestInfo"></div>
        </div>

        <div class="sdpi-heading">Action (appui)</div>
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-hint">Un appui acquitte l'alerte en cours, puis envoie la commande, le raccourci et le son d'appui.</div>
        </div>
{command_search_block()}
{command_block(1)}
        <div class="sdpi-item">
            <div class="sdpi-item-label empty"></div>
            <div class="sdpi-item-value generic-info" id="genericCommandInfo"></div>
        </div>
{press_block(1, "Son (appui)")}
{PRESS_HINT}
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
            and_props(prop, "rule", v, r)
        prop(f"pressCommand{v}", "string", '""')
        prop(f"pressHotkey{v}", "string", '""')
        prop(f"pressHotkeyText{v}", "string", '""')
        prop(f"clickSound{v}", "string", '""', filename=True)
        drawn_text_props(prop, v)
    return "\n".join(lines)


def property_writer(lines):
    def prop(json_name, cs_type, default, filename=False):
        name = json_name[0].upper() + json_name[1:]
        if filename:
            lines.append("            [FilenameProperty]")
        init = "" if default is None else f" = {default};"
        lines.append(f'            [JsonProperty(PropertyName = "{json_name}")] public {cs_type} {name} {{ get; set; }}{init}')
    return prop


# every setting of Graph.html (the page has no hand-written field): views 1 to 4, then the whole key
def graph_settings_block():
    lines = []
    prop = property_writer(lines)
    for v in range(1, VIEWS + 1):
        s = sfx(v)
        lines.append(f"            // ---- view {v}")
        for base, default in (("source", '""'), ("prefix", '""'), ("suffix", '""'), ("decimals", '"0"'), ("scale", '"1"'), ("offset", '"0"')):
            prop(f"{base}{s}", "string", default)
        prop(f"compact{s}", "bool", None)
        prop(f"showText{s}", "bool", "true")
        drawn_text_props(prop, v, "top")
        prop(f"graphType{s}", "string", '"hbar"')
        prop(f"graphMin{s}", "string", '"0"')
        prop(f"graphMax{s}", "string", '"100"')
        prop(f"graphColor{s}", "string", "GraphConfig.DefaultColor")
        for r in range(1, RULES + 1):
            prop(f"colorRule{r}Op{s}", "string", '""')
            prop(f"colorRule{r}Value{s}", "string", '""')
            prop(f"colorRule{r}Color{s}", "string", "GraphConfig.DefaultRuleColor")
            and_props(prop, "colorRule", v, r)
        prop(f"curvePoints{s}", "string", '"60"')
        prop(f"curveInterval{s}", "string", '"5"')
        prop(f"pressCommand{s}", "string", '""')
        prop(f"pressHotkey{s}", "string", '""')
        prop(f"pressHotkeyText{s}", "string", '""')
        prop(f"clickSound{s}", "string", '""', filename=True)
    lines.append("            // ---- whole key")
    prop("emptyText", "string", "ValueSettings.DefaultEmptyText")
    prop("pressMode", "string", "PressGesture.ShortActLongViewName")
    return "\n".join(lines)


def replace_block(path, start, end, content):
    with open(path, encoding="utf-8-sig") as f:
        source = f.read()
    pattern = re.compile(re.escape(start) + r".*?" + re.escape(end), re.S)
    if not pattern.search(source):
        raise SystemExit("markers not found in " + path)
    source = pattern.sub(lambda m: start + "\n" + content + "\n            " + end, source)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(source)


def main():
    with open(HTML, "w", encoding="utf-8", newline="\n") as f:
        f.write(page())
    replace_block(ACTION, "// <generated-views-2-4>", "// </generated-views-2-4>", settings_block())

    with open(GRAPH_HTML, "w", encoding="utf-8", newline="\n") as f:
        f.write(page("graph"))
    replace_block(GRAPH_ACTION, "// <generated-graph-settings>", "// </generated-graph-settings>", graph_settings_block())

    with open(ALARM_HTML, "w", encoding="utf-8", newline="\n") as f:
        f.write(alarm_page())

    print("written:", ", ".join(os.path.relpath(p, ROOT) for p in (HTML, ACTION, GRAPH_HTML, GRAPH_ACTION, ALARM_HTML)))


if __name__ == "__main__":
    main()
