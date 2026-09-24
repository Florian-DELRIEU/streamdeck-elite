# L5 — Tiroir : chaque vue de « Donnée » est une action complète

Statut : **terminé** le 2026-09-24 (code). Le test en jeu **D5 reste à faire avec Florian** : voir la dernière section.

## Pourquoi

Retours de D4 (2026-09-24) :
- **ce qui marche** : les icônes qui suivent un état (vision nocturne, train) avec leur commande ;
- **défaut 1** : quand la vue change, le texte suit mais **pas l'image**, parce qu'en L4 l'icône suivait toujours la vue 1 ;
- **défaut 2** : avec une commande et « Défiler », **chaque appui change de vue ET envoie la commande**.

**Décision D6** (Florian, idée du « tiroir ») :
- chaque vue devient une **action complète** : donnée, texte, icône, commande et son ;
- **deux gestes distincts**, réglables par touche. Par défaut, un **appui court agit** et un **appui long (0,5 s) passe à la vue suivante** ;
- on reste dans l'action « Donnée », sans nouvelle action ni nouvel UUID.

**Non-régression de D4** : les anciennes touches affichaient l'image par défaut. Ce n'était **pas** le plugin : le dossier d'icônes de Florian avait quitté Téléchargements. Ses 87 chemins ont été corrigés dans son profil Stream Deck (sauvegarde du profil sur le Bureau).

## Résultat

- `build.ps1` compile en Debug et en Release (`== BUILD OK`, seulement MSB3884), et `test.ps1` passe **91 tests**, dont 25 nouveaux :
  - gestes ;
  - facteurs `/32`, `*4`, `*100/32` ;
  - vues complètes ;
  - héritage ;
  - compatibilité avec la vraie touche « night vision » de D4.
- La page de réglages a été vérifiée dans le navigateur à 360 px, avec les réglages réels de cette touche :
  - « Afficher » hérité par la vue 2 ;
  - la commande reste sur la vue 1 ;
  - les tests proposés dépendent du type de donnée de **la vue éditée** ;
  - les 4 listes de commandes sont remplies ;
  - le geste est réglable ;
  - 94 réglages sont envoyés, et `pressCycle` a disparu.
- **Non vérifié :** les gestes sur le vrai Stream Deck et le comportement en jeu. C'est l'objet de D5.

## Fonctionnement

| Élément | Règle |
|---|---|
| Vue 1 | Mêmes réglages JSON qu'en L3/L4 (`source`, …, `showText`, `backgroundImage`, `rule1Op…`, `pressCommand`, `clickSound`). |
| Vues 2 à 4 | Mêmes noms suivis du numéro : `showText2`, `backgroundImage2`, `rule1Op2`, `rule1Value2`, `rule1Image2`, `pressCommand2`, `clickSound2`… Une vue existe si elle a une clé, **ou** une commande, **ou** sa propre icône. On peut donc faire une vue sans donnée, par exemple une icône fixe et « ouvrir la carte ». |
| Icône | Une vue **avec** icône propre (image par défaut ou règle) teste **sa** donnée. Une vue **sans** icône propre reprend l'icône de la vue 1, calculée sur la donnée de la vue 1, comme en L4. |
| Afficher le texte | Absent pour une vue (touches créées avant L5) → même choix que la vue 1. Le plugin fait cette copie au chargement (`InheritShowText`), et la page de réglages aussi. |
| Commande et son | **Jamais hérités.** Une vue sans commande ne fait rien à l'action. |
| Geste (`pressMode`) | `shortActLongView` (défaut) ou `shortViewLongAct`. Une minuterie déclenche l'action longue **dès 0,5 s**, sans attendre le relâchement ; l'action courte part au relâchement. Avec une seule vue, les deux gestes agissent. |
| « Agir » | `EliteKeys.SendKeypress(commande de la vue affichée)`, puis le son de la vue. |
| Facteur | Un nombre, ou une suite `*`/`x`/`/` : `/32`, `*4`, `100/32`, `*100/32`. Une saisie invalide (ou une division par 0) donne 1 et un WARN. |
| `pressCycle` (L4) | Ignoré, puis supprimé des réglages à la sauvegarde suivante. |

**Code :**
- `PressGesture` (pure) : `Resolve(isLong, mode, viewCount)` → `Act` ou `NextView` ;
- `DataKeyConfig` : `Views`, `IconViewOf(view)`, `PressMode` ;
- `ValueSettings.ParseScale` ;
- `ValueAction` : `KeyPressed` arme la minuterie, `KeyReleased` décide, `OnLongPress` s'exécute sur le thread de la minuterie.

## Page de réglages générée

`Elite/PropertyInspector/Elite/Generic.html` (4 vues × 23 champs + 2 réglages de touche = **94** réglages) et le bloc de propriétés C# des vues 2 à 4 de `ValueAction.cs` (entre les marqueurs `<generated-views-2-4>`) sont **produits par `tools/make-generic-html.py`**. Pour ajouter ou modifier un champ :
1. modifier le générateur ;
2. lancer `python tools/make-generic-html.py` ;
3. si `generic.js` a été touché, repasser l'échappement ASCII ;
4. lancer `build.ps1` puis `test.ps1`.

**Ne jamais renommer un nom JSON existant** : des touches sont déjà placées dans le profil de Florian.

---

## D5 — Test en jeu (à faire par Florian)

### 1. Installer L5
Dis à Claude « déploie L5 ». À partir de maintenant, il installe le build **Release 64 bits**, identique à la version officielle. La sauvegarde de la version d'origine reste sur ton Bureau.

### 2. Tes touches de test existantes
- **Night vision** :
  - un appui **court** envoie la commande de la vue affichée ; un appui **long** (tenir environ ½ s) change de vue ;
  - sa vue 2 n'a pas encore de commande : choisis-en une si tu veux (page de réglages → « Vue 2 » → « Action (appui) ») ;
  - ses images d'état restent celles de la vue 1 tant que la vue 2 n'a pas d'icône propre.
- Les autres touches à une seule vue fonctionnent comme avant : un appui, court ou long, envoie la commande.

### 3. Une touche « tiroir » à créer
Une touche « Donnée » avec 3 vues. Pour chaque vue, choisis-la dans « Vue éditée », puis règle sa donnée, décoche « Afficher » si tu ne veux pas de texte, et règle son icône et sa commande :

| Vue | Donnée | Icône | Commande |
|---|---|---|---|
| 1 | `status.Flags.LightsOn` | règle « est vrai » → `lights on.png`, défaut `lights off.png` | Ship Spot Light Toggle |
| 2 | `status.Flags.NightVision` | règle « est vrai » → `night vision on.png`, défaut `night vision off.png` | Night Vision Toggle |
| 3 | (aucune) | défaut `galaxy map off.png` | Galaxy Map Open |

À vérifier :
- l'appui long fait défiler lumières → vision nocturne → carte, et l'image suit ;
- l'appui court agit sur la vue affichée.

### 4. Geste inversé et facteur
- Sur une autre touche à 2 vues, choisis « Court : vue · long : action » et vérifie l'inversion.
- Sur ta touche carburant, mets le facteur `*100/32` et le suffixe ` %` (pour un réservoir de 32 t).

### 5. Non-régression et retour
Vérifie que tes anciennes touches sont toujours correctes, puis dis à Claude ce qui va ou non. En particulier : le délai de 0,5 s de l'appui long te convient-il ?
