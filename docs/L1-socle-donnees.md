# L1 — Socle de données (`RawEventHandler` + `EliteStore`)

Statut : **terminé** le 2026-09-23. Ce document précise, sans le contredire, le §4 du cahier des charges. C'est la référence pour les lots L2 à L4.

## Résultat

- `build.ps1` compile en Debug et en Release : `== BUILD OK`, avec seulement l'avertissement MSB3884 de la baseline.
- `test.ps1` : **23 tests NUnit OK**.
- Contrôle ponctuel hors dépôt : j'ai rejoué tous les journaux de Florian (60 fichiers, 27 466 lignes, 161 types d'événements) dans `StoreKeys`/`EliteStore`. Résultat : 0 échec et 1 166 clés en mémoire, `Status.json` compris.
- **Non vérifié :** le comportement en jeu. Rien n'est encore visible sur le Stream Deck, car aucune action n'utilise le magasin avant L3.

## Ce qui a été ajouté ou modifié

| Fichier | Changement |
|---|---|
| `EliteJournalReader/JournalWatcher.cs` | Événement `RawEventHandler` (`RawJournalEventArgs` : `EventName`, `Event`, `IsLive`), déclenché dans `Parse()` **avant** `FireEvent`, pour toute ligne qui a un champ `event`. Son appel est protégé par un `try/catch`. |
| `EliteJournalReader/StatusWatcher.cs` | Événement `RawStatusUpdated` (`RawStatusEventArgs` : `Status`), émis **avant** `StatusUpdated`, dans la même garde « horodatage plus récent », lui aussi protégé par un `try/catch`. |
| `EliteJournalReader/Events/StatusFileEvent.cs` | `BreathableAtmosphere = 0x00010000` au lieu de `0x00010001`. Ce correctif **change le comportement** du bouton Toggle historique « atmosphère respirable » : il ne s'allume plus dès qu'on est à pied, seulement si l'atmosphère est respirable. À vérifier en D3. |
| `Elite/Generic/StoreKeys.cs` | Fonctions pures qui transforment le JSON en clés. |
| `Elite/Generic/EliteStore.cs` | Le magasin. |
| `Elite/Program.cs` | Branchement des deux événements bruts **avant** les `StartWatching()`, puis une ligne INFO `EliteStore: N keys after journal replay` dans `pluginlog.log`. |
| `Elite.Tests/`, `Elite.sln`, `test.ps1` | Projet de tests : NUnit 3.14.0 + NUnit.ConsoleRunner 3.22.0, compilé en Debug seulement. |

## Conventions de clés (compléments au §4.3)

- **Clés insensibles à la casse**, pour le stockage, `TryGet` et `DataChanged.Contains()`. Par exemple, `journal.DropShipDeploy.Body` trouve `DropshipDeploy`.
- **Règle générale** :
  - un objet imbriqué donne un chemin à points (`journal.Docked.StationFaction.Name`) ;
  - un tableau donne **uniquement** `<chemin>.#count` ;
  - une valeur `null` est ignorée ;
  - le champ `event` est exclu ;
  - `timestamp` est gardé. Attention : Newtonsoft convertit les chaînes au format ISO en **dates** (`JTokenType.Date`, en UTC). Il faudra le gérer à l'affichage (L3/L4).
- **Journal** : `journal.<Nom>.<chemin>`, avec le nom **tel qu'écrit par le jeu**.
- **Statut** (calculé depuis le JSON brut, plus depuis le `StatusFileEvent` typé) :
  - `status.Flags.<Nom>` et `status.Flags2.<Nom>` sont des booléens. On les obtient avec `Enum.GetValues`, sans `None`, par le test `(brut & v) == v`.
  - Si `Flags2` est absent (Horizons ou menu), aucune clé `Flags2` n'existe.
  - `status.Pips.System/Engine/Weapons` sont en demi-pips.
  - `status.GuiFocus` contient le **nom** de l'énumération (`"GalaxyMap"`), ou le nombre si la valeur est inconnue.
  - Tout le reste suit la règle générale, avec les noms JSON du jeu : `status.Fuel.FuelMain`, `status.Destination.Name`, `status.FireGroup`, etc.
- **Un champ absent donne une clé absente.** Par exemple, `status.Oxygen` n'existe qu'à pied. Ce sera « — » pour Valeur et l'image « faux » pour État.
- **Remplacement par source** : un nouveau statut, ou un nouvel événement d'un type donné, remplace **toutes** les clés de cette source. Une clé disparue est retirée et signalée dans `DataChanged`.

## API `EliteStore` (namespace `Elite.Generic`)

| Membre | Rôle |
|---|---|
| `bool TryGet(string key, out JToken value)` | Renvoie une **copie** de la valeur. |
| `int KeyCount` | Diagnostic. |
| `event Action<IReadOnlyCollection<string>> DataChanged` | Clés modifiées, apparues ou disparues. Émis hors verrou, depuis des threads d'arrière-plan. Un abonné qui plante est logué en WARN et n'empêche pas les autres d'être appelés. |
| `event EventHandler<RawJournalEventArgs> JournalEventReceived` | **Ajout au §4.3** : chaque occurrence, même identique à la précédente, avec son `IsLive`. C'est la source de l'Alarme en L4 : garde `IsLive` et relance de la durée. |
| `HandleRawStatus` / `HandleRawJournal` | Handlers branchés dans `Program.cs`. Aucune exception n'en sort : elles sont logguées en ERROR. |
| `internal GetLiveCount(eventName)` | Compteur d'occurrences *live*, réservé à Compteur et non exposé. |
| `internal Reset()` | Réservé aux tests (`InternalsVisibleTo("Elite.Tests")`, déclaré dans `EliteStore.cs`). |

Reporté à L3 : le log DEBUG « changement de valeur d'une clé suivie » (§7). Il faut que les actions existent pour savoir quelles clés sont suivies.

## Constat : événements perdus par la bibliothèque

Dans les journaux de septembre 2026, la bibliothèque ignorait **4 types d'événements**, qui arrivent maintenant dans le magasin :
- `GameModeChange` ;
- `SupercruiseDestinationDrop` ;
- `ScanBaryCentre` ;
- `DropshipDeploy`. La bibliothèque déclare `DropShipDeploy`, et son dictionnaire distingue les majuscules.

**Pour L2 :**
- Le catalogue généré par réflexion sur `Events/` ne contiendra pas ces 4 événements. Il faudra soit une petite liste complémentaire, soit la saisie manuelle de clé prévue au §4.4.
- Pour l'affichage, préférer les noms du jeu, par exemple `DropshipDeploy`. Grâce aux clés insensibles à la casse, le nom de la bibliothèque fonctionne aussi pour la lecture.

## Tests

- **Lancement :** `powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1`, après `build.ps1` en Debug. Le script doit afficher `== TESTS OK`.
- **Piège :** NUnit.ConsoleRunner 3.22.0 **ignore `--noresult`**. `test.ps1` utilise donc `--work=Elite.Tests\bin\Debug`, pour que `TestResult.xml` et les `nunit-agent_*.log` restent dans `bin/` (ignoré par git).
- **Données** (`Elite.Tests/Data/`) :
  - `journal-extrait.txt` : 21 vraies lignes, sans nom de CMDR ni FID, vérifié par grep. L'extension est `.txt` parce que `*.log` est ignoré par `.gitignore`.
  - `status-menu.json` : un vrai fichier.
  - `status-vaisseau.json` et `status-a-pied.json` : **synthétiques**, écrits au format officiel. À remplacer par de vrais fichiers capturés en jeu si l'occasion se présente.

## À contrôler en D3

- La ligne `EliteStore: N keys after journal replay` dans `pluginlog.log`.
- Le bouton Toggle historique « atmosphère respirable », à pied dans une atmosphère non respirable : il doit être **éteint**.
