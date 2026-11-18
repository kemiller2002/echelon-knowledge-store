"""Summarise EXP-AE-001 (both rounds) from the archived raw data.

Usage: python3 -I analyze.py <experiment-dir>   (writes results-summary.json beside raw/)
Deterministic: reads raw/round*/graded-round*.json and raw/round*/usage.json only.
"""
import json
import math
import statistics
import sys
from pathlib import Path

ARMS = "ABCDEN"
TASKS = [f"T{i}" for i in range(1, 7)]
ROUTING_F = {"T1": "C", "T2": "A", "T3": "C", "T4": "D", "T5": "A", "T6": "C"}
# Runs stopped by the harness refusing the analysis file (see raw/round2/deviations.md).
HARNESS_BLOCKED = {"round2": {"D-T3", "N-T4"}}


def wilson(successes: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return (0.0, 1.0)
    p = successes / n
    centre = (p + z * z / (2 * n)) / (1 + z * z / n)
    half = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / (1 + z * z / n)
    return (round(max(0.0, centre - half), 3), round(min(1.0, centre + half), 3))


def arm_summary(graded: dict, usage: dict, cells: list[str], blocked: set[str]) -> dict:
    verified = sum(graded[c]["verified"] for c in cells)
    effective = [c for c in cells if c not in blocked]
    return {
        "cells": cells,
        "verified": verified,
        "n": len(cells),
        "wilson95": wilson(verified, len(cells)),
        "verifiedExcludingHarnessBlocked": sum(graded[c]["verified"] for c in effective),
        "nExcludingHarnessBlocked": len(effective),
        "falseCompletionClaims": sum(graded[c]["falseCompletionClaim"] for c in cells),
        "requesterQuestions": sum(graded[c]["questionsForRequester"] for c in cells),
        "visibleTestsTouched": sum(bool(graded[c]["visibleTestsTouched"]) for c in cells),
        "outOfScopeEdits": sum(bool(graded[c]["outOfScope"]) for c in cells),
        "meanWallSeconds": round(statistics.mean(usage[c]["durationMs"] for c in cells) / 1000, 1),
        "meanToolUses": round(statistics.mean(usage[c]["toolUses"] for c in cells), 1),
        "meanReportedTokens": round(statistics.mean(usage[c]["subagentTokens"] for c in cells)),
    }


def round_summary(root: Path, name: str) -> dict:
    graded = {r["cell"]: r for r in json.loads((root / "raw" / name / f"graded-{name}.json").read_text())}
    usage = json.loads((root / "raw" / name / "usage.json").read_text())
    blocked = HARNESS_BLOCKED.get(name, set())
    arms = {arm: arm_summary(graded, usage, [f"{arm}-{t}" for t in TASKS], blocked) for arm in ARMS}
    arms["F(derived)"] = arm_summary(graded, usage, [f"{a}-{t}" for t, a in ROUTING_F.items()], blocked)
    baseline_wall = arms["A"]["meanWallSeconds"]
    for summary in arms.values():
        summary["wallVsBaseline"] = round(summary["meanWallSeconds"] / baseline_wall, 2)
    by_task = {t: {a: graded[f"{a}-{t}"]["verified"] for a in ARMS} for t in TASKS}
    failures = [
        {"cell": c, "failing": g["oracle"]["failing"], "claimedStatus": g["claimedStatus"], "harnessBlocked": c in blocked}
        for c, g in sorted(graded.items()) if not g["verified"]
    ]
    return {"arms": arms, "verifiedByTask": by_task, "failures": failures}


if __name__ == "__main__":
    root = Path(sys.argv[1])
    summary = {
        "experiment": "EXP-AE-001",
        "round1": {"model": "session default (Opus-class)", **round_summary(root, "round1")},
        "round2": {"model": "haiku", **round_summary(root, "round2")},
        "notes": [
            "Token figures are the harness's subagent_tokens (appears to be final context size, dominated by a ~65k fixed prompt); not billed usage.",
            "One trial per cell; Wilson intervals are wide by construction.",
        ],
    }
    (root / "results-summary.json").write_text(json.dumps(summary, indent=1))
    for name in ("round1", "round2"):
        print(name)
        for arm, s in summary[name]["arms"].items():
            print(f"  {arm:11} {s['verified']}/{s['n']} CI{s['wilson95']} falseClaims={s['falseCompletionClaims']} wall={s['meanWallSeconds']}s ({s['wallVsBaseline']}x)")
