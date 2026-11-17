---
id: RQ-FXL-0104
title: Amendment to approval - high-value entries need Approver and Admin
status: accepted
created: 2026-10-07
depends_on: [RQ-FXL-0004]
---

# High-value approvals

Amends RQ-FXL-0004. Approving an entry whose amount is greater than 10,000
requires the session to hold **both** `Approver` and `Admin`; otherwise the
result is `NotAuthorized`. This check runs after the entry is found (step 3) and
before the self-approval check (step 4).

## Acceptance

- An Approver without Admin approving an entry of 10,000.01 gets `NotAuthorized`.
- An entry of exactly 10,000 needs only Approver.
