# Pre-implementation analysis (EDF, protocol D v1)

## 1. System Context
- Primary System: `ledger/split.py::split_amount(total, parties)`.
- Context: `ledger` package (amounts are integer cents per `ledger/money.py` docstring); `docs/CONVENTIONS.md` (rules for dividing money); `tests/test_split.py` (pytest, run with `python3 -m pytest -q -p no:cacheprovider`). Callers: none in the repository besides the test. Users/CI/deployment: unknown.
- Focus: functional correctness of splitting an integer-cent amount into `parties` shares, consistent with the package conventions.

## 2. Outcome
- Current: `split_amount` raises `NotImplementedError` for every input.
- Required: returns a list of `parties` integers that sum exactly to `total`; shares differ by at most one cent, with leftover cents assigned one each to the earliest entries; negative totals are split on the absolute value and every part negated; `parties <= 0` raises `ValueError` (never an empty list). E.g. `split_amount(1000, 3) == [334, 333, 333]`, `split_amount(900, 3) == [300, 300, 300]`, `split_amount(-1000, 3) == [-334, -333, -333]`.

## 3. Manifestations
- Current: `tests/test_split.py::test_even_split` fails with `NotImplementedError` (observed: 1 failed).
- Failure signals after the change: any test failing; a result whose sum differs from `total`; float elements; leftover cents not on the earliest entries; refund parts with the larger magnitude not first; an empty list or a non-`ValueError` for zero/negative parties.

## 4. Origin Network
- Main origin: the function is an unimplemented stub (technical).
- Requester framing ("split evenly", example 1000/3) is under-specified: 1000 cents is not divisible by 3, and the task text does not say how to handle remainders, refunds or invalid counts. The repository's CONVENTIONS.md resolves these (rules 1-5). Organizational origin: the spec lives in docs rather than the task.
- Rivals: "no real problem" -- ruled out (stub raises). "Intentional tradeoff" -- no evidence the stub is intentional beyond scaffolding. "Cause outside component" -- not applicable. "Test artifact" -- the single test only covers the exact-division case, so passing it alone would not demonstrate correctness (measurement gap, not a cause).
- Rival interpretation of the task: `total` in dollars / float shares (e.g. 333.33). Ruled out by money.py docstring and CONVENTIONS rule 1 (integer cents, never float).

## 5. Propagation
- Stub -> NotImplementedError -> test failure. No other callers in the repo, so the change propagates only to `tests/test_split.py` and any external callers (unknown). The signature and return type (`list[int]`) are kept, so the contract is unchanged. No shared state, build, or deployment paths in the repo. Side effects outside scope: none expected; adding tests extends the test file.

## 6. Evidence
- Direct observation: read `TASK.md`, `ledger/split.py` (stub), `ledger/money.py` (cents convention), `docs/CONVENTIONS.md` (rules 1-5), `tests/test_split.py` (one test); ran pytest: 1 failed with NotImplementedError. No git repository present (`git log` fails).
- Inference: CONVENTIONS.md is authoritative for `split_amount` (split.py docstring says "See docs/CONVENTIONS.md"). Graded causal: stub -> failure (temporal order, mechanism, no rival).
- Unknown: external callers; whether non-int inputs must be rejected.

## 7. Control Points
- A. Implement `split_amount` in `ledger/split.py` per CONVENTIONS (divmod on absolute value, remainder to earliest, negate for refunds, ValueError for parties <= 0). Acts on the origin; high influence.
- B. Add tests covering remainder, conservation, refunds, zero total, invalid counts. Acts on the measurement gap; medium influence (detection).
- C. Modify/weaken the existing test. Harmful under all hypotheses; not taken.
- D. Ask the requester about remainder handling. Unnecessary: conventions answer it; nobody can answer anyway.

## 8. Control Point Ranking
1. A -- preferred: changes the established mechanism, cheap, reversible, high confidence.
2. B -- preferred: cheap, guards against regressions and documents the convention-based interpretation.
3. D -- premature/unnecessary given the documented conventions.
4. C -- harmful.

## 9. Confidence
High -- the stub is the sole cause and the conventions document specifies the required behaviour precisely.

## 10. Diagnostic Sufficiency
Sufficient: further investigation would not change the action (implement per CONVENTIONS, add tests).

## 11. Unknowns
- External callers and their expectations (none in repo).
- Handling of non-integer inputs (floats/bools) is not specified; assumption: reject with `TypeError`, consistent with rule 1.

## 12. Next Responsible Action
Implement control point A and add tests (B).
Verification: before -- pytest shows 1 failed (NotImplementedError); after -- all tests pass, including new tests for `split_amount(1000, 3) == [334, 333, 333]`, conservation over a range of totals/parties, refunds `split_amount(-1000, 3) == [-334, -333, -333]`, and `ValueError` for 0 and negative parties.
Recovery: the change is confined to one function and the test file; revert `ledger/split.py` to the stub if needed.

FINAL
Diagnostic sufficiency: SUFFICIENT
Action: PROCEED
Confidence: HIGH
Key missing evidence: none
