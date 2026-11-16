# Architecture recommendation

## Answer to the mission question

> Do compiled, validated, task-specific knowledge packages let autonomous agents produce more
> correct software, with fewer missed requirements and lower total cost, than reading repository
> documentation?

**Measured, on a synthetic benchmark:**

- When every relevant requirement is reachable from the work item, no. On the base fixture all four
  conditions solved 16/16 tasks and packages cost about 3% more tokens.
- When requirements are amended by records the work item does not mention, yes. On the hard fixture
  (two models, two seeds, 32 tasks per condition), compiled packages solved 32/32 tasks with no false
  completion claims. The existing workflow solved 24/32 with 8 false claims, and keyword retrieval
  28/32 with 4. Cost per correctly verified task fell 24% against the existing workflow (25,250 vs
  33,386 tokens), entirely because more tasks were right. Tokens per run were unchanged.

**Hypothesis (not measured):** how often real Echelon work items depend on records they do not cite.
The Praxis measurement (796 real work items: 9.7% cite records in structured fields, 25.3% only in
prose, 65.1% not at all) suggests the "amendment not cited" situation is common. But no real work item
was run through the benchmark.

The evidence supports a **staged, advisory integration**: compile packages for agents, attach
manifests as evidence, and leave completion and verification decisions with Praxis and Dokimos. It
does not yet support making EKS a gate.

## Decision gates

| # | Component | Gate | Evidence | Correctness | Completeness | Security |
|---|---|---|---|---|---|---|
| 1 | Work-item packages (`eks work-item`), including incoming requirement edges | **ADOPT** (advisory, opt-in) | hard trials: 32/32 vs 24/32, 2 models × 2 seeds; base: no harm beyond +3% tokens | unit tests; 0 base-test failures in any run | derived from listed conditions; INCOMPLETE for drafts, missing links and missing criteria | packages state authority, quote records as data; injection followed 0/44 |
| 2 | Derived completeness, rule-run records, package manifests | **ADOPT** | adversarial unit tests (skipped mandatory rule ⇒ INCOMPLETE; completeness ⇔ conditions) | tested | tested | n/a |
| 3 | Incremental compilation with a parse cache keyed by (file hash, compiler version, parse configuration) | **ADOPT** | 0 stale of 1,525 packages over 60 edits; 2.6× faster than full | benchmark + regression test | full-compile equivalence | n/a |
| 4 | Transactional change manifest (`eks change-manifest`), verdict `rejected` or `requires-review`, never approved | **ADOPT** (attach to knowledge PRs) | 3/3 defective change sets caught, 0 flags on 17 correct | unit tests | covers files, records, diagnostics, impact | cannot approve by construction |
| 5 | Source-existence rule fix; unverifiable sources reported | **ADOPT** | planted-violation tests; +3 real warnings on the public corpus | tested | no silent skips | n/a |
| 6 | CI denylist privacy check | **ADOPT, insufficient alone** | 0/6 historical leak lines caught | — | — | must be paired with #7 |
| 7 | Local private-name check (`eks privacy-check-private`) | **ADOPT** for any research that reads private repositories | caught the historical lines with private names; found 3 live leaks | tested on history | local only | never prints names |
| 8 | Dokimos obligations from declared `verified_by` bindings | **EXPERIMENTAL** | parsed by the real Dokimos CLI; reports `not-run` without results | mechanically correct | bindings coarse (per requirement); binding correctness unreviewed | cannot mark anything verified |
| 9 | `requires-review` for accepted evidence with title-only sources (as a compiler rule) | **EXPERIMENTAL** | would have flagged 2/2 trial fabrications; implemented in the change manifest, not as a rule | — | false-positive rate on real corpora unmeasured | — |
| 10 | Verification/integrity policy for agents (condition D) | **INCONCLUSIVE** | no outcome difference from C (both 100%); +1.5% tokens; more agent-written tests | — | — | — |
| 11 | Independent evidence review by a second agent | **INCONCLUSIVE** | reviewers had nothing to catch | — | — | — |
| 12 | Keyword (BM25) retrieval instead of compiled packages | **REJECT** as a substitute | 28/32, missed reference-linked amendments | — | misses incoming links | — |
| 13 | Package memoisation keyed by item files | **REJECT** | stale on 60/60 steps | fails | — | — |
| 14 | On-demand compilation of the outgoing closure | **REJECT** | wrong before any edit; slower | fails | misses incoming edges | — |
| 15 | Fully keyed package memo (hybrid) | **REJECT** for now | correct but slower than not memoising | ok | ok | — |
| 16 | EKS as a completion gate in Praxis | **INCONCLUSIVE** (do not build yet) | no real-repository trial | — | — | — |

Phase 1 rejections stand: DSL, F#-source storage, JSON storage, graph database, central service,
central repository.

No component is adopted for speed alone. #3 is adopted because it was never stale, and its speed is
secondary. No component trades missed requirements for tokens. The one that reduces tokens per
correct task (#1) does so by reducing missed requirements.

## Architecture

```
Markdown + YAML records (canonical, in each repository)
        │  parse (cached by hash + compiler version + parse config)
        ▼
EKS compiler ── diagnostics, rule-run records, derived links and closures
        │
        ├─ eks work-item REPO WI      → package (+ manifest) for agents   [advisory]
        ├─ eks change-manifest B H    → reviewable change evidence         [advisory]
        └─ eks dokimos-obligations    → dokimos.verification-obligations/1 [experimental]
                                                    │
Praxis (work items, completion gates)     Dokimos (decides verification from results)
```

EKS never decides semantic support, verification or completion. It makes them checkable.
