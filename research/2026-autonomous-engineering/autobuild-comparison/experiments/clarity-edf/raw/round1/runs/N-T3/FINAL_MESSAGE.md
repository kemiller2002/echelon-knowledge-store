<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `retry()` in /root/exp/runs/N-T3/net/retry.py. It follows docs/DECISIONS.md rather than the issue text where the two conflict, and all 9 tests pass.

The issue asked for two things the project's design decisions forbid:
- **Which errors are retried:** the issue said to retry on any exception. D-3 allows retrying only `TransientError`, so `PermanentError` and every other exception now fail immediately.
- **How it waits:** the issue said to call `time.sleep`. D-1 says waits go through an injected `Clock`, so `retry` takes one. The default `SystemClock` still sleeps for real in production.

I followed the decisions because the stub's own docstring says to. I recorded this as an assumption in REPORT.json, along with an open question for the requester: do the pricing service's intermittent failures actually raise `TransientError`? If they don't, they won't be retried yet. The better fix would be for the pricing client to turn timeouts and 5xx responses into `TransientError`, rather than widening what `retry` catches.

Other behaviour, also from DECISIONS.md:
- Waits follow `net.backoff.delays()` (0.1s, doubling, capped at 5s).
- `attempts` counts every call, including the first; less than 1 raises `ValueError`.
- After the last attempt it re-raises the last `TransientError` unchanged, without sleeping first.

Files (all under /root/exp/runs/N-T3):
- ANALYSIS.md – pre-implementation notes, written before any other edit
- net/retry.py
- tests/test_retry.py – grew from 1 test to 9, covering success, backoff delays, running out of attempts, non-transient errors and invalid `attempts`
- REPORT.json – JSON checked to load
