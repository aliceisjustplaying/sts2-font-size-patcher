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
- debug footer now shows `[version + Font Patch 1.20x] [date]`
- patch notes body text gets an extra release-notes-only bump on top of the base scale

## Configuration

Copy `.env.example` to `.env` and adjust it for your machine:

- `STS2_DECK_HOST`
- `STS2_GAME_DLL_DIR`
- `STS2_LOG_PATH`
- `STS2_LOCAL_DLL_DIR`
- `STS2_PATCH_SCALE`
- `STS2_DEBUG_FOOTER_EXTRA_SCALE`
- `STS2_PATCH_NOTES_EXTRA_SCALE`

The helper scripts in `./scripts` read `.env` automatically.
Quote any value that contains spaces, such as the Steam install path or process-match pattern.

## Current scale factor

- `1.20x`
- debug footer/version labels get an extra footer-only bump to `1.70x`
- patch notes body text gets an extra release-notes-only bump to `1.45x`

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
- the patch notes screen body text is patched through `NPatchNotesScreen._patchText` so release notes can get a small extra bump without affecting other `MegaRichTextLabel` screens

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

## Modded save migration

If you use the `mods/` loader build, STS2 keeps profile-scoped saves in a separate modded namespace.

Important distinction:

- account-scoped files stay at the normal root
  - `profile.save`
  - `settings.save`
- profile-scoped files move under `modded/profileN/`
  - `progress.save`
  - `prefs.save`
  - `current_run.save`
  - run history
  - replays

On Steam Deck, the two relevant roots are:

- Steam cloud store:
  - `~/.local/share/Steam/userdata/58189749/2868840/remote/`
- local synced copy:
  - `~/.local/share/SlayTheSpire2/steam/76561198018455477/`

To migrate a normal `profile1` save into the modded namespace safely:

```bash
remote_root="$HOME/.local/share/Steam/userdata/58189749/2868840/remote"
local_root="$HOME/.local/share/SlayTheSpire2/steam/76561198018455477"

stamp=$(date +%Y%m%d-%H%M%S)
backup_dir="$HOME/tmp/sts2-save-backups/modded-migrate-$stamp"

mkdir -p "$backup_dir"
cp -a "$remote_root/modded" "$backup_dir/remote-modded-before"
cp -a "$local_root/modded" "$backup_dir/local-modded-before"

mkdir -p "$remote_root/modded/profile1" "$local_root/modded/profile1"

rsync -a --delete "$remote_root/profile1/" "$remote_root/modded/profile1/"
rsync -a --delete "$remote_root/profile1/" "$local_root/modded/profile1/"

cp -a "$remote_root/profile.save" "$remote_root/profile.save.backup" "$remote_root/settings.save" "$remote_root/settings.save.backup" "$remote_root/modded/"
cp -a "$local_root/profile.save" "$local_root/profile.save.backup" "$local_root/settings.save" "$local_root/settings.save.backup" "$local_root/modded/"
```

Why both roots matter:

- on startup, STS2 syncs cloud files into the local save directory
- if the local modded profile tree is stale or blank, the next modded launch can regenerate a fresh empty `progress.save`
- copying only the cloud tree is not enough; the local modded profile tree must match too

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
