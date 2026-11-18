<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `retry` in /root/exp/runs/D-T3/net/retry.py and added tests in /root/exp/runs/D-T3/tests/test_retry.py; all 9 tests now pass, where before the one test failed.

Where the issue and the repo's rules disagreed, I followed the rules in docs/DECISIONS.md, which the module's own docstring points to:
- **Which errors are retried:** the issue asks to retry on any exception. Only `TransientError` is retried, and every other exception goes straight back to the caller.
- **How it waits:** the issue asks for `time.sleep`. The function waits through the injected clock, whose default calls `time.sleep`, and uses the shared `net.backoff.delays()` schedule.
- **Counting:** `attempts` is the total number of calls, including the first. Below 1 raises `ValueError`, and after the last failure the last `TransientError` is re-raised unchanged.

Question for the API team (in REPORT.json): their intermittent pricing failures are only retried if their client raises them as `TransientError`. If it raises other exception types, it should convert them, or the net package owners would need to agree to change the "only `TransientError`" rule.

I wrote /root/exp/runs/D-T3/ANALYSIS.md before making any edits; its verdict was SUFFICIENT / PROCEED / HIGH. The report is /root/exp/runs/D-T3/REPORT.json.
