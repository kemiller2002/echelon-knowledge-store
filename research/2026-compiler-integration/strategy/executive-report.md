# Executive report: EKS-RESEARCH-2026-002 (Phase 2: compiler integration)

**Status: PARTIALLY COMPLETE.** All 22 completion criteria have evidence, but two essential parts of
the benchmark are missing: long-running tasks (category J) and tasks on real repositories. See
"Completion criteria" below.

Every statement is marked **[measured]** (from data in `../experiments/raw/`, tests or runs) or
**[hypothesis]**.

## Bottom line

- [measured] Compiled work-item packages did not help when the relevant knowledge was easy to find
  (16/16 tasks solved in every condition), and they did help when requirements were amended by
  records the work item does not mention. There, compiled packages solved 32/32 tasks against 24/32
  for the existing workflow and 28/32 for keyword retrieval, with no false completion claims (against
  8 and 4). The result held for both models and both seeds.
- [measured] Cost per correctly verified task fell 24% (25,250 vs 33,386 tokens) because more tasks
  were right. Tokens per run did not change.
- [hypothesis] Real Echelon work items often lack links to the records they depend on (65% of 796
  cite none), so the hard case is plausibly common. This is untested on real work.
- Recommendation: adopt packages, manifests, incremental compilation and change manifests as
  **advisory** tools (Stage 1). Keep verification in Dokimos and completion in Praxis. Do not gate on
  EKS until real-repository evidence exists (`integration-roadmap.md`).

## Repository revisions, branches and pull requests

- EKS main at start: `c0ba6242516d88db934946748056cbc7eea12a92`. Work on branch
  `claude/echelon-knowledge-store-research-horhs0`, draft PR #4. Praxis `12bf995…`, Dokimos `6ea8624…`,
  Percepta `4a53688…` read-only (`../baseline/repository-revisions.md`). No other repository was
  modified.

## Integration prototypes (all in EKS)

- `eks work-item` / `work-item-manifest`: Praxis queue row → package with the 12 required fields and
  derived completeness [measured: unit tests; fixture].
- `eks dokimos-obligations`: emits `dokimos.verification-obligations/1` from declared bindings; the real
  Dokimos CLI parsed it and reported `not-run` [measured].
- `eks change-manifest`: transactional evaluation of knowledge changes; never approves [measured].
- `eks privacy-check` (CI) and `eks privacy-check-private` (local) [measured].

## Coding benchmark coverage

Eight tasks across categories A–I on a synthetic two-repository fixture with hidden tests (33 base,
41 hard), a reference solution for each variant, and a hard variant with ~300 distractors and four
incoming-edge amendments. Not covered: J (long-running) and real repositories [measured gap].

## Verified completion rates, missed requirements, regressions

| | A existing | B retrieval | C packages | D packages + policy |
|---|---|---|---|---|
| Base fixture (Sonnet, 4 runs each) | 16/16 | 16/16 | 16/16 | 16/16 |
| Hard fixture (2 models × 2 seeds, 8 runs each) | 24/32 | 28/32 | 32/32 | 32/32 |
| False "complete" claims (hard) | 8 | 4 | 0 | 0 |

[measured] Every missed requirement was an amendment the agent never found; no base test failed in any
hard run. Regressions: none. Visible tests passed and `fx-identity` was unchanged in 48 of 48 runs.
The undecided task was abstained on in 24 of 24 runs. The injected instruction was followed in 0 of
44 exposed runs.

## Evidence-integrity failures

[measured] 2 fabrications in 20 runs, both accepted evidence citing only a free-text title, in
conditions 1–2 (one per model). 0 in conditions 3–5, but the referential rule could not have caught that
form, so the difference is not attributable to it (p ≈ 0.15). Misrepresented sources, failed tests
reported as passing and wrong-revision evidence: 0 of 20 runs. The change manifest flags both
fabrications and rejected the one malformed change.

## Security findings

- [measured] The Phase 2 CI privacy check would not have caught the Phase 1 incident (0/6 lines).
  A local check found **three private file names still in Phase 1 documents**; they are redacted.
  They and the original six lines remain in Git history; rewriting it is the owner's decision
  (`../security/privacy-review.md`).
- [measured] Compiler defects found and fixed: the source-existence rule silently skipped `../` paths;
  the parse cache ignored compiler version and schema configuration.
- [measured] Several Haiku agents made stray session-tool calls (archive, message, interrupt, list).
  All were denied or had no effect. Containment must come from harness permissions, not instructions.

## Token consumption, computational cost, wall-clock

- [measured] Agent trials in this phase: 72 runs (48 coding runs, 20 evidence writers, 4 evidence
  reviewers), 6.39 M subagent tokens, 96.7 agent-minutes. This excludes two interface-research
  agents (Praxis about 213 k tokens; Dokimos not recorded) and the orchestrating session.
- Tokens per run were flat across conditions (base 79–82 k; hard about 100 k). Wall time per run was
  flat (hard: 109–124 s mean).

## Compiler overhead

- [measured] A work-item package from the CLI: 0.4–0.5 s end-to-end (including .NET start-up) on the
  331-record hard fixture. Package size about 500 words (retrieval context about 350).
- [measured] Public corpus (532 records): full compile median 159 ms, incremental 62 ms.

## Incremental compilation

[measured] Parse-cache incremental: 0 stale packages over 60 edits × 25 packages, 2.6× faster than full.
Naive package memo: stale at 60/60 steps. On-demand outgoing closure: wrong from the start. Fully keyed
memo: correct but slower. The cache key now includes compiler version and parse configuration.

## Cross-repository impact

[measured] On 8 fixture targets, `eks impact` found 13/13 impacted records with no false positives.
Text search found 11/13, plus 5 non-impacted mentions (precision 69%). It missed dependents linked
transitively across repositories.

## Recommended integrations, rejected approaches

See `architecture-recommendation.md` for gates:
- **ADOPT:** packages (advisory), derived completeness and manifests, parse-cache incremental
  compilation, change manifest, the source-rule fix, and both privacy checks.
- **EXPERIMENTAL:** Dokimos obligations from bindings; a title-only-source review rule.
- **INCONCLUSIVE:** agent integrity policy; second-agent review; EKS as a gate.
- **REJECT:** retrieval as a substitute, package memoisation, on-demand compilation.

## Remaining uncertainties

1. Effect size on real work items (the fixture's amendments were designed in).
2. Long-running tasks; models beyond two from one provider; more repetitions.
3. Fixture contamination: hidden tests are public now (`../benchmarks/held-out-policy.md`).
4. Whether integrity policies or reviewers reduce fabrication (too few events).
5. Correctness of declared test bindings, which neither EKS nor Dokimos can check.
6. Dokimos behaviour on its native .NET 8 runtime (run under roll-forward here).

## Implementation roadmap

`integration-roadmap.md`: Stage 0 consolidate EKS → Stage 1 advisory packages and manifests on real
work, measuring missed requirements → Stage 2 typed links in Praxis → Stage 3 Dokimos obligations →
Stage 4 gating only with Stage 1 evidence.

## Completion criteria

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | Previous research reviewed | met | `../baseline/previous-findings.md` |
| 2 | Baseline revisions recorded | met | `../baseline/repository-revisions.md` |
| 3 | Praxis interfaces verified | met (by source reading; Praxis CLI not executed) | `../architecture/praxis-integration.md` |
| 4 | Dokimos interfaces verified | met (read and executed) | `../architecture/dokimos-integration.md` §7 |
| 5 | Compiled task-package prototype | met | `eks work-item` |
| 6 | Package completeness machine-validated | met | derived conditions + tests |
| 7 | Evidence-integrity controls evaluated | met (small n) | `../evidence-integrity/` |
| 8 | Compiler validation failures covered by tests | met for the failures found; not every rule has a planted-violation test | tests |
| 9 | Real coding benchmarks exist | **partial**: real code and tests on a synthetic fixture; no real-repository tasks; no category J | `../benchmarks/` |
| 10 | All four conditions evaluated | met | `../experiments/` |
| 11 | Multiple trials | met (2 seeds × 2 models; 2 repetitions on base) | `../experiments/replication-results.md` |
| 12 | Independent evaluation | met (hidden tests, deterministic scoring, transcript audit, independent adversarial review) | `../benchmarks/evaluation-rubric.md` |
| 13 | Incremental compilation tested | met | `../architecture/incremental-compilation.md` |
| 14 | Cross-repository impact evaluated | met (small) | `../knowledge-updates/update-correctness.md` |
| 15 | Write-path reliability tested | met | `../knowledge-updates/write-path-design.md` |
| 16 | Security and privacy reviewed | met | `../security/` |
| 17 | Cost per verified task measured | met | `../experiments/compiler-results.md` |
| 18 | Regressions and failures investigated | met | per-test failure analysis |
| 19 | Results reproducible | met for harness and scoring (seeded); agent behaviour is stochastic | harness + raw data |
| 20 | Staged recommendation | met | `integration-roadmap.md` |
| 21 | Findings committed and pushed | met | PR #4 |
| 22 | Remaining uncertainties documented | met | above |
