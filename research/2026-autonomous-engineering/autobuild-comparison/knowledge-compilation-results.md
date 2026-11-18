# Compiled knowledge vs Markdown (Phase 5)

Mission EKS-RESEARCH-2026-003 · 2026-11-18.

**No new head-to-head compiled-vs-Markdown experiment was run in this mission.** This
document consolidates the measured results already in this repository and adds one
indirect observation from EXP-AE-001. It does not assume that compiled knowledge is
superior.

## Measured results already in this repository (cited, not re-run)

| Question | Result | Status | Source |
|---|---|---|---|
| Does syntax matter? | Syntax changes total tokens by at most 13%; a DSL saves 0.7%; F# records add 75% metadata | measured | `research/2026-knowledge-representation/strategy/executive-report.md` |
| Does Markdown with front matter answer questions accurately? | 0.98–1.00 accuracy; removing front-matter metadata costs about 25 points | measured | same, and `adoption-criteria.md` |
| Token reduction from compiled formats? | The 25% target was **not met** by any format (best 2%) | measured | `adoption-criteria.md` lines 35–37 |
| Wall time? | Compiler-backed retrieval roughly halves agent wall time (15 s vs 31 s) | measured | executive report |
| Correctness when requirements are amended by records the work item does not mention (hard fixture) | Compiled packages 32/32 vs existing workflow 24/32 and keyword retrieval 28/32; false completion claims 0 vs 8 and 4 | measured, single synthetic fixture | `research/2026-compiler-integration/continuation/research-status.json` |
| Same, base fixture | 16/16 in every condition | measured | same |
| Tokens per verified task (hard) | 25,250 (compiled) vs 33,386 (existing) | measured | same |

**Caveats recorded by that mission:**
- the fixture was written by the compiler's author;
- its hidden tests are now public;
- no real-repository tasks were run.

## Indirect observation from EXP-AE-001 (measured, small)

Tasks T1–T3 put the governing knowledge in plain Markdown (`docs/CONVENTIONS.md`,
`docs/DECISIONS.md`). An agent had to find and apply it, and the hidden oracles tested
exactly those rules.

- **Opus-class (round 1):** applied the Markdown rules correctly in 18/18 runs.
- **Haiku (round 2):** 17/18; the one miss was harness-blocked, not a misreading.

On small repositories where the relevant record is a short, obviously named file,
Markdown was **not** the bottleneck. This matches the base-fixture result above (16/16 in
every condition).

## Assessment against the brief's criteria

| Criterion | Evidence | Verdict |
|---|---|---|
| Ambiguous interpretation | No misreadings of Markdown rules in 35 effective runs (EXP-AE-001) | No measured benefit available to capture on small repositories |
| Contradictory instructions | The compiler detects structural contradictions (claims, supersession, vendored divergence; 27 diagnostics EKS0001–EKS0027). T3's request-vs-decision conflict was resolved correctly by every agent without it | Useful as a **linter**, unproven as an agent aid |
| Repeated context loading / tokens | ≤2% format savings; about 24% fewer tokens per verified task on the hard fixture | Real only when packages replace exploration |
| Incorrect dependency assumptions | Hard fixture: 0 vs 8 false completion claims | The strongest evidence for compilation, and it targets the same failure class EXP-AE-001 found (false completion) |
| Knowledge drift | `verify` refuses stale outputs; 6 errors found in Limen decision files during baseline (missing `id`) | Measured as a diagnostic capability |
| Verification errors | Dokimos obligation manifests are produced by the compiler | Integration exists in research form only |
| Authoring, compilation, maintenance and migration cost | Not measured. Front matter already exists in most repositories; migration was excluded by the EKS charter (no migrations) | **Unknown.** Must be measured before adoption |

## Recommendation

- **Keep Markdown as the source of truth.**
- **Use the compiler as a validator** (contradictions, drift, missing ids) in CI, where
  its value is measured and its cost is low.
- **Gate agent-facing packages on a real-repository trial** for the one condition where
  they won: requirements amended by records the work item does not cite. The baseline
  estimates this affects 65% of 796 real work items; that is a hypothesis.

This aligns with the existing EKS continuation manifest's first open question.
