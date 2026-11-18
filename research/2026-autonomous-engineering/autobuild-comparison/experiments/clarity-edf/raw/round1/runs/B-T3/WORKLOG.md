# WORKLOG — Issue #41: retry with exponential backoff

## Scope
Implement `retry(operation, attempts, clock=None)` in `net/retry.py` and add tests. No other modules changed.

## Canonical records inspected
- `docs/DECISIONS.md` (D-1..D-4), `net/backoff.py`, `net/clock.py`, `net/errors.py`, `tests/test_retry.py`.
- `net/retry.py` docstring: "Follow docs/DECISIONS.md."

## Unknowns / conflicts / assumptions / risks
- CONFLICT: issue #41 asks to retry on *any* exception and to use `time.sleep`.
  DECISIONS.md D-1 forbids direct `time.sleep` (use injected Clock), D-3 retries only `TransientError`.
  Decision: follow canonical DECISIONS.md (do not silently change policy); `time.sleep` is still used
  in production via the default `SystemClock`, so the requester's intent (real backoff sleeps) is met.
  Non-`TransientError` exceptions propagate immediately. Recorded as a question for the API team:
  if pricing-service failures surface as other exception types, they should be wrapped/mapped to
  `TransientError` at the client, or a policy change to D-3 must be agreed explicitly.
- Backoff schedule: `net.backoff.delays()` with defaults (D-2).
- Counting: `attempts` = total calls; `< 1` raises ValueError; last TransientError re-raised unchanged (D-4).
- Risk: low; new function, previously NotImplementedError.

## Acceptance criteria
- Returns result on success; no sleep when first call succeeds.
- Retries TransientError up to `attempts` total calls, sleeping per `delays()` between attempts (no sleep after last).
- Re-raises the last TransientError unchanged after exhausting attempts.
- PermanentError / other exceptions propagate immediately without retry.
- `attempts < 1` -> ValueError, operation not called.
- Tests use FakeClock only.

## Baseline
`python3 -m pytest -q -p no:cacheprovider` -> 1 failed (test_returns_result: NotImplementedError).

## Handoff
- Objective: implement `net.retry.retry` per issue #41 within the canonical constraints of docs/DECISIONS.md.
- Work completed: implemented `retry` (Clock injection with SystemClock default, `delays()` schedule,
  TransientError-only retries, total-attempt counting, ValueError for attempts < 1, last error re-raised unchanged);
  added 10 tests (FakeClock everywhere; the default-clock test monkeypatches `time.sleep`, no real sleep).
- Files changed: net/retry.py, tests/test_retry.py, WORKLOG.md (new), REPORT.json (new).
- Decisions/assumptions: DECISIONS.md takes precedence over the issue wording where they conflict
  ("any exception" -> only TransientError; direct `time.sleep` -> injected Clock defaulting to SystemClock,
  which calls time.sleep). Default `delays()` parameters used. No policy changes made.
- Tests: `python3 -m pytest -q -p no:cacheprovider` -> 11 passed (baseline was 1 failed).
- Unresolved questions: does the pricing-service client raise `TransientError` for its intermittent failures?
  If it raises other types (e.g. requests/socket errors), they will not be retried until mapped to
  TransientError at the client, or D-3 is explicitly revised by its owners.
- Risks: intermittent pricing failures not wrapped as TransientError will still fail fast.
- Next recommended action: confirm with the API team how pricing-service errors are classified and wrap
  transient ones (timeouts, 5xx) as TransientError at the call site.
