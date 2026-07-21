# Dante Config Editor V3.2

[English release notes](RELEASE_NOTES_EN.md)

## Statut

V3.2 est la version officielle courante pour Windows et macOS. Il s'agit toujours d'un outil tiers non officiel Audinate et cette application peut encore contenir des bugs. Travaillez sur une copie des XML Dante et validez le résultat dans les outils Dante officiels avant toute utilisation réelle.

La V3.2 remplace la V3.1 sur `main` et devient la Release `Latest`. Les Releases historiques `v3.09` et `v3.08` restent disponibles avec leurs tags immuables et leurs propres fichiers.

## Pourquoi cet outil existe

Dante Config Editor est né pour répondre à ce qui me manquait dans Dante Controller : vérifier rapidement une configuration complète sans ouvrir successivement toutes les pages de devices, latences, sample rates, modes réseau, Preferred Master ou IP. Il permet de survoler le preset, de corriger les valeurs nécessaires et de préparer une configuration hors ligne.

Il répond aussi au besoin de renommer une machine ou ses canaux TX sur un réseau déjà patché sans devoir reconstruire manuellement toutes les subscriptions reconnues. L'application reste un éditeur XML hors ligne et ne pilote jamais le réseau Dante en direct.

## Import / Export réorganisé

Les fonctions qui produisent ou lisent des fichiers sont regroupées dans un onglet principal `Import / Export` :

- `Labels` : import et export pour une ou plusieurs machines.
- `Rapports et patchbook` : rapport TXT/PDF, patchbooks TXT/CSV et topologie simple.
- `Synoptique` : préparation et export d'un schéma visuel en couleur.

`Sécurité et journal` conserve les contrôles du fichier, les comparaisons, l'historique et l'accès aux notices.

## Labels et consoles

- JSON et CSV génériques pour les échanges avec d'autres outils.
- XLSX compatible avec **[dLive MIDI Tools (DMT) de togrupe](https://github.com/togrupe/dlive-midi-tools)** : lecture de la feuille `Channels` et export vers une copie d'un modèle dLive ou Avantis.
- CSV natif Allen & Heath dLive/Avantis : lecture et création d'une copie d'un export console, en ne remplaçant que les labels `Input`.
- Yamaha CL/QL : lecture et création d'une copie d'un package ZIP ou d'un fichier `InName.csv`; les autres CSV du package restent inchangés.
- Prévisualisation des labels et adaptation ASCII sur huit caractères uniquement sur demande explicite.
- Les modèles et exports console originaux ne sont jamais modifiés.

Les essais ont couvert les exemples fournis : DMT Avantis/dLive, CSV Avantis/dLive, Yamaha CL5 avec 72 entrées et Yamaha QL5 avec 64 entrées.

## Synoptique visuel

- Affectation d'un emplacement physique à chaque machine.
- Affichage en un clic, masquage et réorganisation des machines sans modifier le preset.
- Réutilisation des emplacements déjà saisis depuis une liste.
- Regroupement des subscriptions consécutives en un seul câble, par exemple `TX 1-32 vers RX 1-32`.
- Routes orthogonales, troncs partagés, câbles colorés et ports espacés automatiquement pour limiter les croisements et les flèches superposées.
- Légende séparée sur deux colonnes pour les projets denses.
- Exports SVG et PDF vectoriels, adaptés à l'impression et aux dossiers techniques.
- Stockage des emplacements et de la présentation dans un fichier local séparé du XML Dante.

La création, l'aperçu et l'export du synoptique ne modifient jamais le document XML chargé.

## Installation

- Installateur Windows autonome : `DanteConfigEditorV3_2_Installer.exe`.
- Dossier par défaut : `C:\Program Files\Dante Config Editor V3.2\`.
- Runtime .NET 8 et notices françaises/anglaises inclus.
- Remplacement des anciennes installations V3 détectées, sans suppression des données locales de travail.
- DMG autonomes macOS Apple Silicon et Intel.

## Sécurité XML

- Les informations de synoptique restent dans un fichier annexe local et n'ajoutent aucune balise au XML Dante.
- Les modifications utilisent l'identité stable des machines et les Dante Id des canaux.
- Les valeurs et chemins XML inconnus restent préservés ou bloqués par le garde-fou existant.
- Les exports n'altèrent pas le projet chargé.
- `Enregistrer sous` reste atomique et sauvegarde une destination existante.

## Limites

- Seul un import réussi dans Dante Controller ou l'outil Dante officiel adapté confirme la compatibilité finale.
- Le synoptique représente les subscriptions présentes dans le fichier ; il ne découvre pas le câblage physique réel.
- Les formats console pris en charge correspondent aux exemples réellement testés ; une évolution d'un format fabricant peut demander une mise à jour.
- L'installateur Windows n'est pas signé Authenticode et les DMG ne sont pas notariés.

## Crédit

Le projet a commencé comme un petit éditeur XML personnel écrit manuellement par Mamat. Les agents de développement actuels ont ensuite permis d'accélérer fortement l'interface, les garde-fous XML, les tests, la documentation et la distribution, sous la direction fonctionnelle de Mamat.

**By Mamat et ses agents**

Dépôt public : https://github.com/Mamat79/DanteConfigEditorV3
