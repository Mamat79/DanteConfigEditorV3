# Dante Config Editor V3.2

[Notes de version en français](RELEASE_NOTES.md)

## Status

V3.2 is the current official Windows and macOS version. This remains an unofficial third-party tool, not affiliated with Audinate, and it may still contain bugs. Always work on a copy of Dante XML files and validate the result with official Dante tools before production use.

V3.2 replaces V3.1 on `main` and becomes the `Latest` Release. Historical Releases `v3.09` and `v3.08` remain available with their own immutable tags and files.

## Why this tool exists

Dante Config Editor was created to provide what I personally was missing in Dante Controller: a quick review of a complete configuration without opening every page for devices, latency, sample rates, network modes, Preferred Master state, or IP settings. It provides one overview, allows required corrections, and supports offline preparation.

It also addresses a recurring need on already patched networks: renaming a device or TX channels without manually rebuilding every recognized subscription. The application remains an offline XML editor and never controls the live Dante network.

## Reorganized Import / Export workspace

Features that read or produce files are grouped under one main `Import / Export` tab:

- `Labels`: import and export for one or several devices.
- `Reports and patchbook`: TXT/PDF reports, TXT/CSV patchbooks, and simple topology.
- `Synoptic`: preparation and export of a colored visual diagram.

`Safety and log` keeps file checks, comparison, action history, and manual access.

## Labels and consoles

- Generic JSON and CSV for exchange with other tools.
- XLSX support for **[togrupe dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools)**: read the `Channels` sheet and export to a copy of a dLive or Avantis template.
- Native Allen & Heath dLive/Avantis CSV: read and create a copy of a console export while replacing only `Input` labels.
- Yamaha CL/QL: read and create a copy of a ZIP package or an individual `InName.csv`; every other CSV in the package remains unchanged.
- Label preview and eight-character ASCII adaptation only after explicit opt-in.
- Original templates and console exports are never modified.

Tests covered the supplied samples: DMT Avantis/dLive, Avantis/dLive CSV, Yamaha CL5 with 72 inputs, and Yamaha QL5 with 64 inputs.

## Visual synoptic

- Assign a physical location to every device.
- Show, hide, and reorder devices without changing the preset.
- Compress consecutive subscriptions into one cable, for example `TX 1-32 to RX 1-32`.
- Use orthogonal routes, shared trunks, and colored cables to reduce crossings.
- Use a separate two-column legend for dense projects.
- Export a standalone SVG suitable for printing and technical documentation.
- Store locations and presentation choices in a local sidecar file separate from Dante XML.

Building, previewing, or exporting the synoptic never changes the loaded XML document.

## Installation

- Self-contained Windows installer: `DanteConfigEditorV3_2_Installer.exe`.
- Default folder: `C:\Program Files\Dante Config Editor V3.2\`.
- .NET 8 runtime and French/English manuals included.
- Replaces detected older V3 installations without deleting local working data.
- Self-contained Apple Silicon and Intel macOS DMGs.

## XML safety

- Synoptic information stays in a local sidecar and adds no element to Dante XML.
- Changes use stable device identity and channel Dante IDs.
- Unknown XML values and paths remain preserved or blocked by the existing guard.
- Exports do not alter the loaded project.
- `Save as` remains atomic and backs up an existing destination.

## Limitations

- Only a successful import into Dante Controller or the appropriate official Dante tool confirms final compatibility.
- The synoptic represents subscriptions found in the file; it does not discover actual physical cabling.
- Supported console formats match the tested samples; future vendor format changes may require an update.
- The Windows installer is not Authenticode signed, and the DMGs are not notarized.

## Credit

The project started as a small personal XML editor written manually by Mamat. Modern development agents then accelerated the interface, XML safeguards, automated tests, documentation, and packaging under Mamat's functional direction.

**By Mamat et ses agents**

Public repository: https://github.com/Mamat79/DanteConfigEditorV3
