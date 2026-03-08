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
- `MegaLabel.AdjustFontSize(...)`
- `MegaRichTextLabel.AdjustFontSize(...)`
- `NGame._Ready()` node-tree scan for plain Godot labels
- `Godot.RichTextLabel` BBCode and direct size-push scaling
- `NDebugInfoLabelManager` footer text + footer bump
- `NPatchNotesScreen` release notes bump

## Config

The runtime mod reads `font_size_config.json` next to the mod `.dll`.

Current defaults:

- `base_scale`: `1.20`
- `debug_footer_extra_scale`: `0.50`
- `patch_notes_extra_scale`: `0.25`

## Build

If you have a Godot 4.5.1 exporter available, set `STS2_GODOT_EXPORTER` in `.env`, then run:

```bash
./scripts/build-runtime-mod.sh
```

This produces:

- `runtime_mod/build/ZSts2FontSizeMod.dll`
- `runtime_mod/build/font_size_config.json`
- `runtime_mod/build/ZSts2FontSizeMod.pck`

## Deploy

```bash
./scripts/deploy-runtime-mod.sh
```

The target directory is `${STS2_MODS_DIR}` on the Steam Deck.

The deploy helper refuses to overwrite files if STS2 is running.
