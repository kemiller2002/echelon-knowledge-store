---
id: RQ-FXL-0107
title: Amendment to CSV export - neutralise spreadsheet formulas
status: accepted
created: 2026-10-08
depends_on: [RQ-FXL-0007]
---

# Formula injection

Amends RQ-FXL-0007. A description that starts with `=`, `+`, `-` or `@` is
written with a leading apostrophe (`'`) before any quoting is applied.

## Acceptance

- Description `=SUM(A1)` is written as `'=SUM(A1)`.
- Description `-5,x` is written as `"'-5,x"`.
