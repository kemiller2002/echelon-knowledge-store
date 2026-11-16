---
id: RQ-FXID-0001
title: Roles are flat, not hierarchical
status: accepted
created: 2026-10-01
---

# Roles

The roles are Viewer, Clerk, Approver and Admin. Roles are **not hierarchical**:
holding Admin does not grant Approver, Clerk or Viewer permissions. A permission
check must name every role it accepts.

## Acceptance

- `hasRole Approver` is false for a session that holds only Admin.
