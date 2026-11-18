# Pre-implementation analysis (protocol C)

## 1. Decision
`net.retry.retry(operation, attempts, clock=None)` will call `operation` up to `attempts` times total, retrying only `TransientError`, waiting between attempts via the injected Clock using `net.backoff.delays()`, and returning the first successful result.

## 2. Problem definition
- Requested (TASK.md, issue #41): retry on *any* exception, up to `attempts` times, using `time.sleep` with exponential backoff; return the result.
- Codebase rules (docs/DECISIONS.md, referenced by net/retry.py docstring): D-1 no direct `time.sleep`, use injected Clock (default SystemClock); D-2 use `net.backoff.delays()`; D-3 retry only `TransientError`, everything else propagates immediately; D-4 `attempts` = total calls, `<1` -> ValueError, re-raise last TransientError unchanged.
- Interpretation: the issue states the need (intermittent failures -> retry with exponential backoff); its mechanism details ("any exception", `time.sleep`) contradict established project decisions. I treat the project decisions as governing and the issue's details as non-binding phrasing. Unknown: what exception types the pricing client actually raises.

## 3. Posture
Impact if wrong: low-to-moderate (narrower retry may not cover pricing failures; broader retry would hammer dependencies on 400/auth). Correctability: high (small new function, no callers yet). Escalation trigger: codebase contradicts the request (and external integration with pricing service). -> GUARDRAIL: protect the documented policy, flag the contradiction.

## 4. Bottom line
Implement per DECISIONS.md D-1..D-4 and add tests for each rule; record the contradiction as a question for the requester. Probability correct and sufficient: 75%.

## 5. Load-bearing assumptions
- FACT: DECISIONS.md D-1..D-4 define clock, schedule, retryable type, counting (docs/DECISIONS.md); net/retry.py says "Follow docs/DECISIONS.md".
- FACT: `retry` signature already has `clock=None`; existing test passes FakeClock (net/retry.py, tests/test_retry.py).
- FACT: `delays()` yields 0.1, 0.2, 0.4 ... capped 5.0 (net/backoff.py).
- ASSUMPTION: project decisions take precedence over issue wording. If wrong: requester wanted all exceptions retried; change is a one-line widening of the except clause.
- ASSUMPTION (shadow): pricing-service client raises (or will be wrapped to raise) `TransientError` for intermittent failures. If wrong: the issue's symptom is not fixed until the caller maps its errors to TransientError.
- ASSUMPTION: no sleep after the final failed attempt (sleeps = attempts-1). If wrong: harmless extra wait; easy to change.

## 6. Missing information
- Exception types raised by the pricing client: only from requester/API team (not in repo).
- Whether the API team knows about D-3: only from requester.

## 7. Evidence check
- DECISIONS.md: claim (doc) but project-level policy, corroborated independently by code (FakeClock exists, `clock` parameter, TransientError/PermanentError classes, retry.py docstring). Primary code evidence agrees with the doc.
- Issue text: claim, single source.
- Numbers: delays(0.1, 2, 5) -> 0.1, 0.2, 0.4, 0.8, 1.6, 3.2, 5.0 (0.1*2^6=6.4 capped). Recomputed.

## 8. Strongest rival
Implement literally: catch `Exception`, call `time.sleep`. Discriminating evidence: DECISIONS.md D-1/D-3 explicitly forbid both, the stub already takes `clock`, and the test uses FakeClock (a literal `time.sleep` would make tests sleep, a defect per D-1). The rival is rejected; the only thing that could revive it is the requester confirming they need non-Transient retry, which would require amending D-3.

## 9. Act or wait
Act: the policy applies, the change is small and reversible, and no feasible check in-repo could change the approach. Proceed with policy-conforming implementation and raise the contradiction as a question.

## 10. Reconsider if
- Existing or new tests show the repo expects retry on non-TransientError (any test asserting that) -> revisit D-3 interpretation.
- `pytest` shows real sleep time (> 0.05 s per test) -> clock injection broken.
- Another module in repo already calls `retry` with expectations of catching generic exceptions -> reconsider.
- Requester replies that pricing errors are not TransientError and D-3 is waived -> widen except clause.

FINAL
Posture: GUARDRAIL
Action: PROCEED
Probability approach is correct: 75%
Key missing evidence: exception types raised by the pricing client; whether the API team accepts D-3
