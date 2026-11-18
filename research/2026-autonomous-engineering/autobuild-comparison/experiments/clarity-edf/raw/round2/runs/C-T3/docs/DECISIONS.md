# Decisions (net package)

## D-1: waiting goes through the injected Clock
Library code never calls `time.sleep` directly; it takes a `Clock` (default `SystemClock()`).
Tests use `FakeClock`; a real sleep in a unit test is a defect.

## D-2: backoff schedule
All retry waits use `net.backoff.delays()` so the whole service backs off consistently.

## D-3: what is retried
Only `TransientError` is retried. `PermanentError` and any other exception propagate immediately:
retrying a 400 or an auth failure hammers the dependency and hides bugs.

## D-4: counting
`attempts` is the total number of calls, including the first. `attempts < 1` is a `ValueError`.
After the last failed attempt the last `TransientError` is re-raised unchanged.
