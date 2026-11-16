# Structured Markdown (Markdown + YAML front matter) — candidate B / A

Already Echelon's practice for 568 records (32% of files). Measured:

* Trials: 1.00 accuracy (Sonnet, Haiku) with front matter and grep.
* Lossless round trip through the F# IR: 568/568, including unrecognised fields.
* Canonical minimal front matter is 6% smaller than current front matter
  (869,764 vs 926,439 tokens), mostly by dropping per-file provenance blocks that
  agents did not use in any task.
* Weaknesses found: two conventions for evidence sources; links declared on one
  side only (28); YAML implicit typing risks (handled by the IR keeping all
  scalars as strings); multi-record concatenation is ambiguous because bodies
  contain `---` (one record per file is required).

**Verdict: ADOPT as the canonical source**, with a validator and a compiler.
