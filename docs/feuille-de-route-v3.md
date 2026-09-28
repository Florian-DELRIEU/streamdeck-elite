# Feuille de route après la v3.0 — issues #1, #2 et #3

Statut : **réalisé** le 2026-09-28, à la demande de Florian **en une seule étape** (lot L10 : v3.1 à v3.7, 7 commits, un seul test en jeu D10) : voir `docs/L10-v3.md`. Les questions ouvertes du §4 y sont tranchées (police Rubik, 4 types de graphiques, portée chargée faite, touches de groupe de tir du SRV, alarme la plus récente). Correction du §1 : le SRV **a** des groupes de tir (`BuggyCycleFireGroupNext/Previous` dans les réglages du jeu). Le reste de ce document est l'état du 2026-09-28 au matin, pour l'historique.

Statut d'origine : **document de référence** du 2026-09-28. Rien n'est encore commencé. Ce document reprend les issues [#1 TODO](https://github.com/Florian-DELRIEU/streamdeck-elite/issues/1), [#2 Question](https://github.com/Florian-DELRIEU/streamdeck-elite/issues/2) et [#3 BUG REPORT](https://github.com/Florian-DELRIEU/streamdeck-elite/issues/3), mes avis, et les décisions de Florian.

> ## Reprise dans une autre discussion
> - **État :**
>   - L0 à L9 sont terminés ;
>   - la **v3.0** est publiée : tag `V3.0` = commit `0d8bb66` = `feature/generic-api` ;
>   - `manifest.json` indique encore `2.8.0`, à corriger en v3.1.
> - **Prochaine étape : v3.1 (lot L10)**, voir le §3.
>   1. Présenter à Florian un **plan court** fondé sur le §3 ;
>   2. **attendre sa validation** ;
>   3. puis coder.
>
>   Même règle pour chaque version : un lot, un plan validé, des tests, une doc `docs/L1x-*.md` avec le mode d'emploi du test en jeu, la mise à jour du tableau de `CLAUDE.md` et un commit.
> - **À lire d'abord :**
>   - `CLAUDE.md` (règles, conventions, pièges) ;
>   - ce document ;
>   - `docs/L1-socle-donnees.md` (clés du magasin) ;
>   - `docs/L5-tiroir.md` (vues, gestes, page de réglages générée) ;
>   - `docs/L8-alarme.md` (structure d'une action générique, garde `IsLive`).
> - **Icônes :** **ignorées** tant que Florian ne les relance pas. `docs/icones-plan.md` reste en attente.
> - **Session dans le cloud** (conteneur Linux) : compiler et tester comme décrit au §5. Avant chaque push de la branche de session : `git fetch origin feature/generic-api`, puis fusionner (voir `CLAUDE.md`, section Git).
> - Répondre à Florian **en français**.

---

## 1. Points des issues, avis et décisions de Florian

| # | Point | Décision (Florian, 2026-09-28) | Version |
|---|---|---|---|
| #3 | `journal.FSDTarget.RemainingJumpsInRoute` reste à 1 en fin de route | C'est le jeu : `FSDTarget` n'est plus écrit à l'arrivée. On fait **comme le bouton d'origine** (`Route.cs`) : nouvelle donnée calculée `calc.Route.RemainingJumps`. | v3.1 |
| #3 | Libérer le blocage des groupes de tir (au sol, SRV…) | **Libérer quand même** : « si le blocage vient du jeu, ça ne changera rien ». Seulement pour les touches Donnée et Alarme ; `EliteKeys` et le bouton Firegroup d'origine ne changent pas. Effet de bord accepté : en SRV ou à pied, la touche de cycle des groupes du **vaisseau** est pressée (le SRV n'a pas de groupes de tir dans le jeu). | v3.1 |
| #2 | Image dynamique (graphique) | **Action distincte « Graphique »**, construite comme « Donnée » (vues, gestes, commande, raccourci, son, texte). Seule la partie image change : les images et leurs règles sont remplacées par un graphique dynamique. | v3.4 |
| #2 | Garder des valeurs en mémoire | Surtout les **valeurs caractéristiques du vaisseau** : capacité de la soute, capacité du réservoir, portée de saut, coque (bouclier : voir plus bas). **Sauvegarde dans un fichier.** | v3.3 |
| #2 | Image selon plusieurs conditions ET/OU | Le OU existe déjà (plusieurs règles). On ajoute le **ET** et une donnée propre à chaque règle. | v3.5 |
| #1 | Police plus grande que 18 | Le plugin **dessine le texte dans l'image**, à chaque changement de valeur, avec une taille libre. | v3.2 |
| #1 | Retour à la ligne (coordonnées longues…) | Avec le texte dessiné : **retour à la ligne automatique** calculé sur la largeur de la touche, coupure après `,` `.` `-` pour les valeurs sans espace, et réduction de la police pour tenir. | v3.2 |
| #1 | Plusieurs alarmes dans une touche | **Seule l'alarme active est contrôlable** : elle seule s'affiche et réagit à l'appui ; les autres alarmes de la touche n'ont aucun effet. | v3.6 |
| #1 | Touche « action » qui ouvre un dossier | Florian voudrait des touches de dossier aux images dynamiques. Un **vrai dossier est impossible** (SDK). Seule voie : les **pages** d'un profil fourni avec le plugin. **Essai d'abord.** | v3.7 |
| #1 | Icônes (PowerPoint) | **Ignorées pour l'instant.** | — |

**Limites du jeu à connaître :**
- **Bouclier** : le jeu n'écrit **aucune valeur** de bouclier, seulement levé/tombé (`status.Flags.ShieldsUp`, événement `ShieldState`). Un pourcentage de bouclier est donc impossible.
- **Portée de saut** : `Loadout.MaxJumpRange` est la portée **soute vide**, avec juste assez de carburant pour un saut. La portée « chargée » n'est pas donnée par le jeu. Elle se calcule à partir des modules (formule du FSD), mais c'est une option **à étudier** (v3.3).

## 2. Faits vérifiés dans le code (2026-09-28)

- **Sauts restants** : `Route.cs` et `Hyperspace.cs` cachent le nombre de sauts quand `EliteData.StarSystem == EliteData.FsdTargetName`. `EliteData` remet `RemainingJumpsInRoute` à 0 sur `NavRouteClear`.
- **Blocage des groupes de tir** : `EliteKeys.HandleFireGroup` bloque à pied, en SRV, à quai, posé, train sorti et pendant un saut. Il fait `cycle = cible − EliteData.StatusData.Firegroup`, puis appelle `StreamDeckCommon.SendKeypress(Program.Binding[BindingType.Ship].CycleFireGroupNext/Previous)` avec 70 ms entre deux appuis. `StreamDeckCommon.SendKeypress(StandardBindingInfo)` et `Program.Binding` sont **publics**.
- **Liaisons SRV et à pied** : `UserBindings.cs` n'a aucune liaison de groupe de tir pour le SRV (`BuggyPrimaryFireButton`, `BuggySecondaryFireButton`, `ToggleBuggyTurretButton`…). À pied : `HumanoidSelectPrimaryWeaponButton`, `…Secondary…`, `…Next/PreviousWeaponButton` (déjà proposées dans les commandes).
- **`Loadout`** (`EliteJournalReader/Events/LoadoutEvent.cs`) : `Ship`, `ShipID`, `ShipName`, `HullHealth`, `UnladenMass`, `FuelCapacity.Main/Reserve`, `CargoCapacity`, `MaxJumpRange`, `Modules`.
- **Dessin dans l'image** : `Route.cs`, `Hyperspace.cs`, `Power.cs`, `Limpet.cs` et `Firegroup.cs` dessinent déjà du texte (System.Drawing, `Graphics.DrawString`, taille ajustée par `MeasureString`).
- **Profils fournis** : le plugin sait déjà les gérer. `manifest.json` `"Profiles"` (`Name`, `DeviceType`, `ReadOnly`, `DontAutoSwitchWhenInstalled`), `Profile.cs`, `StreamDeckCommon` → `Connection.SwitchProfileAsync(nom)`.
- **SDK Elgato** : `switchToProfile(profil, page)` ne vise **que** les profils fournis avec le plugin et déclarés dans le manifeste. Il n'existe aucune API pour ouvrir un dossier ([Profiles](https://docs.elgato.com/streamdeck/sdk/guides/profiles/), [WebSocket plugin](https://docs.elgato.com/streamdeck/sdk/references/websocket/plugin/)).
- **Taille du titre** : `setTitle` n'envoie que le texte. La taille du titre se règle dans le logiciel Stream Deck, qui la plafonne.

## 3. Feuille de route (une version = un lot)

**Règles communes :**
- **noms de réglages uniquement ajoutés**, jamais renommés : une touche existante doit rester lisible ;
- **nouveau UUID** seulement pour l'action « Graphique » : `com.mhwlng.elite.graph`, définitif dès la première touche posée ;
- nouvelles conventions de clés, à documenter dans `docs/L1-socle-donnees.md`, avec description en français et entrée dans `catalog.js` :
  - `calc.*` : données **calculées** par le plugin ;
  - `ship.*` : valeurs **mémorisées** du vaisseau actuel ;
- attention au suffixe de vue : `rule1Op2` = règle 1 de la **vue 2**. Tout nouveau nom de réglage doit rester sans ambiguïté.

### v3.1 (L10) — correctifs (#3)
1. **`calc.Route.RemainingJumps`** :
   - **fonction pure** (par exemple `Elite/Generic/DerivedKeys.cs`) calculée par `EliteStore` après `FSDTarget`, `FSDJump`, `Location`, `CarrierJump` et `NavRouteClear`. Source `calc`, `DataChanged` émis seulement si la valeur change ;
   - règle, **comme `Route.cs`** :
     - **0** si le système actuel (le plus récent de `FSDJump`, `Location`, `CarrierJump` : `StarSystem`) est égal à `FSDTarget.Name` ;
     - **0** si `NavRouteClear` est plus récent que le dernier `FSDTarget` ;
     - sinon `FSDTarget.RemainingJumpsInRoute`.
   - catalogue : catégorie Déplacement, description en français ;
   - tests avec des lignes synthétiques : route de 3 sauts, arrivée, effacement de la route.
2. **Groupes de tir libérés** :
   - `Elite/Generic/FireGroupSender.cs` refait le même cycle que `HandleFireGroup`, **sans** condition de blocage ;
   - `ValueAction` et `EventAlarmAction` l'utilisent pour `FireGroup-A…H` ;
   - `CommandGuard` n'est plus utilisé : on le retire, avec ses tests, ou on l'adapte ;
   - `Elite.CatalogGen/commands-fr.json` : retirer des descriptions `FireGroup-*` les remarques « sans effet à quai / train sorti » ;
   - consigner la décision dans `CLAUDE.md` et dans `docs/L7-retours-d6.md`.
3. **Version** : `manifest.json` → `3.1.0` ; `ManifestTests` mis à jour.

**Test en jeu :**
- sauts restants au départ, pendant la route, à l'arrivée (0) et après effacement de la route ;
- groupe de tir train sorti et posé ;
- en SRV, observer ce que fait la touche ;
- non-régression.

### v3.2 (L11) — texte dessiné dans « Donnée » (#1)
- **Moteur `Elite/Generic/TextRenderer.cs`** (System.Drawing), fonctions testables :
  - mesure du texte et **découpe** : aux espaces, puis après `,` `.` `-`, puis à n'importe quel caractère ;
  - **réduction** de la police jusqu'à une taille minimale si l'option est active ;
  - rendu **144×144** (touche MK.2 de 72×72 en double densité) **par-dessus l'image choisie** par les règles, ou sur un fond noir ;
  - PNG, puis base64, puis `SetImageAsync`, **seulement si le rendu change**.
- **Police** : Rubik, la police du pack (fichier `.ttf` sous licence OFL, livré dans le plugin, chargé par `PrivateFontCollection`), avec Arial en secours.
- **Réglages ajoutés, par vue** (suffixe de vue comme les autres) : `drawText{v}` (faux par défaut = titre du logiciel Stream Deck, comme aujourd'hui), `fontSize{v}`, `fontName{v}`, `fontColor{v}`, `fontBold{v}`, `textPosition{v}` (haut, milieu, bas), `textWrap{v}`, `textFit{v}`.
- **Page de réglages** : section « Texte » complétée, via `tools/make-generic-html.py`.
- **Tests** :
  - découpe d'une coordonnée longue ;
  - réduction ;
  - aucune ligne ne déborde ;
  - `InspectorFieldsTests`, et compatibilité des touches existantes.

### v3.3 (L12) — mémoire du vaisseau et historique (#2)
- **`ShipMemory`** :
  - à chaque `Loadout` (en direct **et** relu au lancement), enregistre par `ShipID` : `Ship`, `ShipName`, `CargoCapacity`, `FuelCapacity.Main/Reserve`, `MaxJumpRange`, `HullHealth`, `UnladenMass` ;
  - fichier `%APPDATA%\ZV Stream Deck Elite\memoire.json`, hors du dossier du plugin pour survivre à une réinstallation ; écriture atomique ; rechargé au démarrage ;
  - expose les clés `ship.*` du **vaisseau actuel** (dernier `Loadout`, `ShipyardSwap`, `LoadGame`).
- **Clés calculées** : `calc.Fuel.Percent`, `calc.Cargo.Percent`, `calc.Hull.Percent`. Pour la coque : la plus récente des valeurs parmi `HullDamage.Health` et `Loadout.HullHealth`, et 1 après `RepairAll`. **Pas de bouclier** (le jeu ne le donne pas).
- **Historique** : des tampons circulaires (par exemple 120 points), **seulement pour les clés affichées par une courbe** (v3.4), sauvegardés dans le même fichier.
- **À étudier, en option** : la portée de saut « chargée », calculée à partir de `Loadout.Modules` (FSD : masse optimale, carburant par saut, ingénierie).

### v3.4 (L13) — action « Graphique » (`com.mhwlng.elite.graph`) (#2)
- **Même structure que « Donnée »** : 4 vues, gestes court/long, commande, raccourci, son, texte dessiné de v3.2, page générée par `tools/make-generic-html.py`.
- **La section « Icône » est remplacée par « Graphique »** :
  - `graphType{v}` : barre horizontale, barre verticale, cadran, courbe ;
  - `graphMin{v}` et `graphMax{v}` : un nombre ou une clé, par exemple `ship.FuelCapacity.Main` pour une jauge de carburant à l'échelle du vaisseau ;
  - couleurs par règles : `colorRule{r}Op{v}`, `colorRule{r}Value{v}`, `colorRule{r}Color{v}`, avec la palette du pack par défaut ;
  - pour la courbe : `curvePoints{v}` et `curveInterval{v}`.
- **Rendu** : 144×144 avec le moteur de v3.2, limité en fréquence.

### v3.5 (L14) — règles ET/OU (#2)
- **Réglages ajoutés**, dans « Donnée » et dans les règles de couleur de « Graphique » :
  - `rule{r}Key{v}` : donnée testée par la règle ; vide = celle de la vue ;
  - `rule{r}AndKey{v}`, `rule{r}AndOp{v}`, `rule{r}AndValue{v}` : 2ᵉ condition, en **ET**.
- Le **OU** reste : plusieurs règles, et la première vraie gagne.
- **Rafraîchissement** : la touche se redessine aussi quand une clé utilisée par une de ses règles change.

### v3.6 (L15) — alarmes multiples (#1)
- **Réglages** : jusqu'à 4 alarmes par touche. L'alarme 1 garde les noms actuels ; les alarmes 2 à 4 prennent les mêmes noms suivis de leur numéro (`event2`, `filterField2`, `filterOp2`, `filterValue2`, `duration2`, `activeImage2`, `alarmSound2`, `pressCommand2`…).
- **Comportement voulu par Florian** : seule l'alarme **active** s'affiche et réagit à l'appui. L'appui l'acquitte et envoie **sa** commande ; les autres alarmes n'ont aucun effet.
- **À confirmer au début de v3.6 :**
  - si plusieurs alarmes sont actives, **la plus récente** s'affiche, et les autres reprennent la main quand elle s'éteint ;
  - au repos : image de repos et commande de l'alarme 1 (comportement actuel, pour que les touches existantes ne changent pas).

### v3.7 (L16) — pages au lieu de dossiers (#1)
- **Essai d'abord**, avant tout développement :
  1. un profil « ZV Elite » (MK.2, 2 pages) fourni dans `manifest.json` `Profiles`, avec `ReadOnly: false` et `DontAutoSwitchWhenInstalled: true` ;
  2. une commande de test « aller à la page N » (`SwitchProfileAsync` avec page, si le SDK C# le permet ; sinon un message WebSocket `switchToProfile` brut) ;
  3. Florian vérifie qu'il peut installer le profil, le **modifier**, y **coller ses touches** (natives comprises), et que la commande change bien de page.
- **Si l'essai réussit** :
  - commandes « page N », « page précédente » et « retour au profil précédent » pour Donnée, Graphique et Alarme ;
  - un mode d'emploi pour remplacer ses dossiers par des pages : nos touches y gardent leurs images dynamiques et leurs graphiques.
- **Sinon** : abandon, on garde les dossiers natifs.

## 4. Questions ouvertes (à trancher au début de la version concernée)

- **v3.2** : taille de police en pixels de la touche (72) ou en points ; couleur par défaut (l'ambre du pack, `#d18105`) ; position par défaut.
- **v3.3** : taille de l'historique ; faut-il faire la portée de saut chargée ?
- **v3.4** : liste exacte des types de graphiques pour la première version (barre et courbe d'abord ?).
- **v3.6** : comportement si plusieurs alarmes sont actives (voir plus haut).
- **v3.7** : résultat de l'essai.

## 5. Compiler et tester dans le cloud (session Claude Code sur Linux)

Cette vérification ne remplace **pas** `build.ps1` / `test.ps1` sous Windows, chez Florian.

1. **Installer Mono** une fois : `apt-get update && apt-get install -y mono-complete`.
2. **Compiler et tester** : `bash tools/cloud/cloudtest.sh`. Le script :
   - lance `tools/cloud/cloudbuild.py`, qui restaure les paquets NuGet de chaque `packages.config` et Roslyn 4.8 depuis nuget.org, puis compile les 5 projets en C# 7.3 et lance `Elite.CatalogGen` ;
   - lance ensuite les tests NUnit sous Mono.

   **Attendu** : `== CLOUD BUILD OK`, puis `Overall result: Passed` (126 tests en v3.0). Pour filtrer : `bash tools/cloud/cloudtest.sh --where "class =~ Alarm"`.
3. **Pièges déjà réglés dans les scripts** :
   - `/codepage:65001`, sinon Roslyn plante sous Mono (`GetCPInfoExW`) ;
   - `System.Core` est ajouté implicitement, comme le fait MSBuild ;
   - le générateur écrit ses fichiers en CRLF : si seul le format des fins de ligne change, ils sont restaurés ;
   - `ManifestTests` et `InspectorFieldsTests` construisent un chemin Windows (`..\..\..\Elite\bin\Debug\...`) : un lien symbolique de ce nom exact est créé dans `Elite.Tests/bin/Debug`.
4. **Pages de réglages** :
   - les servir avec `python3 -m http.server 8765 --bind 127.0.0.1` depuis `Elite/PropertyInspector` ;
   - les tester avec Playwright (Node : `NODE_PATH=$(npm root -g) node script.js`, Chromium préinstallé) ;
   - toujours **frapper lettre par lettre** ;
   - simuler le logiciel Stream Deck : `window.actionInfo = { action: '<uuid>' }`, `loadConfiguration({...})`, et intercepter `setSettingsToPlugin`.
5. **`generic.js` et `alarm.js` doivent rester en ASCII.** L'outil d'écriture, et même un heredoc bash, transforment les échappements en caractères. Après écriture, repasser un petit script Python qui remplace chaque caractère non ASCII par son échappement.
6. **Scripts PowerShell** (`pack.ps1`…) : télécharger PowerShell 7 (`powershell-7.x-linux-x64.tar.gz` depuis GitHub) pour vérifier leur syntaxe et leur logique. Rester compatible avec Windows PowerShell 5.1 : pas de `?:` ni de `??`.
