# Adaptation recommendations: adopt, improve, reject, discontinue

Mission EKS-RESEARCH-2026-003 · 2026-11-18. Each item cites its evidence. Status words:
**implemented** (code in a PR), **proposed** (not built), **experimental** (needs a trial).

## Adopt

| # | Recommendation | Why (evidence) | Status |
|---|---|---|---|
| A1 | Enable Praxis verified completion (`requireVerifiedCompletion` with pinned `trustedVerifierKeyIds`) in **one pilot repository** (Dokimos), via a reviewed change | The design resists the brief's forgery cases but is enabled in 0/13 repos (EV-AE-005, EV-AE-007). The only false completion claims observed came from agents grading themselves (EV-AE-012). | proposed (owner approval required: policy change) |
| A2 | Worst-outcome merging of repeated test identities | Confirmed false verification (EV-AE-001) | **implemented**, dokimos#71 |
| A3 | Repository confinement of completion evidence | Confirmed acceptance of `/etc/hostname` and a destroyed scratchpad file as test evidence (EV-AE-003, EV-AE-004) | **implemented**, praxis#303 |
| A4 | Governed improvement proposals (counterfactual, held-out and regression gates; human approval for protected governance) | AutoBuild consolidates memory with no approval and documents no rule evaluation; Echelon had no learning contract at all | **implemented (contract only)**, praxis#302 |
| A5 | Load agent skills and instructions only from the protected default branch (AutoBuild practice) | Blocks unmerged-branch instruction injection; Praxis already reads completion policy from the merge base (REL-GATE-004), so this extends an existing pattern | proposed |
| A6 | Human-attention minutes and an escaped-defect link in Praxis telemetry | The central economic question cannot be answered without them (EV-AE-012, `economic-analysis.md`) | proposed |
| A7 | Run the EKS compiler as a CI **validator** (contradictions, drift, missing ids) | Measured diagnostic value (6 real errors in Limen) at low cost; agent-facing benefit unproven | proposed |

## Improve

| # | Recommendation | Why | Status |
|---|---|---|---|
| I1 | Split `behavior-verified` into `tests-cited` (attestation) and a judged `behavior-verified` | The name implies proof, but the check is presence (EV-AE-006) | proposed |
| I2 | Pin evidence by sha256 at plain completion | 415 historical citations now point at missing files (EV-AE-004) | proposed |
| I3 | Extend `praxis hub serve` into the unified control plane; do not build a new app | It already exists, consumes pinned Forma and Limen routing, and separates human-required actions (EV-AE-023) | proposed (see roadmap) |
| I4 | One receipt contract shared by Praxis and Dokimos | Duplicated receipt and obligation types (EV-AE-020) | proposed |
| I5 | Revise EDF's control-point ranking: require enumerating every caller of an origin before calling its fix "premature" | Observed root-cause miss plus false completion claim (EV-AE-012, D-T4) | experimental (needs a held-out case; see governed-learning design) |

## Reject (for now)

| # | Item | Why |
|---|---|---|
| R1 | **Mandatory** Clarity, EDF or combined analysis before every implementation | 72 runs: no measured correctness gain over baseline at n=6 per arm; 30–124% more wall time (EV-AE-011, EV-AE-012). Consistent with prior Echelon studies on decision cases (EV-AE-016). |
| R2 | Adaptive routing protocol F as specified | It routes bug reports to EDF alone, the arm that missed the root cause on the only discriminating task |
| R3 | Merge rate as a quality metric (AutoBuild's 96%) | It measures acceptance, not correctness; public Storybook data contradicts its use |
| R4 | Unapproved memory consolidation | Conflicts with provenance and governance rules |
| R5 | Converting knowledge from Markdown to a new syntax | ≤13% token effect, ≤2% best saving; Markdown was not the bottleneck in 35/35 effective runs (EV-AE-017) |

## Keep, but re-test before investing further

| # | Item | Condition for further investment |
|---|---|---|
| K1 | Lightweight Praxis discipline (arm B: scope, acceptance criteria, baseline, handoff) | It matched the best arms in both rounds at lower cost than C, D or E. Re-test on real tasks before mandating it. |
| K2 | Combined Clarity+EDF (E) for tasks with unexplained failures | 6/6 in both rounds, and fixed T4's origin with Haiku. Re-test with ≥20 real bug tasks and 3 trials before routing to it. |
| K3 | Compiled work-item packages | Only for requirements amended by uncited records; trial on real work items |

## Discontinue

Nothing is recommended for discontinuation on this evidence. The data argues against
adding mandatory analysis, not against Clarity or EDF as human-facing methods, which this
program did not test.
