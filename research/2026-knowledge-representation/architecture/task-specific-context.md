# Task-specific context

Implementation: `Context.select` / `Context.compile`. Results:
`experiments/context-delivery-results.md`.

| Task | Package contents (priority order) |
|---|---|
| Implement (requirement) | target (full) → dependencies (closure, cards) → evidence (cards) → supersession chain |
| Evidence review / research | target (full) → supporting and contradicting evidence (full) → supersession |
| Decision history | target (full) → whole supersession chain (full) |
| Impact analysis | target (card) → transitive impact set with hop distance (cards) |
| Description query | top-k candidate cards (no single silent guess) |
| Missing evidence | cards of all claims in scope with derived evidence links |

Designed, not implemented: defect repair (requirement + tests + recent decisions),
PR review (records touched by changed files via `implements`), security evaluation
(security-relevant decisions + threats), documentation (records by audience).

Budgeting: degrade full → card from the lowest priority, then drop with an explicit
`OMITTED` list. Minimum sufficient context, never minimum context: the package
for an impact question includes every impacted record as a card even when that
is large.
