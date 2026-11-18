# Pre-implementation analysis: bug #57

## 1. Decision
`booking.interval.intersects` will treat intervals as half-open (strict `<`), so back-to-back bookings no longer count as sharing time in `overlaps()` or `conflicts()`, while `merge_ranges()` still joins them via `adjacent()`.

## 2. Problem definition
- Requested: `overlaps(9:00-10:00, 10:00-11:00)` must be False; requester says "fix `overlaps`".
- Treated as true: the defect is not in `overlaps` (a one-line delegate in booking/ranges.py) but in `intersects` (booking/interval.py), which uses `<=`, i.e. closed-interval semantics, contradicting its own module docstring and docs/MODEL.md ("half-open ... 540-600 does not contain minute 600").
- Interpretation: "fix overlaps" means "make overlaps correct", not "edit only that function's body". The same bug therefore also affects `conflicts()` (nightly report).

## 3. Posture
Impact if wrong: moderate - `intersects` is shared by `overlaps`, `conflicts`, `merge_ranges`. Correctability: high (one-character, local, no data/schema). Trigger: codebase contradicts the request (bug location differs from the one named) and hidden blast radius on a shared primitive. Posture: GUARDRAIL - fix at root, protect `merge_ranges` behaviour with existing test and add tests for `conflicts` and edge cases.

## 4. Bottom line
Change `intersects` to `a.start < b.end and b.start < a.end`; add regression tests for `conflicts` and `merge_ranges` (overlapping and back-to-back). Probability correct and sufficient: 90%.

## 5. Load-bearing assumptions
- FACT: intervals are half-open (docs/MODEL.md; booking/interval.py docstring).
- FACT: `overlaps` only delegates to `intersects` (booking/ranges.py).
- FACT: `merge_ranges` merges on `intersects or adjacent`, so back-to-back merging survives a stricter `intersects` (booking/ranges.py).
- ASSUMPTION: `conflicts()` reporting back-to-back bookings is also a bug, not desired behaviour. If wrong, the nightly report would stop flagging back-to-back bookings someone relies on (e.g. for cleaning turnaround). MODEL.md defines conflict as clashing times, supporting the assumption.
- ASSUMPTION (shadow): no other callers of `intersects` outside this repo. If wrong, external callers change behaviour; unknown, not obtainable here.
- ASSUMPTION (shadow): zero-length intervals are not meaningful bookings. If wrong, strict semantics make an empty interval never intersect anything; unknown.

## 6. Missing information
- External users of `booking.interval` - unknown, only from requester.
- Whether the nightly report should flag back-to-back bookings - only from requester; docs indicate not.

## 7. Evidence check
- Failing test output (primary): `overlaps(Interval(540,600), Interval(600,660))` returns True. Recomputed: 540<=660 and 600<=600 -> True; with `<`: 600<600 False -> False. Overlapping case 540<630 and 570<600 -> True.
- MODEL.md and docstring (claims, but two independent statements agreeing with each other and with the test author's intent).
- merge_ranges reliance on `adjacent` (primary, code).

## 8. Strongest rival
Patch only `overlaps` (e.g. `intersects(a,b) and not adjacent(a,b)`), leaving `intersects` untouched. Matches the literal request and has minimal blast radius, but leaves `intersects` contradicting its documented semantics and leaves `conflicts()` reporting the same false conflict. Discriminator: `conflicts({'a':540-600,'b':600-660})` currently returns `[('a','b')]`, which the half-open model says is wrong - so the root fix is preferred.

## 9. Act or wait
Act. The fix is tiny and reversible; tests cover `overlaps` and `merge_ranges`; add tests for `conflicts`. Record the `conflicts()` behaviour change as an assumption.

## 10. Reconsider if
- `test_merge_back_to_back` (or any existing test) fails after the change -> switch to the rival (local fix in `overlaps`).
- Any repository file shows a caller relying on closed-interval `intersects` (count > 0) -> local fix instead.
- New `conflicts` test with back-to-back bookings still returns a pair -> investigate further.

FINAL
Posture: GUARDRAIL
Action: PROCEED
Probability approach is correct: 90%
Key missing evidence: external callers of booking.interval; whether nightly report intends to flag back-to-back bookings
