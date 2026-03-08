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
: "${STS2_GAME_DLL_DIR:=/home/deck/.steam/steam/steamapps/common/Slay the Spire 2/data_sts2_linuxbsd_x86_64}"
: "${STS2_LOG_PATH:=/home/deck/.local/share/SlayTheSpire2/logs/godot.log}"
: "${STS2_LOCAL_DLL_DIR:=./game_dlls}"
: "${STS2_PATCH_SCALE:=1.20}"
: "${STS2_DEBUG_FOOTER_EXTRA_SCALE:=0.50}"
: "${STS2_PATCH_NOTES_EXTRA_SCALE:=0.25}"
: "${STS2_RUNNING_PATTERN:=Slay the Spire 2|sts2|godot}"

LOCAL_DLL_DIR="${STS2_LOCAL_DLL_DIR/#.\//${ROOT_DIR}/}"
