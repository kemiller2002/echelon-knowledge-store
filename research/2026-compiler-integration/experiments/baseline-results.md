# Condition A (existing workflow): results

Agents received the two repositories and the task; nothing else. Full comparison and pooled
tables: `compiler-results.md`.

| Fixture | Runs | Solved | Overclaims | Tokens/solved |
|---|---|---|---|---|
| Base (Sonnet) | 4 | 16/16 | 0 | 19,695 |
| Hard (Sonnet + Haiku, two seeds) | 8 | 24/32 | 8 | 33,386 |

Observations (measured):

- On the base fixture, condition A was as accurate as every other condition and the cheapest.
  Agents read `AGENTS.md`, followed the record conventions, preferred accepted records over the stale
  README, honoured supersession, refused the undecided currency task and ignored the injected
  instruction, without any compiled context.
- On the hard fixture, every A failure was a missed amendment, and every failure was reported as
  complete. The agent's own tests passed because they tested the requirement the agent had read.
  This is the failure mode compiled packages address: the agent does not know what it has not found.
- Haiku in condition A used the most tokens per solved task of any cell (37,393).
