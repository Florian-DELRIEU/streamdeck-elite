# streamdeck-elite — contexte pour Claude Code

## Le projet en bref

Fork personnel de Florian (`origin` = github.com/Florian-DELRIEU/streamdeck-elite, **dépôt public**) du plugin Stream Deck « Elite Dangerous » de mhwlng (`upstream` = github.com/mhwlng/streamdeck-elite). C# / .NET Framework 4.8, SDK BarRaider `StreamDeck-Tools` 6.3.1 + `streamdeck-client-csharp` 4.3.0. Matériel cible : Stream Deck MK.2 (touches 72×72 px).

Chantier en cours : exposer **toute l'API du jeu** (`Status.json` + ~250 événements du Journal) via trois nouveaux types d'action paramétrables — **Valeur**, **État**, **Alarme** — pour ne plus avoir à écrire de C# à chaque nouvelle donnée.

## Documents à lire avant de coder

1. `docs/cahier-des-charges-streamdeck-elite.md` — **spécification figée, source de vérité** : décisions validées (§0), architecture (§4), spec des actions (§5), Property Inspector (§6), exigences (§7), critères d'acceptation (§8), lots (§9), risques (§10).
2. `docs/L0-baseline-build.md` — baseline de compilation et recette d'environnement.
3. `docs/L1-socle-donnees.md` — **conventions de clés et API d'`EliteStore`** telles qu'implémentées (complète le §4.3), constats pour L2.
4. `docs/plan-extension-api-toutes-donnees.md` — étude de faisabilité amont, **historique**. Son Annexe A (catégories + emojis des événements) sert de référence au catalogue ; pour tout le reste, le cahier des charges prime (l'étude contient des points corrigés depuis : « 10 actions », « remplacer EliteData », etc.).

Ces documents existent aussi dans un Projet claude.ai de Florian ; pour Claude Code, la copie du dépôt fait foi.

## État d'avancement

| Lot | Contenu | Statut |
|---|---|---|
| L0 | Branche + compilation de référence | ✅ terminé le 2026-09-23 (0 erreur, 1 avertissement MSB3884 bénin) |
| L1 | Correctif `RawEventHandler` (§4.2) + `EliteStore` (§4.3) + tests unitaires | ✅ terminé le 2026-09-23 (build Debug/Release OK, 23 tests OK ; non vérifié en jeu) |
| L2 | Générateur de catalogue + `catalog.js` + liste des commandes | ⏭️ **prochain** |
| L3 | Action Valeur minimale + Property Inspector commun | à faire |
| D3 | Point de contrôle en jeu (Florian devant le jeu, Stream Deck MK.2) | à faire |
| L4 | Valeur complète, État, Alarme | à faire |
| L5 | Non-régression, version 2.8.0, empaquetage, README | à faire |

→ **Mettre à jour ce tableau à la fin de chaque lot**, dans le commit du lot.

## Git

- Branche de travail : `feature/generic-api`, créée depuis `master` @ `71119aa` (= `origin/master` = `upstream/master` au moment du fork, aucune modification locale).
- Contexte projet (`CLAUDE.md`, `docs/`, `build.ps1`) commité le 2026-09-23 (`docs: contexte projet pour Claude Code`).
- Une branche locale `claude/main` existe (commit `9a4df80`, absent d'`origin/master`, origine inconnue — probablement une ancienne session Claude). **Ne pas l'utiliser ni la supprimer sans demander à Florian.**
- Nouveautés de mhwlng : `git fetch upstream` puis `git merge upstream/master`. Ne jamais installer sa version officielle (même UUID : elle écraserait celle-ci).
- Commits atomiques par lot. **Jamais** de push, force-push, `reset --hard` ou suppression de branche sans accord explicite de Florian.
- `.gitignore` exclut déjà `bin/`, `obj/`, `packages/`, `.vs/`, `*.log`, `TestResult.xml`, et `Claude outputs/` (notes de travail locales de Florian, retirées du suivi le 2026-09-23).

## Compiler

Depuis la racine du dépôt :

```
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Release
```

Le script restaure les paquets avec `C:\nuget\nuget.exe` (obligatoire : `msbuild /t:Restore` ne restaure **pas** les projets `packages.config`, silencieusement) puis lance MSBuild trouvé par `vswhere`. Sortie : `Elite\bin\<Config>\com.mhwlng.elite.sdPlugin\`.

Critère : le script se termine par `== BUILD OK` (code de sortie 0) et aucun nouvel avertissement n'apparaît par rapport à la baseline (seul MSB3884, sur `WindowsInput.csproj`, existe aujourd'hui). Script validé sur la machine de Florian le 2026-09-23. Le projet `Elite.Tests` n'est compilé qu'en Debug : le build Release ne produit que le plugin.

Note : `nuget restore` interroge nuget.org à chaque build (liste des vulnérabilités, parfois servie depuis le cache) ; les paquets eux-mêmes ne sont téléchargés que s'ils manquent dans `packages/`.

Piège shell : sous Windows, Claude Code exécute ses commandes dans Git Bash. Si MSBuild est appelé directement depuis bash, utiliser `-p:Configuration=Debug` et jamais `/p:...` (MSYS convertit les arguments commençant par `/` en chemins). Le plus simple : passer par `build.ps1`.

## Déployer pour un test en jeu (D3, L4, L5)

Aucun événement post-build : le déploiement est manuel. Le plugin installé vit dans `%APPDATA%\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin\`.

1. Fermer le logiciel Stream Deck (sinon `com.mhwlng.elite.exe` est verrouillé).
2. Sauvegarder ce dossier : il peut contenir des images personnelles de Florian que le build ne fournit pas (les images des boutons ne sont pas copiées en sortie de build).
3. Copier le contenu de `Elite\bin\Debug\com.mhwlng.elite.sdPlugin\` **par-dessus** (écraser, ne jamais supprimer le dossier).
4. Relancer Stream Deck. Journal du plugin : `pluginlog.log`, dans ce même dossier. Depuis L1, il doit contenir `EliteStore: N keys after journal replay` au démarrage.

Les réglages des touches sont stockés dans les profils Stream Deck, pas dans ce dossier. Ces étapes touchent au logiciel de Florian : les lui proposer, ne pas les exécuter sans son accord.

## Règles impératives

- **Additif, pas de refonte** : `EliteData` et les 12 actions existantes restent intacts. Nouveau code dans `Elite/Generic/`. Fichiers existants modifiables : `Program.cs`, `manifest.json`, `Elite.csproj`, `JournalWatcher.cs`, `Elite.sln` ; en plus, validés par Florian en L1 : `StatusWatcher.cs` (événement brut) et `StatusFileEvent.cs` (correctif `BreathableAtmosphere`, déjà fait). Tout autre fichier existant : demander d'abord.
- **UUID irréversibles** : `com.mhwlng.elite.value`, `com.mhwlng.elite.state`, `com.mhwlng.elite.eventalarm` (`.alarm` est déjà pris). `com.mhwlng.elite.counter` est réservé, non déclaré en V1.
- **1 seul `State`** par action dans `manifest.json` (limite firmware : 2 max) ; l'image change par code (`SetImageAsync`).
- **Garde `IsLive`** obligatoire pour l'Alarme : les événements rejoués au démarrage ne doivent rien déclencher.
- **C# 7.3 maximum** : les deux projets compilent avec `/langversion:7.3`. Interdits : types référence nullables, switch expressions, `using var`, index/range (`^1`, `..`), records, `init`, patterns récursifs, méthodes d'interface par défaut. Tuples et `out var` sont permis.
- **csproj classique, sans globbing** : tout nouveau `.cs` doit être ajouté à `Elite.csproj` (`<Compile Include=...>`) ; tout fichier de contenu (html, js, png) doit être ajouté avec `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`, sinon il n'arrive pas dans le plugin. Nouveau paquet NuGet : `packages.config` + `<Reference>` avec `HintPath` `..\packages\...`.
- **Robustesse** : les watchers appellent depuis des threads d'arrière-plan ; aucune exception ne doit sortir d'un handler (try/catch + log). Journalisation via `Logger.Instance.LogMessage(TracingLevel.X, ...)` → `pluginlog.log`.
- **« Ça compile » ≠ « ça marche »** : toujours dire ce qui a été vérifié (compilation, tests) et ce qui ne l'a pas été (comportement en jeu).

## Faits vérifiés dans le code (2026-09-23)

- `JournalWatcher.Parse(string line)` (`EliteJournalReader/JournalWatcher.cs`) parse la ligne en `JObject` puis appelle `FireEvent`. `FireEvent` n'invoque `AllEventHandler` que si une classe d'événement existe (dictionnaire **sensible à la casse**) ; sinon simple `TraceWarning`. **Depuis L1**, `RawEventHandler` est déclenché dans `Parse` avant `FireEvent`, pour toute ligne (nom, `JObject`, `IsLive`). Événements réels que la lib perdait (journaux de sept. 2026) : `GameModeChange`, `SupercruiseDestinationDrop`, `ScanBaryCentre`, `DropshipDeploy` (la lib déclare `DropShipDeploy`).
- Rejeu : `StartWatching()` appelle `ProcessPreviousJournals()` (`IsLive = false`, seulement les parties `.01`…`.NN` du journal le plus récent), puis passe `IsLive = true` et émet `MagicMau.IsLiveEvent` (via `FireEvent`, donc **pas** via `RawEventHandler`). `Program.cs` abonne `EliteData` puis `EliteStore` **avant** `StatusWatcher.StartWatching()` et `JournalWatcher.StartWatching().Wait()`.
- Pour les tests : `JournalWatcher` et `StatusWatcher` ont des constructeurs `protected` sans argument ; `ParseText(string)` est public ; `IsLive` a un setter `protected` ; `StatusWatcher.UpdateStatus(string, int)` est `protected` → sous-classes `TestJournalWatcher` / `TestStatusWatcher` dans `Elite.Tests/TestSupport.cs`.
- `JournalEventArgs` : `OriginalEvent` (`JObject` brut), `Timestamp` (`DateTime`).
- `StatusWatcher.StatusUpdated` (`EventHandler<StatusFileEvent>`) : `StatusFileEvent` typé, où **un champ absent vaut 0** (Oxygen dans le vaisseau, Latitude dans l'espace…) ; émis seulement si `Timestamp` est plus récent. **Depuis L1**, `RawStatusUpdated` (JSON brut, champ absent = absent) est émis juste avant ; c'est lui qu'utilise `EliteStore`. `EliteData.HandleStatusEvents` reste abonné au typé.
- `StatusFileEvent` : `Flags` (`StatusFlags`), `Flags2` (`MoreStatusFlags`), `Pips` = tuple `(System, Engine, Weapons)` en demi-pips 0–8 (le jeu écrit un tableau), `Firegroup` (JSON `FireGroup`), `GuiFocus` (enum `StatusGuiFocus`), `Fuel` (`FuelMain`, `FuelReservoir`), `Cargo`, `LegalState`, `Latitude`, `Longitude`, `Altitude`, `Heading`, `BodyName`, `PlanetRadius`, `Balance` (long), `Destination` (`System`, `Body`, `Name`), `Oxygen`, `Health`, `Temperature`, `SelectedWeapon`, `Gravity`. Le `Status.json` du menu principal ne contient que `timestamp`, `event` et `Flags: 0`.
- Décomposition des flags par `Enum.GetValues` : **ignorer `None = 0`** (`HasFlag(None)` est toujours vrai).
- `MoreStatusFlags.BreathableAtmosphere` valait `0x00010001` (bug) : **corrigé en L1** à `0x00010000`. Effet de bord assumé : le Toggle historique « atmosphère respirable » (`EliteData.cs`, test `(Flags2 & BreathableAtmosphere) != 0`) ne s'allume plus dès qu'on est à pied — à vérifier en D3.
- Newtonsoft (`JObject.Parse`, `JToken.ReadFrom`) convertit les chaînes ISO (`timestamp`…) en `JTokenType.Date` (UTC).
- `manifest.json` : 12 actions (10 Keypad sans clé `Controllers` + `dial` et `firegroupdial` en `Encoder`), chacune 1 `State`. Pas de champ `UUID` racine (SDKVersion 2 : l'identifiant vient du dossier `.sdPlugin`). L'action Toggle a pour UUID `com.mhwlng.elite`. Actuellement `Version` 2.7.4, `Name` « Elite Dangerous ».
- Rafraîchissement historique : Toggle et Alarm s'abonnent à `JournalWatcher.AllEventHandler` et redessinent aussi à chaque `OnTick` (~1 s) en relisant `EliteData` — un changement de `Status.json` y apparaît donc avec ≤ 1 s de retard.
- `UserBindings.cs` : 316 `StandardBindingInfo` (les commandes clavier de D2) + 21 `ToggleBindingInfo` + 35 `AxisBindingInfo`. Fichiers de bindings lus dans `%LOCALAPPDATA%\Frontier Developments\Elite Dangerous\Options\Bindings\`.
- Fichiers du jeu : `%USERPROFILE%\Saved Games\Frontier Developments\Elite Dangerous\` (`Journal.*.log`, `Status.json`, `Cargo.json`, `NavRoute.json`) — source des vraies lignes pour les tests.

## Tests unitaires

Projet `Elite.Tests` (csproj classique net48, `LangVersion` 7.3), NUnit 3.14.0 + NUnit.ConsoleRunner 3.22.0 via `packages.config`, compilé en Debug seulement. Après `build.ps1` (Debug) :

```
powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1
```

Critère : `== TESTS OK` (code de sortie 0). NUnit.ConsoleRunner 3.22.0 ignore `--noresult` : `test.ps1` passe `--work=Elite.Tests\bin\Debug` pour que `TestResult.xml` et `nunit-agent_*.log` restent dans `bin/`. Tout nouveau fichier de test : `<Compile Include>` dans `Elite.Tests.csproj` ; tout fichier de données : `<None Include>` + `CopyToOutputDirectory`.

Données de test (`Elite.Tests/Data/`) : extraits de vrais journaux de Florian. Le fork est public : ne committer que des extraits réduits aux lignes utiles, **sans** lignes `Commander`/`LoadGame`/`ReceiveText`/`Friends`/`Squadron*`, et vérifier par grep l'absence du nom de CMDR et du FID. Extension `.txt` (`*.log` est ignoré par git). Les `status-vaisseau.json` / `status-a-pied.json` sont synthétiques.

## Travailler avec Florian

- Répondre en **français**, de façon claire et concise. Florian est développeur amateur (surtout Python) : expliquer brièvement les manipulations propres à Windows/.NET quand il doit agir lui-même.
- Début de chaque lot : présenter un plan court et **attendre sa validation** avant d'écrire du code. S'il écrit `!plan`, donner uniquement le plan.
- Fin de chaque lot : compiler, lancer les tests, résumer ce qui est vérifié / non vérifié, mettre à jour le tableau d'avancement, commit du lot.
- Modèle conseillé par lot : §9 du cahier des charges (Opus pour L1–L2, Sonnet ensuite).
