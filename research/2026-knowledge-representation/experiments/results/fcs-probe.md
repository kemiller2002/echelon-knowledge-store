
Records: 416 (unique ids), generated source lines: 2110, relation targets skipped (not record ids): 17
Warm-up check (empty corpus): 1872 ms
Type-check full corpus: 0 errors, first run 777 ms, second run 65 ms

| Mutation | Result | First error | Check ms |
|---|---|---|---|
| status typo (Proposed → Propsed) | COMPILE ERROR | FS0039: The value or constructor 'Propsed' is not defined. | 507 |
| relation id typo (B006 → B060) | COMPILE ERROR | FS0039: The value or constructor 'HY_FE_2026_B060' is not defined. | 262 |
| relation id lower-cased | COMPILE ERROR | FS0039: The value or constructor 'hy_fe_2026_b006' is not defined. | 339 |
| field typo (Supports → Support) | COMPILE ERROR | FS1129: The record type 'Record' does not contain a label 'Support'. | 261 |
| confidence invalid (VeryLow → Hgih) | COMPILE ERROR | FS0039: The value or constructor 'Hgih' is not defined. | 342 |
| status set to Supported despite contradiction | accepted |  | 348 |
| self reference in SupersededBy | accepted |  | 330 |
| evidence moved from Contradicts to Supports | mutation did not apply |  | 341 |
| dependency cycle (A ← B, B ← A) | mutation did not apply |  | 62 |
