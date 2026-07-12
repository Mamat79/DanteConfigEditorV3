# Tests et historique V3

## Promotion de la V3.08 Beta dans main du 2026-07-12

- ancienne tête de `main` : `903ed94114ee419e4063cbf07e55ff8626006a7a` (V3.07) ;
- tag annoté de sauvegarde : `safety-v3.07-before-v3.08-main-20260712` ;
- promotion par avance rapide, sans conflit ni réécriture d'historique ;
- nouvelle base de `main` avant mise à jour documentaire : `9a17e3ea20191e2543be9dd31a4408f89d2944af` ;
- branche `3.08-beta` et anciennes Releases conservées ;
- 88 tests Windows réussis et build Release sans warning sur `main` ;
- installateur principal reconstruit : `66 861 369` octets, SHA-256 `3893DA578196B3B5A327D51BA222DDD92D3BFA82EFA48D8D306C9F695CDFA09A` ;
- mise à niveau locale V3.08 réussie, notices installées identiques au dépôt ;
- V3.07 locale conservée séparément, sans suppression destructive.

## Validation ergonomique Easy patch du 2026-07-12

Source fonctionnelle testée :

- branche : `3.08-beta` ;
- commit fonctionnel : `cc403f190e12be081c682e8439346a2377382a51` ;
- fixture anonymisée : `representative-preset.xml` ;
- aucune sauvegarde ni modification du fichier XML de la fixture.

Résultats :

- 88 tests Release réussis, 0 échec, 0 ignoré ;
- build WPF Release réussi, 0 warning, 0 erreur ;
- rendu Easy patch contrôlé en thème sombre à `1600 x 820` et `1200 x 650` ;
- rendu Easy patch contrôlé en thème clair à `1600 x 820` ;
- menus des machines RX/TX conservés à l'écran pendant la prévisualisation ;
- listes RX/TX et panneau central dotés de leurs propres ascenseurs lorsque l'espace manque ;
- colonnes `RX cible`, `Source actuelle`, `Nouvelle source` et `Action` lisibles sans ascenseur horizontal de page ;
- commandes `Prévisualiser`, `Appliquer`, `Ajouter au lot`, `Appliquer ces changements` et `Appliquer tout le lot` visibles ;
- les conflits de remplacement restent soumis à un choix explicite.

Paquet Windows contrôlé :

- installateur autonome construit en 43,064 s ;
- fichier : `dist/DanteConfigEditorV3_08_Beta_Installer.exe` ;
- taille : `66 859 156` octets ;
- SHA-256 : `9EE2355D983969167AA51CA002D844AF55BE6D34FAEEA5F9D9F62422B94DBF12` ;
- somme `.sha256` concordante ;
- mise à niveau silencieuse V3.08 réussie avec code retour 0 ;
- V3.07 stable toujours présente dans son dossier séparé ;
- notices FR installées identiques aux PDF du dépôt ;
- exécutable installé démarré avec la fixture : 3 devices, 3 TX, 4 RX et 3 patchs actifs détectés.

Ces contrôles ne constituent pas une preuve d'import dans Dante Controller ni un test sur matériel Dante réel.

## Validation V3.08 Beta Windows du 2026-07-12

Source fonctionnelle testée :

- branche : `3.08-beta` ;
- commit source du paquet : `faa5a8d9ece26e4b7c726121252d3a19f9498736` ;
- système : Windows `10.0.26200`, `win-x64` ;
- .NET SDK `8.0.422`, MSBuild `17.11.48` ;
- Inno Setup `6.7.3` ;
- aucune compilation ni aucun test macOS exécuté pour cette bêta.

| Étape | Temps | Code retour | Résultat |
|---|---:|---:|---|
| Restore application Windows | 0,906 s | 0 | réussi |
| Restore tests Core/contrats Windows | 0,878 s | 0 | réussi |
| Tests Release | 9,938 s | 0 | 86 réussis, 0 échec, 0 ignoré |
| Build Windows Release | 2,921 s | 0 | 0 warning, 0 erreur |
| Restore RID `win-x64` | 1,123 s | 0 | réussi |
| Publish Windows `win-x64` framework-dependent | 2,486 s | 0 | réussi |
| Installateur Windows autonome | 39,980 s | 0 | réussi, 0 erreur Inno Setup |
| Installation puis mise à niveau de contrôle | 9,390 s | 0 | deux passages réussis |

Installateur validé :

- fichier : `dist/DanteConfigEditorV3_08_Beta_Installer.exe` ;
- version : `3.08-beta`, version fichier `3.8.0.0` ;
- taille : `66 858 195` octets ;
- SHA-256 : `BEDAC3F0A28D1BFFFED7F26E3700C98C68FEC6C1DCAD438ED5D0D8A59A2BCBB2` ;
- somme `.sha256` concordante ;
- signature Authenticode : absente (`NotSigned`).

Installation vérifiée :

- dossier : `C:\Program Files\Dante Config Editor V3.08\` ;
- une seule entrée V3.08 Beta après deux passages de l'installateur ;
- une entrée V3.07 stable conservée séparément ;
- deux installations Dante Config Editor V3 au total ;
- raccourci Menu Démarrer et quatre notices PDF FR/EN présents ;
- démarrage réel de l'exécutable installé réussi.

Contrôles fonctionnels et visuels réalisés sur les fixtures anonymisées :

- onglets principaux `Patch` et `Easy patch` présents et lisibles en thème sombre ;
- onglet actif Easy patch bleu avec texte lisible ;
- machines et canaux RX à gauche, machines et canaux TX à droite ;
- menus RX/TX et flèches précédent/suivant visibles, accessibles et fonctionnels ;
- passage RX de `DEVICE-A` à `DEVICE-B` avec mise à jour immédiate des canaux ;
- sélecteur de machine visible en haut de `Détail machine` ;
- passage de `DEVICE-A` à `DEVICE-D` sans changement en attente ;
- alerte appliquer/abandonner/annuler affichée après une modification non appliquée ;
- test annulé puis fenêtre fermée sans appliquer : le projet est resté `Non modifié` ;
- contrôle du choix de résolution des conflits et du texte sombre sur fond clair ;
- aucune sauvegarde de fixture ni modification d'un XML de test.

Documentation vérifiée :

- Quick Start FR/EN : 1 page chacun ;
- notice complète FR/EN : 5 pages chacune ;
- `Easy patch`, `V3.08` et `By Mamat` présents dans les quatre PDF ;
- aucun caractère `Ø` parasite ni caractère de remplacement Unicode ;
- rendu Poppler contrôlé page par page, sans zone noire réelle, texte coupé ou coin non blanc.

Le scan `dotnet list package --vulnerable --include-transitive` ne signale aucun package vulnérable dans l'application Windows, le Core, les tests Windows, ValidationPack et Benchmarks avec les sources NuGet du 2026-07-12.

CI distante du commit de validation `4b648e756fe78bfd7b7c5d4e5031ed43135266a9` :

- Windows CI, run `29171972920` : réussi en 1 min 3 s ; restore, 86 tests, build, publish et artifacts inclus ;
- aucun workflow macOS déclenché pour la branche `3.08-beta`.

Aucun import dans Dante Controller et aucun test sur matériel Dante réel n'ont été exécutés pendant cette validation.

## Validation V3.07 officielle

Source fonctionnelle testée :

- branche : `main` ;
- commit : `bdb1de9ae7b6b80342e18d1f2172d83e74b7bc43` ;
- système : Windows `10.0.26200`, `win-x64` ;
- .NET SDK `8.0.422`, MSBuild `17.11.48` ;
- Inno Setup `6.7.3`.

| Étape | Temps | Code retour | Résultat |
|---|---:|---:|---|
| Restore application Windows | 1,361 s | 0 | réussi |
| Restore tests Core | 1,227 s | 0 | réussi |
| Restore tests UI Mac | 1,288 s | 0 | réussi |
| Tests Core Release | 13,112 s | 0 | 67 réussis, 0 échec, 0 ignoré |
| Tests UI Mac Release | 17,712 s | 0 | 7 réussis, 0 échec, 0 ignoré |
| Build Windows Release | 3,151 s | 0 | 0 warning, 0 erreur |
| Build interface Mac Release | 1,673 s | 0 | 0 warning, 0 erreur |
| Publish Windows `win-x64` framework-dependent | 4,366 s | 0 | réussi |
| Publish Mac autonome `osx-arm64` | 8,217 s | 0 | réussi |
| Publish Mac autonome `osx-x64` | 7,953 s | 0 | réussi |
| Installateur Windows autonome | 71,965 s | 0 | réussi, 0 erreur Inno Setup |
| Installation puis mise à niveau de contrôle | 18,732 s | 0 | deux passages, une seule installation conservée |

Installateur validé :

- fichier : `dist/DanteConfigEditorV3_Installer.exe` ;
- version : `3.07` ;
- taille : `66 840 678` octets ;
- SHA-256 : `9F052C09391A274A044B44336C86893967FA64F10C8867266353A3E0AA352CCF` ;
- signature Authenticode : absente (`NotSigned`).

Installation vérifiée :

- dossier : `C:\Program Files\Dante Config Editor V3\` ;
- entrée de désinstallation : `Dante Config Editor V3.07 version 3.07` ;
- version de fichier : `3.7.0.0` ;
- SHA-256 de l'exécutable installé : `13B8FD4F6C2CF489C9A05FC78F885E36EB5C9F9F3C0867E12A516BA9C164A324` ;
- deux passages de l'installateur conservent une seule installation V3 ;
- démarrage réel réussi, titre `Dante Config Editor V3.07` ;
- fixture anonymisée ouverte : 3 devices, 3 TX, 4 RX, 3 patchs actifs ;
- modes Simple et Expert contrôlés : le mode Expert affiche bien les colonnes techniques supplémentaires ;
- fermeture propre après le contrôle.

Le scan `dotnet list package --vulnerable --include-transitive`, rejoué après restauration sur les sept projets du dépôt, ne signale aucun package vulnérable avec les sources NuGet du 2026-07-11.

CI distante du commit officiel `9ba35639e417cfb26e5249caa6375c94faf026a7` :

- Windows CI, run `29168999560` : réussi, tests, build, publish et artifacts inclus ;
- macOS CI, run `29168999565` : réussi, tests Core/UI et deux DMG inclus.

Release publique stable : [`v3.07`](https://github.com/Mamat79/DanteConfigEditorV3/releases/tag/v3.07), publiée le 2026-07-11 avec six assets. Les digests GitHub concordent avec les fichiers vérifiés :

- Windows : `9f052c09391a274a044b44336c86893967fa64f10c8867266353a3e0aa352ccf` ;
- macOS Apple Silicon : `ea560fabe9a6d83da705d9529baa09c4eb74b5351dddc9407dbef40a146fb959` ;
- macOS Intel : `f6be8875e152fe8b31656a6c679e526c0a09649b8c0bc51e92f3f79ebb44fcdd`.

La Release est non brouillon, non prerelease et déclarée comme dernière version du dépôt. Aucun import Dante Controller n'a été exécuté pendant cette validation.

## Validation de la V3.07 Beta du 2026-07-11

Source fonctionnelle testée :

- branche : `main` ;
- commit : `840566b5451d7ddd3985cc1abdc82277a6efa986` ;
- système : Windows `10.0.26200`, `win-x64` ;
- .NET SDK `8.0.422`, MSBuild `17.11.48` ;
- Inno Setup `6.7.3`.

| Étape | Temps | Code retour | Résultat |
|---|---:|---:|---|
| Restore application Windows | 0,862 s | 0 | réussi |
| Restore tests Core | 0,878 s | 0 | réussi |
| Restore tests UI Mac | 0,729 s | 0 | réussi |
| Tests Core Release | 6,946 s | 0 | 63 réussis, 0 échec, 0 ignoré |
| Tests UI Mac Release | 8,919 s | 0 | 7 réussis, 0 échec, 0 ignoré |
| Build Windows Release | 1,785 s | 0 | 0 warning, 0 erreur |
| Build interface Mac Release | 1,040 s | 0 | 0 warning, 0 erreur |
| Publish Windows `win-x64` framework-dependent | 2,468 s | 0 | réussi |
| Publish Mac autonome `osx-arm64` | 3,631 s | 0 | réussi |
| Publish Mac autonome `osx-x64` | 3,505 s | 0 | réussi |
| Pack de validation XML | 1,424 s | 0 | 8 scénarios, rapports, checklist et hashes produits |
| Installateur Windows autonome | 32,764 s | 0 | réussi, 0 erreur Inno Setup |
| Installation puis mise à niveau de contrôle | 13,229 s | 0 | deux passages, une seule installation conservée |

Installateur validé :

- fichier : `dist/DanteConfigEditorV3_Installer.exe` ;
- version : `3.07-beta` ;
- taille : `66 842 463` octets ;
- SHA-256 : `3D06B9EF344A2153AFD67CE8296EEB1FA8EDCB75710C2738BB46EEF9DB5C7FAE` ;
- signature Authenticode : absente (`NotSigned`).

Installation vérifiée :

- dossier : `C:\Program Files\Dante Config Editor V3\` ;
- version de fichier : `3.7.0.0` ;
- SHA-256 de l'exécutable installé : `11BA21D50308C5A5FB69C962E91E4DE71F4B52E7CA39EEFFF1DA36DE1DA004CA` ;
- une seule entrée de désinstallation V3 ;
- raccourci Menu Démarrer, désinstallateur et quatre notices FR/EN présents ;
- démarrage réel réussi, titre `Dante Config Editor V3.07 Beta` ;
- fixture anonymisée ouverte : 3 devices, 3 TX, 4 RX, 3 patchs actifs, état `Non modifié` ;
- fermeture propre et aucune erreur Application Windows liée au processus pendant le contrôle.

Les quatre projets inspectés par `dotnet list package --vulnerable --include-transitive` ne signalent aucun package vulnérable avec les sources NuGet du jour. Les bundles Mac autonomes sont publiables depuis Windows, mais les DMG exigent macOS et sont construits par le workflow macOS.

CI distante du commit de livraison `a9fec0d76154d8a707fce3d8c484ff433f5f544f` :

- Windows CI, run `29167796316` : réussi en 1 min 1 s, tests, build, publish et artifacts inclus ;
- macOS CI, run `29167796298` : réussi, tests UI/Core et deux DMG inclus.

Release publique : `v3.07-beta`, publiée le 2026-07-11 avec six assets. Les digests GitHub concordent avec les fichiers vérifiés localement :

- Windows : `3d06b9ef344a2153afd67ce8296eeb1fa8edcb75710c2738bb46eef9db5c7fae` ;
- macOS Apple Silicon : `1e02b6b8a52d919463240e28c1a87c53b4734023f2e3a278b115db2ccb5b6784` ;
- macOS Intel : `626bd222470d31246ee2b5fe2aff3d2838ec71b10e9566e9c7105e0b9646f37b`.

Aucun import Dante Controller n'a été exécuté pendant cette validation.

## Baseline historique du 2026-07-11

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

## Benchmarks V3.07 après patch visuel

Mesures locales du 2026-07-11 au commit fonctionnel `ff704bf`, trois exécutions par taille et médiane retenue. Chaque device synthétique possède 64 TX et 64 RX. Les allocations sont celles du thread mesuré par le banc ; la mémoire de travail est celle du processus après le scénario. Ces valeurs sont des observations, pas des seuils contractuels.

| Devices | XML | Chargement | Allocation chargement | Édition groupée | Allocation édition | Garde-fou | Sauvegarde | Mémoire de travail |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 0,156 Mio | 32,210 ms | 2,783 Mio | 34,855 ms | 20,075 Mio | 6,064 ms | 71,934 ms | 49,410 Mio |
| 50 | 0,780 Mio | 73,121 ms | 13,735 Mio | 157,599 ms | 97,378 Mio | 31,875 ms | 283,507 ms | 85,945 Mio |
| 200 | 3,122 Mio | 207,301 ms | 54,822 Mio | 269,810 ms | 387,250 Mio | 50,649 ms | 641,289 ms | 179,512 Mio |

Le garde-fou n'a signalé aucune erreur inattendue sur ces trois scénarios. L'allocation de l'édition et de la sauvegarde croît avec le nombre de canaux ; les gros presets restent donc un point de surveillance, même si le scénario de 200 devices termine correctement.

Commande reproductible :

```powershell
dotnet run --project .\benchmarks\DanteConfigEditorV3.Benchmarks\DanteConfigEditorV3.Benchmarks.csproj -c Release -- --phase v3.07-final --commit ff704bf --output .\tmp\benchmarks\v3.07-final-ff704bf.json
```

Le JSON brut est une sortie locale régénérable ignorée par Git.

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

L'installateur utilise un second publish `win-x64` autonome, mono-fichier et compressé. Le publish framework-dependent ci-dessus reste utile pour contrôler séparément la compilation de publication.

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

### Résolution des advisories

Mise à jour appliquée dans un commit séparé :

- Avalonia `11.3.18` et Avalonia Headless `11.3.18` ;
- `Avalonia.Controls.DataGrid` reste en `11.3.13`, dernière version publiée sur la ligne 11 et compatible avec Avalonia 11.3.x ;
- Microsoft.NET.Test.Sdk `18.7.0` ;
- xUnit `2.9.3` et runner Visual Studio `3.1.5` ;
- coverlet.collector `10.0.1`.

Après mise à jour :

- `dotnet list package --vulnerable --include-transitive` ne signale plus aucun package vulnérable dans les deux projets de tests ;
- 38 tests Core et 2 tests UI Mac réussissent ;
- les builds Windows et Mac réussissent sans warning ;
- les publications autonomes `osx-arm64` et `osx-x64` réussissent.

xUnit v2 est désormais une ligne de maintenance dépréciée au profit de xUnit v3. La migration v3 n'est pas mélangée à cette mise à jour corrective, car Avalonia Headless 11 reste intégré au modèle xUnit v2. Elle doit faire l'objet d'un chantier séparé avec validation CI Windows/macOS.

## Ce qui n'est pas validé par cette baseline

- aucun import dans Dante Controller n'a été exécuté pendant cette baseline ;
- aucun modèle matériel réel n'est déclaré compatible sans preuve ;
- le publish Windows framework-dependent n'est pas le paquet transmis aux utilisateurs ; l'installateur final utilise un publish self-contained ;
- les DMG du run macOS sont signés ad hoc, pas notariés par Apple ;
- les tests d'accessibilité et d'échelle d'affichage sont documentés séparément.

## Couverture fonctionnelle actuelle

La suite Core couvre notamment :

- tous les aliases de subscription reconnus par l'application ;
- patch local `.`, TX absent, canal absent et presets partiels ;
- machines sans TX ou sans RX ;
- plusieurs interfaces IPv4 et conservation de l'interface secondaire ;
- zéro, un ou plusieurs preferred masters ;
- namespace par défaut, réordonnancement de balises et valeurs inconnues préservées ;
- Unicode, noms longs, sauvegardes successives, fusion et récupération ;
- presets synthétiques 10/50/200 en 64 TX / 64 RX ;
- contrat de mise à niveau de l'installateur ;
- planification séquentielle et session en attente du patch visuel.

Les tests Mac couvrent la structure du rail latéral, les alertes, le focus initial, les dimensions `1366 x 768` et `1920 x 1080`, ainsi que l'ouverture et l'utilisation minimale de l'atelier de patch. Les contrôles matériels et lecteurs d'écran restants sont détaillés dans `ACCESSIBILITY.md`.
