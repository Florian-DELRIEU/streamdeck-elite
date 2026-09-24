# L7 — Retours de D6 : commandes, raccourci libre, vue mémorisée, groupe de tir bloqué

Statut : **terminé** le 2026-09-25 (code). Le test **D7 reste à faire avec Florian** : voir la dernière section.

## Retours de D6 (2026-09-25)

| Retour | Suite donnée |
|---|---|
| Bouton « i » | ✅ « clair et précis ». |
| « Ajouter des raccourcis dans Commande » | Précisé par Florian : **raccourci clavier libre**, **trouver plus vite**, et une **explication au survol** de chaque commande. Des libellés en français ne sont pas nécessaires. |
| Vue revenue à 1 après être sorti puis revenu dans un dossier | **Défaut corrigé** : la vue affichée est enregistrée dans les réglages de la touche. |
| `Fire Group (A)`… « ne fait rien » | **Ce n'est pas la configuration de Florian** : ses touches N / F5 sont bien des touches clavier. La commande est **volontairement bloquée** par `EliteKeys.HandleFireGroup` (à pied, en SRV, à quai, posé, train sorti, pendant un saut). Son journal montre qu'il était à quai au début du test, puis en vol avec le train sorti. **La touche affiche désormais ⚠** au lieu de ne rien faire. |

## Résultat

- `build.ps1` compile en Debug et en Release (`== BUILD OK`, seulement MSB3884). `test.ps1` passe **106 tests** (11 nouveaux).
- **Vérifié dans le navigateur** :
  - la recherche « train » **tapée lettre par lettre** donne 12 commandes, celles du train d'atterrissage en tête ;
  - le survol d'un résultat affiche sa description ;
  - le clic choisit la commande, la description apparaît sous la liste et la recherche reste affichée ;
  - avec une **vraie frappe**, Ctrl+Maj+F5 est enregistré `ControlLeft+ShiftLeft+F5` et affiché « Ctrl+Maj+F5 » ; de même Ctrl+A et Flèche haut ;
  - un clic sur le libellé efface le raccourci ;
  - la page envoie ses 102 réglages, **sans `currentView`** ;
  - avec des réglages où `currentView` vaut 2, la page s'ouvre sur la vue 2, puis ne change plus de vue.
- **Non vérifié :**
  - le vrai logiciel Stream Deck ;
  - l'envoi réel du raccourci au jeu ;
  - la vue retrouvée ;
  - le ⚠ sur la touche.

  C'est l'objet de D7.

## Ce qui a changé

### Commandes : recherche et description
- **Descriptions :**
  - `Elite.CatalogGen/commands-fr.json` contient **une description française pour chacune des 366 commandes** (UTF-8, éditable à la main) ;
  - au build, le générateur l'ajoute en 3ᵉ élément dans `commands.js` : `["LandingGearToggle","Landing Gear Toggle","Sort ou rentre le train…"]` ;
  - la ligne de build devient `commands.js 366 commands, 366 descriptions` ;
  - un test échoue si une commande n'a pas de description, ou si une description ne correspond à aucune commande.
- **Recherche dans la page**, section « Action (appui) » :
  - un champ « Recherche » commun aux vues ;
  - les résultats s'affichent dans une liste ouverte, dès 2 lettres ;
  - la recherche porte sur le nom, le libellé anglais, la description et le groupe, sans tenir compte des accents ;
  - les correspondances dans le nom, le libellé ou **la première phrase** de la description passent d'abord. Ainsi, les remarques du type « sans effet train sorti » des groupes de tir ne passent pas devant les commandes du train.
- **Description au survol :**
  - le survol d'un résultat affiche sa description sur une ligne, sous la liste ;
  - une infobulle native (`title`) est aussi remplie, mais le logiciel Stream Deck ne l'affiche peut-être pas ;
  - sous la liste « Commande », une ligne décrit la commande choisie pour la vue éditée.

### Raccourci clavier libre
- **Deux nouveaux réglages par vue :**
  - `pressHotkey{n}` : codes **physiques** des touches (`KeyboardEvent.code`), par exemple `ControlLeft+ShiftLeft+F5` ;
  - `pressHotkeyText{n}` : le texte affiché, par exemple « Ctrl+Maj+F5 ».

  Le code physique ne dépend pas de la disposition du clavier : sur un clavier AZERTY, la touche A donne `KeyQ`, affichée « A ». Le plugin envoie le code de cette même touche physique.
- **Saisie dans la page :**
  - un clic dans le champ, puis appuyer sur la combinaison ;
  - une touche de modification seule (par exemple Ctrl droit) est enregistrée au relâchement ;
  - les modificateurs sont repris aussi des indicateurs de l'événement (`ctrlKey`…) : un Ctrl déjà enfoncé avant le clic compte donc ;
  - un clic sur le libellé « Raccourci » efface le raccourci ;
  - une touche inconnue du plugin affiche « touche non prise en charge ».
- **`Elite/Generic/Hotkey.cs`** :
  - une table `KeyboardEvent.code` → `DirectInputKeyCode` : lettres, chiffres, F1–F24, pavé numérique, flèches, Inser/Suppr/Début/Fin/PgPréc/PgSuiv, `<>`, modificateurs, touches multimédia ;
  - `TryParse` est une fonction pure testée ;
  - `Send` s'exécute sur une tâche d'arrière-plan, avec le même moteur que les commandes (`InputSimulator().Keyboard.DelayedModifiedKeyStroke(…, 40)`) ;
  - un test vérifie que toute touche que la page sait enregistrer est connue du plugin.
- **À l'appui :** la commande part d'abord, puis le raccourci. Celui-ci attend que `StreamDeckCommon.InputRunning` retombe, 1 s au maximum.
- **Une vue qui n'a qu'un raccourci** compte comme vue (`DataKeyConfig`).

### Vue mémorisée
- **Nouveau réglage de touche `currentView`** : le numéro de la vue affichée (1 à 4).
  - **Le plugin l'enregistre** à chaque changement de vue (`SetSettingsAsync`) et le relit quand la touche est recréée (entrée dans un dossier, changement de page, redémarrage).
  - **La page ne l'envoie pas.** `AutoPopulateSettings` garde donc la valeur du plugin, que `ReceivedSettings` réenregistre.
  - Si la vue enregistrée n'existe plus, la touche affiche la vue 1.
- **À l'ouverture, la page de réglages se place sur la vue affichée.** Cela ne se fait qu'au premier chargement, pour ne pas changer de vue pendant l'édition. `generic.js` retire `currentView` avant de remplir les champs.

### Groupe de tir bloqué : ⚠
- **`Elite/Generic/CommandGuard.cs`** reprend les conditions de `EliteKeys.HandleFireGroup` : à pied, en SRV, à quai, posé, train sorti, saut FSD. `EliteKeys` et l'action Firegroup historique ne sont **pas modifiés**.
- **Si une commande `FireGroup-X` d'une touche « Donnée » est bloquée :**
  - la touche affiche le triangle ⚠ de Stream Deck (`ShowAlert`) ;
  - `pluginlog.log` reçoit une ligne du type `FireGroup-B ignored (docked)` ;
  - la commande n'est pas envoyée, comme avant. Le raccourci et le son de la vue restent joués.
- **Question ouverte :** refuser le changement de groupe train sorti est un choix de mhwlng. Florian peut vérifier en jeu si le jeu l'accepte : train sorti, appuyer sur N.

---

## D7 — Test (à faire par Florian)

1. Dis à Claude « **déploie L7** ».
2. **Vue mémorisée :** sur une touche à plusieurs vues dans un dossier, passe à la vue 2 (appui long), sors du dossier puis reviens. La touche doit rester sur la vue 2. Même chose après un redémarrage de Stream Deck.
3. **Commandes :** dans les réglages d'une touche « Donnée », section « Action (appui) », tape `train` ou `phares` dans « Recherche ». Survole les résultats (description sous la liste), puis clique. La description de la commande choisie doit s'afficher sous « Commande ».
4. **Raccourci libre :** clique dans « Raccourci », appuie sur une combinaison que le jeu ou un autre logiciel utilise, puis appuie sur la touche Stream Deck. L'action doit se produire.
5. **Groupes de tir :**
   - `Fire Group (B)` **en vol, train rentré, hors station** : le groupe change ;
   - **à quai** : la touche affiche ⚠.
6. Tes autres touches et tiroirs fonctionnent comme avant.
