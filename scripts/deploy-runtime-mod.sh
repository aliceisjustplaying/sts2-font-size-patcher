#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

MOD_BUILD_DIR="${ROOT_DIR}/runtime_mod/build"

if output="$(ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "pgrep -af '${STS2_RUNNING_PATTERN}' | grep -v 'pgrep -af' || true")" && [[ -n "${output}" ]]; then
  printf '%s\n' "STS2 appears to be running on the target host. Refusing to overwrite live mod files." >&2
  printf '%s\n' "${output}" >&2
  exit 2
fi

for required in ZSts2FontSizeMod.dll font_size_config.json ZSts2FontSizeMod.pck; do
  if [[ ! -f "${MOD_BUILD_DIR}/${required}" ]]; then
    printf '%s\n' "Missing runtime mod artifact: ${MOD_BUILD_DIR}/${required}" >&2
    exit 1
  fi
done

ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "mkdir -p \"${STS2_MODS_DIR}\""
ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "cat > \"${STS2_MODS_DIR}/ZSts2FontSizeMod.dll\"" < "${MOD_BUILD_DIR}/ZSts2FontSizeMod.dll"
ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "cat > \"${STS2_MODS_DIR}/font_size_config.json\"" < "${MOD_BUILD_DIR}/font_size_config.json"
ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "cat > \"${STS2_MODS_DIR}/ZSts2FontSizeMod.pck\"" < "${MOD_BUILD_DIR}/ZSts2FontSizeMod.pck"
