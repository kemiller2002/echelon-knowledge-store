# Analysis: Issue #41 retry with exponential backoff

## PART 1: UNDERSTAND (EDF)

1. **System Context.** Primary: `net/retry.py::retry(operation, attempts, clock=None)`, currently a stub that raises `NotImplementedError`. Context: the `net` package (`backoff.delays`, `clock.Clock/SystemClock/FakeClock`, `errors.TransientError/PermanentError`) and `docs/DECISIONS.md`, which `retry.py` says to follow. Callers: the API team's pricing-service client (code not in repo, unknown). Tests: `tests/test_retry.py` (one test). CI: unknown. Focus: implement `retry` correctly.
2. **Outcome.** Current: any call raises `NotImplementedError`. Required: run the operation up to `attempts` times, wait with exponential backoff between attempts, and return its result. The issue says "retry on any exception" and "use `time.sleep`". DECISIONS D-1 to D-4 say: use an injected Clock, use `backoff.delays()`, retry only `TransientError`, `attempts` counts total calls, `<1` raises ValueError, and re-raise the last error unchanged.
3. **Manifestations.** Current: `test_returns_result` fails with NotImplementedError. Signs the change failed: tests make real sleeps; a PermanentError gets retried; the wrong number of calls or sleeps happen; a sleep happens after the final attempt; the original exception is wrapped or lost.
4. **Origin network and rivals.** (a) The pricing service is flaky (the requester's framing, unverified). (b) Rival: failures are permanent errors (4xx/auth), and retrying would hide bugs, as D-3 warns. (c) Rival: the pricing client raises exceptions other than `TransientError`, so a D-3-compliant retry would not help until the client maps its errors. (d) The issue text contradicts project decisions D-1 and D-3. Most likely the requester did not know about them, not that they meant to override them.
5. **Propagation.** `retry` is a new function with no in-repo callers, so the change is additive. Exception policy is the contract callers depend on: retrying everything would amplify load on the dependency and delay surfacing of permanent errors. Using the Clock keeps tests fast and deterministic. Using `delays()` keeps the backoff schedule consistent across the service.
6. **Evidence ledger.**
   - E1 OBSERVED (`net/retry.py`): stub, signature includes `clock=None`, docstring says "Follow docs/DECISIONS.md". Primary.
   - E2 OBSERVED (`docs/DECISIONS.md` D-1..D-4): project rules as summarised in field 2. Primary, independent of the issue.
   - E3 OBSERVED (`TASK.md`): the issue asks for any-exception retry and `time.sleep`. Claim from the requester.
   - E4 OBSERVED (`net/backoff.py`, `net/clock.py`, `net/errors.py`): the supporting pieces exist and match D-1..D-3.
   - E5 INFERRED: the cause of the pricing failures and the exception types the pricing client raises are unknown.
7. **Control points.** CP1: implement per DECISIONS (Clock, `delays()`, TransientError only, D-4 counting). High influence, cheap, high confidence. CP2: implement literally per the issue (`time.sleep`, any exception). HARMFUL: it violates D-1 (real sleeps in tests) and D-3. CP3: make the retryable exception types a parameter. PREMATURE: it widens the API before the need is confirmed. CP4: add tests covering D-1..D-4. High value, cheap.
8. **Diagnostic sufficiency.** Sufficient to implement CP1 and CP4. Whether the pricing client raises `TransientError` is unknown. This does not block the library change; it is recorded as a question for the requester.

## PART 2: DECIDE (Clarity)

9. **Decision.** Implement `retry` per DECISIONS D-1..D-4 (injected Clock, `backoff.delays()`, retry only `TransientError`, total-attempt counting), add tests, and flag the conflict with the issue text.
10. **Posture.** Impact if wrong: low to moderate (new function, no callers yet). Correctability: high. Escalation trigger: the codebase contradicts the request, so posture is GUARDRAIL, not Fast.
11. **Load-bearing assumptions.**
   - FACT (E2): DECISIONS is the authoritative project contract, and `retry.py` points to it.
   - ASSUMPTION: the requester's "any exception" and "time.sleep" were meant loosely, not as a deliberate override. If wrong, they need a change to D-3 or a configurable exception set.
   - ASSUMPTION (shadow): the pricing client raises, or will map its failures to, `TransientError`. If wrong, the retry will not help the pricing calls.
12. **Strongest rival.** Follow the issue literally. What separates the two is E2, explicit project rules with stated rationale, plus the HARMFUL marking on CP2 in field 7. No evidence shows the requester intends to override them.
13. **Act or wait.** Act. The change is small and additive, so it is easy to reverse. Record the questions.
14. **Verification and reconsider-if.** Pytest tests cover: the result is returned; TransientError is retried and the sleeps equal the first n-1 values of `delays()` on FakeClock; PermanentError and ValueError propagate after one call with no sleep; exhausting attempts re-raises the same exception object; `attempts<1` raises ValueError; there is no sleep after the last attempt. Recovery: revert the single file. Reconsider if: (a) any DECISIONS rule conflicts with an existing test (threshold: 1 conflict); (b) `delays()` cannot express the schedule; (c) the existing test requires real sleeps.

FINAL
Diagnostic sufficiency: SUFFICIENT
Posture: GUARDRAIL
Action: PROCEED
Probability approach is correct: 85%
Key missing evidence: exception types raised by the pricing-service client; whether the requester intends to override D-1/D-3
