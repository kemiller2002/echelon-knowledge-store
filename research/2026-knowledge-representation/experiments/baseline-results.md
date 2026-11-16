# Baseline results — the current Markdown knowledge environment

Measured 2026-11-16 at the commits in `benchmarks/corpus.lock.json`.
Raw tables: `results/baseline.md`, `results/validation.md`. Qualitative audit:
`baseline/current-agent-workflows.md`, `baseline/markdown-audit.md`.

## Size and tokens (measured)

| Measure | Value |
|---|---|
| Repositories | 13 (11 public, 2 private) |
| Markdown files | 2,149 |
| Bytes | 15,205,331 |
| Tokens, o200k_base / cl100k_base / Llama SentencePiece | 3,419,590 / 3,431,141 / 4,347,803 |
| Mean tokens per file (o200k) | 1,591 (max 90,414) |
| Files with front matter / with an `id` | 37.6% / 32.3% (0% in three repositories) |
| Root agent-instruction files (AGENTS/CLAUDE/GEMINI/copilot/BOOTSTRAP/HANDOFF) | 80,882 tokens across the 13 repositories |

Tokenizer families differ by < 0.4% between the two OpenAI encodings and by ~27%
for Llama; relative comparisons between formats are stable across all three
(`results/formats.md`). Claude's tokenizer is not public and was not measured.

## Duplication (measured)

| Measure | Value |
|---|---|
| Exact duplicate Markdown file copies (beyond the first) | 580 |
| Tokens in those copies | 868,138 (25.4% of all Markdown tokens) |
| Files in duplicate sets spanning >1 repository | 626 |
| Duplicate paragraph copies (≥200 chars) | 8,519 (883,291 tokens) |

Almost all duplication is the Praxis framework payload (`docs/00-governance`,
`framework/`, `templates/`, BOOTSTRAP/CLAUDE files) installed byte-identically in
10 repositories. This is a **repository-organisation** problem, not a Markdown
syntax problem: any syntax copied ten times costs ten times.

## Links and identifiers (measured)

* Relative links: 1,426; broken: 205 (14.4%). 160 of the broken links are in the
  two private repositories; public repositories: 45 of 1,111 (4.1%).
* 568 records carry a front-matter `id`; 29 more are identifiable only by file name.
* 1,325 resolved relation edges, of which ~1,100 (83%) are untyped
  `related_documents`; 218 are typed (supports 110, derived_from 73,
  supersedes/superseded_by 13+13, depends_on 5, contradicts 4).
* 563 relation targets do not resolve to a record (532 document paths, 30 input ids/commits, 1 genuine missing id).
* 29 ids are defined in more than one repository (vendored governance records).
* **0 typed relations cross a repository boundary** (`results/dependencies.md`).

## Defects found by validation (measured, 599 records)

| Rule | Severity | Count |
|---|---|---|
| evidence-without-structured-source | advisory | 49 |
| cross-repository-id-collision | advisory | 29 |
| evidence-link-asymmetry (link declared on one side only) | advisory | 28 |
| missing-status | advisory | 26 |
| dangling-reference | blocking | 1 |
| requirement-without-acceptance | advisory | 7 |
| support-from-superseded-record | advisory | 3 |
| contested-claim | informational | 1 |

A first validator run reported 217 violations; 51 "evidence without source" and
10 "duplicate id" results were false positives (evidence that cites sources via
links; test fixtures). The rules were corrected and the corrected counts are above.
Independent replication then showed that 8 of the 9 "dangling references" in the
second run were caused by EKS itself: files whose front matter is invalid YAML (e.g.
an unquoted colon in a title) were dropped silently, so references to them looked
dangling. Parse failures are now kept as identity-only records and reported
(EKS0011: 2 public records); 1 genuine dangling reference remains.
Lesson recorded in `findings/accepted-ideas.md`: validation rules must be
calibrated on the real corpus before they block anything.

## Hand-maintained views drift (measured)

Six index-like documents (`context/DECISIONS.md`, `CURRENT-STATE.md`,
`TRACEABILITY.md`) enumerate a record family: five omit existing records
(e.g. praxis `context/DECISIONS.md` lists 3 of 52 decisions; fides
`context/CURRENT-STATE.md` 7 of 16). Only fides `context/DECISIONS.md` is complete.

## How agents consume knowledge today (audit, see current-agent-workflows.md)

Agents enter through CLAUDE/GEMINI/copilot files that point to AGENTS.md, then
BOOTSTRAP.md, then governance documents: about 9.6k–22.3k tokens are mandatory
before work in Praxis-governed repositories (bytes/4 estimate). Retrieval
afterwards is grep/glob over Markdown; no repository ships an index of its records.

## Problem attribution

| Problem | Cause |
|---|---|
| 25% duplicated tokens | repository organisation (vendoring) |
| Broken links concentrated in private repos without conventions | organisation |
| 68% of files lack an id | organisation / convention adoption |
| One-sided evidence links (28) and stale indexes (5 of 6) | maintenance of redundant hand-written data — solvable by deriving views |
| No cross-repository typed edges | missing schema feature |
| Requirements in four Markdown styles | convention, not syntax |
| Retrieval by whole-file reading | retrieval strategy / agent behaviour |

None of the measured problems is caused by Markdown syntax as such.
