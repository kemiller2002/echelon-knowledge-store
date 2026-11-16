---
id: RQ-FXL-0003
title: Tenant total sums approved entries, rounded half away from zero
status: accepted
created: 2026-10-03
depends_on: [DF-FXL-0007]
---

# Tenant total

`total tenantId entries` is the sum of the amounts of that tenant's **approved**
entries, rounded to 2 decimal places with midpoints rounded **away from zero**
(DF-FXL-0007). Refund entries (negative amounts) are included.

## Acceptance

- Approved 0.005 alone totals 0.01; approved -0.005 alone totals -0.01.
- Unapproved entries and other tenants' entries do not count.
