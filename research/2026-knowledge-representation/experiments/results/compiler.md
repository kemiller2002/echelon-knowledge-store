Public corpus: 1615 Markdown files, 587 records. Timings are medians of 5 runs on the research container (single process).

| Scenario | Files | Median ms | Warm output = cold output |
| --- | --- | --- | --- |
| cold (parse + graph + validate + precompute) | 1615 | 322.3 | – |
| parse only (all files) | 1615 | 82.7 | – |
| warm, no change | 1615 | 42.3 | True |
| warm, 1 file changed | 1615 | 39.7 | True |
| warm, 10% of files changed | 1615 | 52.0 | True |
| cold (parse + graph + validate + precompute) — all 13 repositories | 1931 | 159.4 | – |

## Diagnostics (public corpus)

| Code | Level | Count |
| --- | --- | --- |
| EKS0002 | Error | 1 |
| EKS0007 | Warning | 3 |
| EKS0008 | Advisory | 18 |
| EKS0008 | RequiresReview | 1 |
| EKS0009 | Warning | 7 |
| EKS0010 | RequiresReview | 5 |
| EKS0011 | Error | 2 |
| EKS0012 | Advisory | 28 |
| EKS0016 | Advisory | 26 |
| EKS0017 | RequiresReview | 1 |
| EKS0022 | Warning | 4 |
| EKS0024 | Warning | 4 |

## Agent-edit mutation test (25 + 1 mistakes from the adversarial review)

| Mutation | Record | Result | Codes |
| --- | --- | --- | --- |
| field-name typo supporting_evidence->supporting_evidences | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0022 |
| status typo proposed->propsed | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0023 |
| status set to supported (1 supports, 2 contradict) | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | FLAGGED | EKS0010 |
| confidence invalid label (hgih) | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | FLAGGED | EKS0024 |
| confidence numeric 0.8 | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | FLAGGED | EKS0024 |
| confidence raised very-low->high, no new evidence | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | UNDETECTED |  |
| evidence id typo B003->B030 | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0002 |
| evidence id swapped to real but wrong record | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | FLAGGED | EKS0007, EKS0012 |
| evidence id lower-cased | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0002 |
| duplicate status key appended | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0011 |
| unquoted colon in title | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0011 |
| id field deleted | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0001 |
| contradicting evidence removed from list only | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | UNDETECTED |  |
| relation stated in prose only | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | UNDETECTED |  |
| superseded_by cleared, status left superseded | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | BLOCKING | EKS0013, EKS0021 |
| status reverted to accepted, superseded_by kept | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | FLAGGED | EKS0010 |
| superseded_by -> nonexistent A099 | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | BLOCKING | EKS0002, EKS0021 |
| superseded_by -> self | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | FLAGGED | EKS0003, EKS0021 |
| list item indentation broken | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | BLOCKING | EKS0002, EKS0021 |
| superseded_by written as scalar (benign) | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | UNDETECTED |  |
| superseded_by key mis-cased | praxis/DF-ROS-2026-A031--fsharp-starter-template-launcher.md | BLOCKING | EKS0013, EKS0021, EKS0022 |
| derived_from typo -> derive_from | forma/HY-FORMA-2026-0002--layout-physics-improves-agent-layout-correctness.md | BLOCKING | EKS0022 |
| derived_from -> dependsOn | forma/HY-FORMA-2026-0002--layout-physics-improves-agent-layout-correctness.md | BLOCKING | EKS0022 |
| status rejected->refuted | forma/HY-FORMA-2026-0002--layout-physics-improves-agent-layout-correctness.md | BLOCKING | EKS0023 |
| contradicting evidence moved to supporting | forma/HY-FORMA-2026-0002--layout-physics-improves-agent-layout-correctness.md | FLAGGED | EKS0010, EKS0012 |
| copy with id unchanged (duplicate id) | praxis/HY-FE-2026-B006--integrated-profile-incremental-value.md | BLOCKING | EKS0018 |

Blocking 15, flagged 7, advisory-only 0, undetected 4 of 26.

## Task packages for every record (optimisation correctness)

| Packages | Complete | Ledger entries | Lost without ledger | Target lost | Mean tokens | Mean compile ms |
| --- | --- | --- | --- | --- | --- | --- |
| 587 | 586 | 261 | 0 | 0 | 2307.3 | 0.52 |

## Source-level losslessness (original front matter vs regenerated)

555/557 records regenerate an identical front-matter tree.

| Field differing | Records |
| --- | --- |
| superseded_by | 1 |
| supersedes | 1 |

## Reference resolution

| Relations | Resolved | Unresolved | …id-like | …paths | …other (INPUT-*, commits, prose) |
| --- | --- | --- | --- | --- | --- |
| 1888 | 1325 | 563 | 1 | 532 | 30 |
