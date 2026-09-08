#!/usr/bin/env bash
#
# Fails when test coverage drops below the floor. Reads the Cobertura report that
# `dotnet test --collect:"XPlat Code Coverage"` writes, so it runs identically on a laptop and in
# CI. The floor exists to catch a slide, not to chase 100%: raise it when coverage rises, never
# lower it to make a build pass.
#
# Usage: scripts/check-coverage.sh [results-directory]

set -euo pipefail

MIN_LINE_RATE=92
MIN_BRANCH_RATE=75

RESULTS_DIR="${1:-TestResults}"

report=$(find "$RESULTS_DIR" -name 'coverage.cobertura.xml' -print -quit 2>/dev/null || true)

if [[ -z "$report" ]]; then
  echo "No coverage.cobertura.xml under '$RESULTS_DIR'." >&2
  echo "Run: dotnet test -c Release --collect:\"XPlat Code Coverage\" --results-directory $RESULTS_DIR" >&2
  exit 1
fi

# The rates live on the report's root <coverage> element, as fractions.
rate_of() {
  head -c 2000 "$report" \
    | tr ' ' '\n' \
    | sed -n "s/^$1-rate=\"\([0-9.]*\)\".*/\1/p" \
    | head -1
}

line_rate=$(rate_of line)
branch_rate=$(rate_of branch)

if [[ -z "$line_rate" || -z "$branch_rate" ]]; then
  echo "Could not read the coverage rates from $report." >&2
  exit 1
fi

line_pct=$(awk -v r="$line_rate" 'BEGIN { printf "%.2f", r * 100 }')
branch_pct=$(awk -v r="$branch_rate" 'BEGIN { printf "%.2f", r * 100 }')

echo "Coverage: ${line_pct}% lines (floor ${MIN_LINE_RATE}%), ${branch_pct}% branches (floor ${MIN_BRANCH_RATE}%)"
echo "Report:   $report"

failed=0
if awk -v v="$line_pct" -v m="$MIN_LINE_RATE" 'BEGIN { exit !(v < m) }'; then
  echo "::error::Line coverage ${line_pct}% is below the ${MIN_LINE_RATE}% floor." >&2
  failed=1
fi

if awk -v v="$branch_pct" -v m="$MIN_BRANCH_RATE" 'BEGIN { exit !(v < m) }'; then
  echo "::error::Branch coverage ${branch_pct}% is below the ${MIN_BRANCH_RATE}% floor." >&2
  failed=1
fi

exit "$failed"
