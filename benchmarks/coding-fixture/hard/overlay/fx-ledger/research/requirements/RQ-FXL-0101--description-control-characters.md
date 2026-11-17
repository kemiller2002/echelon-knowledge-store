---
id: RQ-FXL-0101
title: Amendment to entry validation - no control characters in descriptions
status: accepted
created: 2026-10-06
depends_on: [RQ-FXL-0001]
---

# Control characters

Amends RQ-FXL-0001. After trimming, a description containing any control
character (`Char.IsControl`, e.g. tab or line feed) is rejected with
`InvalidDescription`. The rule is checked together with the length rule, so it
keeps the description's position in the DF-FXL-0001 order.

## Acceptance

- `"a\tb"` and `"a\nb"` are rejected with `InvalidDescription`.
