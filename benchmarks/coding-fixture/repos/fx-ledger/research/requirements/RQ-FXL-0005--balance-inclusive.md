---
id: RQ-FXL-0005
title: Balance includes entries dated on the cut-off date
status: accepted
created: 2026-10-05
verified_by:
  - dotnet:FxLedger.Tests.LedgerTests.balance counts approved entries well before the cut-off
  - dotnet:FxLedger.Tests.LedgerTests.balance excludes entries after the cut-off
---

# Balance cut-off

`balance tenantId until entries` sums the tenant's approved entries dated **on or
before** `until`.

## Acceptance

- An approved entry dated exactly `until` is included.
- Entries after `until`, unapproved entries and other tenants are excluded.
