#!/usr/bin/env bash
set -euo pipefail

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

ssh -o StrictHostKeyChecking=no "${STS2_DECK_HOST}" "pgrep -af '${STS2_RUNNING_PATTERN}' || true"
