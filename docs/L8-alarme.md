# L8 — Action « Alarme »

Statut : **terminé** le 2026-09-25 (code), fait dans la même session que L9 à la demande de Florian. Il n'y a qu'un seul test en jeu pour L7b, L8 et L9 : **D8**, décrit dans `docs/L9-finalisation.md`.

## Pourquoi

Le cahier des charges (§5.3) prévoit une troisième action générique : une touche qui **passe en alerte quand un événement du Journal survient**, pendant une durée réglable. L'action État du §5.2 a été fusionnée dans « Donnée » (décision D4). L'Alarme reste donc une action à part.

**Décision de Florian du 2026-09-25 :** une touche Alarme qui n'est pas affichée (autre page, dossier fermé) ne fait **rien de spécial**. Le logiciel Stream Deck détruit la touche et la recrée au retour : une alerte survenue entre-temps est perdue, et une alerte en cours est oubliée.

## Résultat

- **Session cloud** (conteneur Linux) :
  - compilation Roslyn en C# 7.3 sous Mono : 0 erreur ;
  - **125 tests OK** sous Mono, dont 19 nouveaux ;
  - une **mutation** (garde `IsLive` retirée) fait bien échouer les 2 tests de la garde.
- **Page de réglages vérifiée dans Chromium** (Playwright, 360 px, frappe lettre par lettre) :
  - la recherche « attaque » trouve `UnderAttack`. Autres recherches : « interdiction » → 3 événements, « surchauffe » → 2, « amarrage » → 6 ;
  - choisir un événement place la catégorie et le menu, et remplit les champs du filtre (`Target`) ;
  - pour un champ texte, les tests `<` et `>` sont désactivés ;
  - la page envoie exactement les **12 réglages** ;
  - des réglages enregistrés se rechargent, **champ de filtre compris** ;
  - un événement hors catalogue affiche un avertissement et garde son champ ;
  - choisir un autre événement retire un champ de filtre qu'il n'a pas ;
  - la recherche de commande (clic sur un résultat) et la saisie du raccourci (`ControlLeft+ShiftLeft+F5`) fonctionnent ;
  - panneau ⓘ, bouton « Tester » ;
  - aucun débordement en largeur, aucune erreur JavaScript.
- **Non-régression de la page « Donnée »** (Chromium) : recherche, choix, 102 réglages envoyés, vue mémorisée rechargée. `Generic.html` est **identique octet pour octet** après la refactorisation du générateur.
- **Non vérifié :**
  - `build.ps1` / `test.ps1` sous Windows ;
  - le logiciel Stream Deck, le son, et le comportement en jeu.

  C'est l'objet de D8.

## Comportement

| Situation | Effet |
|---|---|
| Un événement du journal est écrit **en direct**, du type choisi, et il passe le filtre | Image d'alerte et son d'alerte. Sans image d'alerte, le triangle ⚠ de Stream Deck (`ShowAlert`). |
| Un événement **relu au lancement** du plugin (`IsLive = false`) | **Ignoré.** C'est la garde du §5.3, testée sur le vrai `JournalWatcher`. |
| Un nouvel événement pendant l'alerte | La durée **repart de zéro**. |
| La durée est écoulée (vérifiée à chaque `OnTick`, environ 1 s) | Retour à l'image de repos, ou à l'image par défaut de l'action. |
| `duration = 0` | L'alerte dure **jusqu'à un appui**. |
| Un appui | **Acquitte** d'abord l'alerte en cours (quelle que soit la durée). Envoie ensuite la commande (⚠ si c'est un groupe de tir bloqué, comme sur « Donnée »), puis le raccourci libre, puis le son d'appui. |
| La touche n'est pas affichée | Rien : l'instance n'existe pas (choix de Florian). |
| Le nom d'événement | Comparé sans tenir compte des majuscules : `DropShipDeploy` (bibliothèque) = `DropshipDeploy` (jeu). `journal.UnderAttack` tapé à la main est accepté. |
| Le titre | Jamais envoyé par le plugin : c'est le titre tapé dans le logiciel Stream Deck qui s'affiche. |

## Réglages JSON — noms **définitifs** (UUID `com.mhwlng.elite.eventalarm`)

| Nom | Rôle | Défaut |
|---|---|---|
| `event` | événement, nom écrit par le jeu (`UnderAttack`) | `""` = jamais d'alerte |
| `filterField` | chemin du champ dans l'événement (`Target`, `Health`, `Inventory.#count`) ; vide = aucun filtre | `""` |
| `filterOp`, `filterValue` | test (`isTrue`, `isFalse`, `equals`, `notEquals`, `lt`, `lte`, `gt`, `gte`, les mêmes que les règles de « Donnée ») et sa valeur ; un champ sans test = aucun filtre | `""` |
| `duration` | durée en secondes (la virgule est acceptée) ; `0` = jusqu'à un appui ; une saisie invalide ou négative donne 5 s et un WARN | `"5"` |
| `idleImage`, `activeImage` | image au repos, image d'alerte | `""` = image par défaut de l'action |
| `alarmSound` | son joué au déclenchement | `""` |
| `pressCommand`, `pressHotkey`, `pressHotkeyText`, `clickSound` | commande, raccourci libre (codes physiques + texte affiché), son à l'appui : **mêmes noms que la vue 1 de « Donnée »** | `""` |

**Écarts avec le §5.3 :**
- `clickBinding` s'appelle `pressCommand`. Avec les mêmes noms, la recherche de commandes et la saisie du raccourci de `generic.js` servent telles quelles.
- Le raccourci libre et le son d'appui sont ajoutés, pour la cohérence avec L7.
- Le filtre porte sur **un champ choisi dans une liste** (`filterField`), et non sur une expression libre.

## Code

| Fichier | Rôle |
|---|---|
| `Elite/Generic/AlarmConfig.cs` | Lecture des réglages (fonction pure, sur le modèle de `DataKeyConfig`). `Matches(nom, événement)` lit le champ avec les conventions du magasin (`StoreKeys.FromJournalEvent`) puis applique `Condition.Evaluate` : un champ absent ne passe pas le filtre. `ShouldTrigger(e)` = `e.IsLive && Matches(...)` : c'est la garde. |
| `Elite/Generic/AlarmState.cs` | État d'alerte (pur, l'heure est passée en paramètre) : `Trigger(maintenant, durée)`, `IsActive(maintenant)`, `Acknowledge(maintenant)`. |
| `Elite/Generic/EventAlarmAction.cs` | L'action, `[PluginActionId("com.mhwlng.elite.eventalarm")]` :<br>– abonnements à `EliteStore.JournalEventReceived` (événement de L1, déclenché **après** la mise à jour du magasin, avec `IsLive`) et à `OnSendToPlugin` ;<br>– image envoyée seulement si elle change ;<br>– `OnTick` appelle d'abord `base.OnTick()`, pour garder la bascule de profils ;<br>– tout est désabonné dans `Dispose` ;<br>– try/catch + journal partout. |
| `Elite/Generic/KeyMedia.cs` | Cache des images (base64) et des sons d'une touche. `ValueAction.cs` n'est **pas modifié** : il garde sa copie. |
| `manifest.json` | 14ᵉ action « Alarme » : 1 seul `State`, `Keypad`, `SupportedInMultiActions: false`. |
| `Elite.csproj` | 4 `<Compile>`, plus `EventAlarm.html` et `alarm.js` en `<Content>`. |

`Program.cs`, `EliteStore.cs` et `ValueAction.cs` ne changent pas. `SDWrapper.Run` trouve la nouvelle action par réflexion.

**Messages dans `pluginlog.log` :**
- `EventAlarm[UnderAttack]: triggered` ;
- `... acknowledged` ;
- `... test from the property inspector` ;
- `... FireGroup-B ignored (docked)`.

## Page de réglages

`EventAlarm.html` est **générée** par `python tools/make-generic-html.py`, en même temps que `Generic.html`. Ne pas l'éditer à la main.

**Refactorisation du générateur :**
- l'en-tête (`head`) est partagé par les deux pages ;
- la liste des tests (`OPERATORS`) sert aux règles comme au filtre ;
- la recherche de commandes (`command_search_block`) est partagée ;
- le libellé du son d'appui est paramétrable (`press_block`).

**Sections de la page :**
- **Événement du journal** :
  - catégorie (17 catégories, celles qui ont des événements) puis événement ;
  - **recherche** en français, sans accents, sur le nom, la description de l'événement, la catégorie et **les descriptions de ses champs** ;
  - « Nom (avancé) » pour un événement hors catalogue ;
  - panneau ⓘ : quand l'événement arrive, ses champs, et l'heure du **dernier reçu**, demandée au plugin comme la « valeur actuelle » de « Donnée ».
- **Filtre (optionnel)** :
  - le champ (ceux de l'événement, sauf `timestamp`) ;
  - le test, proposé selon le type du champ ;
  - la valeur, avec les valeurs d'une énumération proposées ;
  - la signification du champ.
- **Alerte** : durée, image au repos, image d'alerte, son d'alerte, bouton **« ▶ Tester l'alarme »**, qui déclenche l'alerte comme un événement en direct.
- **Action (appui)** : identique à « Donnée » (recherche, survol, commande, raccourci, son d'appui).

**Scripts :**
- `alarm.js` (nouveau, ASCII) contient ce qui est propre à l'Alarme. Les résultats de recherche n'ont **pas** d'infobulle native, pour éviter le défaut de D7.
- `generic.js` :
  - `genericInit` est découpé : l'index, les commandes et le raccourci sont communs, puis vient l'initialisation propre à la page ;
  - `GENERIC_ACTION_KINDS` connaît `eventalarm` ;
  - `loadConfiguration` appelle `alarmBeforeLoad` : le menu du champ de filtre doit contenir le champ enregistré **avant** que les valeurs soient chargées, sinon il serait perdu. Il appelle aussi `alarmSettingsLoaded` et `alarmShowValue`.
- **Pourquoi deux pages et non un gabarit unique (§6) :** `setSettings()` envoie **tous** les champs `sdProperty` de la page. Avec une page commune, chaque touche Alarme aurait stocké les 102 réglages de « Donnée ».

## Tests (19 nouveaux, 125 au total)

- **`AlarmConfigTests`** (vraies lignes de `journal-extrait.txt`) :
  - valeurs par défaut, réglages de la page, « No file… » ;
  - durée invalide, négative, vide, avec virgule, numérique ;
  - filtre incomplet ;
  - `UnderAttack` avec `Target` = You / ≠ You, champ absent ;
  - un nombre (`HullDamage.Health`) ;
  - casse `DropShipDeploy` / `DropshipDeploy`, et clés complètes tapées à la main.
- **`AlarmStateTests`** : repos, expiration, relance, durée 0 jusqu'à l'acquittement, acquittement d'une alerte à durée.
- **`AlarmLiveGuardTests`** : le **critère du §8**. Tout l'extrait de journal passe dans le vrai `JournalWatcher` puis dans `EliteStore`, d'abord en rejeu : **aucune** alerte. Ensuite, en direct : seul `UnderAttack` / `You` déclenche.
- **`InspectorFieldsTests`** : les champs `sdProperty` de `EventAlarm.html` sont **exactement** les réglages C# de l'action (les 12 noms ci-dessus) ; ceux de `Generic.html` sont ceux de `ValueAction` moins `currentView`. Un nom changé d'un seul côté fait échouer le test.
- **`ManifestTests`** : Alarme avec 1 `State` en `Keypad`, 14 actions, `com.mhwlng.elite.counter` jamais déclaré, `alarm.js` présent et en ASCII.

## Test en jeu

Voir **D8** dans `docs/L9-finalisation.md` : un seul test pour L7b, L8 et L9.
