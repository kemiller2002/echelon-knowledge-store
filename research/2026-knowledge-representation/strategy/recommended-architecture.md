# Recommended architecture

**Compiler-first knowledge delivery, implemented as an extension of the existing
Praxis pipeline — EXPERIMENTAL, staged adoption.**

```
Markdown + front matter (owned per repository, reviewed on main)
   └─ praxis validate / registry build   ← exists today
        + EKS compiler stages (prototype in this repository):
          · calibrated rules with codes, levels, locations (EKS0001–0026)
          · derived inverse links, closures, current authority (partial evaluation)
          · manifest with content hashes; refusal on stale artefacts
          · task packages, evidence bundles, impact reports, contracts (on demand)
   └─ agents: `eks …` / `praxis …` commands; raw Markdown remains readable
```

F# role: implementation and schema language of the compiler (unions, records,
exhaustive matching, nullness, strict per-kind types). Not a storage format; the F#
compiler is not used to validate knowledge data.

Retrieval: exact id lookup → typed queries over the compiled graph; BM25 only to
produce *candidates* for description queries, never as the answer to relational
questions.

## Why (measured)

| Benefit | Evidence |
|---|---|
| Same accuracy as the best alternatives | T1 1.00; T2 1.00 after fix (A 0.99, C 0.99) |
| Half the agent time, fewer tool calls | T1 22 vs 44 s; T2 15 vs 31 s; 5 vs 8 calls |
| Catches agent edit mistakes | 22 of 26 mutations (15 blocking) |
| Removes drift of hand-maintained views | 5 of 6 index documents stale today |
| Encodes task semantics once | H01: compiler 1.00, agents from source 0.89 |
| Cheap | compile < 0.4 s cold, ~0.05 s warm; package ~0.5 ms |

## Why not more

No token savings end-to-end; accuracy already at ceiling with Markdown + front matter
for capable agents; compiler defects become confident wrong answers. Hence: stage it,
keep raw sources reachable, test every rule, and measure each stage (roadmap).

## Why simpler alternatives were not chosen outright

* **Markdown + grep only (A):** equally accurate, but twice the agent time, no
  validation of agent edits, and stale hand-written views remain.
* **Registries only (C, Praxis today):** equally accurate; lacks inverse links,
  closure, diagnostics and staleness checks — these are exactly the additions
  recommended, as stage 1.
* **Optimised Markdown retrieval (B):** 0.47 on relational questions.
