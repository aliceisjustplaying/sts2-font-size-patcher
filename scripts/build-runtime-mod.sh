#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

MOD_PROJECT="${ROOT_DIR}/runtime_mod/Sts2FontSizeMod/Sts2FontSizeMod.csproj"
MOD_BUILD_DIR="${ROOT_DIR}/runtime_mod/build"
MOD_PCK_PROJECT_DIR="${ROOT_DIR}/runtime_mod/pck"

mkdir -p "${MOD_BUILD_DIR}"

dotnet build "${MOD_PROJECT}" -c Release

cp "${ROOT_DIR}/runtime_mod/Sts2FontSizeMod/bin/Release/net9.0/ZSts2FontSizeMod.dll" "${MOD_BUILD_DIR}/"
cp "${ROOT_DIR}/runtime_mod/Sts2FontSizeMod/bin/Release/net9.0/font_size_config.json" "${MOD_BUILD_DIR}/"

if [[ -n "${STS2_GODOT_EXPORTER}" && -x "${STS2_GODOT_EXPORTER}" ]]; then
  "${STS2_GODOT_EXPORTER}" --headless --path "${MOD_PCK_PROJECT_DIR}" --export-pack "Linux" "${MOD_BUILD_DIR}/ZSts2FontSizeMod.pck"
else
  printf '%s\n' "Skipping .pck export because STS2_GODOT_EXPORTER is not set to an executable." >&2
fi
