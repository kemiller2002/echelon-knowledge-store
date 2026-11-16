---
id: RQ-FXL-0001
title: Entries are validated before they are added
status: accepted
created: 2026-10-03
depends_on: [DF-FXL-0001]
---

# Entry validation

`addEntry` rejects an entry when:

- the amount is not greater than 0 or is greater than 1,000,000 → `InvalidAmount`;
- the description, after trimming whitespace, is empty or longer than 200
  characters → `InvalidDescription`;
- the date is after `today` → `FutureDate`;
- an entry with the same `Id` already exists (any tenant) → `DuplicateId`.

When several rules fail, the error reported follows DF-FXL-0001.

## Acceptance

- Amount 1,000,000 is accepted; 1,000,000.01 and 0 are rejected with `InvalidAmount`.
- A description of 200 characters after trimming is accepted; 201 is rejected.
- An entry dated today is accepted; tomorrow is rejected with `FutureDate`.
