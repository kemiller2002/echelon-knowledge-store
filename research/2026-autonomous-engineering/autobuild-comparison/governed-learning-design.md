# Governed self-improvement (Phase 4)

Mission EKS-RESEARCH-2026-003 · 2026-11-18 · Status: **contract implemented**
(praxis#302, 12/12 tests). Persistence, runners and effectiveness are **not** implemented
or measured.

## AutoBuild's model, as documented (vendor marketing page only)

- **Inputs:** chats, ratings, PR review comments.
- **Memory:** consolidated nightly, with **no approval**.
- **Rules:** a daily scan of recurring review issues proposes "up to three fixes", and
  three matching notes produce a drafted rule. Rule, skill and loop changes **need
  approval** (approve, edit, discard); "Un-apply" removes a rule.
- **Not documented:** evaluation of a proposed rule, version history, or diffs
  (`data/autobuild-research-notes.md` §2).

## Echelon design

A lesson becomes an `ImprovementProposal`
(`praxis/src/Praxis.Domain/Learning/ImprovementProposal.fs`) carrying the nine parts the
brief requires:

| Brief requirement | Field / rule |
|---|---|
| 1. Original failure and evidence | `Failures` (source, summary, `EvidenceRefs`, time); refused if any failure cites no evidence |
| 2. Root-cause classification | `RootCause` (8 classes) |
| 3. Corrective action | `CorrectiveAction`, `Target` (knowledge, skill, prompt, or `Governance of ProtectedClass`) |
| 4. Expected improvement | `ExpectedImprovement` |
| 5. Counterfactual test | `Evaluation.Counterfactual` must be non-empty and all passing |
| 6. Regression tests | `Evaluation.Regression` must be non-empty and all passing |
| 7. Cost | `EstimatedCost` (execution plus maintenance, caller's unit) |
| 8. Approval and rollback | `approve` refuses self-approval and requires a human for protected governance; `RollbackPlan`; `rollBack` from `Applied` |
| 9. Versioned provenance | `Id`, `Version`, `ProposedBy: Actor`, `DerivedFrom`, approver and applied version recorded in state |

**Memorisation guard.** "Do not confuse memorising one failed example with learning a
generalisable rule" is enforced:
- at least one **held-out** case of the same root cause must pass;
- a "held-out" case that is one of the original cases is refused (`PRX.LEARN.MEMORIZED`).

**Self-modification guard.** `Governance` targets reuse Praxis's existing
`ProtectedClass`. Autonomous execution and the learning loop therefore share one
definition of what an agent may not change by itself. Initially every governance change
needs a human approver who is not the proposer.

## First worked case (from this program)

**Failure.** EXP-AE-001 round 2, D-T4. The EDF-derived protocol identified a second
broken caller of a defective helper, but ranked the origin fix "premature". It patched
only the reported function and claimed `complete`.

**Candidate proposal (not applied).** Before ranking a shared-origin fix as premature,
enumerate every caller of the origin and test each one.

**What evaluation would need:**
- Counterfactual: re-run D-T4.
- Held-out: a different shared-helper bug of the same root cause, which this program
  does not have.
- Regression: the other D-arm tasks.

Under the contract this proposal **cannot be approved yet**, because there is no
held-out case. That is the intended behaviour.

## What is not done

1. **Persistence:** an append-only proposal log beside `.ros/events`, CLI verbs, and
   provenance records.
2. **Runners:** producing counterfactual, held-out and regression results. Dokimos
   (test outcomes) and the experiment harness here (`experiments/clarity-edf/grade.py`)
   are the natural producers.
3. **Effectiveness:** whether accepted proposals reduce future failures. This needs real
   longitudinal history; no claim is made.
4. **Unattended learning sources:** human corrections, rejected PRs, and token or
   attention overruns. Their intake adapters are not designed.
