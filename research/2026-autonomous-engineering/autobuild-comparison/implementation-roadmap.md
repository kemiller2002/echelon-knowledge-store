# Prioritised implementation roadmap

Mission EKS-RESEARCH-2026-003 · 2026-11-18. Ordered by expected reduction in false
"complete" claims per unit of effort. That failure class drove cost in every measurement
this program made (`economic-analysis.md`). Every item names its owner repository, its
acceptance test and its evidence.

## Now (merge or decide what already exists)

| ID | Item | Owner | Acceptance | Evidence | State |
|---|---|---|---|---|---|
| R0a | Review and merge worst-outcome merging | Dokimos | dokimos#71 CI green; 602/602 locally | EV-AE-001/002 | draft PR |
| R0b | Review and merge evidence-path confinement (frozen golden `test6` changes) | Praxis | praxis#303 CI; no branch-only failures vs main | EV-AE-003/008/009 | draft PR |
| R0c | Review the governed-proposal contract | Praxis | praxis#302 CI | EV-AE-010 | draft PR |
| R1 | **Pilot verified completion in Dokimos:** `requireVerifiedCompletion` plus `trustedVerifierKeyIds` per PRAXIS-REL-006 | Dokimos (owner decision) | A completion without a verifier-signed passing receipt is refused; a forged `ValidationResult` file is refused | EV-AE-005/007 | needs owner approval |

## Next (small, measurable)

| ID | Item | Owner | Acceptance |
|---|---|---|---|
| R2 | Pin evidence sha256 at plain completion; split `tests-cited` / `behavior-verified` | Praxis | Golden tests; old records still readable |
| R3 | Human-attention and escaped-defect telemetry (`human.attention` steps; reopen links) | Praxis | Telemetry validates; unavailable is never recorded as zero |
| R4 | Default-branch-only instruction loading for agents, plus an adversarial fixture where an unmerged branch weakens `AGENTS.md` | Praxis | Fixture refuses; a test exists for the brief's "unmerged branches changing reviewer instructions" |
| R5 | EKS compiler as a CI validator in 2–3 repositories | EKS / repos | Diagnostics in CI; no agent-facing change |

## Later (experiments before any build)

| ID | Item | Gate |
|---|---|---|
| R6 | EXP-AE-002: 20+ real Echelon tasks with known fixes, 3 trials, production model, arms A, A-effort, B, E; analysis returned inline | Pre-registered; succeeds only if false completion claims or root-cause misses fall by ≥50% with non-overlapping intervals |
| R7 | Proposal runner: produce counterfactual, held-out and regression results for `ImprovementProposal` from Dokimos outcomes; bind approval to the owner-signed `RunAuthorization` | First proposal (disclosure rule, I5) evaluated with a real held-out case |
| R8 | Compiled packages on real work items with uncited amendments | EKS continuation question 1 |

## Unified engineering control plane (Phase 8): incremental plan

**Decision: extend `praxis hub serve`, do not build a new interface.** It already:
- aggregates repositories;
- renders the work queue, work items, validation and executions;
- separates "human-required actions" from automated state;
- consumes a pinned Forma release with Limen routing (`praxis/docs/application-foundations.md`,
  `praxis/src/Praxis.Cli/WebInterface.fs`, `WebExecutions.fs`).

The brief says not to build a large interface before the contracts are validated.
Steps 1–2 depend only on data that exists today; step 3 waits for R1–R3.

| Step | View | Data source (existing) | New code |
|---|---|---|---|
| 1 | **Decisions needed** (the inbox): blocked items, `RequiresOwner` decisions, proposals awaiting human approval | Praxis work state, autonomy decisions, `ImprovementProposal` states | One hub page; Forma `work-queue` and `obligation-panel` patterns |
| 2 | **Verification status per item**: claim-bound result vs cited-only, receipt key ids, revision | `CompletionVerdict`, `ConsumedEvidence`, `verifierKeyIds` | Status lozenge per facet (Forma rule: state in text, never colour alone) |
| 3 | **Cost and attention per item**: provider-reported vs estimated cost, human minutes, escaped defects | `UsageCost`; R3 telemetry | After R3 lands |
| 4 | Trends: false completion rate, regression rate, proposal outcomes | Folio report over the above | After 3 months of R3 data |

Design standards apply as Praxis already applies them: Forma tokens (Charcoal and Bronze
brand), accessible controls, non-blank loading states, and a clear split between
user-facing pages and administrative operations. An application-specific logo and landing
page are Forma and brand work, scheduled with step 1; they are not designed here.
Mobile: Forma components are responsive; verify at step 1 with the Forma conformance
suites.

## Explicitly not planned

- Kubernetes, Terraform, or a hosted orchestrator.
- A new requirements store.
- A Clarity or EDF type system.
- Mandatory pre-implementation analysis.
