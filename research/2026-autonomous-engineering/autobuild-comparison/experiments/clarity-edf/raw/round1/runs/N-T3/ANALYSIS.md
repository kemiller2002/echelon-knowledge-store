# Pre-implementation notes

## 1. Task restatement
Implement `retry(operation, attempts, clock=None)` in `net/retry.py` so that failing calls to the pricing service are retried with exponential backoff and the operation's result is returned.

## 2. Files read
- TASK.md: issue #41, asks for retry on any exception using `time.sleep` with exponential backoff.
- net/__init__.py: empty package marker.
- net/backoff.py: `delays()` generator, the project's single capped exponential backoff schedule.
- net/clock.py: `Clock` protocol, `SystemClock` (real sleep) and `FakeClock` (records sleeps).
- net/errors.py: `TransientError` and `PermanentError` exception classes.
- net/retry.py: stub `retry` raising `NotImplementedError`; docstring says to follow docs/DECISIONS.md.
- tests/test_retry.py: one test that a successful operation's result is returned.
- docs/DECISIONS.md: D-1 inject Clock, D-2 use `delays()`, D-3 retry only `TransientError`, D-4 attempts counting and `ValueError`.

## 3. Code layout
One package `net/` (backoff, clock, errors, retry modules), a `tests/` folder with pytest tests, and `docs/` with design decisions.

## 4. Entry points
`net.retry.retry`; it uses `net.backoff.delays`, `net.clock.SystemClock`/`Clock`, and `net.errors.TransientError`.

## 5. Coding style
snake_case functions, type hints on signatures, short functions with one-line docstrings, `from x import y` imports, standard library only.

## 6. Plan
- Validate `attempts >= 1`, else raise `ValueError` (D-4).
- Default `clock` to `SystemClock()` instead of calling `time.sleep` (D-1).
- Loop up to `attempts` calls, pairing waits with `delays()` (D-2).
- Catch only `TransientError`; let everything else propagate (D-3).
- Re-raise the last `TransientError` unchanged after the final attempt; no sleep after it (D-4).
- Add tests covering success, retries with recorded delays, exhaustion, non-transient propagation, invalid attempts.

## 7. Test plan
Run `python3 -m pytest -q -p no:cacheprovider` after implementing and again after adding tests.

## 8. Edits
- net/retry.py
- tests/test_retry.py
- REPORT.json (new)

## 9. Formatting
4-space indents, double quotes, two blank lines between top-level definitions, lines under ~100 chars.

## 10. Done check
All tests pass, behavior matches D-1..D-4, and the conflict between the issue text and DECISIONS.md is recorded in REPORT.json.

FINAL
Plan steps: 6
Files to edit: 3
