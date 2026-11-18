"""Cost per verified outcome for EXP-AE-001 under explicit, varied assumptions.

Measured inputs: wall seconds, verified count, requester questions, false completion claims
(results-summary.json). Assumed prices: human minutes per requester question (Q), human
minutes to find and rework an escaped false completion (R), and agent cost per wall-minute
relative to a human minute (k). Usage: python3 -I economics.py <experiment-dir>
"""
import json
import sys
from pathlib import Path

SCENARIOS = {"low": (2, 30, 0.05), "central": (5, 60, 0.1), "high": (10, 240, 0.2)}


def cost(arm: dict, q: float, r: float, k: float) -> dict:
    agent_min = arm["meanWallSeconds"] * arm["n"] / 60
    human_min = arm["requesterQuestions"] * q + arm["falseCompletionClaims"] * r
    total = agent_min * k + human_min
    per_verified = total / arm["verified"] if arm["verified"] else float("inf")
    return {"agentMinutes": round(agent_min, 1), "humanMinutes": human_min,
            "totalHumanEquivalentMinutes": round(total, 1), "perVerifiedOutcome": round(per_verified, 2)}


if __name__ == "__main__":
    root = Path(sys.argv[1])
    summary = json.loads((root / "results-summary.json").read_text())
    out = {name: {rnd: {arm: cost(s, *SCENARIOS[name]) for arm, s in summary[rnd]["arms"].items()}
                  for rnd in ("round1", "round2")} for name in SCENARIOS}
    out["assumptions"] = {n: {"minutesPerRequesterQuestion": q, "minutesPerEscapedFalseCompletion": r,
                              "agentWallMinuteInHumanMinutes": k} for n, (q, r, k) in SCENARIOS.items()}
    (root / "economics.json").write_text(json.dumps(out, indent=1))
    for rnd in ("round1", "round2"):
        print(rnd)
        for arm in summary[rnd]["arms"]:
            print(f"  {arm:11}", "  ".join(f"{n}={out[n][rnd][arm]['perVerifiedOutcome']}" for n in SCENARIOS))
