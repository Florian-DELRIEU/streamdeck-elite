# Icônes — plan (théorie, sans production d'image)

Statut : **document de travail**, pas un lot numéroté. Rien n'est encore dessiné ni commité. Discussion du 2026-09-25, à la demande de Florian, après qu'il a ajouté le pack complet au dépôt ([`8316eeb`](../Elite/Images)) et fourni `EDSI-Template.pptx` (agencement de profil complet, 9 diapositives, hors dépôt).

## 1. Grammaire visuelle du pack existant (constatée, pas inventée)

- Fond noir 72×72. Bandeau du haut = nom de la fonction (1-2 lignes), coloré selon l'état. Pastille arrondie en bas = état, texte noir gras sur fond coloré.
- Palette fermée à 5 couleurs ([README du pack](../Elite/Images/README.md)) :

  | Couleur | Hex | Sens observé |
  |---|---|---|
  | Bleu clair | `#a7d7d6` | ON / prêt / actif |
  | Ambre | `#d18105` | OFF / veille / neutre |
  | Ambre foncé | `#7f4400` | fond neutre, faible contraste (valeur affichée seule) |
  | Rouge | `#ff4646` | alerte / déployé-dangereux / non configuré |
  | Rouge foncé | `#4c1515` | variante sombre du rouge (ex. `Firegroup*_DISBL`) |

- Cas hybride important : `Route.png`, `ENG_INFO.png` ont le nom baké en haut mais **la pastille est vide** — c'est le logiciel Stream Deck qui dessine la valeur par-dessus (texte du plugin). Icône statique + texte dynamique, combinés.
- `Static-Button.png` et les `Generic_*.png` sont des **gabarits vides** (pastille colorée, rien d'écrit) : base pour dessiner vite une nouvelle icône dans le même style.

## 2. Types d'images, mappés sur nos mécanismes

| Type | Mécanisme chez nous | Exemple du pack | Volume |
|---|---|---|---|
| **A — Valeur affichée seule** | `ShowText=true`, pas de règle d'image ; la pastille reste vide, le texte est dessiné par le logiciel | `Route.png`, `ENG_INFO.png` | **1 cadre par catégorie**, pas par donnée (§3) |
| **B — Bascule 2 états** | `ShowText=false` + 1 règle (`isTrue`) + image par défaut | dossier *Toggle Button* | 2 images par donnée |
| **C — États multiples (3-4)** | jusqu'à 4 règles d'`ImageRules` (seuils ou égalité) | *Firegroup* (ON/OFF/DISBL), `FSD_ENGD/READY/READOUT_DISBL` | 3-4 images par donnée |
| **D — Alarme** | action L8 (à venir), paire calme/déclenché + garde `IsLive` | *Alarm Button* (base + `_WARN`) | 2 images par alarme |
| **E — Statique, sans donnée** | pas de donnée liée, juste `DefaultImage` | `Static-Button.png` | 1, réutilisable partout |
| **F — Dossiers** | action native « Créer dossier » du logiciel Stream Deck, hors de notre code | aucun dans le pack | famille visuelle à part (§4) |

## 3. Type A : un cadre par catégorie, pas par donnée

Le catalogue a **1926 clés**, mais elles sont déjà regroupées en **20 catégories** (`Elite.CatalogGen/Categories.cs`) : Statut vaisseau, Statut Odyssey, Statut valeurs, Déplacement & accostage, Combat & dangers, Commerce, Minage, Exploration, Vaisseau & modules, Ingénieurs, Missions, Équipage, Escadrons, Flotte-mère, À pied, Powerplay, Communauté, Session, Cargaison, Colonisation, Autres.

Comme le texte de la valeur est dessiné par le logiciel Stream Deck (pas par l'icône), **20 cadres au maximum** couvrent tout le catalogue en type A — pas 1926. On peut même commencer avec moins : un seul cadre neutre partagé, et n'en décliner un par catégorie que si Florian trouve que ça manque de repère visuel.

## 4. Type F — dossiers : signature visuelle propre

Décision de Florian : les dossiers natifs Stream Deck doivent se reconnaître **au style**, indépendamment du texte, sans jamais changer selon l'état du jeu (contrairement à toutes les icônes de données).

Proposition (à valider avec lui avant tout dessin) :
- **Jamais la forme « pastille arrondie en bas »** : cette forme est réservée aux icônes de données (A-D). Un dossier ne doit jamais pouvoir être confondu avec une donnée.
- **Une 6ᵉ couleur, neutre**, hors de la palette « état » (bleu/ambre/rouge) : par exemple un gris ardoise. *Ceci étend la palette fermée du pack : à valider explicitement avec Florian avant utilisation.*
- **Un seul mot centré** (catégorie du dossier : VAISSEAU, SRV, COMBAT, NAV…), police Rubik comme le reste, sans bandeau/pastille séparés.
- **Aucune variante** : aussi bien la couleur que le glyphe interdisent toute confusion avec un état ON/OFF/WARN.

Pas d'image produite : ceci reste une proposition écrite, à confirmer avant le premier dessin.

## 5. Réutilisation du pack existant (types B/C/D) — correspondance

But : ne redessiner que ce qui n'a pas d'équivalent. Le pack couvre déjà la plupart des anciens boutons Toggle/Alarm/Firegroup ; ces mêmes fichiers sont réutilisables tels quels pour les touches « Donnée » équivalentes.

### Bascules (type B) — dossier `Toggle Button/`

| Icônes du pack | Donnée candidate (à vérifier clé exacte au moment de câbler) |
|---|---|
| `Cargo-Scoop_DEPL/_RETR(+_RED)` | `status.Flags.CargoScoopDeployed` |
| `Flight-Assist_ON/_OFF(+_RED)` | `status.Flags.FlightAssistOff` (inversé) |
| `Hard-Points_DEPL/_RETR(+_RED)` | `status.Flags.HardpointsDeployed` |
| `Landing-Gear_DEPL/_RETR(+_RED)` | `status.Flags.LandingGearDown` |
| `Lights_ON/_OFF` | `status.Flags.LightsOn` |
| `Night-Vision_ON/_OFF` | `status.Flags2.NightVision` (à confirmer selon flag exact) |
| `Silent-Running_ON/_OFF(+_RED)` | `status.Flags.SilentRunning` |
| `Supercruise_ON/_OFF` | `status.Flags.Supercruise` |
| `SRV-Drive-Assist_ON/_OFF(+_RED)` | flag SRV à identifier |
| `SRV-Handbrake_ON/_OFF(+_RED)` | à vérifier si le jeu expose cette donnée |
| `SRV-Turret_ON/_OFF(+_RED)` | flag SRV à identifier |
| `Comms/Nav/Role/Systems/Galaxy-Map/System-Map-Panel_ON/_OFF` | `status.GuiFocus` == la valeur d'enum correspondante (règle `equals`, pas `isTrue`) |
| `FSD_ON/_OFF` (dans *Toggle Button*) | à distinguer de `FSD_ENGD/READY/READOUT_DISBL` (racine) — deux jeux d'icônes FSD existants, sens à clarifier au moment venu |

### États multiples (type C)

| Icônes du pack | Donnée candidate |
|---|---|
| `Firegroup Button/Group-X_ON/_OFF/_DISBL` | `status.FireGroup` pour ON/OFF ; **`DISBL` reste inutilisable** (le jeu n'expose pas la configuration des groupes — même limite que la touche Firegroup historique, `docs/L7-retours-d6.md`) |
| `FSD_ENGD/READY/READOUT_DISBL` | état FSD à 3 valeurs, flags à identifier (`FsdCharging`/`FsdCooldown`/`FsdMassLocked` ?) |

### Alarmes (type D, pour L8)

| Icônes du pack | Alarme candidate |
|---|---|
| `Chaff` / `Chaff_WARN` | tir de paillettes (`FireChaffLauncher` ou événement associé) |
| `Heat-Sink` / `Heat-Sink_WARN` | dissipateur thermique éjecté |
| `Highest-Threat` / `Highest-Threat_WARN` | ciblage de la plus grande menace / sous attaque |
| `Shield-Cell` / `Shield-Cell_WARN` | cellule de bouclier utilisée |

À reprendre précisément quand L8 sera lancé (choix des événements exacts, garde `IsLive`).

### Valeurs (type A) déjà couvertes

`Power Button/ENG_INFO,SYS_INFO,WEP_INFO,Reset-Power` (pips), `Limpet Button/*` (cargaison), `Route*` (navigation/cible) : ces cadres existants suffisent pour les données équivalentes sans rien redessiner.

### Générique

`Generic_DISBL/_ENGD/_READY/_WARN.png` : gabarits vides déjà dans le pack, à utiliser comme point de départ pour toute donnée qui n'a **aucun** équivalent dans le pack.

## 6. Méthode pour la suite

Pas de production en lot. Quand Florian crée une touche « Donnée » en bascule/multi-état/alarme :
1. Chercher d'abord un équivalent dans le pack existant (tableau ci-dessus, ou par nom) → réutilisation directe, aucun dessin.
2. Sinon, partir d'un gabarit `Generic_*` et composer le texte (à la main pour l'instant, ou avec un petit script si le volume le justifie — à revoir plus tard, pas maintenant).
3. Jamais de nouvelle couleur pour une icône de **donnée** (palette fermée à 5, §1). Une 6ᵉ couleur n'est envisagée que pour les dossiers (§4), et seulement après validation de Florian.

Cette approche évite de préparer 1926 icônes d'avance : le volume réel de type B/C/D est celui que Florian choisit vraiment de câbler, pas la taille du catalogue.
