# Dante Config Editor V3.3

[Notes de version françaises](RELEASE_NOTES.md)

## Status

V3.3 is the current official Windows and macOS release. Dante Config Editor remains an unofficial third-party tool, not affiliated with Audinate, and may still contain bugs. Work on copies of Dante XML files and always validate generated files in the appropriate official Dante tool before real use.

## Direct DMT exchange

- Direct import of `XLSX` and `ODS` files created by [dLive MIDI Tools (DMT)](https://github.com/togrupe/dlive-midi-tools).
- Direct export to four embedded templates: DMT XLSX dLive, DMT XLSX Avantis, DMT ODS dLive, and DMT ODS Avantis.
- Sheets, styles, and settings outside `Channels` are preserved; only the required `Enabled` and `Name` cells are changed in the exported copy.
- dLive and Avantis templates are bundled with the application. No external template file is requested.
- Generic JSON/CSV, Allen & Heath dLive/Avantis CSV, and Yamaha CL/QL ZIP formats remain available.

## Configurable Atomic Bomb

The tab now uses a more explicit playful title: **Horrible experience generator (but educational)**.

Before the three confirmations, users can independently spare:

- device names;
- Tx and Rx labels;
- subscriptions;
- network modes and Preferred Master;
- latency, sample rate, and bits per sample;
- primary IP settings.

All options remain selected by default. If subscriptions are excluded while names change, recognized references are updated to preserve existing routing. The original file, technical identifiers, DNS, gateways, and secondary interfaces remain protected.

## macOS fixes

- Import/export and preview buttons remain fully visible at Full HD resolution.
- Missing header, search, and filter translations have been corrected.
- A second identical import now clearly explains that there is nothing to apply instead of leaving a disabled button unexplained.
- The new Atomic Bomb panel and all options fit a 1366 × 768 window in headless Avalonia tests.

Special thanks to **Tobias Grupe / togrupe** for screenshots and feedback from a real Intel Mac. This is especially valuable because primary local development and testing are performed on Windows.

## Presentation videos

- [English presentation of DCE v3.3](https://github.com/Mamat79/DanteConfigEditorV3/releases/download/v3.3/dce-v33-presentation-en.mp4)
- [Présentation française de DCE v3.3](https://github.com/Mamat79/DanteConfigEditorV3/releases/download/v3.3/dce-v33-presentation-fr.mp4)

Both videos last about one minute, use a synthetic anonymized preset, and include burned-in subtitles. Separate `.srt` files and SHA-256 checksums are also attached to the Release.

## Distribution and validation

- Self-contained Windows x64 installer: `DanteConfigEditorV3_3_Installer.exe`, including .NET 8.
- Self-contained Apple Silicon and Intel macOS DMGs.
- 150 engine and Windows contract tests pass in Release configuration.
- 11 headless macOS interface tests also pass.
- The V3.2 Release remains available as historical download. V3.08 and V3.09 Release pages are removed at the maintainer's request; their Git tags and commits remain available.

## Limitations

- Only a successful import into Dante Controller or the appropriate official Dante tool confirms final compatibility.
- DMT support targets templates observed in version 2.13.0; future format changes may require an update.
- The Windows installer is not Authenticode signed, and the DMGs are not notarized.

**By Mamat et ses agents**

Public repository: https://github.com/Mamat79/DanteConfigEditorV3
