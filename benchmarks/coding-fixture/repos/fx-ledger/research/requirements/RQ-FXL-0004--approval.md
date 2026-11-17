---
id: RQ-FXL-0004
title: Approval requires an active session, the Approver role and another author
status: accepted
created: 2026-10-04
depends_on: [fx-identity:RQ-FXID-0001, fx-identity:RQ-FXID-0002, fx-identity:RQ-FXID-0004]
---

# Approving an entry

`approve now session entryId entries` checks, in this order:

1. the session is active (decided by `FxIdentity.Identity.isActive`, see
   fx-identity RQ-FXID-0004) → otherwise `SessionExpired`;
2. the session holds the `Approver` role (roles are flat, fx-identity
   RQ-FXID-0001: `Admin` alone is not enough) → otherwise `NotAuthorized`;
3. an entry with that id exists in the session's tenant → otherwise `NotFound`
   (an entry of another tenant is reported as `NotFound`, never `NotAuthorized`);
4. the entry was not created by the session's user → otherwise `SelfApproval`;
5. the entry is not already approved → otherwise `AlreadyApproved`.

On success the entry gets `Approved = true` and `ApprovedBy = Some session.UserId`;
nothing else changes.

## Acceptance

- A session idle for exactly 15 minutes can approve; at 15 minutes 1 second it gets `SessionExpired`.
- An Admin without Approver gets `NotAuthorized`.
