# Étude de faisabilité — Exposer toute l'API Elite Dangerous dans le plugin Stream Deck

> **Document historique (2026-09-22).** Il a été affiné et corrigé par `cahier-des-charges-streamdeck-elite.md`, qui prime en cas de divergence (12 actions et non 10, approche additive au lieu de « remplacer EliteData », état du dépôt local déjà vérifié en L0). Seule l'**Annexe A** reste une référence directe (catégories du catalogue). Ne pas exécuter les « à vérifier » ci-dessous : c'est fait.

**Statut** : étude de faisabilité + plan de jalons. Aucun code n'a été modifié. Document destiné à amorcer une autre discussion dédiée à l'implémentation.

**Base analysée** : dépôt `mhwlng/streamdeck-elite` (C# / .NET, SDK `BarRaider.SdTools` + `streamdeck-client-csharp`), confirmé identique au `.streamDeckPlugin` (`com.mhwlng.elite`) fourni par Florian. Un dossier de travail local (`F:\Github Local\streamdeck-elite`, machine `desktop-cafv827`) a été connecté en fin d'analyse mais n'a pas pu être inspecté en direct (pont `device_bash` momentanément indisponible) — **à vérifier en premier dans l'autre discussion** (branche courante, commits locaux non poussés, éventuel travail déjà commencé).

---

## 1. Objectif

Actuellement, le plugin n'expose qu'un sous-ensemble restreint de l'API du jeu : ~14 bascules, 4 pips, 4 alarmes, quelques boutons FSD/FSS/route/limpet/firegroup. Le but est de pouvoir construire une touche Stream Deck à partir de **n'importe quelle donnée** disponible — n'importe lequel des ~250 événements du Journal, ou n'importe lequel des ~60 champs de `Status.json` — sans devoir écrire une nouvelle classe C# à chaque fois.

## 2. Pourquoi ce n'est pas déjà le cas (constat technique)

Trois verrous identifiés dans le code actuel :

1. **Dispatch en dur.** Chaque type de bouton (`Toggle.cs`, `Firegroup.cs`, `Alarm.cs`, `Power.cs`…) contient un `switch (settings.Function)` codé en dur : une chaîne de cas par fonction supportée. Ajouter une fonction = ajouter un `case` en C#, recompiler, republier. Ce n'est pas extensible à 250+ entrées sans une refonte.
2. **Agrégateur partiel.** La classe `EliteData` (`Elite/EliteData.cs`) recopie la quasi-totalité des flags de `Status.json` (bon point), mais ne traite que **10 types d'événements sur ~250** du Journal (`Location`, `Docked`, `FSDJump`, `FSDTarget`, `UnderAttack`, `Cargo`, `Died`, `NavRouteClear`, etc.) pour en extraire quelques champs (système actuel, cible FSD, nombre de limpets…). Tout le reste du Journal est bien reçu et parsé (`JournalWatcher.AllEventHandler`) mais **jeté** : rien ne le stocke pour affichage.
3. **Property Inspector statique.** Chaque bouton a son propre fichier HTML (`PropertyInspector/Elite/*.html`) avec une liste d'options figée. Pas de mécanisme pour peupler dynamiquement un menu à partir d'un catalogue de champs.

**Bonne nouvelle pour la faisabilité** : aucun de ces verrous n'est un problème de *données* — tout est déjà reçu, parsé et typé côté C# (`EliteJournalReader/Events/*.cs`). Le travail est entièrement **architectural** (dispatch générique + affichage générique), pas de rétro-ingénierie supplémentaire à faire.

## 3. Contrainte matérielle à respecter

Le firmware Stream Deck ne supporte que **2 `States` déclarés par action** dans le manifeste. Le plugin actuel contourne déjà cette limite correctement : un seul `State`, et c'est le code C# qui décide de l'image via `Connection.SetImageAsync(...)` (voir `Toggle.cs`, méthode `HandleDisplay`). **Tout nouveau type d'action générique doit suivre ce même pattern** — ne jamais tenter de déclarer plus de 2 `States` dans `manifest.json`.

Note : le skill `/elgato-plugin` de Florian documente ce point pour le SDK moderne Node/TypeScript (`@elgato/streamdeck` v2) — un tout autre SDK que celui utilisé ici. Ses instructions concrètes (`streamdeck create`, `rollup`, `tsc`) ne s'appliquent **pas** à ce projet C#. Seuls deux principes généraux restent transposables : la limite des 2 états (ci-dessus), et la discipline de transparence sur ce qui a été testé — voir §7.

## 4. Deux approches possibles

**Approche A — Extension mécanique.** Ajouter des `case` dans les classes existantes pour chaque fonction manquante, comme aujourd'hui.
Risque faible, mais ne couvre jamais vraiment "toute l'API" : chaque nouveau champ Elite Dangerous futur redemande une modif C# + recompilation + republication. Volume de code qui grossit linéairement avec le nombre de champs (~250+).

**Approche B — Socle générique (recommandée).** Remplacer/étendre l'agrégateur par une structure clé → valeur couvrant *tous* les champs, et créer un petit nombre de **types d'action paramétrables** où l'utilisateur choisit la donnée à afficher dans le Property Inspector, au lieu qu'elle soit câblée dans le code. Plus de travail initial, mais l'ajout d'un champ futur devient une ligne de configuration, pas une recompilation.

→ Recommandation : **Approche B**, puisque l'objectif explicite est « toutes les informations de l'API », pas une liste de plus.

## 5. Nouveaux types d'action proposés (esquisse fonctionnelle)

| Type d'action | Rôle | Données ciblées | Rendu |
|---|---|---|---|
| **État** | bascule 2 icônes selon un booléen choisi | tout flag de `Status.json` (`Flags`/`Flags2`) | icône (généralise `Toggle`) |
| **Valeur** | affiche un champ numérique/texte en direct | pips, carburant, cargo, altitude, solde, température… | texte via `SetTitle` (pas d'icône à gérer — le plus simple à livrer en premier) |
| **Alarme** | change d'icône/joue un son quand un événement Journal précis survient, pendant une durée réglable | n'importe lequel des ~250 événements | icône temporaire + son (généralise `Alarm`) |
| **Compteur** *(optionnel)* | incrémente un compteur à chaque occurrence d'un événement depuis le lancement de la session | ex. sauts FSD, primes encaissées, scans effectués | texte via `SetTitle` |

Ces 4 types, combinés à un menu déroulant catégorisé (voir Annexe A), couvrent la totalité du catalogue sans qu'aucune fonction future ne nécessite de nouveau code C#.

## 6. Refonte de l'agrégateur (`EliteData`)

Remplacer les ~30 booléens câblés en dur et le `switch` à 10 cas par :

- un `Dictionary<string, object>` (ou équivalent fortement typé par réflexion) reconstruit à chaque `StatusFileEvent`, couvrant tous les champs de `Status.json` sans exception ;
- une table « dernier événement reçu par type » (`Dictionary<string, JObject>`), alimentée génériquement par `JournalWatcher.AllEventHandler` pour les ~250 types du Journal, plutôt qu'un `switch` explicite.

Cette table peut être **générée automatiquement par réflexion** sur les classes déjà présentes dans `EliteJournalReader/Events/*.cs` (chacune expose déjà ses champs typés) — pas besoin de la retaper à la main. C'est le même catalogue que l'Annexe A ci-dessous, qui peut servir de base de test/vérification.

## 7. Ce qui reste à vérifier dans l'autre discussion (limites de cette étude)

Cette étude a été faite sans machine Windows ni Stream Deck physique branché. N'ont donc **pas** pu être vérifiés ici :

- le rendu réel d'un texte long (`SetTitle`) sur une touche 72×72px du Stream Deck MK.2 de Florian ;
- l'impact perf d'un rafraîchissement simultané de nombreuses touches « Valeur »/« État » à 500ms–1s d'intervalle (fréquence du Journal/`Status.json`) ;
- la compatibilité ascendante réelle : les 10 actions existantes ne doivent pas régresser au premier lancement de la version étendue ;
- l'état exact du dossier de travail local de Florian (`F:\Github Local\streamdeck-elite`) — commits en cours, branche active.
- la distinction Horizons/Odyssey : les champs `Flags2` (à pied, température corporelle, etc.) n'existent que sous Odyssey — le Property Inspector catégorisé (Annexe A) doit le signaler pour éviter des touches « État » qui ne réagissent jamais en Horizons pur.

À rappeler explicitement en ouverture de l'autre discussion : « ça compile » n'y voudra pas dire « ça marche en jeu ».

## 8. Jalons proposés

- **J0 — Cadrage.** Vérifier l'état du dépôt local de Florian. Figer les noms d'UUID définitifs des nouveaux types d'action (irréversibles une fois publiés). Valider la table de champs de l'Annexe A comme source de vérité unique (C# ↔ Property Inspector).
- **J1 — Socle générique.** Réécrire `EliteData` en agrégateur générique (§6), sans toucher aux 10 boutons existants. Non régressif par construction : rien de visible ne change encore côté Stream Deck.
- **J2 — Action « Valeur ».** Premier type générique livré (le plus simple : pas de gestion d'icônes, juste `SetTitle`). Sert de test grandeur nature du Property Inspector catégorisé. Validation réelle sur le MK.2 de Florian.
- **J3 — Action « État ».** Généralisation du pattern `Toggle` à un flag choisi dynamiquement.
- **J4 — Action « Alarme ».** Généralisation du pattern `Alarm` à n'importe quel événement du Journal.
- **J5 — Action « Compteur » (optionnel).** Si les jalons précédents valident bien l'approche.
- **J6 — Consolidation.** Property Inspector catégorisé finalisé, tests de non-régression sur les 10 actions historiques, versionnage du manifeste, empaquetage `.streamDeckPlugin`, mise à jour du README.

---

## Annexe A — Catalogue complet des champs disponibles, par catégorie

*(table de référence pour le Property Inspector catégorisé et pour la génération par réflexion de l'agrégateur générique — noms exacts tels qu'ils apparaissent dans l'API du jeu)*

### Événements du Journal (`Journal.*.log`)

- 🧭 **Déplacement & accostage** : `ApproachBody`, `ApproachSettlement`, `FSDJump`, `FSDTarget`, `StartJump`, `SupercruiseEntry`, `SupercruiseExit`, `Liftoff`, `Touchdown`, `LeaveBody`, `Location`, `NavBeaconScan`, `NavRoute`, `NavRouteClear`, `JetConeBoost`, `JetConeDamage`, `USSDrop`, `FuelScoop`, `Docked`, `Undocked`, `DockingCancelled`, `DockingDenied`, `DockingGranted`, `DockingRequested`, `DockingTimeout`, `DockFighter`, `DockSRV`, `LaunchFighter`, `LaunchSRV`, `VehicleSwitch`
- ⚔️ **Combat & dangers** : `Bounty`, `CapShipBond`, `CommitCrime`, `CrimeVictim`, `Died`, `EscapeInterdiction`, `FactionKillBond`, `HeatDamage`, `HeatWarning`, `HullDamage`, `Interdicted`, `Interdiction`, `PVPKill`, `Resurrect`, `SelfDestruct`, `ShieldState`, `ShipTargeted`, `UnderAttack`, `CockpitBreached`, `SRVDestroyed`, `FighterDestroyed`, `FighterRebuilt`, `RebootRepair`, `PayBounties`, `PayFines`, `PayLegacyFines`, `RedeemVoucher`, `DatalinkVoucher`
- 🛒 **Commerce, ravitaillement & entretien** : `Market`, `MarketBuy`, `MarketSell`, `MarketRefined`, `BuyAmmo`, `BuyDrones`, `SellDrones`, `BuyTradeData`, `BuyExplorationData`, `SellExplorationData`, `MultiSellExplorationData`, `SellOrganicData`, `CargoDepot`, `TechnologyBroker`, `MaterialTrade`, `Synthesis`, `RefuelAll`, `RefuelPartial`, `Repair`, `RepairAll`, `RepairDrone`, `AfmuRepairs`, `RestockVehicle`, `ReservoirReplenished`, `Passengers`, `DatalinkScan`, `ScientificResearch`, `SearchAndRescue`
- ⛏️ **Minage & prospection** : `AsteroidCracked`, `ProspectedAsteroid`, `MiningRefined`, `LaunchDrone`
- 🔭 **Exploration & scan** : `Scan`, `Scanned`, `ScanOrganic`, `DataScanned`, `DiscoveryScan`, `FSSAllBodiesFound`, `FSSDiscoveryScan`, `FSSSignalDiscovered`, `SAAScanComplete`, `SAASignalsFound`, `CodexEntry`, `MaterialCollected`, `MaterialDiscarded`, `MaterialDiscovered`, `Materials`
- 🚀 **Vaisseau, modules & chantier naval** : `Loadout`, `LoadoutEquipModule`, `LoadoutRemoveModule`, `ModuleBuy`, `ModuleSell`, `ModuleSellRemote`, `ModuleStore`, `ModuleSwap`, `ModuleRetrieve`, `ModuleInfo`, `ModulesInfo`, `MassModuleStore`, `FetchRemoteModule`, `Outfitting`, `Shipyard`, `ShipyardBuy`, `ShipyardNew`, `ShipyardSell`, `ShipyardSwap`, `ShipyardTransfer`, `StoredModules`, `StoredShips`, `SetUserShipName`, `SellShipOnRebuy`
- 🔧 **Ingénieurs** : `EngineerContribution`, `EngineerCraft`, `EngineerLegacyConvert`, `EngineerProgress`
- 📜 **Missions** : `Missions`, `MissionAccepted`, `MissionAbandoned`, `MissionCompleted`, `MissionFailed`, `MissionRedirected`, `CommunityGoal`, `CommunityGoalDiscard`, `CommunityGoalJoin`, `CommunityGoalReward`
- 👥 **Équipage, ailes (wing) & multicrew** : `ChangeCrewRole`, `CrewAssign`, `CrewFire`, `CrewHire`, `CrewLaunchFighter`, `CrewMemberJoins`, `CrewMemberQuits`, `CrewMemberRoleChange`, `EndCrewSession`, `JoinACrew`, `QuitACrew`, `KickCrewMember`, `NpcCrewPaidWage`, `NpcCrewRank`, `WingAdd`, `WingJoin`, `WingLeave`
- 🎖️ **Escadrons** : `AppliedToSquadron`, `DisbandedSquadron`, `InvitedToSquadron`, `JoinedSquadron`, `KickedFromSquadron`, `LeftSquadron`, `SharedBookmarkToSquadron`, `SquadronCreated`, `SquadronDemotion`, `SquadronPromotion`, `SquadronStartup`, `WonATrophyForSquadron`
- 🛰️ **Flotte-mère** : `CarrierBankTransfer`, `CarrierBuy`, `CarrierCancelDecommission`, `CarrierCrewServices`, `CarrierDecommission`, `CarrierDepositFuel`, `CarrierDockingPermission`, `CarrierFinance`, `CarrierJump`, `CarrierJumpCancelled`, `CarrierJumpRequest`, `CarrierModulePack`, `CarrierNameChanged`, `CarrierShipPack`, `CarrierStats`, `CarrierTradeOrder`
- 🚶 **À pied — Odyssey** : `Backpack`, `BackpackChange`, `BookDropship`, `BookTaxi`, `CancelDropship`, `CancelTaxi`, `CollectItems`, `DropItems`, `DropShipDeploy`, `Embark`, `Disembark`, `CreateSuitLoadout`, `DeleteSuitLoadout`, `RenameSuitLoadout`, `SwitchSuitLoadout`, `SuitLoadout`, `BuySuit`, `SellSuit`, `UpgradeSuit`, `BuyWeapon`, `SellWeapon`, `UpgradeWeapon`, `BuyMicroResources`, `SellMicroResources`, `TradeMicroResources`, `TransferMicroResources`, `ShipLocker`, `ShipLockerMaterials`, `UseConsumable`
- ⚡ **Powerplay** : `Powerplay`, `PowerplayCollect`, `PowerplayDefect`, `PowerplayDeliver`, `PowerplayFastTrack`, `PowerplayJoin`, `PowerplayLeave`, `PowerplaySalary`, `PowerplayVote`, `PowerplayVoucher`
- 🌐 **Communauté & communication** : `Friends`, `ReceiveText`, `SendText`
- 🖥️ **Session, CMDR & système** : `Fileheader`, `Commander`, `NewCommander`, `LoadGame`, `ClearSavedGame`, `Shutdown`, `SystemsShutdown`, `Continued`, `Promotion`, `Rank`, `Reputation`, `Statistics`, `Progress`, `Screenshot`, `Music`
- 📦 **Cargaison** : `Cargo`, `CargoTransfer`, `CollectCargo`, `EjectCargo`

*(`MagicMau.IsLiveEvent` est un événement synthétique généré par le plugin lui-même, pas par le jeu — à exclure du catalogue utilisateur.)*

### État instantané (`Status.json`)

- **`Flags`** (bitmask, disponible Horizons + Odyssey) : `Docked`, `Landed`, `LandingGearDown`, `ShieldsUp`, `Supercruise`, `FlightAssistOff`, `HardpointsDeployed`, `InWing`, `LightsOn`, `CargoScoopDeployed`, `SilentRunning`, `ScoopingFuel`, `SrvHandbrake`, `SrvTurret`, `SrvUnderShip`, `SrvDriveAssist`, `FsdMassLocked`, `FsdCharging`, `FsdCooldown`, `LowFuel`, `Overheating`, `HasLatLong`, `IsInDanger`, `BeingInterdicted`, `InMainShip`, `InFighter`, `InSRV`, `HudInAnalysisMode`, `NightVision`, `AltitudeFromAverageRadius`, `FsdJump`, `SrvHighBeam`
- **`Flags2`** (⚠️ Odyssey uniquement) : `OnFoot`, `InTaxi`, `InMulticrew`, `OnFootInStation`, `OnFootOnPlanet`, `AimDownSight`, `LowOxygen`, `LowHealth`, `Cold`, `Hot`, `VeryCold`, `VeryHot`, `GlideMode`, `OnFootInHangar`, `OnFootSocialSpace`, `OnFootExterior`, `BreathableAtmosphere`, `TelepresenceMulticrew`, `PhysicalMulticrew`, `Fsdhyperdrivecharging`
- **`GuiFocus`** (énumération) : `NoFocus`, `InternalPanel`, `ExternalPanel`, `CommsPanel`, `RolePanel`, `StationServices`, `GalaxyMap`, `SystemMap`, `Orrery`, `FSSMode`, `SAAMode`, `Codex`
- **Champs scalaires** : `Timestamp`, `Pips` (Système/Moteurs/Armes), `Firegroup`, `Fuel` (`FuelMain`/`FuelReservoir`), `Cargo`, `LegalState`, `Latitude`, `Longitude`, `Altitude`, `Heading`, `BodyName`, `PlanetRadius`, `Balance`, `Destination` (Système/Corps/Nom), `Oxygen`, `Health`, `Temperature`, `SelectedWeapon`, `Gravity`

### Fichiers annexes (déjà surveillés séparément)

`Cargo.json` (contenu détaillé de la soute), `NavRoute.json` (route de sauts plottée), `ShipLocker.json` (inventaire Odyssey), `Backpack.json` (inventaire porté à pied).

---

*Document généré à partir de l'analyse du code source de `mhwlng/streamdeck-elite` (clone GitHub) et du package `.streamDeckPlugin` fourni par Florian. Aucun test en conditions réelles (machine Windows + Stream Deck physique) n'a été effectué.*
