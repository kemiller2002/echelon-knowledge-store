# Optimisation correctness

Source: `results/compiler.md`; unit tests in `CompilerTests.fs`.

A task package was compiled for **every** public record (587) and checked:

| Measure | Result |
|---|---|
| Packages | 587 |
| Status COMPLETE | 586 (1 incomplete: the record with the unresolved id) |
| Ledger entries (supersession moves, deduplication, vendored collapses) | 261 |
| Items removed without a ledger entry | 0 |
| Targets lost | 0 — **after a fix**; the first run lost the target in 126 packages |
| Mean package size | 2,307 tokens |
| Mean compile time per package | 0.5 ms |

The defect: a governance record's own package listed it twice (Target and
Governance); the vendored-copy pass grouped the two entries as "identical copies"
and dropped both by key. Found by the target-preservation check, fixed by excluding
the target from its governance section and collapsing distinct records only, and
pinned by a unit test. The check is the point: an optimisation pass cannot be
trusted without a preservation oracle.

Supersession elimination never deletes: the superseded record moves to History,
its replacement takes its place, and the ledger names both (unit-tested).
