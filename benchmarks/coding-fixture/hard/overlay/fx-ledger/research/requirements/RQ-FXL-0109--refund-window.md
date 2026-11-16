---
id: RQ-FXL-0109
title: Amendment to refunds - 90-day refund window
status: accepted
created: 2026-10-09
depends_on: [RQ-FXL-0009]
---

# Refund window

Amends RQ-FXL-0009. A refund is rejected with `Unsupported "refund window closed"`
when `today` is more than 90 days after the original entry's date. The check runs
after the original is found (step 3) and before the duplicate-id check (step 4).

## Acceptance

- Original dated 90 days before today: refund allowed; 91 days: rejected.
