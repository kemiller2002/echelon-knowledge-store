# Compiler economics

Total cost per correctly completed task, trial 2 (12 questions per condition;
tokens = sum of both batch agents' harness-reported tokens; correct = sum of scores):

| Condition | Tokens | Correct | Tokens / correct task | Agent seconds / correct task | Tool calls / correct task |
|---|---|---|---|---|---|
| A raw Markdown | 148,707 | 11.89 | 12.5k | 5.3 | 1.35 |
| B optimised retrieval | 164,874 | 5.67 | 29.1k | 8.3 | 2.29 |
| C registry JSON | 147,176 | 11.89 | 12.4k | 5.5 | 1.26 |
| D compiler, as run | 150,839 | 11.00 | 13.7k | 2.7 | 0.91 |
| D compiler, fixed | 150,892 | 12.00 | 12.6k | 2.5 | 0.83 |

Compiler side (measured): full compile 0.15–0.36 s cold, ~0.05 s warm; one package
~0.5 ms; generating all 12 trial outputs including writing the 1,620-file A corpus
copy: 11.9 s (I/O-bound; the compile itself is < 0.4 s). Per question, compiler cost is
under 1% of agent wall time and has no token cost.

## Interpretation

* Token cost per correct task is flat across A, C and D (12.4k–13.7k) because ~65–70k
  of each ~75k-token agent session is fixed prompt. Marginal tokens for reading
  knowledge are small at this question size in every condition except B (whose
  excerpts were large and mostly irrelevant).
* Time per correct task halves with D. If agent time is the binding cost, the compiler
  pays for itself immediately; if token cost is, it neither pays nor costs at this scale.
* The ≈25% token-reduction target was not met in end-to-end agent sessions; it is met
  for the knowledge payload itself (D's outputs: 12.1k tokens for 12 questions vs
  ~2.7M available in A and 178k in C).
* Maintenance is the real cost: ~1,100 lines of compiler, plus calibration. The adversarial
  review estimates that extending `praxis registry build` would cost <15% of a separate
  EKS (an estimate from line counts, not a measurement).
