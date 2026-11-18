# Pre-implementation analysis (protocol C)

## 1. Decision
`settings.merge.merge(base, override)` will return a new dict implementing ADR-004: recursive merge of nested dicts, replacement of all other values (lists replaced, not concatenated), `None` in override deletes the key, and neither input is mutated.

## 2. Problem definition
Requested (TASK.md): implement `merge`; later layers override earlier ones, including inside nested sections; `load.py` folds layers with it.
Treated as true: the accepted ADR-004 in docs/DECISIONS.md is the governing spec for the details the task leaves out (lists, None, mutation). Interpretation: when override has a dict and base has a non-dict (or vice versa), override replaces; result values are deep-copied so cached layers cannot be mutated through the result.

## 3. Posture
Impact if wrong: low-moderate (config values silently wrong), correctability high (pure function, single file). No escalation trigger: no auth/schema/public external API change; codebase does not contradict the request. Posture: Fast.

## 4. Bottom line
Recursive pure function with deep copies, plus tests covering each ADR-004 rule. Probability correct and sufficient: 90%.

## 5. Load-bearing assumptions
- FACT: ADR-004 is "accepted" and defines list/None/no-mutation rules (docs/DECISIONS.md).
- FACT: `load` uses `reduce(merge, layers, {})` (settings/load.py), so merge must accept `{}` as base.
- FACT: values are JSON-compatible (ADR-002), so `copy.deepcopy` is safe and cheap enough.
- ASSUMPTION: `None` removal applies at every nesting level. If wrong, nested unsets would behave differently from top-level ones; low consequence, easily changed.
- ASSUMPTION (shadow): "never mutates" also means the returned dict must not share mutable sub-objects with inputs (callers cache layers). If wrong, deep copying is merely extra cost.
- ASSUMPTION: a `None` in the base (not override) is kept as a value. If wrong, minor.

## 6. Missing information
- Whether None-removal should apply when base lacks the key (no-op assumed) — requester only; low stakes.
- Nothing else; spec obtainable from repo and already read.

## 7. Evidence check
- ADR-004: claim (doc), but accepted decision record and referenced from merge.py docstring (independent pointer, same author likely).
- load.py and existing test: primary code. Existing test (nested override) is consistent with ADR-004.
No numbers to recompute.

## 8. Strongest rival
A naive shallow `{**base, **override}` or a generic deep merge that concatenates lists / keeps None. Discriminated against by ADR-004 text and by the existing nested test (shallow merge would drop `port`).

## 9. Act or wait
Act: spec is clear, change is small and reversible. Follow ADR-004.

## 10. Reconsider if
- Existing test `test_nested_override` fails after implementation.
- Any other file in the repo is found calling merge with non-dict top-level arguments.
- New ADR-rule tests reveal a conflict between rules (e.g. None removal vs. replacement) that cannot be satisfied simultaneously.

FINAL
Posture: FAST
Action: PROCEED
Probability approach is correct: 90%
Key missing evidence: none
