# Round-trip validation

Measured on 568 front-matter records (`experiments/results/formats.md`):

| Path | Equal |
|---|---|
| Markdown → record (all fields) → canonical Markdown → record | 568/568 |
| record (core) → JSON → record | 568/568 |
| → JSONL → record | 568/568 |
| → YAML stream → record | 568/568 (after fixing document splitting: bodies contain `---`) |
| → DSL → record | 568/568 |
| → per-record structured Markdown → record | 568/568 (concatenated files are ambiguous: 0/568) |

What "equal" means: identical typed record including every unrecognised field
(`Extra`). Not compared: byte identity of the original file. Canonical output
reorders front-matter keys, normalises quoting, and trims leading/trailing blank
lines of the body — formatting changes, not meaning changes, but they would show
in a diff. A migration that rewrites files must therefore be a separate,
reviewed, formatting-only PR, verified by re-parsing to the same record.

Semantic loss detected: none. Broken links introduced: none (paths unchanged).
Requirements or decisions altered: none (equality is on every field).
