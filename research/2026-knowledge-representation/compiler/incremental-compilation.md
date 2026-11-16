# Incremental compilation

Design: parse results are cached by `(repository/path, SHA-256)`. Graph building,
validation and partial evaluation are recomputed every time — they are cheap at
this scale and recomputation avoids incremental-graph invalidation bugs.

Measured (public corpus, 1,615 files, 587 records; medians of 5 runs; `experiments/results/compiler.md`):

| Scenario | Time |
|---|---|
| cold (parse + graph + validate + precompute) | 150–360 ms (varies with JIT tiering) |
| parse only | ~87–95 ms |
| warm, no change | ~45–55 ms |
| warm, 1 file changed | ~41–45 ms |
| warm, 10% changed | ~54–66 ms |

Correctness: warm outputs are byte-identical to a cold compile of the same
inputs in every scenario (benchmark) and in a unit test with an edited record.

Staleness: `manifest.json` records every input hash; `eks verify` and every
query with `--artifacts` compare it with current sources and refuse (exit 2) on
any difference — a stale index cannot answer silently.

Incremental *graph* updates were not implemented: a full graph pass costs less
than parsing one tenth of the files.
