# L0 — Référence de départ (baseline build)

Statut : **terminé** le 2026-09-23.

## Résultat

- Branche `feature/generic-api` : présente et checked out sur la machine de Florian (`F:\Github Local\streamdeck-elite`), aucune modification de code.
- `Elite.sln` compile en configuration Debug : **0 erreur**, 1 avertissement (MSB3884, fichier de règles d'analyse "AllRules.ruleset" introuvable — sans impact, préexistant à l'environnement, pas au code).
- Sortie produite : `Elite\bin\Debug\com.mhwlng.elite.sdPlugin\com.mhwlng.elite.exe` + toutes les DLL/ressources associées.

C'est la référence de départ pour comparer les futurs lots (L1+) : tout écart de compilation après modification de code sera imputable au code, pas à l'environnement.

## Recette d'environnement de build (Windows, .NET Framework 4.8 classique)

Nécessaire une seule fois sur une machine de build :

1. **Visual Studio Build Tools 2022** avec le workload ".NET Desktop Development".
2. **.NET Framework 4.8 Developer Pack** (téléchargement séparé, indispensable — sans lui : erreur `MSB3644` "assemblys de référence introuvables"). Lien : dotnet.microsoft.com/fr-fr/download/dotnet-framework/net48 → "Developer Pack", pas "Runtime".
3. **nuget.exe** (CLI officielle) — nécessaire car le projet utilise `packages.config` (format NuGet legacy), que `msbuild /t:Restore` ne sait PAS restaurer seul (il répond "rien à restaurer" sans erreur, silencieusement).
   ```powershell
   mkdir C:\nuget -Force
   Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile C:\nuget\nuget.exe
   ```

À chaque session de build (PowerShell) :

```powershell
cd "F:\Github Local\streamdeck-elite"
$msbuild = & "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
& C:\nuget\nuget.exe restore "F:\Github Local\streamdeck-elite\Elite.sln"
& $msbuild /p:Configuration=Debug Elite.sln
```

Ces étapes sont encapsulées dans `build.ps1` à la racine du dépôt (`powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1`), utilisable aussi depuis Git Bash / Claude Code.

`vswhere.exe` évite de coder en dur le chemin de MSBuild (qui varie selon la version de Build Tools). `$msbuild` ne survit pas à l'ouverture d'un nouveau terminal — il faut relancer les deux premières lignes à chaque nouvelle fenêtre.

## Dépendances NuGet restaurées (packages.config)

27 packages, dont les principales : Newtonsoft.Json 13.0.3, StreamDeck-Tools 6.3.1, streamdeck-client-csharp 4.3.0, NAudio 2.2.1 (+ sous-modules), CommandLineParser 2.9.1, NLog 5.4.0, ini-parser 2.5.2.
