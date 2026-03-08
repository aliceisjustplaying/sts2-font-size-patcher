#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

cp "${LOCAL_DLL_DIR}/sts2.dll.bak" "${LOCAL_DLL_DIR}/sts2.dll"
cp "${LOCAL_DLL_DIR}/GodotSharp.dll.bak" "${LOCAL_DLL_DIR}/GodotSharp.dll"

dotnet run \
  --project "${ROOT_DIR}/patcher/StsFontPatcher/StsFontPatcher.csproj" \
  -- "${LOCAL_DLL_DIR}/sts2.dll" "${STS2_PATCH_SCALE}"
