#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "cat \"${STS2_LOG_PATH}\"" > "${ROOT_DIR}/deck_godot.log"
