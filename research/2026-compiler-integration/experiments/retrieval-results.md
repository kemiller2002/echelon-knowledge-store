# Condition B (improved retrieval): results

Agents received, per work item, the top-8 BM25 sections over all Markdown in both repositories,
queried with the work item's title and description (`CodingTrial.retrievalContext`). Full comparison:
`compiler-results.md`.

| Fixture | Runs | Solved | Overclaims | Tokens/solved |
|---|---|---|---|---|
| Base (Sonnet) | 4 | 16/16 | 0 | 20,549 |
| Hard (Sonnet + Haiku, two seeds) | 8 | 28/32 | 4 | 28,885 |

Observations (measured):

- B solved more than A on the hard fixture (28 vs 24; amended tasks 12/16 vs 8/16), but the
  difference is not significant (p ≈ 0.27 on amended tasks) and A beat B in one replication.
- The retrieved context for WI-0003 never contained the amendment RQ-FXL-0104 ("high-value entries
  need Approver and Admin"): 0 of 4 runs. Three of those runs failed; one agent found the amendment in
  the repository anyway. RQ-FXL-0107 was missing from WI-0006 context in the two seed-20261119 runs
  (one failure). The amendments share few terms with the work-item text while the distractors share
  many; that is the expected weakness of lexical retrieval for constraints linked by reference rather
  than by vocabulary.
- B passages include stale and superseded text (the README ranked high for WI-0005 and WI-0006).
  Agents still followed the accepted records.
- Retrieval did not reduce tokens per run (agents still opened files).
