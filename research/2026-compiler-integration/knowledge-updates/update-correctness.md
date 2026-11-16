# Update correctness

## Change impact (precision and recall against search)

Question: when a record changes, which other records' requirements change with it? Ground truth
for the fixture was labelled by hand from record bodies and front matter: a record is impacted if
its required behaviour depends, directly or transitively, on the changed record. Records that merely
mention it (supporting evidence, superseded history) are not impacted. Eight targets, 13 impacted
records in total.

| Target | Truly impacted | `eks impact` | Text search for the id |
|---|---|---|---|
| fx-identity RQ-FXID-0002 (session expiry) | RQ-FXID-0004, RQ-FXL-0004, RQ-FXL-0006, RQ-FXL-0009 | all 4 | RQ-FXID-0004, RQ-FXL-0004 (misses the two that depend through RQ-FXID-0004) |
| fx-identity RQ-FXID-0001 (flat roles) | RQ-FXL-0004 | 1 | RQ-FXL-0004 + DF-FXID-0001 (mention) |
| fx-identity RQ-FXID-0004 | RQ-FXL-0004, RQ-FXL-0006, RQ-FXL-0009 | all 3 | all 3 |
| DF-FXL-0007 (rounding) | RQ-FXL-0003 | 1 | RQ-FXL-0003 + EV-FXL-0001 (mention) |
| DF-FXL-0004 (comma CSV) | RQ-FXL-0007 | 1 | RQ-FXL-0007 + DF-FXL-0003 (history) |
| RQ-FXL-0001 | RQ-FXL-0002 | 1 | RQ-FXL-0002 + DF-FXL-0001 (mention) |
| DF-FXL-0009 (rates source) | RQ-FXL-0008 | 1 | 1 |
| DF-FXL-0006 (refund cap) | RQ-FXL-0009 | 1 | RQ-FXL-0009 + EV-FXL-0003 (mention) |

| Method | Precision | Recall |
|---|---|---|
| `eks impact` | 13/13 = 100% | 13/13 = 100% |
| Text search | 11/16 = 69% | 11/13 = 85% |

Caveats: eight targets on a 23-record fixture; the ground truth uses the same `depends_on`
semantics that EKS follows, so EKS's perfect score shows it computes the closure correctly, not that
`depends_on` captures every real dependency. Cross-repository dependencies were not assumed: each
was declared in a record (`fx-identity:RQ-FXID-…`) and checked by hand.

## Incremental correctness

See `../architecture/incremental-compilation.md`: the parse cache was never stale across 60 edits ×
25 packages, and a configuration change with a warm cache now matches a fresh compile.

## Change-set correctness

See `write-path-design.md`: 3 of 3 defective change sets caught, 0 false flags on 17 correct ones.
