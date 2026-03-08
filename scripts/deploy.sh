#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

if output="$(ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "pgrep -af '${STS2_RUNNING_PATTERN}' || true")" && [[ -n "${output}" ]]; then
  printf '%s\n' "STS2 appears to be running on the target host. Refusing to overwrite live DLLs." >&2
  printf '%s\n' "${output}" >&2
  exit 2
fi

scp -o StrictHostKeyChecking=no \
  "${LOCAL_DLL_DIR}/sts2.dll" \
  "${LOCAL_DLL_DIR}/GodotSharp.dll" \
  "${STS2_DECK_HOST}:'${STS2_GAME_DLL_DIR}/'"
