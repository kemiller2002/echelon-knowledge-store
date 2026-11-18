# Final report: EKS-RESEARCH-2026-003

Echelon Foundry autonomous-engineering improvement program, with Obvious AutoBuild as
the competitive reference. Executed 2026-11-18 by one Claude Code session with parallel
subagents. **Completed implementation, proposed work, experimental findings and
hypotheses are labelled throughout.** Evidence IDs resolve in
[`evidence-register.md`](evidence-register.md).

## The central question

> Can Echelon Foundry produce more correct, independently verified engineering outcomes
> with less total cost, less human attention, and fewer downstream failures?

**Answer, on present evidence: not yet, and not by the route the program assumed.**

1. **More analysis does not buy more correctness** on the tasks measured.
   - Mandatory Clarity, EDF or combined pre-implementation analysis gave no measured
     gain over a baseline agent in 72 controlled runs (EV-AE-011, EV-AE-012).
   - It added 30–124% wall time.
   - On the one task that discriminated, the EDF-derived protocol produced a root-cause
     miss and a false completion claim.
   - These samples are small (n=6 per arm per round). The result is "no evidence of
     benefit", not "evidence of no benefit".
2. **The failure that costs the most is a false "complete".**
   - Every place this program looked, cost and risk concentrated there:
     - the T4 failures (EV-AE-012);
     - EKS mission 002's 0 vs 8 overclaims (EV-AE-017);
     - the economics model, under all assumption sets.
   - Echelon already has a verification design that resists false completion (signed
     receipts, claim-bound results, protected policy), but it is enabled in **0 of 13**
     repositories (EV-AE-005).
   - Deploying what exists is the highest-value step available.
3. **Less human attention cannot be shown or refuted.** Echelon does not measure human
   attention (EV-AE-012; `economic-analysis.md`).
4. **Against AutoBuild:**
   - Echelon is stronger *in design* on verification independence and governed learning.
   - AutoBuild is far stronger on product surface and demonstrated throughput: about
     5,700 public PRs.
   - None of AutoBuild's capabilities is independently verified (EV-AE-015).

## Repositories examined

| Repo | Role | How |
|---|---|---|
| praxis | governance / repository OS | read, built, full suite, 2 PRs |
| dokimos | verification / quality evidence | read, built, full suite, 1 PR |
| echelon-knowledge-store | knowledge, this research | built (78/78), research stored |
| ordo, aegis, fides, arca, vigila | orchestration, operational recovery, identity, persistence, follow-ups | read, built, tested (subagent; EV-AE-018) |
| forma, folio, limen, echelon-registry, echelon-engineering-system, echelon-foundry | UI, reporting, browser/WASM boundary, registry, docs, site | read, partially built and tested (EV-AE-018) |
| clarity-framework, clarity-service, echelon-diagnostic-framework | decision and diagnostic methods | read; clarity-service built (23/23) |
| echelon-organization-administration | portfolio governance inventory | read (historical defect register) |

## Changes implemented

| Change | Repo / PR | Tests | State |
|---|---|---|---|
| Repeated test identities keep the worst outcome (fixes a false verification) | dokimos#71 | 5 adversarial tests; full suite 602/602; self-ratchet passes | draft PR, CI pending |
| Completion evidence must resolve inside the repository (fixes acceptance of `/etc/hostname` and a destroyed scratchpad file); frozen golden `test6` changed openly | praxis (confinement PR) | new adversarial test; full suite: no branch-only failures vs main (EV-AE-008, EV-AE-019) | draft PR, CI pending |
| Governed improvement proposal contract (counterfactual, held-out and regression gates; human approval for protected governance) | praxis#302 | 12/12; architecture, naming and internal guards pass | draft PR, CI pending |
| Research package, experiment harness, raw data, scripts | echelon-knowledge-store (this branch) | analysis scripts reproduce every number | draft PR |

Each code change was recorded through the repository's own Praxis work protocol (work
item, checkpoint, completion, `praxis validate`): Dokimos WI-0018, Praxis WI-0108, and
the confinement item.

## Tests executed

- **Dokimos:** baseline 597/597; after the fix 602/602.
- **Praxis:**
  - Baseline subsets on untouched main: 20 tests fail in this container. All spawn real
    subprocesses or validators, so they are environmental.
  - Confinement branch: no failures beyond those 20.
  - Learning branch: 12/12 new tests plus the guard suites.
- **EKS:** 78/78 (subagent).
- **Other repositories (subagents):** Ordo 806 passed in 5 of 6 projects (95 failures
  need an npm build); Aegis 347/347; Fides 331/331; Arca 415/415; Vigila 285/285; Limen
  873 passed, 10 skipped; Forma 399 subtests passed before a time cap; registry
  conformance 5/5.
- **Experiment:** 72 agent runs graded by deterministic hidden oracles; reference
  solutions verified first.

## Experimental results (Phase 6, measured)

See [`clarity-edf-experiment-results.md`](clarity-edf-experiment-results.md).

- Round 1 (Opus-class): every arm 6/6 (a ceiling).
- Round 2 (Haiku): A 5/6, B 6/6, C 6/6, D 4/6, E 6/6, N 5/6, F 5/6 (derived).
- No arm beat baseline under the pre-registered rule.
- Analysis arms cost 1.3–2.2× baseline wall time.
- Protective behaviour (refusing to delete a used function) and decision-record
  compliance were universal, including at baseline.

## Outstanding blockers and limits

- **Policy decisions needing the owner:**
  - enabling verified completion (R1);
  - the frozen-golden contract change in the confinement PR;
  - the Dokimos rerun policy consequence.
- **Not exercised:** prompt-injection and unmerged-branch instruction attacks; whole-suite
  nondeterminism.
- **Not instrumented:** human attention, billed tokens, escaped defects.
- **Harness artifacts:**
  - the subagent runtime refused analysis files in many runs, and stopped 2 runs;
  - smaller-model subagents made unrequested session-tool calls, which were denied.
    `list_sessions` showed no child sessions and no other session updated (EV-AE-013,
    EV-AE-014).
- **No real-repository task experiment yet.** The fixtures are small and were written by
  the same agent that designed the protocols.

## Next recommended actions

1. Review and merge the three draft PRs (R0a–c).
2. Owner decision: pilot `requireVerifiedCompletion` with pinned keys in Dokimos (R1).
3. Instrument human attention and escaped-defect links (R3). Without them, the central
   question stays unanswerable.
4. Run EXP-AE-002 on 20+ real Echelon tasks (R6) before any further investment in
   pre-implementation analysis.

## Adversarial review (Phase 11)

_Pending: incorporated from the independent reviewer below._
