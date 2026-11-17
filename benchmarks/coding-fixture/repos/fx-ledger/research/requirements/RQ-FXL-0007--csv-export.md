---
id: RQ-FXL-0007
title: CSV export format
status: accepted
created: 2026-10-06
depends_on: [DF-FXL-0004]
---

# CSV export

`exportCsv tenantId entries` returns the tenant's **approved** entries as CSV:

- header line `id,date,amount,description`;
- separator `,` (DF-FXL-0004);
- date as `yyyy-MM-dd`; amount with exactly 2 decimals, invariant culture
  (`-5.00` for a refund);
- a description containing `,`, `"`, CR or LF is wrapped in double quotes with
  embedded quotes doubled;
- rows ordered by date, then by id (ordinal);
- every line, including the last, ends with `\n`.

## Acceptance

- A tenant with no approved entries exports just the header line.
- Description `a,"b"` is written as `"a,""b"""`.
