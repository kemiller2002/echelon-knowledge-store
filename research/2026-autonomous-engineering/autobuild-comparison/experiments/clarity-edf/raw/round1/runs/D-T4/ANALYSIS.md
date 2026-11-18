# Pre-implementation analysis: EDF (protocol D, v1) — bug #57

## 1. System Context
- Primary System: `booking/ranges.py::overlaps` and the primitive it delegates to, `booking/interval.py::intersects`.
- Context: `booking/conflicts.py::conflicts()` (used by the nightly report per docs/MODEL.md), `booking/ranges.py::merge_ranges()` (used by the calendar view), the documented half-open interval model in docs/MODEL.md, the pytest suite in tests/test_ranges.py, front-desk users. Build/CI: unknown (no CI config, not a git repository).
- Focus: correctness of the "shares time" predicate for touching (back-to-back) intervals under the half-open model.

## 2. Outcome
- Current: `overlaps(Interval(540,600), Interval(600,660))` returns True. `intersects` returns True whenever `a.start <= b.end and b.start <= a.end`, i.e. it treats intervals as closed. `conflicts()` therefore also reports back-to-back bookings as clashing.
- Required: under half-open `[start, end)` semantics, back-to-back bookings do not overlap (overlaps returns False); genuinely overlapping bookings still overlap; `merge_ranges` still coalesces back-to-back ranges into one block.

## 3. Manifestations
- Current: `test_back_to_back_bookings_do_not_overlap` fails (`assert not True`); the other 2 tests pass (command run: `python3 -m pytest -q -p no:cacheprovider` -> 1 failed, 2 passed). Front desk report of 9:00-10:00 / 10:00-11:00 conflict.
- Failure signals after change: that test still failing; `test_overlapping_bookings_overlap` or `test_merge_back_to_back` regressing; `conflicts()` still returning back-to-back pairs or dropping real overlaps.

## 4. Origin Network
- H1 (requester's framing): `overlaps` is wrong. Partially: `overlaps` is a one-line delegation to `intersects`; the defect lives in the primitive.
- H2 (leading): `intersects` uses `<=` (closed-interval comparison) contrary to the module docstring ("Intervals are half-open") and docs/MODEL.md. Origin is in `booking/interval.py`, a cause outside the blamed component.
- H3 (intentional tradeoff): inclusive `intersects` was chosen so `merge_ranges` merges touching ranges. Ruled out: `merge_ranges` already ORs with `adjacent()` for that purpose, and `intersects`' own docstring says "share at least one instant", which touching half-open intervals do not.
- H4 (no real problem / test artifact): ruled out; the test expectation matches the documented model (MODEL.md explicitly says 540-600 does not contain minute 600).
- Organizational contributor (inference): the bug was reported against the visible API (`overlaps`), not the shared primitive.

## 5. Propagation
- `intersects` -> `overlaps` -> front desk conflict check (manifestation).
- `intersects` -> `conflicts()` -> nightly report: same false positives (not yet reported, inferred).
- `intersects` -> `merge_ranges()`: if `intersects` becomes strict, touching ranges are still merged through `adjacent()`, so the calendar view is unchanged.
- Planned change to `intersects` propagates to all three callers; side effect outside the bug's literal scope: nightly `conflicts()` output stops listing back-to-back pairs (intended correction). Patching only `overlaps` would leave `conflicts()` inconsistent with `overlaps`.
- Edge: zero-length intervals (start == end) — behaviour under strict comparison: an empty interval strictly inside another would still "intersect". Whether empty bookings exist: unknown.

## 6. Evidence
- Direct: code read of interval.py (`<=` comparisons, half-open docstring), ranges.py (overlaps delegates; merge_ranges uses intersects OR adjacent), conflicts.py (uses intersects), MODEL.md (half-open, report uses conflicts, calendar merges back-to-back); test run 1 failed / 2 passed.
- Causal link `<=` in intersects -> overlaps True for touching intervals: causal (mechanism read directly; deterministic; rival "test is wrong" ruled out by MODEL.md).
- Link intersects -> nightly report false positives: inference by code reading (mechanism present), not observed in production output.
- H3 rival ruled out by direct reading of merge_ranges using `adjacent`.

## 7. Control Points
- CP1: change `intersects` to `a.start < b.end and b.start < a.end` (interval.py). Fixes overlaps and conflicts; merge_ranges unchanged via adjacent. High influence.
- CP2: change only `overlaps` (e.g. `intersects(a,b) and not adjacent(a,b)`). Fixes the reported symptom only; leaves `conflicts()` wrong and the primitive contradicting its docstring.
- CP3: change the test expectation. Rejected.
- CP4: add tests covering `conflicts()` back-to-back and `merge_ranges` overlapping cases to lock in behaviour.

## 8. Control Point Ranking
1. CP1 — preferred: corrects the established mechanism, cheap, reversible (one-line), consistent with docs under every live hypothesis.
2. CP4 — preferred as complement: cheap, guards propagation paths.
3. CP2 — premature/inferior: only reasonable if H3 (intentional inclusive primitive) were true, which evidence rules out; leaves a known defect in conflicts().
4. CP3 — harmful: weakens a test that matches the documented contract.

## 9. Confidence
High — the defect is a direct contradiction between code and the documented half-open model, and the only rival (merge relies on it) is ruled out by `adjacent`.

## 10. Diagnostic Sufficiency
Sufficient: further investigation would not change the action (fix `intersects`).

## 11. Unknowns
- Whether any external consumer depends on the inclusive `intersects` behaviour (no other callers in repo).
- Whether zero-length bookings can occur.
- CI configuration / deployment path.

## 12. Next Responsible Action
Change `intersects` to strict comparisons (CP1); add tests for `conflicts()` with back-to-back and overlapping bookings and for merging overlapping ranges (CP4). Leave `overlaps` delegating.
Verification: before — 1 failed/2 passed; after — full suite passes, including the reproducer and new tests. Recovery: revert the one-line comparison change.

FINAL
Diagnostic sufficiency: SUFFICIENT
Action: PROCEED
Confidence: HIGH
Key missing evidence: none
