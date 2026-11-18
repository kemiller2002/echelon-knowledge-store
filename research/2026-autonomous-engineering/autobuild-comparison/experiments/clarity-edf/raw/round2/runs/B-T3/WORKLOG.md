# WORKLOG - B-T3: retry with exponential backoff (Issue #41)

## Scope
Implement `retry(operation, attempts)` in `net/retry.py` (currently `raise NotImplementedError`).
Stay inside `/root/exp2/runs/B-T3`. Only `net/retry.py` is expected to change; tests may be extended.

## Canonical records consulted
- `docs/DECISIONS.md` (D-1 .. D-4) - binding for this package.
- `net/clock.py` (Clock protocol, SystemClock, FakeClock), `net/backoff.py` (`delays()`), `net/errors.py` (TransientError, PermanentError).

## Acceptance criteria
1. `retry(op, attempts)` returns the operation's result on success (D-4 total-call semantics).
2. `attempts` is the total number of calls including the first; `attempts < 1` raises `ValueError` (D-4).
3. Only `TransientError` is retried; any other exception, including `PermanentError`, propagates immediately (D-3).
4. After the last failed attempt the last `TransientError` is re-raised unchanged (D-4).
5. Waits between attempts come from `net.backoff.delays()` (D-2) and go through a `Clock` (D-1); default `SystemClock()`; no direct `time.sleep` in library code.
6. Tests use `FakeClock`; no real sleeps in unit tests.

## Material unknowns / conflicts
- **Conflict:** Issue #41 says "retry on any exception"; DECISIONS D-3 says only `TransientError` is retried. Canonical policy wins (do not silently change policy). Implementing D-3 and escalating the conflict as a question for the requester.
- Issue says "exponential backoff" without parameters; D-2 mandates `net.backoff.delays()` defaults (base 0.1, factor 2, cap 5). Used as-is.
- The `clock` parameter is in the existing stub signature (`clock=None`); None maps to `SystemClock()`.
- `attempts` type/validation beyond `< 1` not specified; treat non-int as caller error (not validated further).

## Risks
- Retrying only `TransientError` means the pricing service calls that currently fail with some other exception type will not be retried. If the pricing client raises a different type for timeouts/503s, the fix will not help. Needs confirmation of what the pricing client raises.

## Baseline (before edits)
Command: `cd /root/exp2/runs/B-T3 && python3 -m pytest -q -p no:cacheprovider`
Result: 1 failed, 0 passed. `tests/test_retry.py::test_returns_result` fails with `NotImplementedError` (expected stub state).
Note: pytest's traceback displays a path under /root/exp/fixtures/T3/tests/ rather than the repo path; I did not open that path (outside the allowed scope).
Repository is not a git repo, so no VCS diff is available; changed files will be recorded by hand.

## Handoff
- **Objective:** implement `retry(operation, attempts)` per Issue #41 and docs/DECISIONS.md.
- **Work completed:** implemented `retry` in `net/retry.py` (total-call `attempts`, ValueError for `attempts < 1`, retries only `TransientError`, waits from `delays()` via injected/default `SystemClock`, re-raises last `TransientError` unchanged). Added 6 unit tests in `tests/test_retry.py` using `FakeClock`, covering success after retries, exhaustion, `PermanentError` and other exceptions not retried, `attempts < 1`, and single-attempt no-sleep.
- **Files changed:** `net/retry.py`, `tests/test_retry.py`, `WORKLOG.md`, `REPORT.json`.
- **Decisions and assumptions:**
  - Followed D-3 (retry only `TransientError`) over Issue #41's "retry on any exception". Canonical policy not changed. Requester must confirm.
  - Used `delays()` defaults (base 0.1, factor 2, cap 5) per D-2.
  - `clock=None` maps to `SystemClock()` per D-1.
- **Tests run and results:**
  - Baseline: 1 failed (`NotImplementedError`), as expected from the stub.
  - After change: `python3 -m pytest -q -p no:cacheprovider` gives 8 passed, 0 failed.
  - Not run: no integration test against the real pricing service; the default `SystemClock` path (real sleep) is not exercised by unit tests by design (D-1).
- **Unresolved questions:** does the pricing client raise `TransientError` for timeouts and 503s? If it raises other types, this change will not retry them. Should D-3 be revisited for the pricing service?
- **Risks:** the fix does nothing for failures not mapped to `TransientError`. The repo is not under git, so the file list is maintained by hand.
- **Next recommended action:** confirm the pricing client's exception mapping, or wrap its transient failures as `TransientError` at the call site. Then close Issue #41.
