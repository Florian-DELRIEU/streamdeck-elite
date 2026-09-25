# ZV Stream Deck Elite

Version personnelle (fork) du plugin Stream Deck « Elite Dangerous » de [mhwlng](https://github.com/mhwlng/streamdeck-elite), version **2.8.0**. Elle ajoute **toute l'API du jeu** : les données de `Status.json` et les quelque 250 événements du Journal, soit un catalogue de 1 926 données décrites en français. On construit une touche à partir de n'importe quelle donnée sans écrire de code.

Les 12 actions d'origine sont inchangées. Les nouvelles actions sont rangées dans la catégorie **« ZV Stream Deck Elite »** du logiciel Stream Deck. Le README d'origine de mhwlng, en anglais, suit plus bas, sans modification.

## Les nouvelles actions

### Donnée
- **Affiche** n'importe quelle donnée du jeu, sous forme de texte réglable : préfixe, suffixe, décimales, facteur (ex. `/32`), décalage, forme abrégée (12,3 M).
- **Change d'icône** selon jusqu'à 4 règles (`=`, `≠`, `<`, `>`, vrai/faux…), avec une image par défaut.
- **Jusqu'à 4 vues (« tiroir »)** : chacune a sa donnée, son texte, son icône et son action. Par défaut, un appui court agit et un appui long (0,5 s) passe à la vue suivante. La vue affichée est mémorisée.
- **À l'appui** :
  - une commande du jeu, parmi 366, avec recherche en français et description de chaque commande ;
  - et/ou un **raccourci clavier libre** ;
  - un son.
- **Bouton ⓘ** : signification de la donnée, valeurs possibles et **valeur actuelle en jeu**.

### Alarme
- **Passe en alerte** (image d'alerte et son) quand un événement du Journal survient. Un **filtre** optionnel porte sur un de ses champs : par exemple `UnderAttack` avec `Target = You`.
- **Durée réglable** ; `0` = jusqu'à un appui. Un appui acquitte l'alerte, puis envoie la commande et le raccourci.
- **Jamais déclenchée** par le journal relu au lancement du plugin.
- Bouton **« Tester l'alarme »** dans les réglages.

## Installer et mettre à jour

- **Première installation** : double-clic sur `com.mhwlng.elite.streamDeckPlugin`, produit par `pack.ps1`.

  ⚠ Ce plugin a **le même identifiant** que la version officielle de mhwlng. Les deux ne peuvent pas coexister, et installer la version officielle remplacerait celle-ci.
- **Mettre à jour** une version déjà installée (le double-clic ne fonctionne que si le plugin n'est pas installé) :
  1. fermer Stream Deck ;
  2. copier le build Release **par-dessus** `%APPDATA%\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin` (`robocopy <dossier Release> <dossier installé> /E`, jamais `/MIR`) ;
  3. relancer Stream Deck.

  Les réglages des touches sont dans les profils Stream Deck, pas dans ce dossier : ils sont conservés.

## Compiler

**Prérequis :** Windows, Visual Studio Build Tools 2022 (« .NET Desktop Development »), .NET Framework 4.8 Developer Pack, `C:\nuget\nuget.exe`. Voir `docs/L0-baseline-build.md`.

```
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1                          # Debug (plugin + tests)
powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1                           # tests unitaires : == TESTS OK
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Release   # plugin 64 bits
powershell -NoProfile -ExecutionPolicy Bypass -File pack.ps1                           # dist\com.mhwlng.elite.streamDeckPlugin
```

- `pack.ps1` utilise `DistributionTool.exe` d'Elgato s'il est présent dans `tools\` ou dans le PATH ; sinon, il crée directement le zip.
- **Récupérer les nouveautés de mhwlng** : `git fetch upstream`, puis `git merge upstream/master`.

## Documentation

- `docs/cahier-des-charges-streamdeck-elite.md` : spécification.
- `docs/L1-…` à `docs/L9-finalisation.md` : les lots, avec leurs décisions et leurs tests en jeu.
- `CLAUDE.md` : état d'avancement et règles du projet.

---

*README d'origine de mhwlng (en anglais) :*

# streamdeck-elite
Elgato Stream Deck button plugin for Elite Dangerous

![Elgato Stream Deck and Flight Instrument Panel](https://i.imgur.com/bE2ODlF.jpg)

This plugin connects to Elite Dangerous, to get the on/off status for 14 different toggle-buttons, 
4 buttons to control the power distributor pips, 4 alarm buttons, 3 FSD related buttons, an FSS toggle button, a firegroup selection button and a generic limpet controller button.

If you press the relevant button on your keyboard or hotas, then the image on the stream deck will change correctly.

When a button has no effect (e.g. when docked) then the image won't change.

There is also a STATIC button type, that works in a similar way to the streamdeck 'Hotkey' button type.
So, there is only one image and there is no game state feedback for these buttons.
The differences with the 'Hotkey' buttons are, that it gets the keyboard binding from the game and doesn't repeat the key when the streamdeck button is held.
For Odyssey, various new buttons are available here.

A sound can be played when pressing a static button.

The static buttons can also be used with multi-action buttons.

The static buttons under the 'Toggles' and 'Fire Group' groups (like Combat Mode or Deploy Hardpoints) are meant for multi-actions. 
These kinds of buttons should not be pressed multiple times in quick succession, because it takes some time for the plugin to receive the game state change.

There is also a 'Repeating Static Button' type. This button is used only for keys, that need to be held down.
So, when the stream deck button is pushed, the 'key down' event is sent to the keyboard
and only after the stream deck button is released, the 'key up' event is sent to the keyboard.
The streamdeck 'hotkey' button also has this behaviour.
For Odyssey, the 'Open Access Panel' button is available here.

The plugin also has a Dial button for use with the 4 dials on the Streamdeck+ model.

There are 5 bindings (They must be keyboard bindings, you can't bind the mouse wheel!) :

- Dial Clockwise
- Dial Counter-Clockwise
- Dial Press
- Touch screen press
- Touch screen long press

When a dial is rotated, the 'key down' event is sent to the keyboard once. 
When you let go of the dial for at least 100ms : the 'key up' event is sent to the keyboard. 

When a dial button is pushed, the 'key down' event is sent to the keyboard. 
When a dial button is released, the 'key up' event is sent to the keyboard. 

When the touch screen is pressed or long-pressed, the behaviour is like the static button.

The plugin also has a Firegroup Dial button for use with the 4 dials on the Streamdeck+ model.

After you install the plugin in the streamdeck software, then there will be several new button types in the streamdeck software.

Choose a button in the streamdeck software (drag and drop), then choose an Elite Dangerous function for that button (that must have a keyboard binding in Elite Dangerous!) and then choose any pictures for that button.

**Example button images, like in above picture, can be found in the source code images directory.**

ONLY add an image to a (repeating) STATIC and DIAL button in this way, do NOT set this image for any of the other button types :

![Button Image](https://i.imgur.com/xkgy7uZ.png)

Animated gif files are only supported for the (repeating) STATIC and DIAL buttons. Dial images are 200x50

If .gif images are configured for Power/Limpet/Hyperspace/Route buttons, then no texts or pips are drawn on top of them.

**You can clear the image/sound path, by clicking on the label in front of the file picker edit box.**

The plugin can (optionally) automatically switch to a different profile, if the in-game state changes. (e.g. deploy hardpoints, enter SRV etc.)
More instructions on the [Wiki](https://github.com/mhwlng/streamdeck-elite/wiki/Automatic-Profile-Switching).

The supported toggle-buttons are:
- Analysis Mode
- Cargo Scoop
- Flight Assist
- Galaxy Map
- Hardpoints
- Landing Gear
- Lights
- Night Vision
- Silent Running
- SRV Drive Assist
- SRV Handbrake
- SRV Turret
- Supercruise (no longer needed)
- System Map
- Comms Panel
- Nav Panel
- Role Panel
- Systems Panel

For Odyssey, when On Foot, the Galaxy Map,System Map,Lights & Night Vision buttons will call the on-foot key bindings, 
but there is no state feedback. So the button image won't change.

You could, for example, use the 'multi-action switch' function, that is built into the streamdeck software, 
to set up a toggle function for on-foot functions. 
You can add the relevant static function of this plugin to both the ON-and OFF-action of the 'multi-action switch' function. 
You can then set up different images for each toggle state.
The disadvantage is: that if you would press e.g. the on-foot shield on/off button when still in a ship, then the button image would be out of sync.

A sound can be played when pressing a toggle button.

The supported power distributor pips buttons are:
- Reset
- System
- Engines
- Weapons

A long press on a button will set the power distributor to 4 pips.
There is a separate button image for 4 pips.

There is a separate alarm image that is only used for the SYS button.
That image is shown when under attack and pips are not set to 4 pips.

The 3 Pip colors can be configured separately for each button state. 
If color #ff00ff is chosen as 'Pip' color or 'No Pip' Color, then that pip type will always be hidden.

The supported alarm buttons are:
- Highest Threat (alarm = under attack status)
- Deploy Chaff (alarm = under attack status)
- Deploy Heatsink (alarm = overheating status)
- Deploy Shield Cell Bank (alarm = shields down status. In that case DON'T fire a shield cell bank.)

A sound can be played when pressing an alarm button.

The supported FSD related buttons are:
- Toggle FSD, also shows Remaining Jumps In Route
- Supercruise
- Hyperspace Jump, also shows Remaining Jumps In Route
- Route, also shows Remaining Jumps In Route

The FSD buttons have 3 images:
- engaged  : supercruise/hyperspace is active
- enabled  : supercruise/hyperspace is inactive
- disabled : supercruise/hyperspace is blocked for reasons like (Docked, Landed, LandingGearDown, CargoScoopDeployed, FsdMassLocked, FsdCooldown, HardpointsDeployed)

Text colors can be configured separately for each button state. 
If color #ff00ff is chosen, then the text will always be hidden.

A normal and disabled sound can be played when pressing an FSD button.

The FSS button has 3 images:
- engaged  : FSS screen is visible
- enabled  : FSS screen is not visible
- disabled : not in supercruise mode (note that throttle position can't be detected)

A normal and disabled sound can be played when pressing the FSS button.

The Route button has 2 images:
- enabled  : Remaining Jumps In Route > 0
- disabled : Remaining Jumps In Route = 0

When the route button is pressed, the 'Next Jump Destination' binding is executed. ('TargetNextRouteSystem' Binding)

Text colors can be configured separately for each button state. 
If color #ff00ff is chosen, then the text will always be hidden.

A normal and disabled sound can be played when pressing the Route button.

The limpet controller button works with any type of limpet controller.

The button shows the current number of limpets in the cargo hold. (The same value is shown on all buttons).

There is no specific keybind for any type of limpet controller.
Instead, you need to set up a fire group letter and primary or secondary fire button.

Text colors can be configured separately for each button state. 
If color #ff00ff is chosen, then the text will always be hidden.

A normal and disabled sound can be played when pressing a limpet button.

The firegroup selection buttons have 3 images:
- on       : firegroup is active
- off      : firegroup is inactive
- disabled : firegroup selection is blocked for reasons like (On Foot, in Srv, Docked, Landed, LandingGearDown, FSD Jump)

A normal and disabled sound can be played when pressing a firegroup button.

The plugin looks for a StartPreset.start file in this Elite Dangerous key bindings directory :

`%LocalAppData%\Frontier Developments\Elite Dangerous\Options\Bindings\`

That .start file should contain the exact name of the key binding file(s). (Without the extension .3.0.binds or .binds)

Also, the steam library directories are searched, for any of the default key binding files :
 
`....\steamapps\common\Elite Dangerous\Products\elite-dangerous-64\ControlSchemes`

This plugin only works with keyboard bindings. 
So, when there is only a binding to a joystick / controller / mouse for a function, then you need to add a keyboard binding.

If you change the key bindings in Elite Dangerous, then you don't have to restart the streamdeck software. The plugin key bindings are updated automatically.

**All bindings must be 'custom'. (this will happen automatically once you make at least one on-foot keyboard binding)
If you see a default binding name, then the plugin won't work correctly.**

![Binding Image](https://i.imgur.com/CpM7HTZ.png)

If nothing happens, when pressing streamdeck buttons:

You may see errors like this in the plugin log file :

`file not found C:\Users\xxx\AppData\Local\Frontier Developments\Elite Dangerous\Options\Bindings\Custom.4.2.binds`

In that case, the plugin has no access to the bindings directory. 

Start streamdeck.exe as administrator.

The plugin installer is here: https://github.com/mhwlng/streamdeck-elite/releases

To install the plugin, double click the file `com.mhwlng.elite.streamDeckPlugin` which should install the plugin.

(This only works, if the plugin not already installed. Otherwise you will need to uninstall or remove the plugin first.)

This .streamDeckPlugin file is a zip file and the contents are simply copied to :

`%appdata%\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin`

To update to a new version :

Stop the Stream Deck application:

`c:\Program Files\Elgato\StreamDeck\StreamDeck.exe`

Then delete the `%appdata%\Elgato\StreamDeck\Plugins\com.mhwlng.elite.sdPlugin` directory. (make a backup copy first)

Then start the streamdeck software again.

Then double click the file `com.mhwlng.elite.streamDeckPlugin` as usual.

MAKE SURE that you save any images, profiles etc. that you put in these directories yourself, BEFORE deleting the directory.
And put them back after the installation.
The plugin installer doesn't come with button images.

Also, the plugin can be uninstalled and the directory completely deleted by right-clicking on any button and selecting uninstall (make a backup copy first) :

![Button Image](https://i.imgur.com/Ky2cH8R.png)

The button configurations are not stored in the plugin directory.

After uninstalling and re-installing the plugin, all the button definition should still be there.

The com.mhwlng.elite.sdPlugin directory contains a pluginlog.log file, which may be useful for troubleshooting.

Best is to create a separate directory for the images, so that they are not deleted when uninstalling/reinstalling the plugin.

Also see companion application for Logitech Flight Instrument Panel and VR :

https://github.com/mhwlng/fip-elite

Thanks to :

https://github.com/BarRaider/streamdeck-tools

https://github.com/MagicMau/EliteJournalReader

https://github.com/ishaaniMittal/inputsimulator

https://nerdordie.com/product/stream-deck-key-icons/

