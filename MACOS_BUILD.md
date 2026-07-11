# Version macOS - V3.07 Beta

La version macOS utilise Avalonia pour l'interface et compile exactement les mêmes classes métier et services XML que la version Windows.

## Paquets utilisateurs

Deux DMG autonomes sont produits :

- `DanteConfigEditorV3_macOS_AppleSilicon.dmg` pour les Mac M1, M2, M3, M4 et suivants ;
- `DanteConfigEditorV3_macOS_Intel.dmg` pour les Mac Intel 64 bits.

Le runtime .NET 8 et les notices FR/EN sont inclus. L'utilisateur ouvre le DMG puis glisse `Dante Config Editor` vers `Applications`.

## Limite de signature de la bêta

Les paquets publics sont signés localement de façon ad hoc, mais ils ne sont pas encore signés avec un certificat Apple Developer ID ni notariés par Apple. Au premier lancement, macOS peut donc afficher une alerte de sécurité.

Pour cette bêta, ouvrir `Applications`, faire un clic droit sur `Dante Config Editor`, choisir `Ouvrir`, puis confirmer. Une future distribution sans cette étape exige un compte Apple Developer, un certificat `Developer ID Application`, le hardened runtime et une notarisation Apple.

## Compilation locale

Prérequis :

- macOS 11 ou plus récent ;
- SDK .NET 8 ;
- outils Apple `codesign`, `iconutil`, `sips` et `hdiutil` fournis avec macOS.

Depuis la racine du dépôt :

```bash
bash packaging/macos/build-macos.sh osx-arm64
bash packaging/macos/build-macos.sh osx-x64
```

Les DMG et leurs sommes SHA-256 sont créés dans `dist/macos`.

## Signature et notarisation officielles

Le script actuel utilise `codesign --sign -`, c'est-à-dire une signature ad hoc. Pour une publication officielle, remplacer cette étape par une signature Developer ID avec hardened runtime, envoyer le DMG au service notarial Apple, attendre son acceptation, puis agrafer le ticket de notarisation au DMG.

Le moteur XML reste protégé par les mêmes tests de non-régression que sous Windows. La validation finale d'un fichier exporté doit néanmoins toujours être faite dans l'outil Dante officiel approprié avant une utilisation terrain.

## Interface et patch visuel

La version Mac inclut l'atelier de patch visuel partagé avec Windows : sélection de plusieurs TX, affectation séquentielle, glisser-déposer et matrice interactive. Les changements restent en attente jusqu'à `Appliquer au projet`.

Pour garder la matrice utilisable sur de gros presets, elle affiche un couple de devices à la fois et limite la vue Mac aux 128 premiers TX et RX. Les listes de canaux conservent tous les éléments.

La suite courante comprend 63 tests du moteur partagé et 7 tests Avalonia headless. Ces derniers couvrent notamment les alertes latérales, la navigation au clavier, les dimensions compactes et le patch visuel ; ils ne remplacent pas un contrôle VoiceOver sur un Mac réel.
