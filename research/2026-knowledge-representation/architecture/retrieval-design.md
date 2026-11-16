# Retrieval design

Evidence: `experiments/comparative-results.md` §2, `experiments/ablation-results.md`.

## Design

```
question ──► id present? ──yes──► exact lookup ──► typed query (evidence | impact | history | deps)
               │                                         │
               no                                        ▼
               └──► BM25 over sections (scoped to repo) ─► top-k candidate cards ─► typed query
                                                                                     │
                                                                  cards (+ bodies for targets) ◄┘
```

* **Exact id lookup first.** 1.0 recall at ~110 tokens vs grep+read at ~24k tokens.
* **Typed queries** for relational questions (evidence, impact, current authority).
  Text strategies cannot match them: chasing references through Markdown reached
  0.83 impact recall at ~485k tokens and ~200 tool calls per question.
* **BM25 for description queries**, scoped by repository, returning several
  candidates. Hybrid recall 0.73 with top-3 seeds; the trials used top-5 candidate
  cards and answered all three description questions correctly.
* **Semantic (embedding) retrieval: not implemented.** No embedding model is
  available offline here and the corpus is small. It may improve description
  queries; unresolved.
* **Never deliver prose mentions as relations.** They cut impact precision to 0.26.
* **Cards by default, bodies on demand.** Cards keep recall at about a tenth of
  the tokens.

## What agents should receive

Coding agents already grep Markdown well. EKS should give them **one more file
and one command**, not a new format:
* `INDEX.md` (or JSONL) generated per repository: one card per record with
  derived inverse links (tested in trials: same accuracy, fewer calls);
* `eks context <task> <id>` producing a compiled package (tested: same accuracy,
  half or a third of the wall time).
