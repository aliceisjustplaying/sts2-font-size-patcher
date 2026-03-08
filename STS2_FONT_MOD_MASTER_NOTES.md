# STS2 Runtime Mod Master Notes

Last updated: 2026-03-08

## Goal

Make Slay the Spire 2 text significantly larger on Steam Deck with a generic runtime mod that can live in the game's `mods/` folder.

## Repo Scope

This repo now keeps only the runtime-mod version:

- Harmony mod source
- mod `.pck` project files
- runtime-mod helper scripts
- readme / notes

The older direct-DLL patcher source has been archived outside the repo.

## Current Status

- The runtime mod loads through the `mods/` folder.
- The modded save namespace is now understood and documented.
- Current deployed base scale factor is `1.20x`.
- Debug footer/version labels get a footer-only scale of `1.70x`.
- Patch notes body text gets a release-notes-only scale of `1.45x`.
- The original stubborn serif text was identified as the character description text:
  - `"The last soldier of the Ironclads."`
- Additional probe strings that helped identify remaining paths were:
  - `"..arise.. my.. warrior..."`
  - `"Until next turn, prevents damage."`
  - `"Unlocked by"`

## Save Migration Findings

- The plain game save is still the authoritative non-modded profile.
- The `mods/` loader build uses a separate modded profile namespace.
- This namespace split is only applied to profile-scoped data, not account-scoped data.

Confirmed from managed save-path code:

- account-scoped root stays normal:
  - `user://steam/<steamid>/profile.save`
  - `user://steam/<steamid>/settings.save`
- profile-scoped root becomes modded when running mods:
  - `user://steam/<steamid>/modded/profile1/...`

In `UserDataPathProvider.GetProfileDir(int profileId)`:

- non-modded => `profile1`
- modded => `modded/profile1`

In `ProgressSaveManager.LoadProgress()`:

- the game only falls back to `ProgressState.CreateDefault()` when loading the modded `progress.save` fails or returns null
- it does not reject a modded save merely because it is in the modded namespace

Practical implication:

- if the modded `progress.save` is blank on launch, the real problem is usually sync/state mismatch, not the folder name itself

## Validated Save Migration Procedure

The migration that finally worked was:

1. Back up everything first.
2. Copy normal `profile1/` into both:
   - Steam cloud modded root
   - local synced modded root
3. Copy top-level `profile.save` and `settings.save` into the `modded/` subdirectory in both roots.
4. Verify hashes match before launching modded STS2.

Steam cloud root:

- `~/.local/share/Steam/userdata/58189749/2868840/remote/`

Local synced root:

- `~/.local/share/SlayTheSpire2/steam/76561198018455477/`

Safe migration commands:

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

Hash verification used after migration:

```bash
python3 - <<'PY'
from pathlib import Path
import hashlib

checks = [
    ("remote progress", Path.home()/".local/share/Steam/userdata/58189749/2868840/remote/profile1/saves/progress.save",
     Path.home()/".local/share/Steam/userdata/58189749/2868840/remote/modded/profile1/saves/progress.save"),
    ("local modded progress", Path.home()/".local/share/Steam/userdata/58189749/2868840/remote/profile1/saves/progress.save",
     Path.home()/".local/share/SlayTheSpire2/steam/76561198018455477/modded/profile1/saves/progress.save"),
]

for label, a, b in checks:
    ah = hashlib.sha256(a.read_bytes()).hexdigest()[:16]
    bh = hashlib.sha256(b.read_bytes()).hexdigest()[:16]
    print(label, ah == bh, a.stat().st_size, b.stat().st_size, ah, bh)
PY
```

Observed good state after fix:

- normal `progress.save`: `90063` bytes
- modded remote `progress.save`: `90063` bytes
- modded local `progress.save`: `90063` bytes

Why the earlier migration failed:

- only part of the modded namespace was copied at first
- the local modded profile tree was left inconsistent
- on the next modded launch, STS2 wrote a fresh blank modded `progress.save`

## Deployment Safety

- Before copying mod files to the Steam Deck, first check whether STS2 is currently running.
- If the game is running, stop and ask the user before overwriting the live mod files.
- If the game is not running, it is fine to copy directly.

Suggested check:

```bash
./scripts/check-running.sh
```

## Key Paths

### Repo

- Working folder: `./`
- Runtime mod source: `./runtime_mod/Sts2FontSizeMod/`
- Mod pack files: `./runtime_mod/pck/`
- Mod tools: `./runtime_mod/tools/`
- Environment example: `./.env.example`
- Helper scripts: `./scripts/`

### Steam Deck

- Put host/path values in `.env`, not in tracked files.
- Recommended variables:
  - `STS2_DECK_HOST`
  - `STS2_MODS_DIR`
  - `STS2_LOG_PATH`
  - `STS2_GODOT_EXPORTER`
  - `STS2_PATCH_SCALE`
  - `STS2_DEBUG_FOOTER_EXTRA_SCALE`
  - `STS2_PATCH_NOTES_EXTRA_SCALE`

## Game Architecture Findings

- Engine: Godot 4.5.1 Mono / C#
- App ID: `2868840`
- Main managed gameplay assembly: `sts2.dll`
- Godot managed runtime assembly: `GodotSharp.dll`
- The `.pck` is encrypted, so direct scene/theme/resource editing through asset extraction is not a practical path right now.
