---
id: RQ-FXL-0002
title: Added entries are stored trimmed and unapproved
status: accepted
created: 2026-10-03
depends_on: [RQ-FXL-0001]
---

# Stored form of a new entry

An accepted entry is appended to the ledger with its description trimmed,
`Approved = false` and `ApprovedBy = None`, whatever the caller supplied.
Existing entries are unchanged and keep their order.

## Acceptance

- Adding an entry with `Approved = true` and description `"  rent "` stores
  `Approved = false`, `ApprovedBy = None`, description `"rent"`, appended last.
