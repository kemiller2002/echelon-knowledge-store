#!/usr/bin/env bash
# Usage: completion-policy-scan.sh <dir-containing-repo-clones>...  (prints completion-policy flags per ros.json)
set -euo pipefail
for root in "$@"; do
  for f in "$root"/*/ros.json; do
    [ -f "$f" ] || continue
    python3 -I -c 'import json,sys;w=json.load(open(sys.argv[1])).get("workProtocol",{});print(sys.argv[1],{k:w.get(k) for k in ["requireVerifiedCompletion","requireAttestedEvidence","allowUnattestedEvidence","trustedVerifierKeyIds","qualityEvidence"]})' "$f"
  done
done
