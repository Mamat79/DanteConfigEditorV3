# Dante Config Editor V3.08

[Français](https://github.com/Mamat79/DanteConfigEditorV3/blob/main/RELEASE_NOTES.md) | **English**

## Status

Official V3.08 build for Windows x64, macOS Apple Silicon and macOS Intel. This is an unofficial third-party tool, not affiliated with Audinate, and it may still contain bugs.

V3.08 replaces V3.07 as the current version on `main`. Older versions are no longer offered for download, while their functional history remains available in the commits and changelog. Always work on a copy of Dante XML files and validate the result with official Dante tools before production use.

## Easy patch

- The classic `Patch` view remains available.
- `Easy patch` is a main Windows tab with RX devices/channels on the left and TX devices/channels on the right.
- Previous/next navigation is available on both sides.
- TX and RX lists support independent multiple selection with `Ctrl` or `Shift`.
- Equal selection sizes are paired one-to-one.
- One TX may feed several selected RX channels.
- Several TX sources cannot be assigned to one RX channel.
- Range patching uses an exact first TX, first RX and channel count.
- Oversized ranges are fully blocked instead of being partially applied.
- Each preview automatically joins one cumulative pending batch without changing the XML.
- `Apply the whole batch` commits every pending operation as one XML change and one undo step.
- Direct apply remains available for the current selection or range.
- Existing subscriptions require an explicit replace, skip or cancel choice.
- The compact matrix displays full TX names in tooltips.
- Horizontal, vertical and exact diagonal gestures prepare safe patch series.

## Device details

- A device selector allows navigation without closing the details window.
- Pending settings are protected by apply/discard/cancel confirmation.
- The `RX patch` tab is locked to the displayed receiver device.
- Compatible project TX devices remain available as sources.
- Patch changes are applied before device/channel renaming so references follow new names.

## Installation

- Windows defaults to `C:\Program Files\Dante Config Editor V3.08\`.
- The V3.08 installer upgrades an existing V3.08 Beta or V3.08 installation.
- The Windows installer is self-contained and includes .NET 8 and both language manuals.
- Apple Silicon and Intel DMGs are self-contained and include both language manuals.
- SHA-256 checksum files are published for every installer and DMG.

## Platforms

- Windows x64: self-contained installer with the WPF interface and Easy patch.
- macOS Apple Silicon: self-contained DMG.
- macOS Intel: self-contained DMG.
- The XML engine and safety guards are shared across platforms.
- macOS retains its Avalonia visual patch workshop; the Windows Easy patch layout is not reproduced identically.
- macOS packages are ad hoc signed and not notarized.

## Automated validation

- 95 Core/Windows contract tests and 8 headless macOS UI tests pass in Release mode.
- WPF and Avalonia Release builds complete without warnings.
- Tests cover selection, ranges, conflicts, replacement, rollback, persistence, matrix behavior, XML guards, atomic saves, namespaces, secondary interfaces and synthetic large presets.

## Important limitations

- The application does not control a live Dante network.
- It does not use an Audinate SDK or API.
- It only edits offline XML files.
- Compatibility depends on the actual preset structure.
- Duplicate TX names on one device remain ambiguous.
- Only a successful Dante Controller import can provide final compatibility proof.
- The Windows installer is not Authenticode signed.
- The macOS DMGs are not Apple-notarized.

## Origin and credit

The project started as a small personal XML editor written manually by Mamat. Modern development agents then enabled a substantial evolution of the interface, XML safeguards, automated tests, documentation and packaging, under Mamat's functional direction.

**By Mamat et ses agents**

## Public repository

https://github.com/Mamat79/DanteConfigEditorV3
