# Guide

More complete user-facing notes for the STS2 font-size runtime mod.

## Repo Layout

- `runtime_mod/Sts2FontSizeMod/`
  - Harmony runtime mod source
- `runtime_mod/pck/`
  - minimal Godot pack content for the mod loader
- `runtime_mod/tools/`
  - helper scripts for building and inspecting the `.pck`
- `scripts/build-runtime-mod.sh`
  - builds the mod DLL and `.pck`
- `scripts/deploy-runtime-mod.sh`
  - installs the mod into the Deck `mods/` folder
- `scripts/package-runtime-mod.sh`
  - zips the runtime-mod artifacts
- `scripts/check-running.sh`
  - checks whether STS2 is running on the target Deck
- `scripts/fetch-log.sh`
  - copies back the Deck Godot log for debugging

## Configuration

The workflow reads `.env` and renders those values into `runtime_mod/build/font_size_config.json` during build.

Useful env vars:

- `STS2_DECK_HOST`
- `STS2_MODS_DIR`
- `STS2_LOG_PATH`
- `STS2_GODOT_EXPORTER`
- `STS2_PATCH_SCALE`
- `STS2_DEBUG_FOOTER_EXTRA_SCALE`
- `STS2_PATCH_NOTES_EXTRA_SCALE`
- `STS2_PREVIEW_CARD_DESCRIPTION_EXTRA_SCALE`
- `STS2_RUNNING_PATTERN`

Quote values that contain spaces.

## Build Output

After running:

```bash
./scripts/build-runtime-mod.sh
```

you should get:

- `runtime_mod/build/ZSts2FontSizeMod.dll`
- `runtime_mod/build/ZSts2FontSizeMod.pck`
- `runtime_mod/build/font_size_config.json`

## Modded Save Migration

When STS2 runs through the mod loader, profile-scoped saves move into a separate modded namespace.

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

Validated migration flow:

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

- STS2 syncs cloud files into the local save directory on startup
- copying only the cloud tree is not enough
- the local modded profile tree must match too, or the next modded launch can regenerate a fresh blank `progress.save`

## Troubleshooting

Check whether STS2 is running before deploy:

```bash
./scripts/check-running.sh
```

Fetch the Deck log:

```bash
./scripts/fetch-log.sh
```

Current known scale defaults:

- base scale: `1.20x`
- debug footer extra: `0.50`
- patch notes extra: `0.25`
- preview card description extra: `0.20`

Notable covered holdouts:

- character-select bio text
- character-select starter-relic description text
- patch notes body text
- secondary preview-card description text

## Notes

- Engine: Godot 4.5.1 Mono
- App ID: `2868840`
- The game content `.pck` is encrypted, so asset extraction is not the current path.
- The older direct-DLL patcher workflow has been archived outside this repo.
