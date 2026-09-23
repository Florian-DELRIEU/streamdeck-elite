using System;
using System.Collections.Generic;

namespace Elite.CatalogGen
{
    /// <summary>
    /// Categories of the property inspector menu. Journal events: Annexe A of docs/plan-extension-api-toutes-donnees.md
    /// (covers the 249 events of EliteJournalReader), plus the events only seen in real journals.
    /// Non-ASCII characters are escaped so that the source file encoding does not matter.
    /// </summary>
    internal static class Categories
    {
        public const string StatusFlags = "status-flags";
        public const string StatusFlags2 = "status-flags2";
        public const string StatusValues = "status-values";
        public const string Other = "other";
        public const string OnFoot = "onfoot";

        // menu order
        public static readonly Category[] All =
        {
            new Category(StatusFlags, "Statut — vaisseau (Flags)", "\U0001F6A6"),
            new Category(StatusFlags2, "Statut — Odyssey (Flags2)", "\U0001F9CD"),
            new Category(StatusValues, "Statut — valeurs", "\U0001F4CA"),
            new Category("travel", "Déplacement & accostage", "\U0001F9ED"),
            new Category("combat", "Combat & dangers", "⚔️"),
            new Category("commerce", "Commerce, ravitaillement & entretien", "\U0001F6D2"),
            new Category("mining", "Minage & prospection", "⛏️"),
            new Category("exploration", "Exploration & scan", "\U0001F52D"),
            new Category("ship", "Vaisseau, modules & chantier naval", "\U0001F680"),
            new Category("engineers", "Ingénieurs", "\U0001F527"),
            new Category("missions", "Missions", "\U0001F4DC"),
            new Category("crew", "Équipage, ailes (wing) & multicrew", "\U0001F465"),
            new Category("squadron", "Escadrons", "\U0001F396️"),
            new Category("carrier", "Flotte-mère", "\U0001F6F0️"),
            new Category(OnFoot, "À pied — Odyssey", "\U0001F6B6"),
            new Category("powerplay", "Powerplay", "⚡"),
            new Category("social", "Communauté & communication", "\U0001F310"),
            new Category("session", "Session, CMDR & système", "\U0001F5A5️"),
            new Category("cargo", "Cargaison", "\U0001F4E6"),
            new Category("colonisation", "Colonisation", "\U0001F3D7️"),
            new Category(Other, "Autres", "❔"),
        };

        private static readonly Dictionary<string, string> EventCategory = Build(
            "travel",
            "ApproachBody ApproachSettlement FSDJump FSDTarget StartJump SupercruiseEntry SupercruiseExit Liftoff Touchdown LeaveBody Location NavBeaconScan NavRoute NavRouteClear JetConeBoost JetConeDamage USSDrop FuelScoop Docked Undocked DockingCancelled DockingDenied DockingGranted DockingRequested DockingTimeout DockFighter DockSRV LaunchFighter LaunchSRV VehicleSwitch",
            "combat",
            "Bounty CapShipBond CommitCrime CrimeVictim Died EscapeInterdiction FactionKillBond HeatDamage HeatWarning HullDamage Interdicted Interdiction PVPKill Resurrect SelfDestruct ShieldState ShipTargeted UnderAttack CockpitBreached SRVDestroyed FighterDestroyed FighterRebuilt RebootRepair PayBounties PayFines PayLegacyFines RedeemVoucher DatalinkVoucher",
            "commerce",
            "Market MarketBuy MarketSell MarketRefined BuyAmmo BuyDrones SellDrones BuyTradeData BuyExplorationData SellExplorationData MultiSellExplorationData SellOrganicData CargoDepot TechnologyBroker MaterialTrade Synthesis RefuelAll RefuelPartial Repair RepairAll RepairDrone AfmuRepairs RestockVehicle ReservoirReplenished Passengers DatalinkScan ScientificResearch SearchAndRescue",
            "mining",
            "AsteroidCracked ProspectedAsteroid MiningRefined LaunchDrone",
            "exploration",
            "Scan Scanned ScanOrganic DataScanned DiscoveryScan FSSAllBodiesFound FSSDiscoveryScan FSSSignalDiscovered SAAScanComplete SAASignalsFound CodexEntry MaterialCollected MaterialDiscarded MaterialDiscovered Materials",
            "ship",
            "Loadout LoadoutEquipModule LoadoutRemoveModule ModuleBuy ModuleSell ModuleSellRemote ModuleStore ModuleSwap ModuleRetrieve ModuleInfo ModulesInfo MassModuleStore FetchRemoteModule Outfitting Shipyard ShipyardBuy ShipyardNew ShipyardSell ShipyardSwap ShipyardTransfer StoredModules StoredShips SetUserShipName SellShipOnRebuy",
            "engineers",
            "EngineerContribution EngineerCraft EngineerLegacyConvert EngineerProgress",
            "missions",
            "Missions MissionAccepted MissionAbandoned MissionCompleted MissionFailed MissionRedirected CommunityGoal CommunityGoalDiscard CommunityGoalJoin CommunityGoalReward",
            "crew",
            "ChangeCrewRole CrewAssign CrewFire CrewHire CrewLaunchFighter CrewMemberJoins CrewMemberQuits CrewMemberRoleChange EndCrewSession JoinACrew QuitACrew KickCrewMember NpcCrewPaidWage NpcCrewRank WingAdd WingJoin WingLeave",
            "squadron",
            "AppliedToSquadron DisbandedSquadron InvitedToSquadron JoinedSquadron KickedFromSquadron LeftSquadron SharedBookmarkToSquadron SquadronCreated SquadronDemotion SquadronPromotion SquadronStartup WonATrophyForSquadron",
            "carrier",
            "CarrierBankTransfer CarrierBuy CarrierCancelDecommission CarrierCrewServices CarrierDecommission CarrierDepositFuel CarrierDockingPermission CarrierFinance CarrierJump CarrierJumpCancelled CarrierJumpRequest CarrierModulePack CarrierNameChanged CarrierShipPack CarrierStats CarrierTradeOrder",
            OnFoot,
            "Backpack BackpackChange BookDropship BookTaxi CancelDropship CancelTaxi CollectItems DropItems DropShipDeploy Embark Disembark CreateSuitLoadout DeleteSuitLoadout RenameSuitLoadout SwitchSuitLoadout SuitLoadout BuySuit SellSuit UpgradeSuit BuyWeapon SellWeapon UpgradeWeapon BuyMicroResources SellMicroResources TradeMicroResources TransferMicroResources ShipLocker ShipLockerMaterials UseConsumable",
            "powerplay",
            "Powerplay PowerplayCollect PowerplayDefect PowerplayDeliver PowerplayFastTrack PowerplayJoin PowerplayLeave PowerplaySalary PowerplayVote PowerplayVoucher",
            "social",
            "Friends ReceiveText SendText",
            "session",
            "Fileheader Commander NewCommander LoadGame ClearSavedGame Shutdown SystemsShutdown Continued Promotion Rank Reputation Statistics Progress Screenshot Music",
            "cargo",
            "Cargo CargoTransfer CollectCargo EjectCargo",
            // events only seen in real journals (not in EliteJournalReader)
            "travel", "SupercruiseDestinationDrop",
            "exploration", "ScanBaryCentre",
            "session", "GameModeChange",
            "crew", "WingInvite",
            "commerce", "Resupply",
            "ship", "ClearImpound");

        // Odyssey-only events outside of the "on foot" category
        private static readonly HashSet<string> OdysseyEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ScanOrganic", "SellOrganicData"
        };

        public static string ForEvent(string eventName)
        {
            string category;
            if (EventCategory.TryGetValue(eventName, out category))
                return category;

            if (eventName.StartsWith("Colonisation", StringComparison.OrdinalIgnoreCase))
                return "colonisation";

            return Other;
        }

        public static bool IsOdysseyEvent(string eventName)
        {
            return ForEvent(eventName) == OnFoot || OdysseyEvents.Contains(eventName);
        }

        private static Dictionary<string, string> Build(params string[] categoryThenEvents)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < categoryThenEvents.Length; i += 2)
            {
                foreach (var eventName in categoryThenEvents[i + 1].Split(' '))
                    result[eventName] = categoryThenEvents[i];
            }

            return result;
        }
    }
}
