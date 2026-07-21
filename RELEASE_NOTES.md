# Dante Config Editor V3.1

[English release notes](RELEASE_NOTES_EN.md)

## Statut

Version officielle V3.1 pour Windows x64, macOS Apple Silicon et macOS Intel. Il s'agit d'un outil tiers non officiel Audinate et cette application peut encore contenir des bugs.

La V3.1 devient la version courante de `main` et la Release `Latest`. Les Releases `v3.08` et `v3.09` restent disponibles avec leurs tags et leurs propres fichiers ; aucun de leurs installateurs n'est remplacé. Travaillez toujours sur une copie des XML Dante et validez le résultat dans les outils Dante officiels avant toute utilisation réelle.

## Pourquoi cet outil existe

Dante Config Editor est né pour répondre à ce qui me manquait dans Dante Controller : vérifier rapidement une configuration complète sans ouvrir successivement toutes les pages de devices, latences, sample rates, modes réseau, Preferred Master ou IP. Il permet de survoler le preset, de corriger les valeurs nécessaires et de préparer une configuration hors ligne.

Il répond aussi au besoin de renommer une machine ou ses canaux TX sur un réseau déjà patché sans devoir reconstruire manuellement toutes les subscriptions reconnues. L'application reste un éditeur XML hors ligne et ne pilote jamais le réseau Dante en direct.

## Échange de labels et DMT

Cette V3.1 peut échanger explicitement des labels avec **[dLive MIDI Tools (DMT) de togrupe](https://github.com/togrupe/dlive-midi-tools)**. La communication se fait par fichiers, dans les deux sens, et non par une connexion temps réel :

- **DMT → Dante Config Editor** : lecture de la feuille `Channels` d'un classeur XLSX DMT et affectation de ses labels aux canaux Dante choisis.
- **Dante Config Editor → DMT** : création d'une copie du modèle XLSX DMT contenant les labels Dante exportés.
- **Lien du logiciel DMT** : [github.com/togrupe/dlive-midi-tools](https://github.com/togrupe/dlive-midi-tools).

- Import et export des labels TX ou RX pour une ou plusieurs machines.
- Choix de la liste source, des machines cibles, du premier canal et du nombre de canaux.
- Prévisualisation ligne par ligne avant toute modification du XML.
- Mise à jour des subscriptions reconnues lors d'un renommage TX.
- JSON versionné pour les échanges complets et CSV lisible par les tableurs.
- Lecture des classeurs XLSX de [dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools).
- Export DMT en créant une copie du modèle choisi ; le classeur original n'est jamais modifié.
- Adaptation ASCII et huit caractères visible dans l'aperçu et activée uniquement sur demande explicite.

Cette fonction a été pensée dans un premier temps pour faciliter les échanges de labels avec DMT. Merci à **togrupe** pour la proposition de collaboration et le format de référence. Le classeur original reste inchangé et chaque adaptation imposée par DMT est visible avant export.

## Atomic Bomb

- Le gros bouton quitte la colonne latérale et `Sécurité et journal`.
- Un onglet `Atomic Bomb` dédié est placé après `Sécurité et journal` sur Windows et macOS.
- Trois confirmations restent obligatoires.
- Le fichier source, les identifiants techniques, le DNS, les passerelles et les interfaces secondaires restent protégés.

Merci à **Charles Bouticourt** pour l'idée de cette fonction de formation.

## Installation

- Installateur Windows autonome : `DanteConfigEditorV3_1_Installer.exe`.
- Dossier par défaut : `C:\Program Files\Dante Config Editor V3.1\`.
- Nouveau raccourci `Dante Config Editor V3.1` dans le menu Démarrer et sur le Bureau.
- Le runtime .NET 8 et les notices françaises/anglaises sont inclus.
- L'installateur remplace les V3.07, V3.08 ou V3.09 détectées, puis sait mettre à niveau une V3.1 existante.
- Deux DMG autonomes sont construits pour macOS Apple Silicon et Intel.
- Les DMG sont signés ad hoc mais ne sont pas notariés par Apple.

## Sécurité XML

- Les modifications de labels passent par l'identité stable des machines et les Dante Id des canaux.
- Les imports sont préparés puis appliqués en une seule mutation groupée et une seule étape d'annulation.
- Les valeurs et chemins XML inconnus restent préservés ou bloqués conformément au garde-fou existant.
- Les exports JSON/CSV/XLSX ne modifient pas le projet Dante chargé.
- `Enregistrer sous` reste atomique et protège une destination existante par une sauvegarde.

## Limites

- La compatibilité finale ne peut être confirmée que par un import réussi dans Dante Controller ou l'outil Dante officiel adapté.
- Le format XLSX DMT pris en charge correspond à la feuille `Channels` observée dans DMT 2.13.0 ; une évolution future du modèle peut demander une adaptation.
- Le transfert de labels est basé sur des canaux et des plages, pas sur une connexion directe entre les deux applications.
- L'installateur Windows n'est pas signé Authenticode et les DMG ne sont pas notariés.

## Crédit

Le projet a commencé comme un petit éditeur XML personnel écrit manuellement par Mamat. Les agents de développement actuels ont ensuite permis d'accélérer fortement l'interface, les garde-fous XML, les tests, la documentation et la distribution, sous la direction fonctionnelle de Mamat.

**By Mamat et ses agents**

Dépôt public : https://github.com/Mamat79/DanteConfigEditorV3
