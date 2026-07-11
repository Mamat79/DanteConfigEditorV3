# Tests et baseline V3.07 Beta

## Baseline du 2026-07-11

Source testée :

- branche : `main` ;
- commit source après nettoyage : `4c286ab27ea614770565a10562e7ba464afceae0` ;
- tag de sécurité : `safety-v3.07-before-validation-20260711` ;
- système : Windows `10.0.26200`, `win-x64` ;
- SDK sélectionné par `global.json` : .NET SDK `8.0.422` ;
- MSBuild : `17.11.48` ;
- runtime .NET 8 : `8.0.28` ;
- Inno Setup : `6.7.3`.

Les temps ci-dessous proviennent d'une seule exécution locale après suppression de toutes les sorties `bin`, `obj`, `dist` et `tmp`. Ils ne constituent pas des seuils de performance.

| Étape | Temps | Code retour | Résultat |
|---|---:|---:|---|
| Restore application Windows | 0,874 s | 0 | réussi |
| Restore tests Core | 1,162 s | 0 | réussi |
| Restore tests UI Mac | 1,252 s | 0 | réussi |
| Tests Core Release | 10,626 s | 0 | 38 réussis, 0 échec, 0 ignoré |
| Tests UI Mac Release | 14,291 s | 0 | 2 réussis, 0 échec, 0 ignoré |
| Build Windows Release | 2,221 s | 0 | 0 warning, 0 erreur |
| Build interface Mac Release | 1,091 s | 0 | 0 warning, 0 erreur |
| Publish Windows `win-x64` framework-dependent | 2,716 s | 0 | réussi |
| Installateur Windows autonome | 63,089 s | 0 | réussi |

Installateur de baseline :

- nom : `DanteConfigEditorV3_Installer.exe` ;
- version : `3.07-beta` ;
- taille : `66 819 004` octets ;
- SHA-256 : `77A196ED5923240F8059EE120775BFC778A6AABB96959A9F340970186B24093D`.

L'installateur est une sortie locale régénérable et n'est pas suivi dans Git.

## Commandes de référence

```powershell
dotnet restore .\DanteConfigEditorV3.csproj
dotnet restore .\tests\DanteConfigEditorV3.Tests\DanteConfigEditorV3.Tests.csproj
dotnet restore .\tests\DanteConfigEditor.Mac.Tests\DanteConfigEditor.Mac.Tests.csproj

dotnet test .\tests\DanteConfigEditorV3.Tests\DanteConfigEditorV3.Tests.csproj -c Release --no-restore
dotnet test .\tests\DanteConfigEditor.Mac.Tests\DanteConfigEditor.Mac.Tests.csproj -c Release --no-restore

dotnet build .\DanteConfigEditorV3.csproj -c Release --no-restore
dotnet build .\src\DanteConfigEditor.Mac\DanteConfigEditor.Mac.csproj -c Release --no-restore

dotnet publish .\DanteConfigEditorV3.csproj -c Release -r win-x64 --self-contained false
.\installer\build_installer.ps1
```

## État CI au début de la baseline

- macOS CI, run `29147477519` : succès sur le commit `46dbfcc` ; les tests et les deux DMG ont été produits.
- Windows CI, run `29147477510` : échec `NETSDK1004` sur le commit `46dbfcc`.

Cause Windows confirmée : après l'extraction du projet Core, le restore des tests ne restaurait plus `DanteConfigEditorV3.csproj`, alors que le build utilisait `--no-restore`. Le workflow a été corrigé pour restaurer explicitement l'application puis les tests.

Le runner Windows sélectionnait également son SDK 10 préinstallé. `global.json` fixe désormais le major .NET 8 et autorise uniquement le dernier patch de la feature band `8.0.400`.

Ces corrections ne seront déclarées validées dans GitHub Actions qu'après un run distant réussi. La réussite locale seule ne vaut pas preuve de CI.

## Baseline des dépendances

Versions directes initiales :

- Avalonia `11.3.13` ;
- Microsoft.NET.Test.Sdk `17.8.0` ;
- xUnit `2.5.3` ;
- xunit.runner.visualstudio `2.5.3` ;
- coverlet.collector `6.0.0`.

Le scan `dotnet list package --vulnerable --include-transitive` a signalé :

| Package transitif | Version | Gravité annoncée | Advisory |
|---|---:|---|---|
| `System.Net.Http` | 4.3.0 | High | `GHSA-7jgj-8wvc-jh57` |
| `System.Text.RegularExpressions` | 4.3.0 | High | `GHSA-cmhx-cq75-c4mj` |
| `Tmds.DBus.Protocol` | 0.21.2 | High | `GHSA-xrw6-gwf8-vvr9` |

Cette table décrit l'état initial, pas l'état final attendu. Les dépendances seront mises à jour séparément, puis le scan et tous les tests seront rejoués.

## Ce qui n'est pas validé par cette baseline

- aucun import dans Dante Controller n'a été exécuté pendant cette baseline ;
- aucun modèle matériel réel n'est déclaré compatible sans preuve ;
- le publish Windows framework-dependent n'est pas le paquet transmis aux utilisateurs ; l'installateur final utilise un publish self-contained ;
- les DMG du run macOS sont signés ad hoc, pas notariés par Apple ;
- les tests d'accessibilité et d'échelle d'affichage sont documentés séparément.
