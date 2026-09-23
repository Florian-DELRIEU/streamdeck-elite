# streamdeck-elite — contexte pour Claude Code

## Le projet en bref

Fork personnel de Florian (`origin` = github.com/Florian-DELRIEU/streamdeck-elite, **dépôt public**) du plugin Stream Deck « Elite Dangerous » de mhwlng (`upstream` = github.com/mhwlng/streamdeck-elite). C# / .NET Framework 4.8, SDK BarRaider `StreamDeck-Tools` 6.3.1 + `streamdeck-client-csharp` 4.3.0. Matériel cible : Stream Deck MK.2 (touches 72×72 px).

Chantier en cours : exposer **toute l'API du jeu** (`Status.json` + ~250 événements du Journal) via trois nouveaux types d'action paramétrables — **Valeur**, **État**, **Alarme** — pour ne plus avoir à écrire de C# à chaque nouvelle donnée.

## Documents à lire avant de coder

1. `docs/cahier-des-charges-streamdeck-elite.md` — **spécification figée, source de vérité** : décisions validées (§0), architecture (§4), spec des actions (§5), Property Inspector (§6), exigences (§7), critères d'acceptation (§8), lots (§9), risques (§10).
2. `docs/L0-baseline-build.md` — baseline de compilation et recette d'environnement.
3. `docs/plan-extension-api-toutes-donnees.md` — étude de faisabilité amont, **historique**. Son Annexe A (catégories + emojis des événements) sert de référence au catalogue ; pour tout le reste, le cahier des charges prime (l'étude contient des points corrigés depuis : « 10 actions », « remplacer EliteData », etc.).

Ces documents existent aussi dans un Projet claude.ai de Florian ; pour Claude Code, la copie du dépôt fait foi.

## État d'avancement

| Lot | Contenu | Statut |
|---|---|---|
| L0 | Branche + compilation de référence | ✅ terminé le 2026-09-23 (0 erreur, 1 avertissement MSB3884 bénin) |
| L1 | Correctif `RawEventHandler` (§4.2) + `EliteStore` (§4.3) + tests unitaires | ⏭️ **prochain** |
| L2 | Générateur de catalogue + `catalog.js` + liste des commandes | à faire |
| L3 | Action Valeur minimale + Property Inspector commun | à faire |
| D3 | Point de contrôle en jeu (Florian devant le jeu, Stream Deck MK.2) | à faire |
| L4 | Valeur complète, État, Alarme | à faire |
| L5 | Non-régression, version 2.8.0, empaquetage, README | à faire |

→ **Mettre à jour ce tableau à la fin de chaque lot**, dans le commit du lot.

## Git

- Branche de travail : `feature/generic-api`, créée depuis `master` @ `71119aa` (= `origin/master` = `upstream/master` au moment du fork, aucune modification locale).
- `CLAUDE.md`, `docs/` et `build.ps1` ne sont **pas encore commités** : premier commit à faire avant L1 (`docs: contexte projet pour Claude Code`).
- Une branche locale `claude/main` existe (commit `9a4df80`, absent d'`origin/master`, origine inconnue — probablement une ancienne session Claude). **Ne pas l'utiliser ni la supprimer sans demander à Florian.**
- Nouveautés de mhwlng : `git fetch upstream` puis `git merge upstream/master`. Ne jamais installer sa version officielle (même UUID : elle écraserait celle-ci).
- Commits atomiques par lot. **Jamais** de push, force-push, `reset --hard` ou suppression de branche sans accord explicite de Florian.
- `.gitignore` exclut déjà `bin/`, `obj/`, `packages/`, `.vs/`.

## Compiler

Depuis la racine du dépôt :

```
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Release
```

Le script restaure les paquets avec `C:\nuget\nuget.exe` (obligatoire : `msbuild /t:Restore` ne restaure **pas** les projets `packages.config`, silencieusement) puis lance MSBuild trouvé par `vswhere`. Sortie : `Elite\bin\<Config>\com.mhwlng.elite.sdPlugin\`.

Critère : 0 erreur et aucun nouvel avertissement par rapport à la baseline (seul MSB3884 existe aujourd'hui).

Piège shell : sous Windows, Claude Code exécute ses commandes dans Git Bash. Si MSBuild est appelé directement depuis bash, utiliser `-p:Configuration=Debug` et jamais `/p:...` (MSYS convertit les arguments commençant par `/` en chemins). Le plus simple : passer par `build.ps1`.

## Déployer pour un test en jeu (D3, L4, L5)

Aucun événement post-build : le déploiement est manuel. Le plugin installé vit dans `%APPDATA%\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin\`.

1. Fermer le logiciel Stream Deck (sinon `com.mhwlng.elite.exe` est verrouillé).
2. Sauvegarder ce dossier : il peut contenir des images personnelles de Florian que le build ne fournit pas (les images des boutons ne sont pas copiées en sortie de build).
3. Copier le contenu de `Elite\bin\Debug\com.mhwlng.elite.sdPlugin\` **par-dessus** (écraser, ne jamais supprimer le dossier).
4. Relancer Stream Deck. Journal du plugin : `pluginlog.log`, dans ce même dossier.

Les réglages des touches sont stockés dans les profils Stream Deck, pas dans ce dossier. Ces étapes touchent au logiciel de Florian : les lui proposer, ne pas les exécuter sans son accord.

## Règles impératives

- **Additif, pas de refonte** : `EliteData` et les 12 actions existantes restent intacts. Nouveau code dans `Elite/Generic/`. Fichiers existants modifiables : `Program.cs`, `manifest.json`, `Elite.csproj`, `JournalWatcher.cs` (et `Elite.sln` pour ajouter un projet de test).
- **UUID irréversibles** : `com.mhwlng.elite.value`, `com.mhwlng.elite.state`, `com.mhwlng.elite.eventalarm` (`.alarm` est déjà pris). `com.mhwlng.elite.counter` est réservé, non déclaré en V1.
- **1 seul `State`** par action dans `manifest.json` (limite firmware : 2 max) ; l'image change par code (`SetImageAsync`).
- **Garde `IsLive`** obligatoire pour l'Alarme : les événements rejoués au démarrage ne doivent rien déclencher.
- **C# 7.3 maximum** : les deux projets compilent avec `/langversion:7.3`. Interdits : types référence nullables, switch expressions, `using var`, index/range (`^1`, `..`), records, `init`, patterns récursifs, méthodes d'interface par défaut. Tuples et `out var` sont permis.
- **csproj classique, sans globbing** : tout nouveau `.cs` doit être ajouté à `Elite.csproj` (`<Compile Include=...>`) ; tout fichier de contenu (html, js, png) doit être ajouté avec `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`, sinon il n'arrive pas dans le plugin. Nouveau paquet NuGet : `packages.config` + `<Reference>` avec `HintPath` `..\packages\...`.
- **Robustesse** : les watchers appellent depuis des threads d'arrière-plan ; aucune exception ne doit sortir d'un handler (try/catch + log). Journalisation via `Logger.Instance.LogMessage(TracingLevel.X, ...)` → `pluginlog.log`.
- **« Ça compile » ≠ « ça marche »** : toujours dire ce qui a été vérifié (compilation, tests) et ce qui ne l'a pas été (comportement en jeu).

## Faits vérifiés dans le code (2026-09-23)

- `JournalWatcher.Parse(string line)` (`EliteJournalReader/JournalWatcher.cs`, l.561) parse la ligne en `JObject` puis appelle `FireEvent`. `FireEvent` n'invoque `AllEventHandler` que si une classe d'événement existe ; sinon simple `TraceWarning` → les événements inconnus sont perdus. Le `RawEventHandler` (§4.2) doit donc être déclenché **dans `Parse`, indépendamment de `FireEvent`**, avec (nom, `JObject`, `IsLive`).
- Rejeu : `StartWatching()` appelle `ProcessPreviousJournals()` (`IsLive = false`), puis passe `IsLive = true` et émet `MagicMau.IsLiveEvent`. `Program.cs` abonne les handlers **avant** `JournalWatcher.StartWatching().Wait()` (l.522) : `EliteStore` doit être branché au même endroit pour recevoir le rejeu.
- `JournalEventArgs` : `OriginalEvent` (`JObject` brut), `Timestamp` (`DateTime`).
- `StatusWatcher.StatusUpdated` (`EventHandler<StatusFileEvent>`) : le JSON est désérialisé en `StatusFileEvent` typé (le JSON brut n'est pas conservé) ; l'événement n'est émis que si `Timestamp` est plus récent que le précédent. Seul `EliteData.HandleStatusEvents` s'y abonne aujourd'hui (l.514).
- `StatusFileEvent` : `Flags` (`StatusFlags`), `Flags2` (`MoreStatusFlags`), `Pips` = tuple `(System, Engine, Weapons)` en demi-pips 0–8 (le jeu écrit un tableau) → clés `status.Pips.System/Engine/Weapons` ; `GuiFocus` (enum `StatusGuiFocus`), `Fuel` (`FuelMain`, `FuelReservoir`), `Cargo`, `LegalState`, `Latitude`, `Longitude`, `Altitude`, `Heading`, `BodyName`, `PlanetRadius`, `Balance` (long), `Destination` (`System`, `Body`, `Name`), `Oxygen`, `Health`, `Temperature`, `SelectedWeapon`, `Gravity`.
- Décomposition des flags par `Enum.GetValues` : **ignorer `None = 0`** (`HasFlag(None)` est toujours vrai).
- **Bug de la bibliothèque** : `MoreStatusFlags.BreathableAtmosphere = 0x00010001` au lieu de `0x00010000` (`EliteJournalReader/Events/StatusFileEvent.cs`, l.163) → avec `HasFlag`, il n'est vrai que si `OnFoot` l'est aussi. Correctif d'une ligne à **proposer à Florian** au début de L1 (fichier hors liste du §4.1) ; à défaut, tester le bit `0x10000` dans `EliteStore`.
- `manifest.json` : 12 actions (10 Keypad sans clé `Controllers` + `dial` et `firegroupdial` en `Encoder`), chacune 1 `State`. Pas de champ `UUID` racine (SDKVersion 2 : l'identifiant vient du dossier `.sdPlugin`). L'action Toggle a pour UUID `com.mhwlng.elite`. Actuellement `Version` 2.7.4, `Name` « Elite Dangerous ».
- Rafraîchissement historique : Toggle et Alarm s'abonnent à `JournalWatcher.AllEventHandler` et redessinent aussi à chaque `OnTick` (~1 s) en relisant `EliteData` — un changement de `Status.json` y apparaît donc avec ≤ 1 s de retard.
- `UserBindings.cs` : 316 `StandardBindingInfo` (les commandes clavier de D2) + 21 `ToggleBindingInfo` + 35 `AxisBindingInfo`. Fichiers de bindings lus dans `%LOCALAPPDATA%\Frontier Developments\Elite Dangerous\Options\Bindings\`.
- Fichiers du jeu : `%USERPROFILE%\Saved Games\Frontier Developments\Elite Dangerous\` (`Journal.*.log`, `Status.json`, `Cargo.json`, `NavRoute.json`) — source des vraies lignes pour les tests.

## Tests unitaires (à mettre en place en L1)

Aucun projet de test n'existe. Recommandation, **à valider avec Florian en ouvrant L1** : projet `Elite.Tests` (csproj classique net48) ajouté à `Elite.sln`, NUnit 3 via `packages.config` + paquet `NUnit.ConsoleRunner` (fournit `nunit3-console.exe` après `nuget restore`, rien à installer en plus). MSTest demanderait `vstest.console.exe`, pas garanti avec les Build Tools.

Données de test : extraits de vrais journaux de Florian. Le fork est public : ne committer que des extraits réduits aux lignes utiles, avec nom de CMDR et FID remplacés.

## Travailler avec Florian

- Répondre en **français**, de façon claire et concise. Florian est développeur amateur (surtout Python) : expliquer brièvement les manipulations propres à Windows/.NET quand il doit agir lui-même.
- Début de chaque lot : présenter un plan court et **attendre sa validation** avant d'écrire du code. S'il écrit `!plan`, donner uniquement le plan.
- Fin de chaque lot : compiler, lancer les tests, résumer ce qui est vérifié / non vérifié, mettre à jour le tableau d'avancement, commit du lot.
- Modèle conseillé par lot : §9 du cahier des charges (Opus pour L1–L2, Sonnet ensuite).
