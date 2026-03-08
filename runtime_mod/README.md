# Runtime Mod Port

This branch contains an in-progress port of the STS2 font patch to the game's `mods/` loader format.

## Layout

- `runtime_mod/Sts2FontSizeMod/`
  - runtime Harmony mod `.dll`
- `runtime_mod/pck/`
  - minimal Godot project used to export the `.pck`
- `runtime_mod/build/`
  - built mod artifacts

## Current behavior

The runtime mod currently ports these patch areas:

- `MegaLabel.SetFontSize(...)`
- `MegaRichTextLabel.SetFontSize(...)`
- `MegaLabel._Ready()` base-size override + refresh
- `MegaRichTextLabel._Ready()` base-size override + refresh
- `NGame._Ready()` node-tree scan for plain Godot labels
- `Godot.RichTextLabel` BBCode and direct size-push scaling
- `NDebugInfoLabelManager` footer text + footer bump
- `NPatchNotesScreen` release notes bump
- `NPreviewCardHolder` preview-card description extra bump

## Config

The runtime mod reads `font_size_config.json` next to the mod `.dll`.

Current defaults:

- `base_scale`: `1.20`
- `debug_footer_extra_scale`: `0.50`
- `patch_notes_extra_scale`: `0.25`
- `preview_card_description_extra_scale`: `0.20`

## Build

If you have a Godot 4.5.1 exporter available, set `STS2_GODOT_EXPORTER` in `.env`, then run:

```bash
./scripts/build-runtime-mod.sh
```

This produces:

- `runtime_mod/build/ZSts2FontSizeMod.dll`
- `runtime_mod/build/font_size_config.json`
- `runtime_mod/build/ZSts2FontSizeMod.pck`

`font_size_config.json` is rendered from the current `.env` values during the build.

## Deploy

```bash
./scripts/deploy-runtime-mod.sh
```

The target directory is `${STS2_MODS_DIR}` on the Steam Deck.

The deploy helper refuses to overwrite files if STS2 is running.

## Modded save namespace

When STS2 runs through the mod loader, profile-scoped saves move into a separate modded namespace.

Profile-scoped files:

- `modded/profile1/saves/progress.save`
- `modded/profile1/saves/prefs.save`
- `modded/profile1/saves/current_run.save`
- `modded/profile1/saves/history/...`
- `modded/profile1/replays/latest.mcr`

Account-scoped files stay at the normal root:

- `profile.save`
- `settings.save`

On Steam Deck, migrate both roots before testing a modded install:

- cloud:
  - `~/.local/share/Steam/userdata/58189749/2868840/remote/`
- local synced copy:
  - `~/.local/share/SlayTheSpire2/steam/76561198018455477/`

Validated migration flow:

```bash
remote_root="$HOME/.local/share/Steam/userdata/58189749/2868840/remote"
local_root="$HOME/.local/share/SlayTheSpire2/steam/76561198018455477"

mkdir -p "$remote_root/modded/profile1" "$local_root/modded/profile1"

rsync -a --delete "$remote_root/profile1/" "$remote_root/modded/profile1/"
rsync -a --delete "$remote_root/profile1/" "$local_root/modded/profile1/"

cp -a "$remote_root/profile.save" "$remote_root/profile.save.backup" "$remote_root/settings.save" "$remote_root/settings.save.backup" "$remote_root/modded/"
cp -a "$local_root/profile.save" "$local_root/profile.save.backup" "$local_root/settings.save" "$local_root/settings.save.backup" "$local_root/modded/"
```

If the modded build still shows a blank profile after that, inspect the modded `progress.save` size first.

- expected migrated size in this test case: `90063` bytes
- if it drops back to around `1038` bytes, the game has regenerated a fresh blank modded profile
