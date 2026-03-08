# Slay the Spire 2 Font Size Patch for Steam Deck

Last updated: 2026-03-08

## What this patch does

This is a managed-DLL font patch for the Steam Deck version of Slay the Spire 2.

It increases text size in the game's three main managed text paths:

- `MegaLabel`
- `MegaRichTextLabel`
- plain Godot `Label` / `RichTextLabel`

It also scales inline rich-text size directives like:

- `font_size=...`
- `outline_size=...`

This is the current clean patch shape:

- patched `sts2.dll`
- patched `GodotSharp.dll`
- no helper DLL
- no `override.cfg`
- debug footer now shows `[version + Font Patch 1.25x] [date]`

## Configuration

Copy `.env.example` to `.env` and adjust it for your machine:

- `STS2_DECK_HOST`
- `STS2_GAME_DLL_DIR`
- `STS2_LOG_PATH`
- `STS2_LOCAL_DLL_DIR`
- `STS2_PATCH_SCALE`

The helper scripts in `./scripts` read `.env` automatically.

## Current scale factor

- `1.25x`

## Why this version is better than the earlier attempts

The early versions were a mix of:

- direct `SetFontSize(...)` scaling
- plain Godot label scaling
- a one-off character-select fix
- a temporary engine patch that depended on an external helper DLL

The current version keeps the useful generic parts and removes the brittle parts:

- `MegaLabel.SetFontSize(...)` and `MegaRichTextLabel.SetFontSize(...)` are scaled
- `MegaLabel._Ready()` now applies a generic scaled `font_size` theme override
- `MegaRichTextLabel._Ready()` now applies generic scaled rich-text base overrides:
  - `normal_font_size`
  - `bold_font_size`
  - `italics_font_size`
  - `bold_italics_font_size`
  - `mono_font_size`
- `NGame._Ready()` still scales plain Godot labels and rich-text labels globally
- `GodotSharp.dll` still scales BBCode and direct rich-text size pushes globally
- the version/build footer is patched through `NDebugInfoLabelManager.UpdateText(...)` so it uses the same scaled `MegaLabel` path

That means the patch now covers:

- most menus
- character select text
- hover tips
- unlock text
- speech / thought bubbles
- other `MegaRichTextLabel`-based UI that was previously slipping through

## Files to replace on Steam Deck

Copy these two files into:

`~/.steam/steam/steamapps/common/Slay the Spire 2/data_sts2_linuxbsd_x86_64/`

Files:

- `sts2.dll`
- `GodotSharp.dll`

## Back up first

From the Steam Deck:

```bash
cd ~/.steam/steam/steamapps/common/Slay\ the\ Spire\ 2/data_sts2_linuxbsd_x86_64/
cp sts2.dll sts2.dll.orig
cp GodotSharp.dll GodotSharp.dll.orig
```

## Install

Use the repo helper:

```bash
./scripts/deploy.sh
```

Then fully quit and relaunch the game.

## Deployment safety

Before copying patched DLLs to the Steam Deck, first check whether STS2 is currently running.

If the game is running, ask before overwriting the live install.

Suggested check:

```bash
./scripts/check-running.sh
```

## Revert

Either restore the backups:

Restore the backups you made on the Deck, or use Steam's file verification.

## Rebuild the patch locally

Rebuild:

```bash
./scripts/rebuild.sh
```

## Repository contents

Tracked in git:

- patcher source
- helper inspection tool source
- `.env.example`
- helper scripts
- project docs / notes

Ignored in git:

- copied game DLLs
- backups
- generated disassembly / scan output
- local release zips

## Known limitations

- The game content `.pck` is encrypted, so scene/resource extraction is not the current path.
- Some rare text may still be custom-drawn or otherwise outside the managed label pipeline.
- Game updates will overwrite these DLLs.

## Technical summary

- Engine: Godot 4.5.1 Mono
- App ID: `2868840`
- Main game assembly: `sts2.dll`
- Managed engine assembly: `GodotSharp.dll`
- Patcher tech: Mono.Cecil
