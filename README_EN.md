# Dante Config Editor V3.2

[Français](README.md) | **English**

V3.2 is the official Windows and macOS version. Published source is available on `main`.

**Stable version: [V3.2 Release for Windows and macOS](https://github.com/Mamat79/DanteConfigEditorV3/releases/tag/v3.2)**

> **Status: official V3.2. Unofficial third-party tool, not affiliated with Audinate.**
> This software may still contain bugs. Previous versions remain available in the GitHub Releases history. Always work on a copy and validate generated XML with official Dante tools.

> **Importing and exporting labels through JSON, CSV, DMT XLSX for dLive and Avantis, A&H CSV, and Yamaha CL/QL ZIP/CSV.** This bridge works offline and never modifies the original console template.

## Origin and agent-assisted development

Dante Config Editor began as an attempt to provide what I personally was missing in Dante Controller. It started as a small personal application written manually by Mamat to solve a practical field need: checking a Dante configuration quickly without opening each page of the application in turn. The goal was to provide one overview of devices, latency, sample rates, network modes, Preferred Master state, IP addresses and channels, with the ability to correct preset values when needed.

Renaming on an already patched network was another recurring problem. Changing a device name or TX channel names can require revisiting the affected subscriptions and rebuilding part of the patch. The editor was therefore designed to update recognized XML references during renaming and preserve the patch as far as the preset structure allows.

Finally, the offline workflow available in Dante Controller did not provide the consolidated preparation process required for this use case. Reviewing, editing, merging and preparing a preset without being connected to the Dante network therefore became one of the project's central goals.

Modern development agents then enabled a much larger step forward: safer saving, regression tests, a bilingual interface, self-contained installers, a macOS build, reports and more advanced patching tools. Product needs, functional decisions and field validation remain directed by Mamat; the agents contribute to analysis, implementation, testing and documentation.

## Importing and exporting labels

The `Import / Export` workspace contains channel-label exchange for one or several devices. TX or RX labels can be exported to JSON or CSV, imported with explicit device and range mapping, and checked row by row before application. TX renames still update recognized XML subscriptions.

JSON and CSV remain generic exchange formats. Native profiles also support these console and tool formats:

- **DMT → Dante Config Editor**: read the `Channels` sheet from a DMT XLSX workbook, then map its labels to the TX or RX channels of one or several Dante devices.
- **Dante Config Editor → DMT**: create a copy of the selected DMT XLSX template containing labels exported from the selected Dante devices.
- **DMT project**: [togrupe/dlive-midi-tools](https://github.com/togrupe/dlive-midi-tools).
- **Allen & Heath dLive / Avantis**: read or create a copy of a console CSV export; only `Input` name fields are changed.
- **Yamaha CL / QL**: read or create a copy of the ZIP package, or an individual `InName.csv`; every other CSV in the package remains unchanged.

Original DMT workbooks and console exports are never modified. Every eight-character ASCII adaptation is visible in the preview and must be enabled explicitly; generic JSON and CSV retain full Unicode labels. This is an offline file bridge, not a direct or real-time connection between applications.

## Visual synoptic

V3.2 adds a colored synoptic under `Import / Export > Synoptic`. Each device can be assigned to a physical location, shown or hidden, and reordered. Consecutive subscriptions between two devices are compressed into one cable, for example `TX 1-32 to RX 1-32`, instead of drawing dozens of unreadable lines.

Locations and presentation choices are saved in a separate local sidecar file. They are never inserted into Dante XML. The SVG export contains devices, numbered cables, and a detailed legend and can be opened in a browser, printed, or included in technical documentation.

## General presentation

[![Dante Config Editor V3.09 presentation using a synthetic anonymized preset](docs/media/guide/en/overview.png)](docs/media/dante-config-editor-v309-overview-en.mp4)

- **[Watch the full English presentation (MP4)](docs/media/dante-config-editor-v309-overview-en.mp4)** - readable subtitles are burned into the video; [separate SRT file](docs/media/dante-config-editor-v309-overview-en.srt).
- **[Watch the full French presentation (MP4)](docs/media/dante-config-editor-v309-overview-fr.mp4)** - [separate SRT file](docs/media/dante-config-editor-v309-overview-fr.srt).
- **[Read the full English guide (PDF)](docs/Notice_DanteConfigEditorV3_EN.pdf)** or the [quick start](docs/QuickStart_DanteConfigEditorV3_EN.pdf).
- **[Lire la notice complète en français (PDF)](docs/Notice_DanteConfigEditorV3_FR.pdf)** ou le [démarrage rapide](docs/QuickStart_DanteConfigEditorV3_FR.pdf).

The screenshots, manuals and videos use only a synthetic anonymized preset. They contain no production device name, file or path.

## What the application does

- Opens Dante XML configuration files offline.
- Displays devices, TX/RX channels, latency, network mode and Preferred Master state.
- Renames devices and TX/RX channels, including channel ranges.
- Imports and exports channel labels for one or several devices through JSON, CSV, DMT XLSX, A&H dLive/Avantis CSV, or Yamaha CL/QL packages, with range mapping and preview.
- Groups label exchange, reports, patchbooks, and the synoptic in one `Import / Export` tab with three clear subtabs.
- Generates a colored SVG synoptic with locations, optional devices, custom ordering, and compressed consecutive cable ranges.
- Keeps synoptic layout information outside Dante XML in a separate local file.
- Updates recognized RX subscriptions when a referenced TX channel is renamed.
- Resets channel names.
- Deletes a device and removes recognized subscriptions that reference it.
- Merges devices from a second XML file into the open project.
- Handles duplicate device names with manual or automatic renaming during merge.
- Edits supported audio and network values exposed by recognized XML structures.
- Provides the classic `Patch` view and the Windows `Easy patch` workspace.
- Supports cumulative patch previews, direct apply, strict ranges and explicit conflict handling.
- Displays a compact interactive TX/RX patch matrix with horizontal, vertical and diagonal gestures.
- Opens a device details window to edit formats, IP data, channel names and RX patches.
- Applies global actions only to the selected or locked target scope.
- Resets all RX patches, TX references, or both for a device.
- Saves through a validated temporary file and protects an existing destination with a backup.
- Blocks unexpected changes to protected Dante XML areas.
- Preserves default XML namespaces and recognized unknown values.
- Provides French and English interfaces, quick starts and full PDF manuals.
- Exports TXT/PDF reports and read-only patchbooks.
- Uses `Atomic Bomb` to generate a deliberately scrambled troubleshooting preset for training, after three explicit confirmations.
- Displays file-health warnings, compatibility information and a simple TX-to-RX topology.
- Provides search, recent files, undo, recovery, dark theme and light theme.

## Important limitations

- The application does not control a live Dante network.
- It does not use an Audinate SDK or API.
- It only works on offline XML files.
- It does not bypass Audinate protections or reimplement a proprietary protocol.
- Compatibility depends on the actual structure of the supplied preset.
- Some subscriptions may not be detected if their XML structure is not currently recognized.
- `subscribed_device="."` is treated as a local source on the RX device.
- A missing TX device is reported as a warning because a preset may be partial.
- Dante IDs are preserved; the UI label is `Dante Id`, while the XML attribute remains `danteId`.
- Only a successful import into Dante Controller can provide final compatibility confirmation.

## Download and install

Download the [V3.2 GitHub Release](https://github.com/Mamat79/DanteConfigEditorV3/releases/tag/v3.2).

### Windows x64

For Windows x64, use `DanteConfigEditorV3_2_Installer.exe`, a self-contained installer that replaces older V3 installations.

The self-contained installer includes the required .NET 8 runtime, French and English documentation, Start menu and desktop shortcuts, destination selection, and clean uninstall support. It installs by default in `C:\Program Files\Dante Config Editor V3.2\` and removes detected V3.1, V3.09, V3.08, V3.07, and V3.2 Beta installations.

### macOS

- `DanteConfigEditorV3_macOS_AppleSilicon.dmg` supports Apple Silicon Macs.
- `DanteConfigEditorV3_macOS_Intel.dmg` supports Intel 64-bit Macs.

Open the DMG and drag `Dante Config Editor` into `Applications`. The .NET runtime and both language manuals are included.

The macOS builds are ad hoc signed but are not notarized with an Apple Developer account. On first launch, you may need to right-click the application, choose `Open`, and confirm. Verify the published SHA-256 checksum before installation.

## Distributed version

- `main` contains the official V3.2 source for Windows and macOS.
- Immutable tag `v3.2` exactly matches the V3.2 Release assets and is marked `Latest`.
- The V3.1 Release is removed at the maintainer's request; its source history remains in Git.
- The historical [`v3.09`](https://github.com/Mamat79/DanteConfigEditorV3/releases/tag/v3.09) and [`v3.08`](https://github.com/Mamat79/DanteConfigEditorV3/releases/tag/v3.08) Releases retain their own installers and documentation.
- Every version uses a separate immutable tag under the [release policy](RELEASE_POLICY.md).
- Each Release contains the files built for its own tagged source.
- Functional history remains available through the commits and `CHANGELOG_V3.md`.

## Quick start

1. Launch the application.
2. Select `Open XML` and choose a copy of a Dante configuration file.
3. Review detected devices and warnings.
4. Make the required changes.
5. In `Easy patch`, choose RX and TX devices and preview a selection or range.
6. Repeat as needed; previews accumulate without changing the XML.
7. Select `Apply the whole batch`, or use direct apply for the current operation.
8. Save under a new name.
9. Import and validate the result in the appropriate official Dante tool before production use.

## Atomic Bomb: troubleshooting exercise

`Atomic Bomb`, placed in its own tab after `Safety and log`, prepares a deliberately disordered network preset for training. It only changes the XML copy loaded in memory and requires three successive confirmations.

Thanks to **Charles Bouticourt** for the idea behind this training feature.

Devices receive unique names from a mythological, audio-themed, and deliberately playful catalogue, such as `ATHENA`, `RAVENNA`, `PYRAMIX`, `INFERNO`, or `PATCHOS`. They therefore do not share an obvious uniform prefix.

- Device and TX/RX channel names are replaced with exercise names.
- Redundant/daisy-chain modes, Preferred Master states, latencies, sample rates, encodings, and primary IP modes are deliberately mixed.
- Subscriptions are redistributed and about one quarter of the RX channels are left free.
- The displayed seed identifies the generated scenario and makes automated reproductions possible.
- Technical `device_id`, `danteId`, and `mediaType` identifiers, along with DNS, gateways, and secondary interfaces, remain protected.
- The whole operation creates a single undo step.
- The source file is never overwritten: use `Save as` to create the trainee preset.

The result is intentionally inconsistent at the functional level. Import it into the appropriate official Dante tool before using it as a training exercise.

## What's new in V3.2

- New `Import / Export` main tab organized into `Labels`, `Reports and patchbook`, and `Synoptic`.
- Colored visual synoptic generated from the open project.
- Physical locations, device visibility, and custom device ordering.
- Consecutive subscriptions compressed into synthetic cables with a separate legend.
- Orthogonal routes, shared trunks, and a two-column legend for dense synoptics.
- Standalone SVG export; layout data stays in a separate local sidecar and never modifies Dante XML.
- Native A&H dLive/Avantis CSV and Yamaha CL/QL ZIP/CSV label exchange alongside DMT XLSX and generic formats.
- Official V3.2 installer that replaces older V3 installations.

## What's new in V3.1

- TX/RX label exchange for one or several devices, with range selection and preview before application.
- Documented JSON and CSV formats for generic exchange and collaboration with external tools.
- Read and copy-based export of [dLive MIDI Tools](https://github.com/togrupe/dlive-midi-tools) XLSX workbooks, with optional explicit DMT ASCII/eight-character adaptation.
- The same label workflow on Windows and macOS through the shared XML engine.
- `Atomic Bomb` moved into a dedicated tab so it no longer dominates the main navigation.
- V3.1 installer cleanly replacing installed V3.07, V3.08 and V3.09 versions.
- New immutable `v3.1` tag; the `v3.08` and `v3.09` Releases remain untouched.

## What's new in V3.09

- Shared Windows/macOS `Atomic Bomb` troubleshooting generator with three confirmations, Save As protection, and XML non-regression tests.
- Deliberate mixing of names, channels, subscriptions, network modes, Preferred Master states, latencies, sample rates, encodings, and primary IP settings.
- Dante technical identifiers, namespaces, DNS, gateways, and secondary interfaces remain protected.
- The V3.09 installer cleanly replaces legacy V3.07/V3.08 installations.

## Easy patch introduced in V3.08

- RX devices and channels are displayed on the left; TX sources are on the right.
- Previous/next controls make it quick to move between devices.
- `Ctrl` and `Shift` provide independent multi-selection in TX and RX lists.
- Equal TX/RX selections are paired one-to-one.
- One TX may feed multiple RX channels.
- Multiple TX sources cannot be assigned to one RX channel.
- Range patching uses a first TX, first RX and exact channel count.
- Oversized or ambiguous ranges are blocked atomically.
- Every preview joins one cumulative pending batch.
- The XML remains unchanged until the whole batch is applied.
- Existing subscriptions require an explicit replace, skip or cancel decision.
- The matrix uses compact cells and full TX names are available in tooltips.
- Horizontal gestures prepare consecutive TX/RX pairs.
- Vertical gestures feed one TX into several RX channels.
- Exact diagonals prepare one-to-one series.
- The final operation creates one undo step.

## Build from source

Requirements:

- Windows for the WPF application and installer
- .NET 8 SDK
- Inno Setup 6 for the Windows installer

Build the application:

```powershell
.\build.ps1
```

Build the self-contained Windows installer:

```powershell
.\installer\build_installer.ps1
```

Run all automated test suites:

```powershell
.\tests\run-tests.ps1
```

The macOS packaging process is documented in `MACOS_BUILD.md`.

## Validation and maintenance

- `TESTING.md`: automated results and validation history.
- `COMPATIBILITY_MATRIX.md`: evidence level for recognized XML structures.
- `MANUAL_DANTE_CONTROLLER_TESTS.md`: checklist for real imports.
- `ACCESSIBILITY.md`: completed and remaining accessibility checks.
- `KNOWN_LIMITATIONS.md`: technical and distribution limitations.
- `ARCHITECTURE_REFACTORING.md`: progressive architecture work.

## File safety

- Always work on a copy.
- Do not overwrite a production preset without testing.
- Keep automatically generated backups.
- Validate the final file in official Dante tools before deployment.
- The application checks generated XML consistency, but a real Dante Controller import remains the final validation.

## Public repository

https://github.com/Mamat79/DanteConfigEditorV3

## Credit

**By Mamat**<br>
<sub>et ses agents</sub>

Special thanks to **Charles Bouticourt** for the idea behind the `Atomic Bomb` feature.
