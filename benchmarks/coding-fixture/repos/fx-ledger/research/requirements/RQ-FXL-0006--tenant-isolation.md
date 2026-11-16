---
id: RQ-FXL-0006
title: Users only ever see their own tenant's entries
status: accepted
created: 2026-10-05
depends_on: [DF-FXL-0005, fx-identity:RQ-FXID-0004]
---

# Tenant isolation

`view now session entries` returns `SessionExpired` when the session is not
active, otherwise the entries whose `TenantId` equals the session's tenant, in
their original order. No role, including `Admin`, widens this (DF-FXL-0005).

## Acceptance

- An Admin of t1 sees only t1 entries.
- An expired session gets `SessionExpired`.
