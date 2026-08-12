#!/usr/bin/env bash
set -euo pipefail
SOLN=MedicalView.sln
SERVICES=(Identity Studies Imaging Reporting)

# create a project only if it doesn't already exist (safe to re-run)
new_proj() { # <template> <name> <dir> [extra args...]
  local tmpl=$1 name=$2 dir=$3; shift 3
  if [[ -f "$dir/$name.csproj" ]]; then
    echo "skip: $name already exists"
  else
    dotnet new "$tmpl" -n "$name" -o "$dir" "$@"
  fi
}

# add a project-to-project reference only if it isn't there yet
add_ref() { # <from.csproj> <to.csproj>
  if dotnet list "$1" reference 2>/dev/null | grep -qF "$(basename "$2")"; then
    echo "skip: $(basename "$1") -> $(basename "$2") already referenced"
  else
    dotnet add "$1" reference "$2"
  fi
}

[[ -f "$SOLN" ]] || dotnet new sln -n "${SOLN%.sln}"

for S in "${SERVICES[@]}"; do
  base="src/Services/$S"
  new_proj classlib "MediView.$S.Domain"         "$base/MediView.$S.Domain"
  new_proj classlib "MediView.$S.Application"    "$base/MediView.$S.Application"
  new_proj classlib "MediView.$S.Infrastructure" "$base/MediView.$S.Infrastructure"
  new_proj web      "MediView.$S.Api"            "$base/MediView.$S.Api"

  # inward-only references
  add_ref "$base/MediView.$S.Application/MediView.$S.Application.csproj"       "$base/MediView.$S.Domain/MediView.$S.Domain.csproj"
  add_ref "$base/MediView.$S.Infrastructure/MediView.$S.Infrastructure.csproj" "$base/MediView.$S.Application/MediView.$S.Application.csproj"
  add_ref "$base/MediView.$S.Api/MediView.$S.Api.csproj"                       "$base/MediView.$S.Infrastructure/MediView.$S.Infrastructure.csproj"
done

# gateway + web
# NOTE: the old `blazorserver` template was removed in .NET 8+.
# The Blazor Web App template (`blazor`) with `-int Server` is its replacement.
new_proj web    MediView.Gateway src/Gateway/MediView.Gateway
new_proj blazor MediView.Web     src/Web/MediView.Web -int Server -ai

# add everything to the solution
find src -name '*.csproj' -exec dotnet sln "$SOLN" add {} +
dotnet build
