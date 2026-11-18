# WORKLOG - bug #57

## Scope
Fix bug #57: back-to-back bookings (9:00-10:00, 10:00-11:00) are reported as overlapping by `booking.ranges.overlaps`.

## Canonical records inspected
- TASK.md, docs/MODEL.md (intervals are half-open [start, end); `booking.interval` holds primitive predicates that `ranges` and `conflicts` build on).
- booking/interval.py, booking/ranges.py, booking/conflicts.py, tests/test_ranges.py. No git history available (not a git repo).

## Findings / unknowns / risks
- `overlaps` just delegates to `interval.intersects`, which uses `<=` (closed-interval semantics). This contradicts the documented half-open model and its own docstring ("share at least one instant"): [540,600) and [600,660) share no instant.
- Root cause is therefore in `intersects`, not `overlaps`. `intersects` is also used by `conflicts()` (nightly report) and `merge_ranges()` (calendar view).
- Risk: fixing `intersects` changes `conflicts()` output (back-to-back pairs no longer reported) - this is the same bug and is the documented-correct behaviour. `merge_ranges` also checks `adjacent`, so back-to-back blocks still merge (calendar view unchanged).
- Alternative (patch only `overlaps`) would leave the nightly report wrong and the primitive inconsistent with MODEL.md; rejected.

## Assumptions
- MODEL.md (half-open intervals) is the canonical policy; the code is wrong, not the doc.
- Fixing the primitive is within scope of "fix overlaps" since overlaps is defined as intersects.

## Acceptance criteria
- `overlaps(540-600, 600-660)` is False; genuinely overlapping intervals still True.
- `conflicts()` does not report back-to-back bookings; still reports real clashes.
- `merge_ranges` still merges back-to-back ranges into one block.
- Full test suite passes.

## Baseline
`python3 -m pytest -q -p no:cacheprovider` -> 1 failed, 2 passed (test_back_to_back_bookings_do_not_overlap fails, as expected).

## Handoff
- Objective: fix bug #57 (back-to-back bookings reported as overlapping).
- Work completed: fixed root cause in `booking/interval.py::intersects` (`<=` -> `<`) so it matches the documented half-open model; `overlaps` (which delegates to it) is now correct, as is `conflicts()`. Added regression tests for the primitive, `conflicts()`, and a non-adjacent merge case.
- Files changed: booking/interval.py, tests/test_ranges.py, tests/test_conflicts.py (new), WORKLOG.md, REPORT.json.
- Decisions: fixed the shared primitive rather than special-casing `overlaps`; this also changes `conflicts()` (nightly report) to stop flagging back-to-back bookings, which is the same bug per MODEL.md. `merge_ranges` behaviour unchanged (still merges via `adjacent`). No policy/doc changes.
- Tests: `python3 -m pytest -q -p no:cacheprovider` -> 6 passed (baseline was 1 failed, 2 passed). No other checks (lint/type) exist in the repo; none run.
- Unresolved questions: none blocking. Front desk / report owners may want to know the nightly conflict report will no longer list back-to-back pairs.
- Risks: any unseen consumer relying on closed-interval `intersects` would change behaviour; none exist in this repo.
- Next action: review and merge; notify nightly-report consumers of the corrected behaviour.
