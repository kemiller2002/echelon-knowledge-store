# Knowledge compilation results

Question: can one authoritative record compile into several representations
consistently, and does that do more than format conversion?

## Measured

| Check | Result |
|---|---|
| Markdown → IR → canonical Markdown → IR, all fields incl. unrecognised ones | 568/568 identical |
| IR → JSON / JSONL / YAML / DSL → IR (core view) | 568/568 identical each |
| Generated index of 585 records | 63,021 tokens; compiled in 63–76 ms |
| Determinism (input order permuted) | **initially false** (link order depended on input order); fixed by sorting; now true and unit-tested |
| Diff locality: one status edit | 1 changed line in the generated index |
| Hand-maintained index views vs records | 5 of 6 index documents omit existing records (3/52, 7/16, …) |
| Compiled context packages vs raw corpus (agent trials) | equal accuracy, 2–3× less wall time |

## What compilation adds beyond conversion

1. **Consistency of derived views.** Hand-maintained indexes drift (5 of 6).
   Generated views cannot omit a record that exists.
2. **Single direction of truth for links.** 28 links are declared on one side
   only; a compiler derives the inverse instead of storing it twice.
3. **Task-specific delivery.** Cards and packages cut tokens ~10× relative to
   bodies at equal recall (`ablation-results.md`).
4. **Validation as a build step.** Dangling references, cycles and missing
   replacements fail the build instead of misleading agents.

## Not measured

* Dokimos validation obligations, Praxis work dependencies and ADR rendering as
  compiler targets: designed (`architecture/knowledge-compilation.md`), not built.
* Human review effort for generated vs hand-written Markdown.
* Incremental compilation: the full public corpus compiles in < 0.1 s, so it is
  not needed at the current scale.
