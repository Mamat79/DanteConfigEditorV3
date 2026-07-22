# Dante Config Editor V3.4

[English release notes](RELEASE_NOTES_EN.md)

## Statut

V3.4 est la version officielle courante pour Windows et macOS. Dante Config Editor reste un outil tiers non officiel Audinate et peut encore contenir des bugs. Travaillez sur une copie des XML Dante et validez toujours le fichier généré dans l'outil Dante officiel adapté.

## Ergonomie Patch

- Renommage direct des machines, canaux RX et canaux TX dans la page Patch.
- Renommage direct des canaux RX/TX dans Easy patch.
- Extension d'une série numérique, par exemple `Mic 1`, `Mic 2`, puis glisser jusqu'au canal cible.
- Grille de patch ouverte en premier ; `Sélection et plage` reste disponible dans le second onglet.
- Filtres RX placés au-dessus des filtres TX.
- Action globale permettant de choisir l'unique Preferred Master du projet.

## Synoptique

- `Reset` efface les déplacements manuels et reconstruit un ordre propre dans chaque emplacement.
- Un flux simple garde une flèche TX vers RX.
- Deux flux opposés entre les mêmes machines sont regroupés en une seule ligne avec une flèche à chaque extrémité.
- L'aperçu, le SVG et le PDF utilisent la même représentation directionnelle.

## Affichage

- Les panneaux de réglages Configuration sont visibles au premier lancement ; leur état est ensuite mémorisé.
- Les boutons et contrôles principaux ont une hauteur minimale adaptée à l'échelle Windows 125 %.
- La zone centrale Atomic Bomb est agrandie.

## Distribution

- Installateur Windows x64 autonome : `DanteConfigEditorV3_4_Installer.exe`, runtime .NET 8 inclus.
- Paquets macOS Apple Silicon et Intel prévus par le workflow de publication.
- La V3.3 reste disponible comme version historique et n'est pas remplacée par la V3.4.

## Limites

- Seul un import réussi dans Dante Controller ou l'outil Dante officiel adapté confirme la compatibilité finale.
- Le renommage direct d'une source TX externe absente du XML reste bloqué, car la machine ne peut pas être identifiée de manière sûre.
- L'installateur Windows n'est pas signé Authenticode et les paquets macOS ne sont pas notariés.
