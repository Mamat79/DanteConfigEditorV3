# Dante Config Editor V3.4

[Notes de version françaises](RELEASE_NOTES.md)

## Status

V3.4 is the current official Windows and macOS release. Dante Config Editor remains an unofficial third-party tool, not affiliated with Audinate, and may still contain bugs. Work on copies of Dante XML files and validate generated files in the appropriate official Dante tool.

## Patch usability

- Direct device, Rx channel, and Tx channel renaming in the Patch page.
- Direct Rx/Tx channel renaming in Easy patch.
- Numeric series extension, for example `Mic 1`, `Mic 2`, then drag to the last target channel.
- The patch matrix opens first; `Selection and range` remains available in the second tab.
- Rx filters are placed above Tx filters.
- A global action selects the only Preferred Master in the project.

## Synoptic

- `Reset` clears manual positions and rebuilds a clean order inside each location.
- A one-way flow keeps one Tx-to-Rx arrow.
- Opposite flows between the same two devices are merged into one line with an arrow at each end.
- Preview, SVG, and PDF use the same directional representation.

## Display

- Configuration settings panels are visible on first launch; their state is then remembered.
- Main buttons and controls use minimum heights suitable for Windows 125% scaling.
- The central Atomic Bomb work area is larger.

## Distribution

- Self-contained Windows x64 installer: `DanteConfigEditorV3_4_Installer.exe`, including .NET 8.
- Apple Silicon and Intel macOS packages are produced by the release workflow.
- V3.3 remains available as a historical release and is not overwritten by V3.4.

## Limitations

- Only a successful import into Dante Controller or the appropriate official Dante tool confirms final compatibility.
- Direct renaming of an external Tx source missing from the XML remains blocked because the device cannot be safely identified.
- The Windows installer is not Authenticode signed, and macOS packages are not notarized.
