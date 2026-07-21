# Dante Config Editor V3.2

## Français

La V3.2 réunit toutes les fonctions de fichiers dans un onglet **Import / Export** organisé en trois espaces : **Labels**, **Rapports et patchbook** et **Synoptique**.

Le synoptique transforme les subscriptions du projet en un schéma SVG en couleur. Les machines peuvent être regroupées par emplacement physique, masquées ou réordonnées. Les séries consécutives sont condensées en un seul câble, par exemple **TX 1-32 vers RX 1-32**. Les routes orthogonales, troncs partagés et la légende sur deux colonnes rendent les projets denses plus lisibles.

Les emplacements et préférences de présentation sont enregistrés dans un fichier local séparé. **Aucune information de synoptique n'est ajoutée au XML Dante.**

L'échange de labels accepte les formats JSON/CSV génériques, les modèles XLSX **[dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools)**, les CSV Allen & Heath dLive/Avantis et les packages ZIP ou `InName.csv` Yamaha CL/QL. Chaque export crée une copie : les modèles et exports console originaux ne sont jamais modifiés.

Dante Config Editor reste un éditeur XML hors ligne tiers et non officiel Audinate. Travaillez sur une copie et validez toujours le résultat dans Dante Controller ou l'outil Dante officiel adapté.

### Distribution

- Installateur autonome Windows x64 : `DanteConfigEditorV3_2_Installer.exe`, runtime .NET 8 inclus.
- DMG autonomes macOS Apple Silicon et Intel.
- La V3.2 remplace les anciennes installations V3 détectées et devient la Release `Latest`.
- Les Releases historiques `v3.09` et `v3.08` restent disponibles avec leurs propres fichiers.

## English

V3.2 brings every file-oriented feature into one **Import / Export** tab organized into **Labels**, **Reports and patchbook**, and **Synoptic**.

The synoptic turns project subscriptions into a colored SVG diagram. Devices can be grouped by physical location, hidden, or reordered. Consecutive ranges are compressed into one cable, for example **TX 1-32 to RX 1-32**. Orthogonal routes, shared trunks, and a two-column legend keep dense projects readable.

Locations and presentation preferences are stored in a separate local sidecar file. **No synoptic metadata is ever added to Dante XML.**

Label exchange supports generic JSON/CSV, **[dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools)** XLSX templates, Allen & Heath dLive/Avantis CSV files, and Yamaha CL/QL ZIP packages or `InName.csv`. Every export creates a copy, so original templates and console exports are never modified.

Dante Config Editor remains an offline third-party XML editor and is not an official Audinate product. Work on a copy and always validate the result in Dante Controller or the appropriate official Dante tool.

### Distribution

- Self-contained Windows x64 installer: `DanteConfigEditorV3_2_Installer.exe`, including .NET 8.
- Self-contained Apple Silicon and Intel macOS DMGs.
- V3.2 replaces detected older V3 installations and becomes the `Latest` Release.
- Historical Releases `v3.09` and `v3.08` remain available with their own assets.

**By Mamat et ses agents**
