# Canonical source analysis

| Candidate | Accuracy (trials) | Tokens (full / metadata) | Round-trip | Human editing & diffs | Tooling / agent familiarity | Security | Verdict |
|---|---|---|---|---|---|---|---|
| A. Markdown + YAML front matter, F#-validated | 1.00 | 1.00 / 69.9k | lossless 568/568 | best; already used for 568 records | all coding agents read it natively | data-only parse | **ADOPT** |
| B. JSON / JSONL + F# schema | 0.98–1.00 | 1.10–1.13 / 69.6–91.9k | 568/568 | bodies become escaped strings; poor review diffs | high | safe | REJECT as source; ADOPT as export |
| C. F# source records | not trialled (equivalent content) | 1.00 / 121.9k (+75% metadata) | writer only; reading safely needs the F# compiler parser | verbose; compile on every edit | moderate | executing `.fsx` = arbitrary code | REJECT |
| D. Compact DSL parsed into F# types | 1.00 | 0.98 / 63.3k (−9% metadata) | 568/568 | readable but new | none native; one-paragraph spec sufficed | data-only parser possible | REJECT (gain ≈0.7% of tokens, new language to maintain) |
| E. Hybrid: A + derived index/graph + compiled views | 1.00 | packages 3.2k per question | — | as A | as A | as A + generated views | **ADOPT** (E = A + compiler) |
| F. Simpler: A + validator only, no compiler | 1.00 for capable agents | as A | as A | as A | as A | as A | viable floor; loses derived inverse links, generated indexes and packages |

Architecture E is A plus derived outputs, so the choice of canonical source is
A. F is the fallback if the compiler is not worth maintaining; the ablation shows
what it loses (−32 points evidence recall without derived inverse links when
reading single files; stale hand-written indexes).
