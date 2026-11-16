# Diagnostics

Catalogue (`Diagnostics.fs`, experimental; reviewed against the corpus, not adopted):

| Code | Level | Meaning |
|---|---|---|
| EKS0001 | Error | file in a record directory has no id |
| EKS0002 | Error | unresolved or mis-cased reference |
| EKS0003 | Warning | invalid relationship (e.g. self-reference) |
| EKS0004 | Error | unsupported schema version |
| EKS0005 | Error | missing required provenance |
| EKS0006 | Error | dependency cycle |
| EKS0007 | Warning | support from / reference to a superseded record |
| EKS0008 | Advisory / Review | id defined in several repositories (identical vendored copies / differing texts) |
| EKS0009 | Warning | requirement without acceptance criteria |
| EKS0010 | Review | potentially stale: status disagrees with evidence, or evidence older than horizon |
| EKS0011 | Error | front matter cannot be parsed |
| EKS0012 | Advisory | link declared on one side only |
| EKS0013 | Error | superseded without replacement |
| EKS0014 | Error | claim asserts support without evidence |
| EKS0015 | Review | record text addresses agents with instructions |
| EKS0016 | Advisory | missing status |
| EKS0017 | Review | contested claim (suspected contradiction, not proven) |
| EKS0018 | Error | duplicate id within a repository |
| EKS0019 | Warning | high confidence without supporting evidence |
| EKS0020 | Error | private knowledge would flow to a public audience |
| EKS0021 | Review | supersession claimed by one side only |
| EKS0022 | Error / Warning | unknown field resembling a known field |
| EKS0023 | Error | status not allowed by the record kind's schema |
| EKS0024 | Warning | confidence outside the standard scale |
| EKS0025 | Warning | unrecognised status value |
| EKS0026 | Warning | experiment names no hypothesis |

Every diagnostic carries file, line (field line where known) and remediation.

## Calibration (measured on the public corpus)

First run: 36 errors — 33 false positives (Praxis `INPUT-*` input ids, file
paths, commit SHAs, starter templates; `related` suggested as `created`).
After calibration: **3 errors, all true** (one dangling id in dokimos; two praxis
records whose front matter is invalid YAML), 18 warnings, 7 review items, 72
advisories. Rules must be calibrated against the corpus before they block.

## Detection of agent edit mistakes (26 mutations; `experiments/results/compiler.md`)

| | v1 validator (adversarial review) | Compiler |
|---|---|---|
| Blocking | — | 15 |
| Flagged (warning / review) | — | 7 |
| Undetected | 13 of 25 (52%) | 4 of 26 |

Still undetected (by design or by limits of structure): confidence raised without
new evidence; a contradicting link removed from one side when the other side never
declared it (detectable by `eks diff`, not by validation); a relation stated only in
prose; a scalar written instead of a one-element list (benign).

Dokimos interop: Dokimos defines a fatal diagnostic envelope
(`dokimos.diagnostic`, State `error`, Code, Message). EKS errors can be emitted in
that envelope for gate failures; warnings and reviews have no Dokimos equivalent.
