#!/usr/bin/env bash
# Usage: evidence-path-scan.sh <repo>...  Classifies historical tests/implementation evidence paths
# recorded in each repo's .ros state against its current checkout: FILE, DIR, GONE, or ESCAPE (absolute or ..).
set -uo pipefail
for r in "$@"; do
  [ -d "$r/.ros" ] || continue
  grep -rhoE '"(type|Type)": *"(tests|implementation)", *"(path|Path)": *"[^"]*"' "$r/.ros" 2>/dev/null \
    | sed -E 's/.*"(path|Path)": *"//; s/"$//' \
    | while read -r p; do
        if [[ "$p" = /* || "$p" == *..* ]]; then echo "$r ESCAPE $p"
        elif [ -d "$r/$p" ]; then echo "$r DIR $p"
        elif [ -f "$r/$p" ]; then echo "$r FILE"
        else echo "$r GONE"; fi
      done
done | sort | uniq -c | sort -rn
