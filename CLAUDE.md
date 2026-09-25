# streamdeck-elite — contexte pour Claude Code

## Le projet en bref

Fork personnel de Florian (`origin` = github.com/Florian-DELRIEU/streamdeck-elite, **dépôt public**) du plugin Stream Deck « Elite Dangerous » de mhwlng (`upstream` = github.com/mhwlng/streamdeck-elite). C# / .NET Framework 4.8, SDK BarRaider `StreamDeck-Tools` 6.3.1 + `streamdeck-client-csharp` 4.3.0. Matériel cible : Stream Deck MK.2 (touches 72×72 px).

Chantier en cours : exposer **toute l'API du jeu** (`Status.json` + ~250 événements du Journal) via trois nouveaux types d'action paramétrables — **Valeur**, **État**, **Alarme** — pour ne plus avoir à écrire de C# à chaque nouvelle donnée.

## Documents à lire avant de coder

1. `docs/cahier-des-charges-streamdeck-elite.md` — **spécification figée, source de vérité** : décisions validées (§0), architecture (§4), spec des actions (§5), Property Inspector (§6), exigences (§7), critères d'acceptation (§8), lots (§9), risques (§10).
2. `docs/L0-baseline-build.md` — baseline de compilation et recette d'environnement.
3. `docs/L1-socle-donnees.md` — **conventions de clés et API d'`EliteStore`** telles qu'implémentées (complète le §4.3).
4. `docs/L2-catalogue.md` — **format de `catalog.js` / `commands.js`**, générateur, régénération des champs observés, consignes pour L3/L4.
5. `docs/L3-valeur.md` — mise en forme du texte (`ValueFormatter`), page de réglages commune (mécanique), mode d'emploi D3.
6. `docs/L4-donnee.md` — action universelle « Donnée » (règles d'image, conditions), décisions D4/D5.
7. `docs/L5-tiroir.md` — **tiroir : vues complètes, gestes court/long, héritage, page de réglages générée**, **mode d'emploi D5**.
8. `docs/L6-retours-d5.md` — recherche, bouton « i » (valeur actuelle en jeu), **descriptions françaises** (`descriptions-fr.json`), types entier/décimal, **mode d'emploi D6**.
9. `docs/L7-retours-d6.md` — commandes (recherche, `commands-fr.json`, survol), **raccourci clavier libre** (`Hotkey.cs`), **vue mémorisée** (`currentView`), ⚠ groupe de tir bloqué (`CommandGuard.cs`), **mode d'emploi D7**.
10. `docs/L8-alarme.md` — **action Alarme** (`com.mhwlng.elite.eventalarm`) : réglages définitifs, garde `IsLive`, page `EventAlarm.html` + `alarm.js`, tests.
11. `docs/L9-finalisation.md` — version 2.8.0, nom et catégorie « ZV Stream Deck Elite », `pack.ps1`, README, **liste de non-régression** et **mode d'emploi D8** (test unique L7b + L8 + L9).
12. `docs/icones-plan.md` — **plan des icônes** (document de travail de Florian, théorie sans image) : grammaire visuelle du pack `Elite/Images`, types A à F, réutilisation du pack, dossiers.
13. `docs/plan-extension-api-toutes-donnees.md` — étude de faisabilité amont, **historique**. Son Annexe A (catégories + emojis des événements) sert de référence au catalogue ; pour tout le reste, le cahier des charges prime (l'étude contient des points corrigés depuis : « 10 actions », « remplacer EliteData », etc.).

Ces documents existent aussi dans un Projet claude.ai de Florian ; pour Claude Code, la copie du dépôt fait foi.

## État d'avancement

| Lot | Contenu | Statut |
|---|---|---|
| L0 | Branche + compilation de référence | ✅ terminé le 2026-09-23 (0 erreur, 1 avertissement MSB3884 bénin) |
| L1 | Correctif `RawEventHandler` (§4.2) + `EliteStore` (§4.3) + tests unitaires | ✅ terminé le 2026-09-23 (build Debug/Release OK, 23 tests OK ; non vérifié en jeu) |
| L2 | Générateur de catalogue + `catalog.js` + liste des commandes | ✅ terminé le 2026-09-23 (1 926 clés, 366 commandes, 33 tests OK ; non vérifié dans le logiciel Stream Deck) |
| L3 | Action Valeur (complète, §5.1) + Property Inspector commun | ✅ terminé le 2026-09-23 (50 tests OK, PI vérifiée dans un navigateur ; non vérifié sur le Stream Deck) |
| D3 | Point de contrôle en jeu (Florian, MK.2, profil de test « Test ZV ») | ✅ 2026-09-23 : valeurs à jour, lisibles (police un peu petite → icône « T », défaut passé à 14), page de réglages correcte. Non-régression des anciens boutons : à confirmer en D4 |
| L4 | Action universelle « Donnée » : vues + règles d'image + appui (décisions D4/D5) | ✅ terminé le 2026-09-23 (66 tests OK, PI vérifiée dans un navigateur ; non vérifié sur le Stream Deck) |
| D4 | Test en jeu de « Donnée » + non-régression | ✅ 2026-09-24 : icônes selon état + commandes OK ; l'image ne suivait pas la vue et un appui faisait vue + commande → L5. Anciennes touches cassées = dossier d'icônes déplacé (pas le plugin) → 87 chemins corrigés dans le profil de Florian |
| L5 | Tiroir : chaque vue = action complète, appui court/long (décision D6), facteur `/32` `*4` | ✅ terminé le 2026-09-24 (91 tests OK, PI vérifiée dans un navigateur ; non vérifié sur le Stream Deck) |
| D5 | Test en jeu du tiroir | ✅ 2026-09-24 : tiroirs OK. Recherche impossible au clavier (seul le collage marchait), demande d'un bouton « i », rôle de « Clé » peu clair → L6. Firegroup « non configuré » : impossible (le jeu ne l'écrit pas), laissé tel quel |
| L6 | Retours D5 : recherche corrigée (FR, sans accents), bouton « i » + valeur actuelle, 635 descriptions FR, types entier/décimal | ✅ terminé le 2026-09-25 (95 tests OK, PI vérifiée dans un navigateur en frappe lettre par lettre ; non vérifié sur le Stream Deck) |
| D6 | Test de la page de réglages | ✅ 2026-09-25 : bouton « i » clair et précis. Demandes : raccourci libre, recherche + description des commandes ; vue revenue à 1 en revenant dans un dossier ; `Fire Group (A)` sans effet (bloqué par `EliteKeys` à quai / train sorti, pas un problème de config) → L7 |
| L7 | Retours D6 : 366 descriptions de commandes + recherche, raccourci clavier libre, vue mémorisée (`currentView`), ⚠ si groupe de tir bloqué | ✅ terminé le 2026-09-25 (106 tests OK, PI vérifiée dans un navigateur avec vraie frappe ; non vérifié sur le Stream Deck) |
| D7 | Test en jeu — mode d'emploi : `docs/L7-retours-d6.md` | ✅ 2026-09-25 : vue mémorisée, raccourci (le « l » parasite était la commande de la vue), groupes de tir OK. Clic sur un résultat de **commande** sans effet dans Stream Deck → L7b. Le jeu accepte un changement de groupe de tir train sorti : blocage de mhwlng **conservé** (choix de Florian) |
| L7b | Retours D7 : résultats de commande sans infobulle native, ligne de description à hauteur fixe (plus de mouvement au survol) | ✅ terminé le 2026-09-25 (session cloud : compilation Roslyn C# 7.3 sous Mono + 106 tests OK, PI vérifiée dans Chromium ; non vérifié sur le Stream Deck → D7b, inclus dans D8) |
| L8 | Alarme (`com.mhwlng.elite.eventalarm`, garde `IsLive`) : événement + filtre sur un champ, durée (0 = jusqu'à l'appui), images repos/alerte, son, commande + raccourci, bouton « Tester » | ✅ terminé le 2026-09-25, avec L9 à la demande de Florian (session cloud : compilation Roslyn C# 7.3 sous Mono + 125 tests OK, PI vérifiée dans Chromium en frappe lettre par lettre ; non vérifié sous Windows ni sur le Stream Deck → D8) |
| L9 | Version 2.8.0, nom et catégorie « ZV Stream Deck Elite », `pack.ps1` (zip ou DistributionTool), README (section FR en tête), liste de non-régression | ✅ terminé le 2026-09-25, avec L8 (126 tests OK dans le cloud ; `pack.ps1` exécuté sous PowerShell 7 Linux, archive vérifiée ; non vérifié sous Windows PowerShell 5.1) |
| D8 | Test unique L7b + L8 + L9 — mode d'emploi : `docs/L9-finalisation.md` (D7b, nom, Alarme, non-régression des 12 actions, paquet) | ⏭️ **prochain** (Florian) |
| — | Icônes des nouvelles actions dans la charte du pack de Florian : plan dans `docs/icones-plan.md` (2026-09-25) | plus tard, à sa demande |

### Décisions postérieures au cahier des charges (Florian, 2026-09-23)

- **D4 — action universelle** : l'action Valeur (`com.mhwlng.elite.value`, UUID conservé) devient « **Donnée** » et absorbe l'action État du §5.2 (icône on/off = Donnée sans texte). `com.mhwlng.elite.state` n'est **jamais déclaré** (réservé). L'Alarme reste une action séparée.
- **D5 — Donnée enrichie** : jusqu'à 4 vues qui défilent à l'appui, jusqu'à 4 règles d'image (seuils, égalité…) + image par défaut, commande clavier et son à l'appui. Remplace « Valeur : affichage seul » (D2).
- **D6 — tiroir** (2026-09-24) : chaque vue de « Donnée » a sa donnée, son texte, son icône (une vue sans icône propre reprend celle de la vue 1), sa commande et son son (jamais hérités). Gestes réglables par touche : par défaut appui court = agir, appui long (0,5 s) = vue suivante. `pressCycle` (L4) abandonné.
- **Firegroup** (2026-09-25) : l'action historique n'est pas modifiée ; le jeu n'expose que le groupe actif (`Status.json` `FireGroup`), pas la configuration des groupes. Les commandes `FireGroup-X` sont ignorées par `EliteKeys.HandleFireGroup` à pied / SRV / à quai / posé / train sorti / saut : sur une touche « Donnée », ⚠ (L7). D7 a montré que le jeu, lui, accepte le changement train sorti : Florian a choisi de **garder** le blocage de mhwlng (2026-09-25).
- **Commandes** (2026-09-25) : libellés anglais conservés, mais description française de chaque commande (`Elite.CatalogGen/commands-fr.json`) + recherche ; **raccourci clavier libre** par vue, envoyé après la commande.
- **Descriptions** (2026-09-25) : tout en français, dans `Elite.CatalogGen/descriptions-fr.json` (éditable à la main, UTF-8), fusionnées dans `catalog.js` au build.
- Ces décisions priment sur le cahier des charges (§0, §5.1–5.2, §9), qui reste inchangé pour l'historique.

→ **Mettre à jour ce tableau à la fin de chaque lot**, dans le commit du lot.

## Git

- Branche de travail : `feature/generic-api`, créée depuis `master` @ `71119aa` (= `origin/master` = `upstream/master` au moment du fork, aucune modification locale).
- Contexte projet (`CLAUDE.md`, `docs/`, `build.ps1`) commité le 2026-09-23 (`docs: contexte projet pour Claude Code`).
- Une branche locale `claude/main` existe (commit `9a4df80`, absent d'`origin/master`, origine inconnue — probablement une ancienne session Claude). **Ne pas l'utiliser ni la supprimer sans demander à Florian.**
- Nouveautés de mhwlng : `git fetch upstream` puis `git merge upstream/master`. Ne jamais installer sa version officielle (même UUID : elle écraserait celle-ci).
- Commits atomiques par lot. **Jamais** de push, force-push, `reset --hard` ou suppression de branche sans accord explicite de Florian.
- **Session Claude Code dans le cloud** (conteneur Linux, depuis le 2026-09-25) : le travail se fait sur la branche de session `claude/...` (partie de `feature/generic-api`), poussée à la fin de chaque lot avec l'accord de Florian ; il la fusionne en avance rapide sur sa machine (`git fetch`, puis `git merge --ff-only origin/claude/<branche>` sur `feature/generic-api`), puis lance `build.ps1` / `test.ps1`. Dans le cloud : pas de MSBuild ni de déploiement, mais pré-compilation Roslyn (C# 7.3) sous Mono avec les paquets NuGet de nuget.org, tests NUnit sous Mono, et pages de réglages testées dans Chromium (Playwright). Cette vérification ne remplace pas `build.ps1` / `test.ps1` sous Windows.
- Avant chaque push de la branche de session : `git fetch origin feature/generic-api` et fusionner ce que Florian y a poussé entre-temps (ex. `1021d1c` « add plan », le 2026-09-25), sinon son `git merge --ff-only` échoue (« Diverging branches »).
- `.gitignore` exclut déjà `bin/`, `obj/`, `packages/`, `.vs/`, `*.log`, `TestResult.xml`, et `Claude outputs/` (notes de travail locales de Florian, retirées du suivi le 2026-09-23).

## Compiler

Depuis la racine du dépôt :

```
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Release
```

Le script restaure les paquets avec `C:\nuget\nuget.exe` (obligatoire : `msbuild /t:Restore` ne restaure **pas** les projets `packages.config`, silencieusement) puis lance MSBuild trouvé par `vswhere`. Sortie : `Elite\bin\<Config>\com.mhwlng.elite.sdPlugin\`.

Critère : le script se termine par `== BUILD OK` (code de sortie 0) et aucun nouvel avertissement n'apparaît par rapport à la baseline (seul MSB3884, sur `WindowsInput.csproj`, existe aujourd'hui), en Debug **et** en Release. Script validé sur la machine de Florian le 2026-09-23. Le projet `Elite.Tests` n'est compilé qu'en Debug : le build Release ne produit que le plugin.

Depuis L2, chaque build lance `Elite.CatalogGen` (compilé avant Elite) qui régénère `Elite/PropertyInspector/catalog.js` et `commands.js` (écrits seulement s'ils changent ; ligne `Elite.CatalogGen: ... (written|unchanged)` dans la sortie). En cas d'échec : `error CATGEN01` et build en échec. **Ne jamais éditer ces deux fichiers à la main.** Après une mise à jour du jeu : `powershell -NoProfile -ExecutionPolicy Bypass -File update-observed-keys.ps1` (réanalyse les journaux → `Elite.CatalogGen/observed-keys.txt`, noms de champs uniquement ; fonctionne jeu lancé, lecture partagée), relire le diff, rebuild, tests, commit.

Depuis L6, le générateur fusionne aussi `Elite.CatalogGen/descriptions-fr.json` dans `catalog.js` (section `info`, ligne `... keys, N descriptions`). Après une mise à jour de la bibliothèque : `Elite.CatalogGen.exe descriptions <racine du dépôt>` régénère `descriptions-en.json` (commentaires anglais de `EliteJournalReader/Events`) ; comparer le diff, compléter `descriptions-fr.json` ; `DescriptionsTests` échoue si un événement ou une donnée de statut n'a pas de description, ou si une description ne correspond à rien. Depuis L7, idem pour les commandes : `Elite.CatalogGen/commands-fr.json` → 3ᵉ élément de chaque commande de `commands.js` (`CommandsTests` : aucune commande sans description, aucune description orpheline) ; une nouvelle commande dans `EliteKeys.cs` (fusion upstream) demande sa description.

Depuis L9, `powershell -NoProfile -ExecutionPolicy Bypass -File pack.ps1` (après le build Release) produit `dist\com.mhwlng.elite.streamDeckPlugin` (`dist/` ignoré par git) : `DistributionTool.exe` d'Elgato s'il est dans `tools\` ou le PATH, sinon zip dont la racine est le dossier `com.mhwlng.elite.sdPlugin`, sans `*.pdb` ni `*.log` ; critère `== PACK OK`. Ce paquet sert à une première installation (double-clic) ; pour mettre à jour la version installée, garder la procédure robocopy ci-dessous.

Note : `nuget restore` interroge nuget.org à chaque build (liste des vulnérabilités, parfois servie depuis le cache) ; les paquets eux-mêmes ne sont téléchargés que s'ils manquent dans `packages/`.

Piège shell : sous Windows, Claude Code exécute ses commandes dans Git Bash. Si MSBuild est appelé directement depuis bash, utiliser `-p:Configuration=Debug` et jamais `/p:...` (MSYS convertit les arguments commençant par `/` en chemins). Le plus simple : passer par `build.ps1`.

## Déployer pour un test en jeu (D3, D4…)

Aucun événement post-build : le déploiement est manuel. Le plugin installé vit dans `%APPDATA%\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin\` ; Stream Deck est installé dans `D:\Programmes\Elgato\StreamDeck.exe`.

1. Fermer Stream Deck : `Stop-Process -Name StreamDeck` puis attendre la fin de `com.mhwlng.elite` (il s'arrête avec).
2. Sauvegarde : **déjà faite** le 2026-09-23 — version officielle 2.7.4 d'origine, 73 fichiers, dans `C:\Users\Florian\Desktop\com.mhwlng.elite.sdPlugin.sauvegarde-2026-09-23` (le dossier installé ne contenait aucune image personnelle). Ne pas l'écraser.
3. Copier le build **Release** (x64, comme la version officielle — le Debug est AnyCPU et tourne en 32 bits) **par-dessus**, sans rien supprimer : `robocopy <repo>\Elite\bin\Release\com.mhwlng.elite.sdPlugin <dossier installé> /E` (codes 0–7 = succès ; jamais `/MIR` ni `/PURGE`). Lancer `build.ps1 -Configuration Release` juste avant.
4. Relancer : `Start-Process 'D:\Programmes\Elgato\StreamDeck.exe'`. Le plugin met **~50 s** à démarrer. Vérifier dans `pluginlog.log` la ligne `EliteStore: N keys after journal replay` (676 au premier déploiement).

Retour arrière : même procédure en copiant le dossier de sauvegarde. Les réglages des touches sont dans les profils Stream Deck, pas dans ce dossier ; Florian teste sur un profil « Test ZV ». La bascule automatique de profils du plugin n'est pas configurée (`Profiles: []`). Ces étapes touchent au logiciel de Florian : ne les exécuter qu'à sa demande explicite (« déploie »).

## Règles impératives

- **Additif, pas de refonte** : `EliteData` et les 12 actions existantes restent intacts. Nouveau code dans `Elite/Generic/`. Fichiers existants modifiables : `Program.cs`, `manifest.json`, `Elite.csproj`, `JournalWatcher.cs`, `Elite.sln` ; en plus, validés par Florian en L1 : `StatusWatcher.cs` (événement brut) et `StatusFileEvent.cs` (correctif `BreathableAtmosphere`, déjà fait). Tout autre fichier existant : demander d'abord.
- **UUID irréversibles** : `com.mhwlng.elite.value` (action « Donnée », **déjà utilisé dans le profil de Florian**), `com.mhwlng.elite.eventalarm` (action « Alarme », déclarée en L8 ; `.alarm` est déjà pris ; ses 12 noms de réglages sont définitifs, voir `docs/L8-alarme.md`, et `InspectorFieldsTests` vérifie qu'ils correspondent à la page). Réservés, non déclarés : `com.mhwlng.elite.state` (fusionné dans Donnée, D4), `com.mhwlng.elite.counter`. Les réglages JSON d'une action déjà utilisée ne doivent être qu'**ajoutés** (jamais renommés) : une touche existante doit rester lisible (voir `DataKeyConfig`).
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
- `manifest.json` : 12 actions historiques (10 Keypad sans clé `Controllers` + `dial` et `firegroupdial` en `Encoder`), chacune 1 `State` ; depuis L8, **14 actions** avec « Donnée » (`com.mhwlng.elite.value`) et « Alarme » (`com.mhwlng.elite.eventalarm`). Pas de champ `UUID` racine (SDKVersion 2 : l'identifiant vient du dossier `.sdPlugin`). L'action Toggle a pour UUID `com.mhwlng.elite`. Depuis L9 : `Version` 2.8.0, `Name` et `Category` « ZV Stream Deck Elite » (avant : 2.7.4, « Elite Dangerous ») ; `CodePath`, UUID et dossier inchangés.
- Rafraîchissement historique : Toggle et Alarm s'abonnent à `JournalWatcher.AllEventHandler` et redessinent aussi à chaque `OnTick` (~1 s) en relisant `EliteData` — un changement de `Status.json` y apparaît donc avec ≤ 1 s de retard.
- `UserBindings.cs` : 316 `StandardBindingInfo` + 21 `ToggleBindingInfo` + 35 `AxisBindingInfo`, liste plate sans notion de contexte. Fichiers de bindings lus dans `%LOCALAPPDATA%\Frontier Developments\Elite Dangerous\Options\Bindings\`.
- `EliteKeys.SendKeypress(string function)` (`Elite/Buttons/EliteKeys.cs`) sait envoyer **366 commandes** par leur nom : 336 bindings (tous sauf `SelectTargetBuggy`), chacun avec son contexte `Program.Binding[BindingType.X]` (Ship 153, Srv 42, OnFoot 60, General 81), + 30 commandes « selon l'état » (`LandingGearToggle-ON/OFF`, `FireGroup-A…H`…). C'est la source de `commands.js` et le moyen d'envoyer une commande en L4 (aucune modification nécessaire).
- Actions : `[PluginActionId("uuid")]` + héritage de `EliteKeypadBase` (dont `OnTick` appelle `StreamDeckCommon.HandleOnTick`, bascule de profils) ; réglages = classe `PluginSettings` avec `[JsonProperty]`, `payload.Settings.ToObject<>()` au constructeur, `BarRaider.SdTools.Tools.AutoPopulateSettings` dans `ReceivedSettings` (`[FilenameProperty]` pour les fichiers). Une instance est recréée à chaque apparition de la touche. API utile : `Connection.SetTitleAsync`, `SetImageAsync`, `SetDefaultImageAsync`. Dans `namespace Elite.*`, `Tools` = `Elite.Tools` (image → base64), pas celui de BarRaider.
- Property Inspector (`sdtools.common.js`) : chaque élément de classe `sdProperty` = un réglage (id = nom JSON) ; `setSettings()` les envoie tous ; `loadConfiguration(payload)` les remplit (`sdCheckbox`, `sdFile` gérés). Un champ fichier vide envoie le texte « No file... ».
- Aperçu d'une page PI dans le navigateur de Claude Code : `file://` = instantané statique sans JS → servir en HTTP via un `.claude/launch.json` temporaire (voir `docs/L3-valeur.md`), puis le supprimer. Après un rebuild, forcer `fetch(url, {cache: 'reload'})` sur les `.js` (sinon ancien cache). Tester la saisie **lettre par lettre** (une frappe à la fois), jamais un mot entier d'un coup : c'est ainsi que le bug de recherche de D5 était passé inaperçu.
- `EliteJournalReader` : `[Description]` sur certaines valeurs d'enum = texte écrit par le jeu (« Metal rich body ») ; propriétés calculées sans setter (ex. `Reputation.*Status`) absentes du JSON ; en Release, la lib (et Elite) sont en x64 → tout projet qui la référence doit l'être aussi en Release (sinon MSB3270).
- Fichiers du jeu : `%USERPROFILE%\Saved Games\Frontier Developments\Elite Dangerous\` (`Journal.*.log`, `Status.json`, `Cargo.json`, `NavRoute.json`) — source des vraies lignes pour les tests.

## Tests unitaires

Projet `Elite.Tests` (csproj classique net48, `LangVersion` 7.3), NUnit 3.14.0 + NUnit.ConsoleRunner 3.22.0 via `packages.config`, compilé en Debug seulement. Après `build.ps1` (Debug) :

```
powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1
```

Critère : `== TESTS OK` (code de sortie 0) ; 126 tests à la fin de L9 (125 à la fin de L8) (magasin, clés, événements bruts, catalogue, commandes et leurs descriptions, mise en forme et facteurs, conditions, règles d'image, gestes, réglages de « Donnée » dont compatibilité L3/L4 et `currentView`, manifeste, descriptions, raccourci, groupe de tir bloqué ; Alarme : réglages, filtre, durée, **garde `IsLive` sur le vrai `JournalWatcher`** ; champs des pages = réglages C#).

`Generic.html`, `EventAlarm.html` (depuis L8) et le bloc des réglages des vues 2 à 4 de `ValueAction.cs` sont **générés** par `python tools/make-generic-html.py` : ne pas les éditer à la main (voir `docs/L5-tiroir.md`, `docs/L8-alarme.md`).

Piège : l'outil Write de Claude Code convertit les séquences `é` écrites dans un fichier en vrais caractères (constaté aussi dans un heredoc bash le 2026-09-25). Pour `generic.js` et `alarm.js` (qui doivent rester ASCII), repasser un petit script d'échappement (caractère non ASCII → `\uXXXX`) ; `ManifestTests` échoue sinon. NUnit.ConsoleRunner 3.22.0 ignore `--noresult` : `test.ps1` passe `--work=Elite.Tests\bin\Debug` pour que `TestResult.xml` et `nunit-agent_*.log` restent dans `bin/`. Tout nouveau fichier de test : `<Compile Include>` dans `Elite.Tests.csproj` ; tout fichier de données : `<None Include>` + `CopyToOutputDirectory`.

Données de test (`Elite.Tests/Data/`) : extraits de vrais journaux de Florian. Le fork est public : ne committer que des extraits réduits aux lignes utiles, **sans** lignes `Commander`/`LoadGame`/`ReceiveText`/`Friends`/`Squadron*`, et vérifier par grep l'absence du nom de CMDR et du FID. Extension `.txt` (`*.log` est ignoré par git). Les `status-vaisseau.json` / `status-a-pied.json` sont synthétiques.

## Travailler avec Florian

- Répondre en **français**, de façon claire et concise. Florian est développeur amateur (surtout Python) : expliquer brièvement les manipulations propres à Windows/.NET quand il doit agir lui-même.
- Début de chaque lot : présenter un plan court et **attendre sa validation** avant d'écrire du code. S'il écrit `!plan`, donner uniquement le plan.
- Fin de chaque lot : compiler, lancer les tests, résumer ce qui est vérifié / non vérifié, mettre à jour le tableau d'avancement, commit du lot.
- Modèle conseillé par lot : §9 du cahier des charges (Opus pour L1–L2, Sonnet ensuite).
