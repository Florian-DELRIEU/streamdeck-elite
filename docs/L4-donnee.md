# L4 — Action universelle « Donnée » : seuils, vues, appui

Statut : **terminé** le 2026-09-23 (code). Le test en jeu **D4 reste à faire avec Florian** : voir la dernière section.

## Pourquoi ce lot a changé

Le point de contrôle D3 (2026-09-23) a validé l'action Valeur : les valeurs se mettent à jour, le texte est lisible mais un peu petit, et la page de réglages est correcte. Florian a ensuite demandé :
- une **icône qui change selon des seuils** ;
- une **action à l'appui** : faire défiler les données affichées, et/ou envoyer une commande au jeu.

Décisions de Florian, postérieures au cahier des charges :
- **D4** : une **action universelle « Donnée »**, qui garde l'UUID `com.mhwlng.elite.value` de la Valeur, ce qui rend les touches de test de D3 compatibles. L'action « État » du §5.2 est fusionnée dedans : une icône on/off, c'est « Donnée » sans texte. `com.mhwlng.elite.state` n'est jamais déclaré et reste réservé. L'Alarme reste une action séparée.
- **D5** : « Donnée » accepte **plusieurs images selon des règles**, **jusqu'à 4 vues** qui défilent à l'appui, et une **commande clavier** à l'appui. Cela remplace « Valeur : affichage seul » (D2).
- **Découpage** : L4 = Donnée, L5 = Alarme, L6 = non-régression, version 2.8.0, nom, empaquetage et README.

## Résultat

- `build.ps1` compile en Debug et en Release (`== BUILD OK`, seulement MSB3884), et `test.ps1` passe **66 tests**, dont 16 nouveaux :
  - conditions, règles d'image ;
  - lecture des réglages, avec la **compatibilité des touches de L3** ;
  - manifeste, avec un test qui vérifie que `generic.js` est 100 % ASCII.
- La page de réglages a été vérifiée dans le navigateur intégré, servie en HTTP, en largeur réelle d'environ 360 px :
  - les réglages d'une touche de L3 se rechargent correctement (texte affiché, commande conservée) ;
  - changer de vue fonctionne ;
  - le sélecteur écrit bien dans la vue éditée ;
  - les tests d'une règle dépendent du type de la donnée (« < » désactivé pour une énumération) ;
  - les valeurs d'énumération sont proposées ;
  - les 366 commandes sont dans la liste ;
  - rien ne déborde en largeur.
- **Non vérifié :** le comportement sur le Stream Deck et en jeu (images, défilement, commande envoyée). C'est l'objet de D4.

## Fichiers

| Fichier | Rôle |
|---|---|
| `Elite/Generic/Condition.cs` | Tests `isTrue`, `isFalse`, `equals`, `notEquals`, `lt`, `lte`, `gt`, `gte` (fonction pure). |
| `Elite/Generic/ImageRules.cs` | Choix de l'image : première règle vraie sur la donnée principale, **après facteur et décalage**, sinon `null` (= image par défaut). |
| `Elite/Generic/DataKeyConfig.cs` | Lecture des réglages JSON → vues, règles, appui. Les réglages absents prennent leur valeur par défaut, ce qui rend les touches de L3 compatibles. |
| `Elite/Generic/ValueAction.cs` | L'action : rendu du texte et de l'image (envoyés seulement s'ils changent), `KeyPressed` (vue suivante → `EliteKeys.SendKeypress` → son). |
| `Elite/PropertyInspector/Elite/Generic.html`, `generic.js` | Sections Donnée du jeu (avec la vue éditée), Texte, Icône, Appui. |
| `Elite/manifest.json` | Action renommée « Donnée », `FontSize` 14 pour les nouvelles touches. |

## Réglages (noms JSON)

| Réglage | Rôle |
|---|---|
| `source`, `prefix`, `suffix`, `decimals`, `scale`, `offset`, `compact` | Vue 1 (principale), mêmes noms qu'en L3. |
| `source2…4`, `prefix2…4`, … `compact2…4` | Vues 2 à 4. Une vue sans clé est ignorée. |
| `emptyText` | Texte si la donnée est absente (commun à toutes les vues). |
| `showText` | Vrai par défaut. Faux → le plugin n'écrit pas de titre (`SetTitleAsync(null)`), et c'est le titre tapé dans le logiciel Stream Deck qui s'affiche. |
| `backgroundImage` | Image par défaut. |
| `rule1Op…rule4Op`, `rule1Value…`, `rule1Image…` | Règles d'image. Une règle sans test ou dont l'image n'existe pas est ignorée. |
| `pressCycle` | Un appui passe à la vue suivante, en boucle. La vue revient à 1 quand la touche réapparaît (changement de page). |
| `pressCommand` | Nom de commande `commands.js`, envoyé par `EliteKeys.SendKeypress`. Vide = aucune. |
| `clickSound` | Fichier .wav joué à l'appui (`CachedSound` / `AudioPlaybackEngine`, comme les boutons Static). |

**Sémantique des tests :**
- une donnée absente ne satisfait **aucun** test, et c'est l'image par défaut ;
- `isTrue` : booléen vrai, nombre ≠ 0, texte non vide (et différent de faux/non/0) ;
- `equals` : numérique si les deux côtés sont des nombres (virgule acceptée), sinon texte sans tenir compte des majuscules. Pour un booléen : oui/non/vrai/faux/true/false/1/0 ;
- `lt` / `lte` / `gt` / `gte` : nombres seulement, faux sinon.

**Rafraîchissement :** la touche réagit si la clé de la vue affichée **ou** la clé principale change. L'icône suit donc la donnée principale même quand le texte montre une autre vue.

## Notes techniques

- **Ordre d'initialisation de la page** : `generic.js` enveloppe `loadConfiguration`. Il remplit d'abord les listes (commandes…), puis charge les valeurs. Sans cet ordre, la commande enregistrée serait perdue au premier `setSettings()`.
- **Largeur** : une `<select>` prend la largeur de son option la plus longue. Le style `.sdpi-item > select.sdpi-item-value { width: 0; flex: 1 1 auto; }` l'empêche de faire déborder la page, dont la largeur utile est d'environ 344 px (`sdpi.css`).
- **Piège** : l'outil d'écriture de fichiers de Claude Code **convertit les séquences `\u00e9` en vrais caractères**. Pour un fichier qui doit rester en ASCII, il faut repasser un script d'échappement. Le test `ManifestTests` échoue si `generic.js` contient autre chose que de l'ASCII.

---

## D4 — Test en jeu (à faire par Florian)

### 1. Installer la version L4
Dis à Claude « déploie L4 ». Il fermera Stream Deck, copiera la nouvelle version par-dessus (sans rien supprimer) et relancera. La sauvegarde de la version d'origine 2.7.4 est déjà sur ton Bureau (`com.mhwlng.elite.sdPlugin.sauvegarde-2026-09-23`) : pas besoin d'en refaire une. Le plugin met environ 50 s à démarrer.

### 2. Essais sur ton profil « Test ZV »
Tu trouveras des images d'essai dans `F:\Github Local\streamdeck-elite\Elite\Images\` (par exemple `landing gear on.png` / `off.png`, `galaxy map on.png` / `off.png`), ou tu peux utiliser les tiennes.

| Touche | Réglages | À vérifier |
|---|---|---|
| **Carburant** (ta touche de D3) | Icône : image par défaut = verte ; Règle 1 `<` et un seuil bas (par exemple 8 pour un réservoir de 32 t) → rouge ; Règle 2 `<` et un seuil moyen → orange. Vue 2 : `Fuel.FuelReservoir`, préfixe `Rés.\n`, 2 décimales. Appui : case « passer à la vue suivante » cochée. | L'icône change avec le carburant ; un appui fait alterner principal et réservoir. |
| **Train** | `status.Flags.LandingGearDown` (catégorie Flags vaisseau). « Afficher » décoché (tu peux taper « GEAR » dans le champ Titre). Défaut = `landing gear off.png`, Règle 1 « est vrai » → `landing gear on.png`. Commande : Vaisseau → « Landing Gear Toggle ». | L'appui sort ou rentre le train, et l'icône suit. |
| **Carte** | `status.GuiFocus`. « Afficher » décoché. Défaut = `galaxy map off.png`, Règle 1 `=` `GalaxyMap` (proposé dans la saisie) → `galaxy map on.png`. Commande : Vaisseau → « Galaxy Map Open ». | L'appui ouvre la carte, et l'icône passe sur « on ». |

### 3. Non-régression
Sur ton profil habituel, vérifie que tes anciens boutons fonctionnent comme avant.

### 4. Retour
Dis à Claude ce qui marche ou non, ce qui est gênant, et s'il peut lire `pluginlog.log`.
