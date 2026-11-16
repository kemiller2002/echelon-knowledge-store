# Coding task catalog

Fixture: `benchmarks/coding-fixture/` (synthetic, public). Two repositories: `fx-identity`
(sessions, flat roles) and `fx-ledger` (a multi-tenant ledger that depends on it). Eight work
items in Praxis queue shape (`fx-ledger/.ros/work/queue.json`). Each maps to one function in
`fx-ledger/src/FxLedger/Ledger.fs` and to one module of hidden tests.

| Task | Work item | Function | Records | Hidden tests (base / hard) | Categories covered |
|---|---|---|---|---|---|
| T1 | WI-0001 | `total` | RQ-FXL-0003, DF-FXL-0007 (half away from zero), superseded RQ-FXL-0103 (hard) | 5 / 5 | A small, B requirement-heavy (rounding rule lives in a decision) |
| T2 | WI-0002 | `addEntry` | RQ-FXL-0001/0002, DF-FXL-0001 (error order); hard: RQ-FXL-0101 amendment | 6 / 8 | B requirement-heavy, error precedence |
| T3 | WI-0003 | `approve` | RQ-FXL-0004 → fx-identity RQ-FXID-0001/0002/0004; hard: RQ-FXL-0104 amendment | 7 / 10 | C cross-repository, E security (authorization), F architecture (must call `isActive`) |
| T4 | WI-0004 | `balance` (bug) | RQ-FXL-0005; hard: proposed RQ-FXL-0105 decoy | 2 / 2 | D regression (visible tests must keep passing) |
| T5 | WI-0005 | `view` | RQ-FXL-0006, DF-FXL-0005 superseding DF-FXL-0002, stale README | 3 / 3 | E security (tenant isolation), G conflicting documentation |
| T6 | WI-0006 | `exportCsv` | RQ-FXL-0007, DF-FXL-0004 superseding DF-FXL-0003, stale README; hard: RQ-FXL-0107 formula injection, rejected RQ-FXL-0108 decoy | 3 / 4 | G conflicting documentation, E security (formula injection, hard) |
| T7 | WI-0007 | `convertCurrency` | proposed RQ-FXL-0008 → draft DF-FXL-0009 | 1 / 1 | H incomplete information (correct answer: do not implement) |
| T8 | WI-0008 | `refund` | RQ-FXL-0009, DF-FXL-0006, EV-FXL-0003 with an injected instruction, stale README; hard: RQ-FXL-0109 refund window | 6 / 8 | B requirement-heavy, I adversarial knowledge, G conflicting documentation |

Total hidden tests: 33 (base), 41 (hard). Reference solutions: `benchmarks/coding-fixture/reference/Ledger.fs`
(33/33 base) and `benchmarks/coding-fixture/hard/reference-Ledger.fs` (41/41 hard; the base reference
scores 35/41 on hard, so the six amendment tests discriminate).

## Variants

- **Base:** 23 records. Everything relevant is cited by the work item or one hop from it.
- **Hard:** base + 7 overlay records + about 300 generated distractor records (`CodingTrial.distractors`,
  deterministic per seed). Four accepted amendments reach their target requirement **only through
  an incoming `depends_on` edge**: the work item does not mention them. The distractors share
  vocabulary with the tasks and sometimes name target records in prose.

## Not covered

- **J long-running tasks.** Every run took 1–3 minutes. Nothing here measures context decay over
  long sessions.
- Real repositories. The fixture is designed, so its difficulty was chosen by the designer. The
  hard variant favours any method that follows incoming edges, by construction
  (`held-out-policy.md`, `experimental-design.md`).
