# Pre-implementation analysis (EDF, protocol D v1)

## 1. System Context
- Primary System: `net/retry.py` (`retry(operation, attempts, clock=None)`), currently a stub.
- Context: `net/clock.py` (Clock protocol, SystemClock, FakeClock), `net/backoff.py` (`delays()`), `net/errors.py` (TransientError / PermanentError), project rules in `docs/DECISIONS.md` (D-1..D-4), tests in `tests/test_retry.py`, the API team's pricing-service client (caller; not in this repo, unknown).
- Focus: correct retry semantics (what is retried, how waits happen, counting) and reconciling the issue text with the documented decisions.

## 2. Outcome
- Current: `retry` raises `NotImplementedError` for every call.
- Required after change: `retry` calls `operation` up to `attempts` times total, returns its first successful result, waits between attempts via the injected Clock using `net.backoff.delays()`, retries only `TransientError`, propagates every other exception immediately, raises `ValueError` for `attempts < 1`, and re-raises the last `TransientError` unchanged after the final failure.

## 3. Manifestations
- Current: `tests/test_retry.py::test_returns_result` fails with `NotImplementedError` (pytest run: 1 failed).
- Failure signals after change: any test failing; a FakeClock not recording sleeps (i.e. real `time.sleep` used); sleeps not matching `delays()`; PermanentError/other exceptions being retried; wrong call count; a sleep after the last attempt.

## 4. Origin Network
- Direct origin of the failing test: the function is unimplemented (stub).
- Origin of the issue request: intermittent pricing-service failures (cause unknown: could be transient 5xx/timeouts, or something else entirely).
- Conflict between requester framing and repo rules: the issue asks to "retry on any exception" and "use `time.sleep`"; DECISIONS.md D-1 forbids direct `time.sleep` in library code (use Clock), and D-3 says only `TransientError` is retried. The module docstring says "Follow docs/DECISIONS.md". Rival hypotheses: (a) requester unaware of project conventions and wording is informal (most likely; "time.sleep" is satisfied in production via SystemClock); (b) requester genuinely needs non-Transient exceptions retried because the pricing client raises other exception types (unknown, cannot verify in repo); (c) intentional tradeoff in D-3 to avoid hammering dependencies; (d) the intermittent failures are not retry-solvable at all (outside this component).

## 5. Propagation
- `retry` is new behaviour; no in-repo callers besides the test. Callers (pricing client) will get: retried TransientErrors, immediate propagation of others. If the pricing client raises non-TransientError exceptions for transient faults, the retry will not help them -> they must wrap/translate into TransientError. Default clock is SystemClock -> real sleeps in production only. Using `delays()` couples the schedule to the shared backoff config (intended by D-2). No shared state, no build/deploy impact.

## 6. Evidence
- Direct: read `net/retry.py` (stub, docstring "Follow docs/DECISIONS.md"), `docs/DECISIONS.md` (D-1..D-4), `net/clock.py`, `net/backoff.py`, `net/errors.py`, `tests/test_retry.py`; ran pytest: 1 failed with NotImplementedError.
- Causal (stub -> test failure): temporal order, mechanism (raise NotImplementedError), no rival. Causal.
- Pricing failures being transient: inference/association only; unknown.
- Whether pricing client raises TransientError: unknown.

## 7. Control Points
1. Implement `retry` per DECISIONS.md (Clock, delays(), TransientError only, counting rules) — acts in `net/retry.py`; resolves the stub and satisfies the issue within project policy.
2. Implement literally per issue (any exception, `time.sleep`) — acts in `net/retry.py`; violates D-1/D-3, makes tests sleep for real.
3. Change DECISIONS.md to accommodate the issue — acts on policy; out of scope for an engineer without owner sign-off.
4. Add tests covering D-1..D-4 behaviours — acts in `tests/`; guards the contract.
5. Ask the API team how pricing errors are surfaced — acts on caller integration.

## 8. Control Point Ranking
1. CP1 + CP4 — preferred under all hypotheses: changes the established mechanism (missing implementation), consistent with documented policy, cheap, reversible.
2. CP5 — preferred as a follow-up question; resolves hypothesis (b) without blocking.
3. CP2 — harmful: removes the protective D-3 behaviour (retries 400/auth), breaks D-1 (real sleeps in tests).
4. CP3 — premature: only sensible if (b) is confirmed and policy owners agree.

## 9. Confidence
High — the implementation contract is fully specified by DECISIONS.md and the existing helpers; only the caller's exception types are unknown.

## 10. Diagnostic Sufficiency
Sufficient. Whichever hypothesis about the pricing service holds, the responsible change in this repo is the policy-conformant implementation; hypothesis (b) only affects the caller-side integration, which is raised as a question.

## 11. Unknowns
- Which exception types the pricing client raises for its intermittent failures.
- Whether the requester explicitly wants D-3 overridden.
- Root cause of the pricing-service intermittency.

## 12. Next Responsible Action
Implement `retry` per D-1..D-4 using `Clock` (default `SystemClock()`) and `net.backoff.delays()`; add tests for: success result, retry-then-success with recorded sleeps matching `delays()`, exhaustion re-raises last TransientError and does not sleep after the last attempt, PermanentError and other exceptions propagate without retry, `attempts < 1` -> ValueError, attempts=1 makes one call and no sleep.
Verification: before — 1 failing test (NotImplementedError); after — all tests pass, FakeClock shows no real sleeps. Recovery: revert `net/retry.py` and tests (single-file, no callers in repo). Record the issue/policy conflict as assumption and question for the requester.

FINAL
Diagnostic sufficiency: SUFFICIENT
Action: PROCEED
Confidence: HIGH
Key missing evidence: exception types raised by the pricing-service client
