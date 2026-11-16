# fx-ledger (synthetic benchmark fixture)

A tiny multi-tenant ledger used by the EKS coding benchmark. Invented content;
not a real product.

- Code: `src/FxLedger/Ledger.fs`
- Tests: `tests/FxLedger.Tests`
- Requirements, decisions and evidence: `research/`
- Work queue: `.ros/work/queue.json`

## Notes (last revised 2026-08)

- CSV exports use `;` as the separator so spreadsheets in every locale open them.
- Administrators can view entries of every tenant for support purposes.
- Legacy refunds: a refund may exceed the original when the customer was
  overcharged twice; support staff reconcile it later.
