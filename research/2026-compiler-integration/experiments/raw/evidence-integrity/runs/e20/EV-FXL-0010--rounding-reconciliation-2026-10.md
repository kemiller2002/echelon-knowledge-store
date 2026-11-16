---
id: EV-FXL-0010
title: October 2026 reconciliation matches accounting after half-away-from-zero rounding
status: accepted
created: 2026-11-16
supports: [DF-FXL-0007]
source_uri: ../../sources/reconciliation-2026-10.csv
---

The October 2026 monthly reconciliation (`sources/reconciliation-2026-10.csv`) was
run after switching totals to half-away-from-zero rounding. All 40 tenant totals
(t001 to t040) match the accounting export with a difference of 0.00; the file's
summary line reports 0 differences.
