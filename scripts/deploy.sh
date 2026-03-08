#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

if output="$(ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "pgrep -af '${STS2_RUNNING_PATTERN}' | grep -v 'pgrep -af' || true")" && [[ -n "${output}" ]]; then
  printf '%s\n' "STS2 appears to be running on the target host. Refusing to overwrite live DLLs." >&2
  printf '%s\n' "${output}" >&2
  exit 2
fi

ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "cat > \"${STS2_GAME_DLL_DIR}/sts2.dll\"" < "${LOCAL_DLL_DIR}/sts2.dll"
ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "cat > \"${STS2_GAME_DLL_DIR}/GodotSharp.dll\"" < "${LOCAL_DLL_DIR}/GodotSharp.dll"
