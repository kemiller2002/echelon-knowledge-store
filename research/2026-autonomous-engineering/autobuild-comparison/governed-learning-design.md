# Governed self-improvement (Phase 4)

Mission EKS-RESEARCH-2026-003 · 2026-11-18 · Status: **contract implemented**
(praxis#302; 18/18 tests after hardening against gaming found by independent review). Persistence, runners and effectiveness are **not** implemented
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
| 8. Approval and rollback | `approve` refuses self-approval (trimmed, case-insensitive) and, initially, requires an identified human for every change; refuses a proposal changed after evaluation; `RollbackPlan`; `rollBack` from `Applied` |
| 9. Versioned provenance | `Id`, `Version`, `ProposedBy: Actor`, `DerivedFrom`, approver and applied version recorded in state |

**Memorisation guard.** "Do not confuse memorising one failed example with learning a
generalisable rule" is enforced:
- at least one **held-out** case of the same root cause must pass;
- a "held-out" case that is one of the original cases is refused (`PRX.LEARN.MEMORIZED`).

**Self-modification guard.**
- `Governance` targets reuse Praxis's existing `ProtectedClass`.
- A "knowledge" change to agent-governing files (`AGENTS.md`, `CLAUDE.md`, `ros.json`,
  `.github/`, `.claude/`, ...) is classified as governance.
- Initially **every** change needs a human approver who is not the proposer. Identities
  are compared trimmed and case-insensitively; `Unknown` and extension actor kinds
  never approve.
- States carry the evaluated proposal, so a proposal swapped after evaluation is refused.

**Hardening (PRX-LEARN-002).** An independent reviewer gamed the first version with:
- a re-spelt held-out id;
- blank evidence references;
- a re-cased self-approval;
- an `Unknown` approver;
- another agent approving a change to `AGENTS.md`;
- a proposal swapped after evaluation.

Each is now refused and has a regression test (`adversarial-review/learning-gaming.*`
holds the original reproduction).

**Known limit:** actor kinds are self-reported provenance, not authentication. An
agent declaring itself `human` under another id passes `approve`. Binding approval to
Praxis's owner-signed `RunAuthorization` is required before this loop runs unattended.

## First worked case (from this program)

**Failure.** EXP-AE-001 round 2, A-T4. The baseline agent patched only the reported
function, did not mention the second broken caller (`conflicts()`), and claimed
`complete`. This is the program's only undisclosed incomplete fix
(`clarity-edf-experiment-results.md`, Task T4).

(An earlier draft used D-T4 as the worked case. That run disclosed the residual bug and
asked the requester, so it is not an undisclosed failure; withdrawn after review.)

**Candidate proposal (not applied).** Before reporting `complete`, list every other
caller of any function you changed or diagnosed as defective, and state for each whether
it was tested.

**What evaluation would need:**
- Counterfactual: re-run round-2 A-T4.
- Held-out: a different shared-helper bug of the same root cause, which this program
  does not have.
- Regression: the other round-2 A-arm tasks.

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
