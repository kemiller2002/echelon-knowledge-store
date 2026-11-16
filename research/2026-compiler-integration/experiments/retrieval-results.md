# Condition B (improved retrieval): results

Agents received, per work item, the top-8 BM25 sections over all Markdown in both repositories,
queried with the work item's title and description (`CodingTrial.retrievalContext`). Full comparison:
`compiler-results.md`.

| Fixture | Runs | Solved | Overclaims | Tokens/solved |
|---|---|---|---|---|
| Base (Sonnet) | 4 | 16/16 | 0 | 20,549 |
| Hard (Sonnet + Haiku, two seeds) | 8 | 28/32 | 4 | 28,885 |

Observations (measured):

- Retrieval helped relative to A on the hard fixture (28 vs 24 solved) but did not close the gap.
- Its failures were concentrated on WI-0003 (3 of 4 B runs that had it): the query "Implement
  entry approval (RQ-FXL-0004)" did not rank the amendment RQ-FXL-0104 ("high-value entries need
  Approver and Admin") in the top 8. The amendment shares few terms with the query; the distractors
  share many. That is the expected weakness of lexical retrieval for constraints linked by reference
  rather than by vocabulary.
- B passages include stale and superseded text (the README ranked high for WI-0005 and WI-0006).
  Agents still followed the accepted records.
- Retrieval did not reduce tokens per run (agents still opened files).
