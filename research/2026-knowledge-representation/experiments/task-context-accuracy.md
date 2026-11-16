# Task-context accuracy

| Evidence | Result |
|---|---|
| Trial 1, compiled packages (16 questions; Sonnet, Haiku) | 1.000 both models; 22 s vs 44 s (Sonnet), 35 s vs 102 s (Haiku) vs raw Markdown |
| Trial 2, compiler-first (12 held-out questions; Sonnet) | 0.917 as run → 1.000 after fixing one unwired rule; 15 s vs 31 s |
| H01 task package (implement DF-ROS-2026-A059) | D 1.00; A and C 0.89 (both added a record the target *supports*, which is not a dependency) |
| Packages for all 587 public records | 586 COMPLETE; mean 2,307 tokens; 0 silent losses |
| Requirement completeness | packages list the requirements reached by typed links; 20 of 56 public requirement records have no recognisable acceptance list, so obligations are reported missing, not invented |

The one place where compilation improved accuracy (H01) is where task semantics
("dependencies", not "everything linked") were encoded in the compiler instead of
being re-derived by the agent.
