# Analysis: bug #57 (protocol E, v1)

## PART 1: UNDERSTAND (EDF)

1. **System Context.** Primary system: the overlap predicate used for bookings. Requester names `booking/ranges.py::overlaps`; it is a one-line delegate to `booking/interval.py::intersects`. Context: `booking/conflicts.py::conflicts()` (nightly report) and `ranges.merge_ranges()` (calendar view busy blocks) also call `intersects`; tests in `tests/test_ranges.py`; no CI config, no git repo. Focus: why back-to-back half-open intervals are reported as sharing time.

2. **Outcome.** Current: `overlaps(Interval(540,600), Interval(600,660))` returns True; `conflicts()` would likewise flag 9-10 and 10-11 as clashing. Required: intervals that only touch (one's end == other's start) do not overlap/conflict, consistent with the documented half-open model; genuinely overlapping intervals still overlap; calendar still draws back-to-back bookings as one block.

3. **Manifestations.** Current: `test_back_to_back_bookings_do_not_overlap` fails (`assert not True`). Failure signals after change: that test still fails; `test_overlapping_bookings_overlap` or `test_merge_back_to_back` fails; `conflicts()` still reports touching pairs; merge_ranges stops coalescing back-to-back ranges.

4. **Origin Network and rivals.**
   - H1 (leading): `intersects` uses closed-interval comparisons (`<=`) while `docs/MODEL.md` and the module docstring define intervals as half-open `[start, end)`. The defect is in `interval.py`, not in `overlaps` itself (cause outside blamed component).
   - Rival R1: `overlaps` alone is wrong and `intersects` is intentionally closed. Ruled out-ish: `intersects` docstring says "share at least one instant"; under half-open [540,600) and [600,660) share no instant, so `intersects` violates its own contract. `merge_ranges` separately ORs in `adjacent`, which would be redundant only if intersects already covered touching, suggesting the author expected intersects to exclude touching.
   - Rival R2: no real problem / intentional tradeoff (buffer between bookings). No evidence for this; docs say 540-600 does not contain 600.
   - Rival R3: test artifact. Test matches documented model; not an artifact.

5. **Propagation.** Origin `intersects` -> `overlaps` (bug #57) and -> `conflicts()` (nightly report, same false conflicts, not yet reported) and -> `merge_ranges` (masked there because `adjacent` is OR-ed in). Fixing `intersects` propagates to all three: overlaps and conflicts fixed; merge_ranges unchanged in behaviour for touching ranges (adjacent still merges them) and for overlapping ones. Patching only `overlaps` would leave `conflicts()` wrong and the two predicates diverging.

6. **Evidence ledger.**
   - E1 OBSERVED (`booking/interval.py`): `intersects` = `a.start <= b.end and b.start <= a.end`; module docstring says half-open. Primary, independent.
   - E2 OBSERVED (`docs/MODEL.md`): half-open model; 540-600 excludes 600; back-to-back drawn as one block in calendar. Primary (project doc), independent of code.
   - E3 OBSERVED (`booking/ranges.py`): `overlaps` returns `intersects(a,b)`; `merge_ranges` uses `intersects or adjacent`. Primary.
   - E4 OBSERVED (`booking/conflicts.py`): `conflicts` uses `intersects`. Primary.
   - E5 OBSERVED (pytest run): 1 failed (back-to-back), 2 passed. Primary.
   - E6 INFERRED: `conflicts()` currently reports back-to-back pairs (follows from E1+E4; will confirm with a test). 
   - Causal link E1 -> bug #57: order (overlaps delegates), mechanism (`600 <= 600` true), rival R1 contradicted by E1 docstring + E2. Graded causal.

7. **Control Points.**
   - CP1: change `intersects` to strict `<` (half-open). High influence (fixes overlaps + conflicts), trivial cost, high confidence. Recommended.
   - CP2: special-case `overlaps` only (e.g. `intersects and not adjacent`). Fixes reported symptom only; leaves `conflicts()` wrong. Inferior, not harmful per se.
   - CP3: edit/remove the failing test. HARMFUL.
   - CP4: remove `adjacent` from merge_ranges after the fix. PREMATURE/HARMFUL: would break calendar block merging (E2).

8. **Diagnostic Sufficiency.** Sufficient. Evidence is direct; cheap confirmation via added tests.

## PART 2: DECIDE (Clarity)

9. **Decision.** Fix the root cause by making `intersects` use half-open semantics (`a.start < b.end and b.start < a.end`), leave `overlaps` delegating, and add regression tests for `conflicts()` and overlapping merges.

10. **Posture.** Impact if wrong: low (small internal library, tested); correctability: high. Mild escalation: the change touches a shared primitive beyond the file the requester named (codebase contradicts the request's location). Posture: GUARDRAIL (add tests covering all callers).

11. **Load-bearing assumptions.**
   - A1 FACT (E1, E2): intervals are half-open by design. If false, the requested behaviour would itself be wrong.
   - A2 ASSUMPTION (shadow): no caller relies on `intersects` treating touching intervals as intersecting, other than merge_ranges, which also uses `adjacent`. Consequence if wrong: some unseen caller changes behaviour. Repository only has the callers seen (E3, E4).
   - A3 ASSUMPTION: the requester wants the behaviour fixed, not specifically the code inside `overlaps`; fixing the shared predicate satisfies "fix overlaps". Consequence if wrong: minor; overlaps is fixed either way.

12. **Strongest rival approach.** CP2 (patch only `overlaps`). Discriminating evidence: E4 shows `conflicts()` shares the same defect, and E1's docstring contract ("share at least one instant") is violated by `intersects` itself; patching only overlaps leaves the nightly report wrong.

13. **Act or wait.** Act: smallest reversible change (one operator pair) plus tests. No further check would change the decision.

14. **Verification and reconsider-if.** Verify: full test suite passes, including existing 3 tests and new tests (conflicts excludes back-to-back, includes overlapping; merge still coalesces back-to-back and overlapping). Recovery: revert the one-line change. Reconsider if: (a) any existing test other than the target fails after the change (>0 regressions); (b) a further caller of `intersects` is found that depends on closed semantics; (c) merge_ranges output changes for any back-to-back input.

FINAL
Diagnostic sufficiency: SUFFICIENT
Posture: GUARDRAIL
Action: PROCEED
Probability approach is correct: 92%
Key missing evidence: none
