---
id: RQ-FXID-0002
title: Sessions expire after 15 minutes idle or 8 hours absolute
status: accepted
created: 2026-10-01
---

# Session expiry

A session is active while it has been idle for **at most 15 minutes** (exactly
15 minutes is still active) and is **at most 8 hours** old.

## Acceptance

- A session idle for exactly 15 minutes is active; at 15 minutes and 1 second it is expired.
- A session older than 8 hours is expired even if it was used recently.
