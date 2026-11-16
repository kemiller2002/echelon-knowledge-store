# Optimisation passes

Implemented in `Optimize.fs`; each pass returns `(items, ledger)`. Invariant,
checked for every package: every input item is in the output or in the ledger
(`Optimize.unaccounted` = ∅).

| Pass | Effect | Preservation rule |
|---|---|---|
| supersession elimination | superseded record replaced in active sections by its current replacement; original moved to History | history kept; ledger names old and new |
| deduplicate | a record selected by several rules appears once, in its highest-priority section | ledger lists the other sections |
| vendored copies | byte-identical records with the same id collapse to one (never the target) | ledger; collapse only when bodies are identical |
| cards | bodies replaced by cards except for the target | card keeps status, links, source path and hash |
| canonical id resolution | done by the resolver (`repo:ID`) | unresolved targets surfaced, not removed |
| dead-reference detection | unresolved typed references listed in the package and make it INCOMPLETE | never hidden |

Measured on all 587 public records (`experiments/optimization-correctness.md`):
261 ledger entries, 0 items lost without a ledger entry, 0 targets lost (after
fixing a defect that collapsed a governance target with itself — found by this check).

Not implemented: common-subexpression reuse across packages (shared cards are
cheap at this scale), cross-package caching.
