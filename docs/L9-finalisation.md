# L9 — Finalisation : version 2.8.0, « ZV Stream Deck Elite », empaquetage, README

Statut : **terminé** le 2026-09-25 (code), dans la même session que L8, à la demande de Florian. Le **test unique D8** (L7b + L8 + L9) est décrit en fin de document ; il est à faire par Florian.

## Décisions de Florian (2026-09-25)

- **Nom et catégorie** : « ZV Stream Deck Elite », pour le plugin comme pour la catégorie de la liste des actions dans le logiciel Stream Deck.
- **Empaquetage** : un script `pack.ps1` simple (zip), qui utilise l'outil officiel d'Elgato s'il est présent.
- **README** : une section en français en tête, suivie du README d'origine de mhwlng sans modification.
- **Icônes** des nouvelles actions dans la charte du pack (`Elite/Images`, police Rubik, palette ambre) : plus tard, à sa demande.

## Ce qui a changé

| Fichier | Changement |
|---|---|
| `Elite/manifest.json` | `Version` 2.7.4 → **2.8.0** ; `Name` et `Category` « Elite Dangerous » → **« ZV Stream Deck Elite »**. Ne changent pas : les UUID, `CodePath` (`com.mhwlng.elite`), le dossier `com.mhwlng.elite.sdPlugin`, `Author`, les icônes. Les touches déjà posées dans les profils restent liées à leurs UUID : elles ne bougent pas. |
| `pack.ps1` (nouveau, ASCII) | Produit `dist\com.mhwlng.elite.streamDeckPlugin` à partir de `Elite\bin\Release\com.mhwlng.elite.sdPlugin`, sans `*.pdb` ni `*.log`. Si `DistributionTool.exe` d'Elgato est dans `tools\` ou dans le PATH, il est utilisé et valide aussi le plugin. Sinon, le script crée un zip dont la racine est le dossier `com.mhwlng.elite.sdPlugin` : c'est le format d'un `.streamDeckPlugin`. Le script se termine par `== PACK OK` et la taille du fichier. |
| `.gitignore` | `dist/`. |
| `README.md` | Section « ZV Stream Deck Elite » en français, en tête : nouvelles actions, installation et mise à jour, compilation, documentation. |
| `Elite.Tests/ManifestTests.cs` | Nom, catégorie et version 2.8.0 vérifiés ; `CodePath` inchangé ; pas de `UUID` racine. |
| `CLAUDE.md` | État d'avancement, faits sur le manifeste, empaquetage. |

## Vérifications faites (session cloud)

- **Compilation et tests :** Roslyn en C# 7.3 sous Mono, **126 tests OK**.
- **`pack.ps1`** : exécuté avec PowerShell 7 (Linux) sur le build du cloud.
  - 0 erreur d'analyse ;
  - `== PACK OK` ;
  - l'archive a une seule racine `com.mhwlng.elite.sdPlugin/`, aucun `.pdb` ni `.log`, aucune barre oblique inverse dans les chemins ;
  - elle contient `manifest.json`, `EventAlarm.html` et `alarm.js`.

  Le script n'utilise que des commandes de Windows PowerShell 5.1.
- **Non vérifié :**
  - `pack.ps1` sous Windows PowerShell 5.1 ;
  - l'installation du paquet par double-clic ;
  - le nouveau nom dans le logiciel Stream Deck ;
  - la non-régression en jeu.

  C'est l'objet de D8.

---

## D8 — Test unique (à faire par Florian) : L7b + L8 + L9

### 0. Récupérer, compiler, installer

Dans PowerShell, à la racine du dépôt (`F:\Github Local\streamdeck-elite`) :

```
git fetch origin
git checkout feature/generic-api
git merge --ff-only origin/claude/pensive-meitner-657620
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Release
```

**Attendu :**
- `== BUILD OK` deux fois, avec seulement l'avertissement MSB3884 ;
- `== TESTS OK` avec **126 tests**.

Si quelque chose échoue, copie-moi le message.

Ensuite, **déploie** : dis « déploie » à Claude Code sur ton PC, ou fais-le toi-même :

```
Stop-Process -Name StreamDeck
robocopy "F:\Github Local\streamdeck-elite\Elite\bin\Release\com.mhwlng.elite.sdPlugin" "$env:APPDATA\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin" /E
Start-Process 'D:\Programmes\Elgato\StreamDeck.exe'
```

**Précautions :**
- attends quelques secondes après `Stop-Process`, le temps que `com.mhwlng.elite` s'arrête aussi ;
- robocopy renvoie un code de 0 à 7 quand tout va bien ;
- le plugin met environ 50 s à démarrer ;
- dans `pluginlog.log`, on doit voir `EliteStore: N keys after journal replay`.

### 1. D7b — Recherche de commande (correctif L7b)

Dans une touche « Donnée », section « Action (appui) » :
1. tape `train` dans « Recherche » ;
2. survole quelques résultats : la description s'affiche sous la liste, et **rien ne bouge** ;
3. **clique sur un résultat**.

**Attendu :** la « Commande » change et sa description s'affiche dessous, sans avoir effacé la recherche.

### 2. Nom et actions

- Dans la liste des actions, à droite du logiciel Stream Deck, la catégorie s'appelle **« ZV Stream Deck Elite »**. Elle contient les 12 actions d'origine, plus **« Donnée »** et **« Alarme »**.
- Tes touches existantes, historiques comme « Donnée », sont **toujours là** et fonctionnent : voir le point 4.

### 3. Alarme (L8)

1. Pose une touche **« Alarme »**.
2. Réglages :
   - dans Recherche, tape `attaque` et clique sur `UnderAttack` ;
   - Filtre : champ `Target`, test `=`, valeur `You` ;
   - Durée : `5` ;
   - choisis une image au repos, une image d'alerte (par exemple celles de `Elite/Images/Alarm Button`) et un son d'alerte (`.wav`).
3. **Bouton « ▶ Tester l'alarme »** : l'image d'alerte s'affiche, le son part, puis la touche revient au repos après 5 s. Ouvre aussi le panneau **ⓘ** : il affiche « Dernier reçu ».
4. **Redémarre Stream Deck** alors que ton journal du jour contient déjà des `UnderAttack` : **aucune alerte** au démarrage.
5. **En jeu, en combat**, quand tu es attaqué : l'alerte se déclenche puis s'éteint après 5 s. Une nouvelle attaque relance les 5 s.
6. Mets la **durée à 0** et une **commande** (par exemple `Deploy Chaff`) :
   - l'alerte reste affichée jusqu'à l'appui ;
   - l'appui l'éteint **et** envoie la commande.
7. **Sans image d'alerte :** le test affiche le triangle ⚠ de Stream Deck.

Dans `pluginlog.log`, on doit voir `EventAlarm[UnderAttack]: triggered`, puis `acknowledged`.

### 4. Non-régression des 12 actions d'origine

Coche celles de ton profil :

| Action | Vérification |
|---|---|
| Toggle Button (ex. train, phares, vision nocturne) | L'image suit l'état du jeu ; l'appui bascule. |
| Alarm Button (Highest Threat, Chaff, Heatsink, Shield Cell) | L'appui agit ; l'image d'alarme apparaît sous attaque ou en surchauffe. |
| FSS Button | 3 images : actif, prêt en supercruise, désactivé hors supercruise. |
| Hyperspace Button | 3 images et le nombre de sauts restants. |
| Route Button | Sauts restants ; l'appui sélectionne le prochain système de la route. |
| Limpet Button | Nombre de limpets ; l'appui tire avec le groupe réglé. |
| Power Button (SYS, ENG, WEP, RST) | Les pips suivent ; un appui long donne 4 pips. |
| Static Button | La commande part une seule fois, même touche maintenue. |
| Repeating Static Button | La touche reste enfoncée tant que le bouton l'est. |
| Firegroup Button | Le groupe actif s'affiche ; image désactivée à quai et train sorti (comportement de mhwlng, conservé). |
| Dial Button, FireGroup Dial | Stream Deck+ seulement : **non testable sur le MK.2**. |

Vérifie aussi tes touches **« Donnée »** : valeurs, icônes, tiroirs, vue mémorisée, raccourcis.

### 5. Paquet (L9)

```
powershell -NoProfile -ExecutionPolicy Bypass -File pack.ps1
```

**Attendu :** `== PACK OK: dist\com.mhwlng.elite.streamDeckPlugin (… KB)`.

⚠ **Ne l'installe pas** par-dessus ta version en place : l'installation par double-clic ne fonctionne que si le plugin n'est pas déjà installé. Ce fichier sert à une nouvelle machine ou de sauvegarde.

### 6. Me dire

Dis à Claude ce qui va ou non, point par point, comme pour D7.
