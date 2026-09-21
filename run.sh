#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

projects=(
  src/Services/Identity/MediView.Identity.Api
  src/Services/Studies/MediView.Studies.Api
  src/Services/Imaging/MediView.Imaging.Api
  src/Services/Reporting/MediView.Reporting.Api
  src/Gateway/MediView.Gateway
  src/Web/MediView.Web
)

stop_all() {
  trap - INT TERM EXIT
  kill $(jobs -p) 2>/dev/null || true
  wait 2>/dev/null || true
  docker compose stop
}

if [[ ! -f .env ]]; then
  cp .env.example .env
  echo "Created .env from .env.example"
fi

set -a
source .env
set +a

trap stop_all INT TERM EXIT

docker compose up -d --wait
dotnet build MedicalView.sln --nologo --verbosity quiet

for project in "${projects[@]}"; do
  dotnet run --project "$project" --no-build --launch-profile http &
done

echo
echo "Web:  http://localhost:5217"
echo "Seq:  http://localhost:${SEQ_PORT}"
echo "Press Ctrl+C to stop."

wait
