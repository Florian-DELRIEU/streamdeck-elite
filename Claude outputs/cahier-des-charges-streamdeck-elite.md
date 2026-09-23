# Cahier des charges — Extension « toute l'API » du plugin Stream Deck Elite Dangerous

**Phase** : 2 (plan / cahier des charges) — suit l'étude de faisabilité et le cadrage.
**Base de code** : fork `Florian-DELRIEU/streamdeck-elite`, branche `master`, commit `71119aa` (identique à `mhwlng/streamdeck-elite`, aucune modification locale).
**Statut** : **figé** — décisions de cadrage validées par Florian.

---

## 0. Décisions validées

| # | Décision | Choix retenu |
|---|---|---|
| D1 | **Identité du plugin** | Identifiant **`com.mhwlng.elite` conservé** (tes 12 boutons actuels restent fonctionnels, un seul plugin installé). Nom affiché renommé en **« ZV Stream Deck Elite »**. Les nouveautés de mhwlng se récupèrent par `git merge`, pas en installant sa version officielle (qui écraserait la tienne). |
| D2 | **Appui sur les touches génériques** | **Commande clavier optionnelle** pour État et Alarme, choisie dans la liste des 316 raccourcis du jeu (défaut : aucune). Valeur : affichage seul. |
| D3 | **Point de contrôle intermédiaire** | **Oui** : test en jeu sur le MK.2 après le socle + une action Valeur minimale, avant de terminer les 3 actions. |

Les autres choix techniques sont tranchés ci-dessous avec leur justification.

---

## 1. Objectif

Permettre de construire une touche Stream Deck à partir de **n'importe quelle donnée** du jeu (champ de `Status.json` ou champ d'un événement du Journal) sans écrire de C# pour chaque nouvelle donnée, via trois nouveaux types d'action paramétrables : **Valeur**, **État**, **Alarme**. **Compteur** est réservé (conçu pour être ajouté plus tard sans refonte) mais hors périmètre.

---

## 2. Constats vérifiés dans le code (et corrections de l'étude)

1. **12 actions existantes, pas 10.** Le manifeste déclare aussi `dial` et `firegroupdial` (Stream Deck+). Toutes ont **1 seul `State`** et changent d'image par code — pattern confirmé.
2. **Environnement de build** : .NET Framework **4.8**, projet `.csproj` classique avec `packages.config`, SDK `StreamDeck-Tools` 6.3.1 + `streamdeck-client-csharp` 4.3.0, manifeste `SDKVersion: 2`, Stream Deck ≥ 6.1. → **Compilation sous Windows (Visual Studio / MSBuild)** ; le bac à sable Linux de Claude ne peut au mieux que pré-vérifier.
3. **Le Journal est rejoué au démarrage.** `JournalWatcher.StartWatching()` relit les journaux de la session en cours avant de passer « live » (`IsLive = true`, puis événement synthétique `MagicMau.IsLiveEvent`). Conséquences : bonne nouvelle pour **Valeur** (les dernières valeurs sont restaurées au lancement), piège pour **Alarme** (sans garde, toutes les alarmes de la session se déclencheraient au démarrage).
4. **Les événements inconnus de la bibliothèque sont perdus.** `AllEventHandler` ne se déclenche que pour les ~250 types ayant une classe C# dans `EliteJournalReader/Events/`. Un nouvel événement ajouté par Frontier serait ignoré silencieusement → l'objectif « sans recompilation » exige un petit correctif (§4.2).
5. **Chaque événement expose son JSON brut** (`JournalEventArgs.OriginalEvent`, un `JObject`) : on peut lire n'importe quel champ par son chemin, sans réflexion sur les classes typées pour les *valeurs*.
6. **Rafraîchissement des touches** : les touches existantes se redessinent sur événement Journal, sur appui et (pour certaines) au tick d'1 s du SDK. D'après le code, **aucune ne s'abonne à `StatusWatcher.StatusUpdated`** — `Toggle` ne réagit donc pas directement à un changement de `Status.json`. *À confirmer en jeu*, mais les nouvelles actions devront, elles, s'abonner explicitement aux changements de statut.
7. **`EliteData` applique des règles dérivées** : les flags SRV ne valent `true` que si on est dans le SRV ; `HardpointsDeployed` est forcé à `false` en supercruise/saut. Les nouvelles actions liront les **valeurs brutes** ; les règles dérivées restent propres aux boutons historiques.
8. **Fichiers annexes** : seuls `Cargo.json` et `NavRoute.json` sont surveillés. `ShipLockerWatcher` et `BackpackWatcher` existent dans la bibliothèque mais **ne sont pas branchés** (correction de l'Annexe A de l'étude).
9. **Canal PI ↔ plugin disponible** : `sdtools.common.js` gère `sendToPropertyInspector` et `sendToPlugin`.
10. Les handlers sont correctement désabonnés dans `Dispose()` — pas de fuite à corriger, pattern à reproduire.

---

## 3. Périmètre

**Dans le périmètre**
- Magasin de données générique (Status.json + dernier événement de chaque type du Journal).
- Correctif minimal d'`EliteJournalReader` pour ne perdre aucun événement.
- Catalogue des champs généré automatiquement, utilisé par un Property Inspector commun.
- Actions **Valeur**, **État**, **Alarme** (+ commande clavier optionnelle sur État et Alarme, D2).
- Champs Odyssey (`Flags2`, oxygène, santé, température, gravité, arme sélectionnée) inclus.
- Non-régression des 12 actions existantes.

**Hors périmètre (V1)**
- Action **Compteur** (réservée, voir §5.4).
- Agrégations sur des tableaux (ex. somme de l'inventaire, déjà couverte par le bouton Limpet) — V1 ne lit que des champs scalaires, plus le **nombre d'éléments** d'un tableau.
- Rendu de texte dessiné dans l'image (GDI) : V1 utilise `SetTitle`.
- Branchement de `ShipLocker.json` / `Backpack.json`.
- Publication sur le Marketplace Elgato.

---

## 4. Architecture cible

### 4.1 Principe directeur : additif

On **ajoute à côté**, on ne remplace pas. `EliteData` et les 12 boutons restent intacts (l'étude parlait de « remplacer » l'agrégateur : trop risqué, et inutile). Les nouveaux fichiers vont dans un dossier dédié (`Elite/Generic/`) pour limiter les conflits lors d'un futur `git merge` depuis mhwlng. Modifications de fichiers existants limitées à : `Program.cs` (branchement), `manifest.json` (3 actions), `Elite.csproj` (nouveaux fichiers), `JournalWatcher.cs` (§4.2).

### 4.2 Correctif `EliteJournalReader` — événement brut

Ajouter dans `JournalWatcher` un événement `RawEventHandler` déclenché dans `Parse()` pour **chaque** ligne valide, que le type soit connu ou non, avec : nom de l'événement, `JObject` brut, et l'état `IsLive`. Environ 10 lignes, sans effet sur le comportement existant.

### 4.3 Magasin de données — `EliteStore`

Classe statique, thread-safe (les watchers appellent depuis des threads d'arrière-plan : `ConcurrentDictionary` ou verrou).

- **Statut** : à chaque `StatusUpdated`, conversion du `StatusFileEvent` en entrées clé → valeur ; les bitmasks `Flags`/`Flags2` sont décomposés en booléens par `Enum.GetValues` (aucun nom retapé à la main).
- **Journal** : à chaque `RawEventHandler`, mémorisation du dernier `JObject` par type d'événement, avec son horodatage et un indicateur « reçu en live ».
- **Lecture** : `bool TryGet(string key, out JToken value)`.
- **Notification** : événement `DataChanged(IReadOnlyCollection<string> changedKeys)`, émis uniquement pour les clés dont la valeur a réellement changé.
- **Prévu pour Compteur** : le magasin garde en interne un compteur d'occurrences *live* par type d'événement (coût nul, non exposé en V1).

**Convention de clés** (source unique partagée par le C# et le Property Inspector) :

| Source | Forme de la clé | Exemples |
|---|---|---|
| Flags de statut | `status.Flags.<Nom>` / `status.Flags2.<Nom>` | `status.Flags.LandingGearDown`, `status.Flags2.OnFoot` |
| Champs de statut | `status.<Chemin>` | `status.Fuel.FuelMain`, `status.GuiFocus`, `status.Pips.Engine`, `status.Balance` |
| Champ d'événement | `journal.<Événement>.<Chemin JSON>` | `journal.FSDJump.JumpDist`, `journal.Bounty.TotalReward` |
| Taille d'un tableau | `journal.<Événement>.<Chemin>.#count` | `journal.Cargo.Inventory.#count` |
| Horodatage | `journal.<Événement>.timestamp` | `journal.Docked.timestamp` |

Les chemins d'événements utilisent **les noms JSON du jeu** (tels qu'écrits dans le Journal), pas les noms des propriétés C#.

### 4.4 Catalogue des champs

- **Généré à la compilation**, pas maintenu à la main : un petit outil (projet console ou test) parcourt par réflexion les classes `Events/*EventArgs` (en respectant les attributs `[JsonProperty]` pour retrouver les noms JSON), `StatusFlags`, `MoreStatusFlags` et `StatusFileEvent`, et écrit `PropertyInspector/catalog.js`.
- Chaque entrée : clé, catégorie (celles de l'Annexe A de l'étude), type (`bool` / `number` / `text` / `enum` + valeurs possibles), marqueur **Odyssey**.
- Embarqué dans le Property Inspector : le menu fonctionne même jeu éteint, sans aller-retour avec le plugin.
- **Champ personnalisé** : le PI permet aussi de saisir une clé à la main — c'est ce qui couvre un événement futur absent du catalogue (le magasin le reçoit déjà grâce au §4.2).
- L'événement synthétique `MagicMau.IsLiveEvent` est exclu.

### 4.5 Rafraîchissement

- Chaque instance d'action s'abonne à `EliteStore.DataChanged` et ne se redessine que si **sa** clé a changé.
- Chaque instance mémorise le dernier titre / la dernière image envoyés et **n'appelle `SetTitleAsync` / `SetImageAsync` que si le rendu change** — protège contre la fréquence d'écriture de `Status.json`.
- Le tick d'1 s ne sert qu'aux temporisations (expiration d'une alarme).
- Désabonnement systématique dans `Dispose()`.

---

## 5. Spécification des actions

UUID sous le préfixe `com.mhwlng.elite` (D1). **Les UUID sont définitifs dès la première utilisation** : une touche configurée garde son UUID dans le profil Stream Deck.

Toutes : 1 seul `State` dans le manifeste, `SupportedInMultiActions: false`, `Controllers: ["Keypad"]`, images pilotées par le code.

### 5.1 Valeur — `com.mhwlng.elite.value`

Affiche en texte la valeur d'une clé.

| Réglage | Rôle | Défaut |
|---|---|---|
| `source` | clé du catalogue | — |
| `prefix` / `suffix` | texte autour de la valeur (ex. « Fuel ») | vide |
| `decimals` | nombre de décimales | 0 |
| `scale` / `offset` | conversion linéaire (ex. K → °C : `offset = -273.15`) | 1 / 0 |
| `compact` | abrégé 12 345 678 → « 12,3 M » (utile pour le solde) | non |
| `emptyText` | affiché tant qu'aucune donnée n'est reçue | `—` |
| `backgroundImage` | image de fond | image par défaut |

Rendu via `SetTitle` ; police et position du titre réglées dans le logiciel Stream Deck. Booléens affichés « Oui/Non », énumérations par leur nom. Appui : sans effet.

### 5.2 État — `com.mhwlng.elite.state`

Bascule entre deux images selon une condition.

| Réglage | Rôle |
|---|---|
| `source` | clé du catalogue |
| `operator` | `isTrue`, `isFalse`, `equals`, `notEquals`, `lt`, `lte`, `gt`, `gte` (proposés selon le type de la clé) |
| `operand` | valeur de comparaison (ex. `GalaxyMap` pour `status.GuiFocus`, `25` pour le carburant) |
| `onImage` / `offImage` | images condition vraie / fausse |
| `clickBinding` | commande clavier optionnelle (D2) |
| `clickSound` | son optionnel à l'appui |

Donnée absente → image « faux ». Les comparaisons sur `GuiFocus`, `LegalState` ou le carburant couvrent des cas que le Toggle actuel ne sait pas faire.

### 5.3 Alarme — `com.mhwlng.elite.eventalarm`

(`com.mhwlng.elite.alarm` est déjà pris par l'alarme historique.)

Passe en image d'alerte quand un événement du Journal survient.

| Réglage | Rôle | Défaut |
|---|---|---|
| `event` | type d'événement (catalogue ou saisie libre) | — |
| `filter` | condition optionnelle sur un champ de l'événement (mêmes opérateurs que État ; ex. `UnderAttack.Target equals You`) | aucune |
| `duration` | durée d'alerte en secondes ; `0` = jusqu'à un appui | 5 |
| `activeImage` / `idleImage` | images alerte / repos | — |
| `alarmSound` | son joué au déclenchement | aucun |
| `clickBinding` | commande clavier optionnelle (D2) | aucune |

Règles impératives : **aucun déclenchement pendant le rejeu** (événements reçus avec `IsLive = false`) ; un nouvel événement pendant l'alerte relance la durée ; en mode `duration = 0`, l'appui acquitte l'alerte *avant* d'envoyer la commande éventuelle.

### 5.4 Compteur — réservé

UUID prévu `com.mhwlng.elite.counter`, non déclaré dans le manifeste en V1. Le compteur live interne du magasin (§4.3) permettra de l'ajouter sans toucher au socle.

---

## 6. Property Inspector commun

- Un gabarit HTML partagé par les 3 actions (sections affichées selon l'action), réutilisant `sdpi.css` et `sdtools.common.js` existants.
- **Sélecteur de donnée à deux niveaux** : catégorie (avec les emojis de l'Annexe A) puis champ, plus une **zone de recherche** — ~250 événements et leurs champs ne tiennent pas dans un menu plat.
- Les champs Odyssey sont suffixés « (Odyssey) ».
- Les opérateurs proposés s'adaptent au type de la clé ; pour une énumération, `operand` devient une liste déroulante.
- Liste des commandes clavier (D2) générée de la même façon que le catalogue, par réflexion sur `UserBindings.cs` (Ship / SRV / À pied / Général).
- Option « Saisie manuelle de la clé » pour les champs hors catalogue.

---

## 7. Exigences non fonctionnelles

- **Non-régression** : aucun comportement des 12 actions existantes ne change ; les réglages déjà enregistrés dans tes profils restent valides.
- **Robustesse** : une clé inconnue, un JSON inattendu ou une conversion impossible affichent `emptyText` / l'image « faux » et écrivent un avertissement dans `pluginlog.log` — jamais d'exception qui fasse tomber le plugin.
- **Performance** : aucune écriture vers le Stream Deck sans changement de rendu ; objectif, 15 touches génériques actives sans latence perceptible.
- **Journalisation** : niveau DEBUG listant, pour une clé suivie, chaque changement de valeur (outil de diagnostic en jeu).
- **Identité** : dans `manifest.json`, `Name` devient « ZV Stream Deck Elite » ; `UUID`, `CodePath` et nom du dossier `com.mhwlng.elite.sdPlugin` inchangés. La catégorie (`Category`) peut suivre le même renommage.
- **Traçabilité** : `manifest.json` passe en version `2.8.0` ; commits atomiques par lot sur une branche `feature/generic-api`.

---

## 8. Vérification et critères d'acceptation

**Ce que Claude peut vérifier sans ta machine** : cohérence du code, validité JSON du manifeste, tests unitaires du magasin et du catalogue (alimentés par de vraies lignes de Journal), éventuellement une pré-compilation sous Mono.
**Ce qui exige ta machine** : compilation réelle .NET 4.8, installation dans le logiciel Stream Deck, comportement en jeu. « Ça compile » ≠ « ça marche ».

| Test | Critère d'acceptation |
|---|---|
| Magasin (unitaire) | Un extrait réel de ton Journal + un `Status.json` rejoués produisent les bonnes valeurs pour 10 clés de référence |
| Catalogue | Chaque type d'événement de `Events/` y figure ; les noms JSON correspondent à ceux du Journal |
| Valeur | Carburant, solde (abrégé) et système actuel s'affichent et suivent le jeu en moins de 2 s |
| État | Train d'atterrissage (flag), Carte galactique (`GuiFocus`), À pied (Odyssey) basculent correctement |
| Alarme | `UnderAttack` filtré sur `You` se déclenche en combat, **pas** au lancement du plugin avec un journal de combat déjà écrit |
| Commande (D2) | L'appui envoie bien le raccourci choisi |
| Non-régression | Les 12 actions historiques d'un profil existant fonctionnent comme avant |
| Robustesse | Jeu éteint, puis lancé, puis quitté : aucun plantage, affichage cohérent |

---

## 9. Découpage pour la phase 3 (exécution)

| Lot | Contenu | Modèle conseillé |
|---|---|---|
| L0 | Branche `feature/generic-api`, vérification que le projet compile *tel quel* sur ta machine (référence de départ) | Sonnet 5 |
| L1 | Correctif §4.2 + `EliteStore` + tests unitaires | **Opus 5.5** (cœur du projet, multi-fichiers, erreurs coûteuses) |
| L2 | Générateur de catalogue + `catalog.js` + liste des commandes | Opus 5.5 |
| L3 | Action Valeur minimale + PI commun (sélecteur) | Sonnet 5 |
| ✔ | **Point de contrôle en jeu** (D3) | — |
| L4 | Valeur complète, État, Alarme | Sonnet 5 (plan suffisamment granulaire) |
| L5 | Non-régression, version, empaquetage `.streamDeckPlugin`, README | Sonnet 5 |

L'idéal pour l'exécution est un environnement qui a accès à ton dépôt et peut lancer MSBuild sur ta machine (Claude Code), plutôt que le chat.

---

## 10. Risques

| Risque | Parade |
|---|---|
| Écart entre noms C# et noms JSON d'un événement | Lecture des attributs `[JsonProperty]` + test unitaire sur de vraies lignes de Journal |
| Rafraîchissement trop fréquent | Filtrage par clé + comparaison du rendu avant envoi (§4.5) |
| Titres illisibles sur 72×72 px | Options `compact` / `decimals` ; vérifié au point de contrôle D3 |
| Conflit avec une future version officielle | Code isolé dans `Elite/Generic/`, modifications minimales des fichiers existants |
| Faux déclenchements d'alarmes au démarrage | Garde `IsLive` obligatoire, testée explicitement (§8) |
| Bibliothèque `EliteJournalReader` en retard sur le jeu | Correctif §4.2 + saisie manuelle de clé |
