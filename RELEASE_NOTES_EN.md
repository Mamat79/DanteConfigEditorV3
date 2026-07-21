# Dante Config Editor V3.2

[Français](https://github.com/Mamat79/DanteConfigEditorV3/blob/main/RELEASE_NOTES.md) | **English**

## Status

V3.2 is the current official Windows and macOS version. This remains an unofficial third-party tool, not affiliated with Audinate, and it may still contain bugs. Always work on a copy of Dante XML files and validate the result with official Dante tools before production use.

V3.2 replaces V3.1 on `main` and becomes the `Latest` Release. Historical Releases `v3.09` and `v3.08` remain available with their own immutable tags and files.

## Maintenance update included in V3.2

- Export the synoptic as vector SVG or PDF.
- Automatically space connection ports and enlarge device cards in dense projects to reduce overlapping arrows.
- Toggle device visibility with one click and reuse previously entered locations from a list.
- Select and preview label imports directly, with the proposed range limited to the TX/RX capacity of selected devices.
- Use generic CSV by default: only the new file name and destination folder are requested.
- Bundle native DMT dLive/Avantis, Allen & Heath dLive/Avantis, and Yamaha CL/QL templates: export only asks for a file name.
- Automatically switch to RX for a device without TX channels and clearly distinguish generic DCE CSV from native console CSV.
- Fix export opening for a device without TX channels: RX is selected before the first preview and an empty range remains handled inside the dialog.
- Updated French and English manuals.

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
- XLSX support for **[togrupe dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools)**: read the `Channels` sheet and directly create a dLive or Avantis workbook from the bundled MIT-licensed templates.
- Native Allen & Heath dLive/Avantis CSV: read an existing export or directly create a new file from the bundled template while replacing only `Input` labels.
- Yamaha CL/QL: read a ZIP package or an individual `InName.csv`, and directly create a complete ZIP; the eight other CSV files remain unchanged.
- Label preview and eight-character ASCII adaptation only after explicit opt-in.
- Bundled templates are never modified, and every output is written atomically to a new file.

Tests covered the supplied samples: DMT Avantis/dLive, Avantis/dLive CSV, Yamaha CL5 with 72 inputs, and Yamaha QL5 with 64 inputs.

## Visual synoptic

- Assign a physical location to every device.
- Show devices with one click, hide them, and reorder them without changing the preset.
- Reuse previously entered locations from a list.
- Compress consecutive subscriptions into one cable, for example `TX 1-32 to RX 1-32`.
- Use orthogonal routes, shared trunks, colored cables, and automatically spaced ports to reduce crossings and overlapping arrows.
- Use a separate two-column legend for dense projects.
- Export vector SVG and PDF files suitable for printing and technical documentation.
- Store locations and presentation choices in a local sidecar file separate from Dante XML.

Building, previewing, or exporting the synoptic never changes the loaded XML document.

## Easy patch and Atomic Bomb exercise

- `Easy patch` keeps RX channels on the left and TX channels on the right, with quick device navigation.
- Selections and ranges can be previewed, accumulated, and applied in one operation.
- The compact matrix supports single assignments and horizontal, vertical, or diagonal series.
- Conflicts always require an explicit choice before an existing subscription is replaced.
- After three confirmations, `Atomic Bomb` creates a deliberately disordered preset for offline troubleshooting exercises.
- The source file remains intact and technical Dante identifiers stay protected.

## Installation

- Self-contained Windows installer: `DanteConfigEditorV3_2_Installer.exe`.
- Default folder: `C:\Program Files\Dante Config Editor V3.2\`.
- .NET 8 runtime and French/English manuals included.
- Replaces detected older V3 installations without deleting local working data.
- Self-contained Apple Silicon and Intel macOS DMGs.

## Platforms

- Windows x64: self-contained installer with the .NET 8 runtime and FR/EN manuals.
- macOS Apple Silicon: self-contained DMG for M1 and later Macs.
- macOS Intel: self-contained DMG for Intel 64-bit Macs.
- The XML engine and its safeguards are shared by Windows and macOS.
- DMGs are ad hoc signed but not notarized; first launch may require right-clicking the app and selecting `Open`.

## XML safety

- Synoptic information stays in a local sidecar and adds no element to Dante XML.
- Changes use stable device identity and channel Dante IDs.
- Unknown XML values and paths remain preserved or blocked by the existing guard.
- Exports do not alter the loaded project.
- `Save as` remains atomic and backs up an existing destination.

## Automated validation

- 143 engine and Windows contract tests pass in Release configuration.
- 10 headless macOS interface tests also pass.
- Windows and macOS builds complete without warnings.
- Additional checks cover the Rectorat preset, a real DMT workbook, SVG/PDF synoptic export, and preservation of the loaded XML.

## Limitations

- Only a successful import into Dante Controller or the appropriate official Dante tool confirms final compatibility.
- The synoptic represents subscriptions found in the file; it does not discover actual physical cabling.
- Supported console formats match the tested samples; future vendor format changes may require an update.
- The Windows installer is not Authenticode signed, and the DMGs are not notarized.

## Credit

The project started as a small personal XML editor written manually by Mamat. Modern development agents then accelerated the interface, XML safeguards, automated tests, documentation, and packaging under Mamat's functional direction.

**By Mamat et ses agents**

Public repository: https://github.com/Mamat79/DanteConfigEditorV3
