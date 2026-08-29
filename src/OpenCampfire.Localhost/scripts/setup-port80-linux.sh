#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

PROJECT="$(
    find "$SCRIPT_DIR/.." \
        -type f \
        -name "OpenCampfire.Localhost.csproj" \
        -print -quit
)"

if [[ -z "$PROJECT" ]]; then
    echo "ERROR: Could not find OpenCampfire.Localhost.csproj"
    exit 1
fi

CONFIGURATION="${1:-Debug}"
FRAMEWORK="net10.0"

echo "OpenCampfire - configuring port 80 access"
echo
echo "Project:"
echo "  $PROJECT"
echo

echo "Building..."
dotnet build "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --framework "$FRAMEWORK"

PROJECT_DIR="$(dirname "$PROJECT")"
EXECUTABLE="$PROJECT_DIR/bin/$CONFIGURATION/$FRAMEWORK/OpenCampfire.Localhost"

if [[ ! -x "$EXECUTABLE" ]]; then
    echo
    echo "ERROR: Could not find executable:"
    echo "  $EXECUTABLE"
    exit 1
fi

echo
echo "Executable:"
echo "  $EXECUTABLE"
echo

echo "The following command requires administrator privileges:"
echo
echo "  sudo setcap cap_net_bind_service=+ep \"$EXECUTABLE\""
echo

read -r -p "Continue? [y/N] " answer

if [[ "$answer" != "y" && "$answer" != "Y" ]]; then
    echo "Cancelled."
    exit 0
fi

echo
echo "Requesting administrator privileges..."

sudo -v

echo "Applying CAP_NET_BIND_SERVICE..."

sudo setcap 'cap_net_bind_service=+ep' "$EXECUTABLE"

echo
echo "Verifying capability..."

getcap "$EXECUTABLE"

echo
echo "Port 80 access configured successfully."