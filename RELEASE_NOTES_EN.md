# Dante Config Editor V3.5 - development

[Notes de version françaises](RELEASE_NOTES.md)

## Status

V3.5 is a Windows and macOS development version that can be installed alongside stable V3.4.2. Dante Config Editor remains an unofficial third-party tool, not affiliated with Audinate, and may still contain bugs. Work on copies of Dante XML files and always validate generated files in the appropriate official Dante tool.

## Patch and performance

- The visual matrix updates only affected cells instead of rebuilding every control after each click.
- Changes remain pending until they are explicitly applied to the project.
- Tx/Rx headers stay visible and scroll bars remain synchronized.
- One-to-one range patching, selection swap, 50 to 200% zoom, and fit-to-window.
- Tab and Shift+Tab validate direct renaming and move to the next or previous channel.

## Label imports

- Separate adapters for JSON, CSV, DMT XLSX/ODS, and console packages.
- Strict validation for versions, required columns, duplicate channels, and unknown JSON fields.
- Visible pre-apply report: format, source version, lists, devices, channels, ignored rows, empty labels, duplicates, and warnings.
- JSON/CSV compatibility tested against DMT 2.14.0-RC1 exporters at commit `3c34052`.

## XML safety

- Device deletion and cleanup of associated subscriptions are tested through save and reload.
- Generic device creation has been abandoned.
- Role duplication is not offered without Dante Controller import evidence for generated technical identifiers.
- Atomic saving, recovery, and default blocking of unknown XML paths remain active.

## Documentation

- Updated full and quick guides in French and English.
- Two 55-second presentation videos with no voice-over or audio track and text burned into the image.
- Screens use only a synthetic anonymized preset.

## Distribution

- Self-contained Windows x64 installer: `DanteConfigEditorV3_5_Installer.exe`, including .NET 8.
- V3.5 uses its own AppId and Program Files folder; it does not replace V3.4.2.
- Self-contained V3.5 DMGs for Apple Silicon and Intel, including .NET 8 and the FR/EN guides.
- The V3.5 Mac app uses its own bundle name and identifier, so it can coexist with V3.4.2.

## Limitations

- Only a successful import into Dante Controller or the appropriate official Dante tool confirms final compatibility.
- Direct renaming of an external Tx source missing from the XML remains blocked.
- The Windows installer is not Authenticode signed.
- The Mac DMGs are ad hoc signed without an Apple Developer ID certificate or notarization.
