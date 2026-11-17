---
id: RQ-FXID-0004
title: Applications use FxIdentity.Identity.isActive for session checks
status: accepted
created: 2026-10-02
depends_on: [RQ-FXID-0002]
---

# One expiry implementation

Applications must decide whether a session is usable by calling
`FxIdentity.Identity.isActive`. They must not re-implement the timeouts.

## Acceptance

- Application authorization code calls `isActive` and contains no timeout constants.
