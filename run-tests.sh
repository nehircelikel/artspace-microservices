#!/usr/bin/env bash
# Runs every backend service's test suite (unit + integration).
# Integration tests use in-memory SQLite, so no Postgres/RabbitMQ/Docker is needed.
set -uo pipefail

# The .NET SDK here lives in a non-default location; dotnet-ef and the test host
# need DOTNET_ROOT to resolve the runtime.
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"

cd "$(dirname "$0")"

SOLUTIONS=(
  "AuthService/artspace.sln"
  "ArtService/ArtService.sln"
  "CommentService/CommentService.sln"
  "NotificationService/NotificationService.sln"
  "RequestService/RequestService.sln"
)

failed=0
for sln in "${SOLUTIONS[@]}"; do
  echo "===================== dotnet test $sln ====================="
  if ! dotnet test "$sln" --nologo; then
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "❌ One or more test suites failed."
  exit 1
fi

echo "✅ All test suites passed."
