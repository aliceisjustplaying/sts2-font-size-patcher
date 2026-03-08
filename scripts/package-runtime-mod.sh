#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

MOD_BUILD_DIR="${ROOT_DIR}/runtime_mod/build"
RELEASE_DIR="${ROOT_DIR}/release/runtime-mod"
ZIP_PATH="${RELEASE_DIR}/sts2-font-size-mod-runtime.zip"

mkdir -p "${RELEASE_DIR}"

for required in ZSts2FontSizeMod.dll font_size_config.json ZSts2FontSizeMod.pck; do
  if [[ ! -f "${MOD_BUILD_DIR}/${required}" ]]; then
    printf '%s\n' "Missing runtime mod artifact: ${MOD_BUILD_DIR}/${required}" >&2
    exit 1
  fi
done

cp "${MOD_BUILD_DIR}/ZSts2FontSizeMod.dll" "${RELEASE_DIR}/"
cp "${MOD_BUILD_DIR}/font_size_config.json" "${RELEASE_DIR}/"
cp "${MOD_BUILD_DIR}/ZSts2FontSizeMod.pck" "${RELEASE_DIR}/"

(
  cd "${RELEASE_DIR}"
  zip -q -r "${ZIP_PATH}" ZSts2FontSizeMod.dll font_size_config.json ZSts2FontSizeMod.pck
)
