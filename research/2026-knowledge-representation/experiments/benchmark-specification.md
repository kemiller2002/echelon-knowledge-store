# Benchmark specification

Status: implemented (deterministic suite) and executed (agent trials), 2026-11-16.
Harness: `benchmarks/Echelon.KnowledgeStore.Bench` (F#). Corpus pinned in
`benchmarks/corpus.lock.json` (13 repositories, exact commits). Results:
`experiments/results/`.

## Reproduce

```bash
export EKS_CORPUS_ROOT=/path/to/clones          # one directory per repository
dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- \
  baseline validate formats retrieval dependencies ablation
EKS_TRIALS_DIR=/tmp/trials dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- trials
# run agents on /tmp/trials/<condition>/corpus with questions-batch{1,2}.md, answers to /tmp/trials/answers
EKS_TRIALS_DIR=/tmp/trials dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- score-trials
```

`.github/workflows/benchmarks.yml` runs the deterministic suite on the public
corpus at the pinned commits and keeps results as artifacts for 90 days. Private
repositories (clarity-framework, echelon-organization-administration) are only
included in local runs and only as aggregates.

## Corpus rules

* Markdown files of the 13 audited repositories. `vendor/`, `archive/`, `tests/`,
  `fixtures/`, `templates/` excluded from the record corpus (baseline includes them).
* A **record** is a file whose YAML front matter has `id` (568), or whose file
  name begins with an id (`ID--slug.md`, 29; typed metadata absent).
* Records are keyed `repository:id`; references resolve same-repository first.

## Suites

| Suite | Command | Measures | Gold |
|---|---|---|---|
| Baseline | `baseline` | files, bytes, tokens (o200k, cl100k, Llama SP), front matter, ids, links, broken links, duplicate files/paragraphs, instruction tokens | — |
| Validation | `validate` | defects by rule and repository | rule definitions |
| Formats | `formats` | tokens/bytes per format, full and metadata-only; round-trip equality; lossless Markdown migration | identity |
| Retrieval | `retrieval` | recall, precision, complete answers, tokens, tool calls for 6 strategies × 5 task kinds (252 tasks, public corpus) | authored typed metadata |
| Dependencies | `dependencies` | repository-level dependency detection precision/recall for 5 methods × 6 scenarios | 100 edges verified by the audit workstream (independent of the typed graph) |
| Ablation | `ablation` | marginal value of inverse links, traversal, relation types, mentions, cards; strict-type coverage; index drift; compilation determinism; epistemic standing | full typed system |
| Agent trials | `trials`, `score-trials` | end-to-end answer accuracy (set F1 / exact), tokens, tool calls, wall time | authored typed metadata (`trial-questions.json`) |

## Task catalogue

The 16 mission benchmark tasks map to suites as follows (see `benchmark-catalog.json`):

| # | Mission task | Covered by | Status |
|---|---|---|---|
| 1 | Find a requirement | retrieval `lookup-by-id`, `lookup-by-description`; trials Q04/Q11/Q14 | executed |
| 2 | Identify all related requirements | retrieval `change-impact` (typed closure) | executed (records of all kinds, not requirements only) |
| 3 | Find supporting evidence | retrieval `find-evidence`; trials Q01/Q07/Q09/Q13 | executed |
| 4 | Detect contradictions | validation `contested-claim`; trials Q05/Q15 | executed |
| 5 | Identify superseded decisions | retrieval `current-authority`; trials Q03/Q10 | executed |
| 6 | Determine change impact | retrieval/ablation `change-impact`; trials Q02/Q08/Q12; dependencies suite | executed |
| 7 | Generate a test plan | — | specified only |
| 8 | Prepare an implementation task | `Context.compile Implement` (unit-tested) | implemented, not benchmarked with agents |
| 9 | Update a requirement | round-trip and diff-locality checks | partially (no agent write trial) |
| 10 | Merge concurrent knowledge changes | security/versioning workstream git experiment | executed by workstream I/J (git 2.43.0) |
| 11 | Answer a cross-repository question | dependencies suite | executed (deterministic only) |
| 12 | Source-linked research summary | — | specified only |
| 13 | Identify missing evidence | trials Q06/Q16; validation rules | executed |
| 14 | Detect stale knowledge | validation (`stale-evidence`, `support-from-superseded-record`), index drift | executed |
| 15 | Reconstruct decision history | retrieval `current-authority`; `Context.compile DecisionHistory` | executed |
| 16 | Minimal context package | trials `compiled` condition; ablation cards vs bodies | executed |

## Controls and known threats

* Same corpus and questions in every condition; questions fixed in code
  (`Trials.questions`); gold computed before agents ran and kept outside the
  agents' directory.
* Agents restricted by instruction (not sandbox) to their condition directory.
* Order: batches alternate question kinds; not randomised per agent (threat).
* One model family (Claude Sonnet and Haiku tiers); n = 16 questions per condition per run.
* Graph-derived gold makes the typed strategies' perfect retrieval scores
  **definitional**: they measure what text strategies cost to recover authored
  metadata, not whether the metadata is true. The dependency suite uses
  independent gold for that reason.
* Token usage reported by the harness includes each agent's fixed system prompt
  (~65–70k tokens), which compresses differences between conditions.
