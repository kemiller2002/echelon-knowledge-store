---
id: RQ-FXL-0009
title: Refunds never exceed what remains of the original entry
status: accepted
created: 2026-10-08
depends_on: [DF-FXL-0006, fx-identity:RQ-FXID-0004]
---

# Refunds

`refund now session today refundId originalId amount entries` checks, in order:

1. active session (`isActive`) → otherwise `SessionExpired`;
2. the session holds `Clerk` or `Admin` → otherwise `NotAuthorized`;
3. the original exists in the session's tenant, is approved and is not itself a
   refund → otherwise `NotFound`;
4. no entry already has id `refundId` → otherwise `DuplicateId`;
5. `amount` is greater than 0 → otherwise `InvalidAmount`;
6. `amount` plus the amounts of earlier refunds of the same original does not
   exceed the original amount → otherwise `RefundExceedsOriginal` (DF-FXL-0006).

On success a new entry is appended: `Id = refundId`, the session's tenant,
`Amount = -amount`, `Description = "Refund of <originalId>"`, `Date = today`,
`CreatedBy = session.UserId`, `Approved = false`, `ApprovedBy = None`,
`RefundOf = Some originalId`.

## Acceptance

- Original 100: refunds of 60 then 40 succeed; a further 0.01 is rejected.
- A refund larger than the original is rejected, never accepted for later reconciliation.
