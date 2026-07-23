# Dante Config Editor V3.5 - développement

[English release notes](RELEASE_NOTES_EN.md)

## Statut

V3.5 est une version de développement pour Windows et macOS, installable à côté de la V3.4.2 stable. Dante Config Editor reste un outil tiers non officiel Audinate et peut encore contenir des bugs. Travaillez sur une copie des XML Dante et validez toujours le fichier généré dans l'outil Dante officiel adapté.

## Patch et performances

- La matrice visuelle met à jour uniquement les cellules concernées au lieu de reconstruire tous les contrôles après chaque clic.
- Les changements restent en attente jusqu'à leur application explicite au projet.
- Les en-têtes TX/RX restent visibles et les ascenseurs sont synchronisés.
- Patch 1:1 par plage, échange des sélections, zoom de 50 à 200 % et ajustement à la fenêtre.
- Tab et Maj+Tab valident un renommage direct puis passent au canal suivant ou précédent.

## Imports de labels

- Adaptateurs séparés pour JSON, CSV, DMT XLSX/ODS et packages console.
- Validation stricte des versions, colonnes obligatoires, canaux dupliqués et champs JSON inconnus.
- Rapport visible avant application : format, version source, listes, machines, canaux, lignes ignorées, labels vides, doublons et avertissements.
- Compatibilité JSON/CSV testée avec les exporteurs DMT 2.14.0-RC1 au commit `3c34052`.

## Sécurité XML

- La suppression d'une machine et de ses subscriptions associées est testée jusqu'à la sauvegarde et la relecture.
- La création de machines génériques est abandonnée.
- La duplication de rôle n'est pas proposée sans preuve d'import Dante Controller pour les identifiants techniques générés.
- Les règles de sauvegarde atomique, de récupération et de protection des chemins XML inconnus restent actives.

## Documentation

- Notices complète et rapide actualisées en français et en anglais.
- Deux vidéos de présentation de 55 secondes, sans voix ni piste audio, avec texte intégré.
- Les captures utilisent exclusivement un preset synthétique anonymisé.

## Distribution

- Installateur Windows x64 autonome : `DanteConfigEditorV3_5_Installer.exe`, runtime .NET 8 inclus.
- La V3.5 possède son propre AppId et son propre dossier Program Files ; elle ne remplace pas la V3.4.2.
- DMG autonomes V3.5 pour Apple Silicon et Intel, avec runtime .NET 8 et notices FR/EN.
- L'application Mac V3.5 possède son propre nom de bundle et son propre identifiant ; elle peut cohabiter avec la V3.4.2.

## Limites

- Seul un import réussi dans Dante Controller ou l'outil Dante officiel adapté confirme la compatibilité finale.
- Le renommage direct d'une source TX externe absente du XML reste bloqué.
- L'installateur Windows n'est pas signé Authenticode.
- Les DMG Mac sont signés ad hoc, sans certificat Apple Developer ID ni notarisation.
