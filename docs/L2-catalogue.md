# L2 — Catalogue des champs et liste des commandes

Statut : **terminé** le 2026-09-23. Ce document précise le §4.4 et le §6 du cahier des charges. C'est la référence pour L3 (Property Inspector) et L4 (commandes D2).

## Résultat

- `build.ps1` compile en Debug et en Release : `== BUILD OK`, avec seulement l'avertissement MSB3884 de la baseline. Le générateur tourne à chaque build, et un second build ne modifie aucun fichier.
- `Elite/PropertyInspector/catalog.js` : **261 groupes, 1 926 clés**, environ 90 Ko, 100 % ASCII. Les clés proviennent de trois sources :
  - la réflexion sur la bibliothèque : 249 événements ;
  - `Status.json` : 4 groupes ;
  - les champs observés dans les journaux : 8 événements supplémentaires et 266 clés.
- `Elite/PropertyInspector/commands.js` : **366 commandes**, réparties en Vaisseau 153, SRV 42, À pied 60, Général 81 et « Selon l'état du jeu » 30.
- `test.ps1` : **33 tests OK**, dont les 2 critères du §8 pour le catalogue.
- **Non vérifié :** l'affichage dans le logiciel Stream Deck, qui viendra en L3 puis en D3.

## Fonctionnement

| Élément | Rôle |
|---|---|
| `Elite.CatalogGen/` | Générateur (exe console net48). `Elite.csproj` le référence avec `ReferenceOutputAssembly=false`, pour qu'il soit compilé **avant** Elite. Sa cible `AfterTargets="Build"` lance `Elite.CatalogGen.exe generate <racine>`, qui écrit les deux `.js` **seulement si leur contenu change**. En cas d'échec (exception, moins de 1 000 clés, moins de 300 commandes), il affiche `error CATGEN01` et **le build échoue**. En Release, il est compilé en x64, comme la bibliothèque. |
| `Elite.CatalogGen/observed-keys.txt` | Clés vues dans de vrais journaux mais inconnues de la bibliothèque : **uniquement des noms et des types**, jamais de valeur. Ce fichier est commité. |
| `update-observed-keys.ps1` | Relance l'analyse des journaux (`Elite.CatalogGen.exe scan`). Toute clé contenant le nom de CMDR ou le FID (lus dans `Commander` et `LoadGame`) est rejetée. |

**Pour enrichir le catalogue** après une mise à jour du jeu :
1. lancer `build.ps1` ;
2. lancer `update-observed-keys.ps1` ;
3. **relire le diff** de `observed-keys.txt` ;
4. relancer `build.ps1`, puis `test.ps1` ;
5. commiter.

**Ne jamais éditer `catalog.js` ou `commands.js` à la main** : ils sont régénérés à chaque build.

## Format de `catalog.js` (`var ELITE_CATALOG`)

```js
{
  "version": 1,
  "categories": [ {"id":"travel","label":"Déplacement & accostage","emoji":"🧭"}, ... ],  // ordre du menu
  "enums": { "StatusGuiFocus": ["NoFocus", ..., "GalaxyMap", ...], "PlanetClass": ["Metal rich body", ...], ... },
  "groups": [
    {"id":"journal.FSDJump","prefix":"journal.FSDJump","label":"FSDJump","category":"travel","odyssey":false,
     "fields":[["JumpDist","number"],["StarPos.#count","number"],["BodyType","text"],["timestamp","date"], ...]},
    {"id":"status.odyssey","prefix":"status","label":"Status.json (Odyssey)","category":"status-values","odyssey":true,
     "fields":[["Oxygen","number"], ...]}
  ]
}
```

- **Clé complète** : `prefix + "." + field[0]`. C'est exactement la clé d'`EliteStore` (L1), en ignorant la casse.
- **Types** : `bool`, `number`, `text`, `date`, et `enum`, avec `field[2]` = nom dans `enums`. Les valeurs d'énumération sont **celles écrites par le jeu** : l'attribut `[Description]` s'il existe, sinon le nom.
- **Odyssey** : c'est un marqueur **par groupe**. Le statut a donc un groupe « Status.json » et un groupe « Status.json (Odyssey) », qui partagent le préfixe `status`. Sont marqués Odyssey :
  - les événements de la catégorie À pied, plus `ScanOrganic` et `SellOrganicData` ;
  - `status.Flags2.*` ;
  - les champs `Oxygen`, `Health`, `Temperature`, `SelectedWeapon*` et `Gravity`.
- **Catégories** :
  - les 3 catégories du statut ;
  - les 16 catégories de l'Annexe A, qui couvrent les 249 événements de la bibliothèque ;
  - « Colonisation », pour les événements `Colonisation*` ;
  - « Autres », par défaut.

  Seules les catégories non vides sont écrites.
- **Règles de la réflexion** :
  - un tableau, une liste, ou un convertisseur de tableau (`StarPos`, `Parents`…) donne `#count` ;
  - un objet donne une récursion, jusqu'à une profondeur de 4 ;
  - les propriétés calculées (sans setter public), `JToken` et les dictionnaires sont ignorés ;
  - `timestamp` est ajouté à chaque événement.
- **Écriture du jeu prioritaire** : `journal.DropshipDeploy`, et non `DropShipDeploy` comme dans la bibliothèque.

## Format de `commands.js` (`var ELITE_COMMANDS`)

`groups: [{id, label, commands: [[nom, libellé], ...]}]`, avec `id` parmi `ship`, `srv`, `onfoot`, `general`, `smart`.

- **Source** : le `switch` de `EliteKeys.SendKeypress(string)` (`Elite/Buttons/EliteKeys.cs`). Il est lu comme du texte, et le contexte (`BindingType.X`) de chaque `case` donne le groupe.
- **Pour L4** : une touche État ou Alarme qui a une commande appelle simplement `EliteKeys.SendKeypress(nom)`. C'est une fonction existante, sans modification.
- **Commandes « Selon l'état du jeu »** (`LandingGearToggle-ON/OFF`, `FireGroup-A…H`, `GalaxyMapOpen-ON`…) : elles ne sont envoyées que si l'état du jeu le justifie, par exemple le train n'est sorti que s'il est rentré.
- **Écart avec le §6**, qui prévoyait « réflexion sur `UserBindings.cs` » : `UserBindings` donne les noms, pas le contexte (Vaisseau / SRV / À pied / Général). `SendKeypress` est la source exacte de ce que le plugin sait envoyer. Un test vérifie que chaque commande simple est bien une propriété `StandardBindingInfo` de `UserBindings`. `SelectTargetBuggy` n'est pas proposé, car `SendKeypress` ne le gère pas.

## Décisions et écarts

- **Catalogue complété par les journaux**, choix de Florian du 2026-09-23. Avec la réflexion seule, on couvrait 88 % des champs réellement écrits par le jeu. Il manquait 230 champs (`FSSSignalDiscovered.SignalType`, `FSDJump.BodyType`, `Statistics.*`…) et 8 événements (`Colonisation*`, `SupercruiseDestinationDrop`…).
- **Valeurs de `status.LegalState`** : liste écrite à la main à partir de la documentation Frontier (Clean, IllegalCargo, Speeding, Wanted, Hostile, PassengerWanted, Warrant).
- **`status.Destination.System` et `Destination.Body`** sont en `number`, alors que la bibliothèque les déclare en `string`. **`FireGroup`** porte le nom JSON du jeu, alors que la bibliothèque écrit `Firegroup`.
- **Limites de V1** : pas de clés pour les éléments d'un tableau (seulement `#count`). Les objets à clés dynamiques sont ignorés. Le type des champs observés est déduit des valeurs rencontrées ; en cas de mélange, c'est `text`.

## Pour L3 (Property Inspector)

- Les pages du PI sont dans `PropertyInspector/Elite/*.html`. Les scripts s'y chargent avec `<script src="../catalog.js"></script>` et `<script src="../commands.js"></script>`.
- Menu à deux niveaux : catégorie (emoji + libellé), puis groupe et champ. Il faut aussi une zone de recherche sur les clés complètes (1 926 clés), le suffixe « (Odyssey) » pour les groupes `odyssey:true`, une liste déroulante d'opérandes pour les champs `enum`, et la saisie manuelle de clé.
