# Semantic preservation

| Transformation | Check | Result |
|---|---|---|
| Markdown → IR → canonical Markdown → IR | all fields incl. unrecognised | 568/568 equal |
| Original front-matter tree vs regenerated tree (source level) | field-by-field | 555/557 identical; 2 differ only by scalar→one-element list for `supersedes`/`superseded_by` |
| Confidence labels outside the scale (`medium-high`, 4 records) | preserved verbatim since the fix (`Label`) | the earlier "lossless" claim was false for these 4 records (adversarial review O7); fixed |
| Files with invalid front matter | kept as identity-only records + EKS0011 | earlier versions dropped them silently (replication finding); fixed |
| Optimisation passes | preservation oracle (every input item in output or ledger) | 0 silent losses over 587 packages; one target-loss defect found and fixed |
| Supersession elimination | superseded record retained in History | unit-tested |
| Incremental build | warm = cold | equal |
| Generated index | deterministic; one status edit → one changed line | measured |

Lessons: (1) round-trip tests that go through the same parser twice cannot detect
what that parser drops — compare against the original source; (2) every optimisation
needs an oracle; (3) completeness labels must be backed by tests that every rule runs.
