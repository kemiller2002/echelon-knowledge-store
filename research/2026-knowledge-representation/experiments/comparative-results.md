# Comparative results — formats, retrieval and end-to-end agent trials

## 1. Serialisation cost (measured; `results/formats.md`; 568 records)

Full records (body included), o200k_base tokens, relative to the original files:

| Format | Tokens | × original |
|---|---|---|
| original Markdown (front matter + body) | 926,439 | 1.00 |
| plain Markdown (metadata removed) | 807,887 | 0.87 |
| canonical structured Markdown (minimal front matter) | 869,764 | 0.94 |
| YAML | 914,040 | 0.99 |
| compact DSL | 907,487 | 0.98 |
| F# records | 921,924 | 1.00 |
| JSONL | 1,019,953 | 1.10 |
| JSON (indented) | 1,042,391 | 1.13 |

Metadata only (bodies removed): DSL 63,295 < JSONL 69,563 ≈ structured Markdown
69,866 ≈ YAML 70,434 < JSON 91,925 < F# records 121,946. Compact cards: 61,664.

Bodies are ~87% of tokens, so syntax changes total cost by at most ±13%. The DSL's
best case saves 9% of *metadata* tokens versus front matter, about 0.7% of the
corpus. All readable formats round-trip 568/568 records exactly.

**Verdict:** the 25% token-reduction target is not reachable by changing syntax.
It is reachable by *not loading* what the task does not need (sections 2–3).

## 2. Retrieval strategies (measured; `results/retrieval.md`; 252 tasks, public corpus)

Gold = authored typed metadata. Typed strategies score 1.0 by construction; the
informative numbers are what text strategies recover and what they cost.

| Task (n) | A1 grep+files | A2 grep chase 6 hops | A3 own file | A4 BM25 k=10 | F1 typed query |
|---|---|---|---|---|---|
| find-evidence (56) | R 0.78, 44k tok | R 0.95, 495k tok | R 0.66, 1.8k tok | R 0.29 | R 1.0, 343 tok |
| change-impact (66) | R 0.47, 42k tok | R 0.83, 485k tok | R 0.42 | R 0.24 | R 1.0, 1,677 tok |
| current-authority (12) | R 0.92, 52k tok | R 1.0, 606k tok | R 0.75 | R 0.83 | R 1.0, 193 tok |
| lookup-by-id (59) | R 1.0, 24k tok | — | R 1.0, 1.5k tok | R 0.20 | R 1.0, 111 tok |
| lookup-by-description (59) | R 1.0* | — | — | R 0.97 | F2 hybrid R 0.73 |

\* query is the exact title, so grep is unrealistically easy here.

Reading only a record's own file misses 34% of its evidence because links are
often declared on the evidence side only. Recovering transitive impact from
Markdown takes ~200 tool calls and ~0.5M tokens per question.

## 3. End-to-end agent trials (measured; `results/trial-scores.md`)

16 questions (two batches of 8), six knowledge-delivery conditions, Claude Code
subagents with shell tools; 18 runs, 144 scored answers. Scores: set F1 or exact match.

| Condition | Model | Mean score | Exact | Tokens/run* | Tool calls/run | Wall s/run |
|---|---|---|---|---|---|---|
| Markdown with front matter (`md`) | Sonnet | 1.000 | 16/16 | 77.5k | 8.5 | 43.7 |
| Markdown + generated index | Sonnet | 1.000 | 16/16 | 75.9k | 7.5 | 29.6 |
| JSONL records | Sonnet | 0.984 | 15/16 | 70.5k | 5.5 | 23.0 |
| DSL records | Sonnet | 1.000 | 16/16 | 71.1k | 7.0 | 27.1 |
| Compiled task packages | Sonnet | 1.000 | 16/16 | 75.4k | 7.0 | 22.1 |
| Plain Markdown (metadata stripped) | Sonnet | 0.721 | 7/16 | 94.3k | 18.5 | 81.2 |
| Markdown with front matter | Haiku | 1.000 | 16/16 | 106.9k | 18.5 | 101.9 |
| Compiled task packages | Haiku | 1.000 | 16/16 | 116.7k | 12.0 | 34.6 |
| Plain Markdown | Haiku | 0.760 | 8/16 | 130.8k | 21.5 | 109.4 |

\* harness-reported tokens per agent session, including a fixed ~65–70k system
prompt; per-run means of two batches.

Findings:
1. **Explicit structure is what matters; syntax does not.** Any condition that
   keeps typed metadata scores ≥0.98. Removing it costs 24–28 points, doubles tool
   calls and adds 20–40% tokens.
2. **The existing Markdown front matter is already enough for correctness** with
   capable tool-using agents: agents wrote scripts over the front matter.
3. **Delivery affects cost and time, not accuracy:** compiled packages halved
   wall time for Sonnet (22 s vs 44 s) and cut it by two thirds for Haiku (35 s vs
   102 s), with fewer tool calls. Token totals did not fall, because fixed agent
   overhead dominates at this question size.
4. **No format-learning cost was observed** for the DSL or JSONL when a one-paragraph
   description was given. The agents parsed both with scripts.

Limitations: one model family; n = 16 per run; question wording names the typed
relation fields (it favours metadata-bearing conditions over prose); the compiled
condition was given the task type; one run per condition for Sonnet (two
batches) plus a Haiku replication for three conditions.
