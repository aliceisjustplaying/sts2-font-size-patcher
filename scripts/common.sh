#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -f "${ROOT_DIR}/.env" ]]; then
  set -a
  source "${ROOT_DIR}/.env"
  set +a
fi

if [[ -f "${ROOT_DIR}/.env.local" ]]; then
  set -a
  source "${ROOT_DIR}/.env.local"
  set +a
fi

: "${STS2_DECK_HOST:=deck@steamdeck.local}"
: "${STS2_LOG_PATH:=/home/deck/.local/share/SlayTheSpire2/logs/godot.log}"
: "${STS2_MODS_DIR:=/home/deck/.steam/steam/steamapps/common/Slay the Spire 2/mods}"
: "${STS2_GODOT_EXPORTER:=}"
: "${STS2_PATCH_SCALE:=1.20}"
: "${STS2_DEBUG_FOOTER_EXTRA_SCALE:=0.50}"
: "${STS2_PATCH_NOTES_EXTRA_SCALE:=0.25}"
: "${STS2_PREVIEW_CARD_DESCRIPTION_EXTRA_SCALE:=0.20}"
: "${STS2_RUNNING_PATTERN:=Slay the Spire 2|sts2|godot}"
