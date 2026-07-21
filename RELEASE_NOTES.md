# Dante Config Editor V3.3

[English release notes](RELEASE_NOTES_EN.md)

## Statut

V3.3 est la version officielle courante pour Windows et macOS. Dante Config Editor reste un outil tiers non officiel Audinate et peut encore contenir des bugs. Travaillez sur une copie des XML Dante et validez toujours le fichier généré dans l'outil Dante officiel adapté avant une utilisation réelle.

## Échange direct avec DMT

- Import direct des fichiers `XLSX` et `ODS` créés par [dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools).
- Export direct vers quatre modèles intégrés : DMT XLSX dLive, DMT XLSX Avantis, DMT ODS dLive et DMT ODS Avantis.
- Les feuilles, styles et réglages hors `Channels` sont conservés ; seules les cellules `Enabled` et `Name` nécessaires sont adaptées dans la copie exportée.
- Les modèles dLive et Avantis sont embarqués dans l'application. Aucun fichier modèle externe n'est demandé.
- Les formats JSON/CSV génériques, CSV Allen & Heath dLive/Avantis et ZIP Yamaha CL/QL restent disponibles.

## Atomic Bomb configurable

L'onglet a été rebaptisé avec un ton plus assumé : **Générateur d'expérience horrible (mais pédagogique)**.

Avant les trois confirmations, il est maintenant possible de protéger séparément :

- les noms de machines ;
- les labels TX et RX ;
- les patchs/subscriptions ;
- les modes réseau et Preferred Master ;
- les latences, fréquences et bits par échantillon ;
- les IP principales.

Toutes les options restent cochées par défaut. Si les patchs sont exclus mais que des noms changent, les références reconnues sont mises à jour pour conserver le routage existant. Le fichier original, les identifiants techniques, DNS, passerelles et interfaces secondaires restent protégés.

## Corrections macOS

- Boutons d'import/export et de prévisualisation entièrement visibles en Full HD.
- Traductions manquantes du bandeau, de la recherche et des filtres corrigées.
- Après un second import identique, l'interface explique désormais qu'aucun changement n'est à appliquer au lieu de laisser un bouton désactivé sans raison visible.
- Le nouveau panneau Atomic Bomb et ses options tiennent dans une fenêtre 1366 × 768 lors des tests Avalonia sans écran.

Merci à **Tobias Grupe / togrupe** pour ses captures et ses retours sur un Mac Intel réel, particulièrement précieux puisque le développement local principal est effectué sous Windows.

## Vidéos de présentation

- [Présentation française de DCE v3.3](https://github.com/Mamat79/DanteConfigEditorV3/releases/download/v3.3/dce-v33-presentation-fr.mp4)
- [English presentation of DCE v3.3](https://github.com/Mamat79/DanteConfigEditorV3/releases/download/v3.3/dce-v33-presentation-en.mp4)

Les deux vidéos durent environ une minute, utilisent un preset synthétique anonymisé et comportent des sous-titres intégrés. Des fichiers `.srt` séparés et des sommes SHA-256 sont également joints à la Release.

## Distribution et validation

- Installateur autonome Windows x64 : `DanteConfigEditorV3_3_Installer.exe`, runtime .NET 8 inclus.
- DMG autonomes macOS Apple Silicon et Intel.
- 150 tests du moteur et des contrats Windows réussissent en configuration Release.
- 11 tests d'interface macOS sans écran réussissent également.
- La Release V3.2 reste disponible comme version historique. Les pages de Releases V3.08 et V3.09 sont retirées à la demande du mainteneur ; leurs tags et commits restent dans Git.

## Limites

- Seul un import réussi dans Dante Controller ou l'outil Dante officiel adapté confirme la compatibilité finale.
- Le support DMT correspond aux modèles observés en version 2.13.0 ; une évolution future peut demander une adaptation.
- L'installateur Windows n'est pas signé Authenticode et les DMG ne sont pas notariés.

**By Mamat et ses agents**

Dépôt public : https://github.com/Mamat79/DanteConfigEditorV3
