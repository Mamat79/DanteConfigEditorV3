# Dante Config Editor V3.1

[Notes de version en français](RELEASE_NOTES.md)

## Status

Official V3.1 build for Windows x64, macOS Apple Silicon and macOS Intel. This is an unofficial third-party tool, not affiliated with Audinate, and it may still contain bugs.

V3.1 becomes the current `main` version and the `Latest` Release. Releases `v3.08` and `v3.09` remain available with their own immutable tags and files; none of their installers are replaced. Always work on a copy of Dante XML files and validate the result with official Dante tools before production use.

## Why this tool exists

Dante Config Editor was created to provide what I personally was missing in Dante Controller: a quick review of a complete configuration without opening every page for devices, latency, sample rates, network modes, Preferred Master state or IP settings. It provides one overview, allows required corrections and supports offline preparation.

It also addresses a recurring need on already patched networks: renaming a device or TX channels without manually rebuilding every recognized subscription. The application remains an offline XML editor and never controls the live Dante network.

## Importing and exporting labels

V3.1 imports and exports labels through JSON, CSV, or XLSX. JSON and CSV are generic; XLSX uses a copy of a **[togrupe's dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools)** template compatible with dLive / Avantis. Communication is file-based and is not a real-time connection:

- **DMT → Dante Config Editor**: read the `Channels` sheet from a DMT XLSX workbook and map its labels to selected Dante channels.
- **Dante Config Editor → DMT**: create a copy of the DMT XLSX template containing exported Dante labels.
- **DMT software link**: [github.com/togrupe/dlive-midi-tools](https://github.com/togrupe/dlive-midi-tools).

- Import and export TX or RX labels for one or several devices.
- Choose the source set, target devices, first channel and channel count.
- Review every mapping before the XML is changed.
- Update recognized subscriptions when imported TX labels rename their sources.
- Versioned JSON for complete exchange and spreadsheet-friendly CSV.
- Read XLSX workbooks from [dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools).
- Export to a copy of a selected DMT template; the original workbook is never modified.
- Show ASCII/eight-character changes in the preview and apply them only after explicit opt-in.

This feature was initially designed to simplify label exchange with DMT. Thanks to **togrupe** for proposing the collaboration and providing the reference format. The original workbook remains unchanged, and every DMT-specific adaptation is visible before export.

## Atomic Bomb

- The large button no longer occupies the project sidebar or `Safety and log`.
- A dedicated `Atomic Bomb` tab now follows `Safety and log` on Windows and macOS.
- Three confirmations remain mandatory.
- The source file, technical identifiers, DNS, gateways and secondary interfaces remain protected.

Special thanks to **Charles Bouticourt** for the idea behind this training feature.

## Installation

- Self-contained Windows installer: `DanteConfigEditorV3_1_Installer.exe`.
- Default folder: `C:\Program Files\Dante Config Editor V3.1\`.
- `Dante Config Editor V3.1` Start menu and desktop shortcuts.
- .NET 8 runtime and both French/English manuals are included.
- The installer replaces detected V3.07, V3.08 or V3.09 installations and upgrades an existing V3.1 installation.
- Two self-contained DMGs are built for Apple Silicon and Intel Macs.
- DMGs are ad hoc signed but are not Apple-notarized.

## XML safety

- Label changes use stable device identity and channel Dante IDs.
- Imports are staged and applied as one grouped mutation and one undo step.
- Unknown XML values and paths remain preserved or blocked by the existing guard.
- JSON/CSV/XLSX exports do not modify the loaded Dante project.
- `Save as` remains atomic and backs up an existing destination.

## Limitations

- Final compatibility can only be confirmed by a successful import into Dante Controller or the appropriate official Dante tool.
- DMT XLSX support targets the `Channels` sheet observed in DMT 2.13.0; a future template change may require an update.
- Label exchange is file and range based, not a live connection between both applications.
- The Windows installer is not Authenticode signed and the DMGs are not notarized.

## Credit

The project started as a small personal XML editor written manually by Mamat. Modern development agents then accelerated the interface, XML safeguards, automated tests, documentation and packaging under Mamat's functional direction.

**By Mamat et ses agents**

Public repository: https://github.com/Mamat79/DanteConfigEditorV3
