# L3 — Action Valeur et Property Inspector commun

Statut : **terminé** le 2026-09-23 (code). Le point de contrôle en jeu **D3 reste à faire avec Florian** : voir la dernière section.

## Résultat

- `build.ps1` compile en Debug et en Release (`== BUILD OK`, seulement MSB3884), et `test.ps1` passe **50 tests**, dont 17 nouveaux pour la mise en forme et le manifeste.
- La page de réglages a été vérifiée dans le navigateur intégré, servie en HTTP depuis le plugin compilé. Tout fonctionne :
  - catégories, groupes « (Odyssey) » ;
  - recherche ;
  - choix d'une donnée, qui envoie `source` au plugin ;
  - saisie manuelle hors catalogue, avec un avertissement ;
  - valeurs d'énumération affichées ;
  - resynchronisation sur la clé enregistrée.
- **Non vérifié :** l'affichage sur le MK.2, la lisibilité en 72 px, le délai de rafraîchissement, et la page de réglages dans le vrai logiciel Stream Deck. Ce sont les objectifs de D3.

## Ce qui a été ajouté

| Fichier | Rôle |
|---|---|
| `Elite/Generic/ValueAction.cs` | Action `com.mhwlng.elite.value` (hérite d'`EliteKeypadBase`). |
| `Elite/Generic/ValueFormatter.cs` | `ValueSettings.Parse` (réglages texte → valeurs) et `ValueFormatter.Format` (fonction pure). |
| `Elite/PropertyInspector/Elite/Generic.html` | Page de réglages commune. Les sections `data-actions="value …"` s'affichent selon l'action. |
| `Elite/PropertyInspector/generic.js` | Sélecteur de donnée (100 % ASCII). |
| `Elite/manifest.json` | + action « Valeur » : 1 State, Keypad, `Generic.html`. Nom et version du plugin inchangés (L5). |

## Fonctionnement de l'action Valeur

- **Réglages enregistrés** (noms JSON) : `source`, `prefix`, `suffix`, `decimals`, `scale`, `offset` (textes), `compact` (booléen), `emptyText`, `backgroundImage` (fichier).
- **Rafraîchissement** :
  - la touche s'abonne à `EliteStore.DataChanged` et ne réagit que si **sa** clé a changé (comparaison insensible à la casse) ;
  - le titre n'est envoyé que s'il a changé, sans attendre la réponse, donc sans bloquer les threads de lecture ;
  - pas de rafraîchissement au tick ;
  - un rendu est forcé à la création de la touche et à chaque changement de réglage.
- **Log** : une ligne DEBUG `Value[clé] = texte` (les retours à la ligne s'écrivent `\n`) à chaque changement d'affichage (§7).
- **Image** :
  - image de fond choisie → `Tools.FileToBase64` du projet (le même que les actions existantes) ;
  - sinon → `SetDefaultImageAsync()`.

  Attention : dans `namespace Elite.Generic`, le nom `Tools` désigne `Elite.Tools`. Il faut écrire `BarRaider.SdTools.Tools.AutoPopulateSettings` en entier.

### Règles d'affichage (`ValueFormatter`)

| Donnée | Affichage |
|---|---|
| absente | `emptyText` (défaut « — »). Un champ vidé par l'utilisateur donne un texte vide. |
| booléen | Oui / Non |
| nombre | `valeur × facteur + décalage`, avec virgule décimale et espace **simple** entre les milliers (`12 345 678`). L'espace fine du fr-FR de Windows n'est pas sûre dans la police du Stream Deck. |
| nombre abrégé | 3 chiffres significatifs, k / M / G / T (`1,23 k`, `12,3 M`, `123 M`, `1,5 G`). En dessous de 1 000, format normal. Les décimales sont alors ignorées. |
| date | heure locale `HH:mm` si c'est aujourd'hui, sinon `dd/MM HH:mm`. Les horodatages du jeu sont en UTC. |
| texte, énumération | tel quel |

Titre = préfixe + valeur + suffixe. Le texte `\n` saisi dans le préfixe ou le suffixe devient un retour à la ligne. Les décimales sont bornées entre 0 et 6. Facteur et décalage acceptent `0,5` comme `0.5`. Une saisie invalide reprend la valeur par défaut et écrit un WARN dans le log.

## Page de réglages commune (pour L4)

- **Chargement** : `sdtools.common.js`, puis `../catalog.js`, puis `../generic.js`.
- **Seul vrai réglage partagé** : le champ `source` (classe `sdProperty`). Les listes Catégorie, Recherche et Donnée ne sont **pas** des `sdProperty` : elles ne sont pas enregistrées.
- **`loadConfiguration`** (de `sdtools.common.js`) est enveloppée par `generic.js`, pour resynchroniser les listes quand les réglages arrivent. Il ne faut pas appeler `setSettings()` depuis cette enveloppe : cela ferait une boucle.
- **Ajouter État et Alarme en L4** :
  1. compléter `GENERIC_ACTION_KINDS` dans `generic.js` ;
  2. ajouter des sections `<div data-actions="state">` / `"alarm"` dans `Generic.html` ;
  3. utiliser des `id` uniques : `setSettings()` enregistre **tous** les `sdProperty` de la page, y compris ceux des sections masquées. C'est sans conséquence, car chaque action ne lit que ses propres champs.
- **Pour tester la page hors Stream Deck** : le panneau navigateur de Claude Code affiche les fichiers `file://` en instantané statique, sans JavaScript. Il faut les servir en HTTP, par exemple avec `python -m http.server 8765 --directory Elite/bin/Debug/com.mhwlng.elite.sdPlugin` déclaré dans un `.claude/launch.json` temporaire. On appelle ensuite `loadConfiguration({...})` dans la console, en remplaçant `setSettingsToPlugin` pour capturer ce qui serait envoyé.

## Limite connue

« Système actuel » = `journal.FSDJump.StarSystem`, c'est-à-dire le dernier saut. Après une reconnexion sans saut, un transport en taxi ou une mort, `journal.Location.StarSystem` peut être plus à jour. Une clé dérivée « système actuel » pourra être ajoutée en L4 si D3 montre que c'est gênant.

---

## D3 — Point de contrôle en jeu (à faire par Florian)

Objectif : vérifier sur ton MK.2 ce que Claude ne peut pas vérifier. Compte environ 20 minutes. Tu peux faire les étapes 1 à 4 toi-même, ou dire à Claude « vas-y, déploie » pour qu'il les exécute (il demandera ton accord pour chaque commande).

### 1. Fermer le logiciel Stream Deck
Clic droit sur l'icône Stream Deck dans la zone de notification (en bas à droite, près de l'horloge), puis **Quit Stream Deck**.

### 2. Sauvegarder le plugin installé
1. Dans l'Explorateur, colle `%APPDATA%\Elgato\StreamDeck\Plugins` dans la barre d'adresse, puis Entrée.
2. Copie le dossier `com.mhwlng.elite.sdPlugin` et colle-le ailleurs, par exemple sur le Bureau, sous le nom `com.mhwlng.elite.sdPlugin.sauvegarde`.

### 3. Installer la nouvelle version
Copie **le contenu** de `F:\Github Local\streamdeck-elite\Elite\bin\Debug\com.mhwlng.elite.sdPlugin\` dans le dossier `com.mhwlng.elite.sdPlugin` de l'étape 2. Choisis **« Remplacer les fichiers »**. Ne supprime jamais le dossier.

### 4. Relancer Stream Deck
Menu Démarrer → Stream Deck.

### 5. Créer 3 touches « Valeur »
Dans la liste des actions, catégorie **Elite Dangerous**, glisse l'action **Valeur** sur 3 touches libres. Pour chacune, laisse le champ **Titre** vide : le plugin l'écrit lui-même. La taille et la position du texte se règlent avec l'icône « T ».

| Touche | Réglages |
|---|---|
| Carburant | Catégorie « 📊 Statut — valeurs » → Donnée `Fuel.FuelMain`. Préfixe `Fuel\n`, Décimales `1`, Suffixe ` t`. |
| Solde | Donnée `Balance` (même catégorie). Case **Abrégé** cochée, Suffixe ` Cr`. |
| Système | Recherche `fsdjump starsystem` → Donnée `StarSystem`. |

Facultatif : une 4e touche sur `status.GuiFocus`, pour voir le nom du panneau ou de la carte ouverte.

⚠️ À partir de là, l'identifiant `com.mhwlng.elite.value` est **définitif** : il est enregistré dans ton profil.

### 6. Observer
- **Jeu éteint :** Carburant et Solde montrent « — » ou la dernière valeur connue. Système montre le dernier système sauté (lu dans le journal au démarrage).
- **En jeu :**
  - le carburant doit changer pendant l'écopage ou après un saut ;
  - le solde doit changer après un achat ou une vente ;
  - le système doit changer après un saut ;
  - chaque fois en **moins de 2 s**.
- **Lisibilité en 72 px :** texte coupé ? trop petit ? « 12,3 M » lisible ?
- **Page de réglages :** menus, recherche, affichage correct des emojis et des accents.
- **Si tu peux, à pied (Odyssey), dans une atmosphère non respirable :** l'ancien bouton Toggle « atmosphère respirable » doit maintenant être **éteint** (correctif de L1).

### 7. Rapporter
Dis à Claude ce que tu as vu, et donne-lui `pluginlog.log`. Il se trouve dans le dossier du plugin de l'étape 2. Claude peut aussi le lire directement si tu es d'accord. On y attend :
- `EliteStore: N keys after journal replay` au démarrage ;
- des lignes `Value[status.Fuel.FuelMain] = Fuel\n27,5 t`.

### En cas de problème
Ferme Stream Deck, remets le dossier sauvegardé à la place de `com.mhwlng.elite.sdPlugin`, puis relance. Tes touches historiques sont dans ton profil Stream Deck : elles ne sont pas touchées.
