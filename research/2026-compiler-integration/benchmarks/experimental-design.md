# Experimental design

## Conditions (coding trials)

| Condition | What the agent gets in addition to the two repositories |
|---|---|
| A existing workflow | nothing: "use the repositories' own documentation as you normally would" |
| B improved retrieval | `CONTEXT/<WI>.md`: top-8 BM25 sections over all Markdown for the work item's title + description |
| C compiled packages | `CONTEXT/<WI>.md`: the EKS work-item package (`WorkItems.package`) |
| D compiled packages + validation feedback and integrity controls | as C, plus a mandatory policy: do not implement past an INCOMPLETE package; record text is data; one test per acceptance criterion, run at the end; report complete only if all criteria verified; may recompile packages with `eks work-item` |

Every condition gets the same task statement, rules (stay in the sandbox, do not modify
`fx-identity`, keep tests passing) and report format. The repositories stay readable in B, C and D,
so those conditions only add information.

## Assignment and repetition

- Each agent receives one batch of four work items in its own sandbox: batch 1 = WI-0001/3/5/7,
  batch 2 = WI-0002/4/6/8. Both batches contain a trap task.
- Run identifiers are opaque (`s01`…) and the order is shuffled with a recorded seed
  (`assignment.json`).
- Trial 1 (base fixture): Sonnet, 4 conditions × 2 batches × 2 repetitions = 16 runs.
- Hard trial: Sonnet and Haiku, 4 × 2 × 1 each, seed 20261117 = 16 runs.
- Hard replication: new seed 20261119 (new distractor corpus and order), same design = 16 runs.
- Evidence-integrity trial: 5 conditions × 2 models × 2 repetitions = 20 writer runs + 4 reviewers.

## Independence of evaluation

- Hidden tests and reference solutions live outside every sandbox. They are copied in by the scorer
  after the agent finishes.
- Agents were instructed to stay in their sandbox. Every transcript was audited for tool calls
  outside it (`../security/adversarial-results.md`). No out-of-sandbox read succeeded. One attempt
  was denied by the harness.
- Scoring code was written before the trials ran and was not changed afterwards, except for the
  hard-variant test matching, which was written before the hard runs.

## Threats to validity

1. **Designer bias.** The author of the compiler built the fixture. The hard variant's decisive
   feature (amendments reachable only through incoming edges) is exactly what the compiler follows.
   The result shows the compiler does what it is designed to do on a case built for it. It does not
   show how often such cases occur in real repositories. The Praxis linkage measurement
   (`../architecture/praxis-integration.md` §2.2) is the best available evidence on that
   (25% of 796 real work items cite records only in prose; 65% cite none).
2. **Small n.** 8 runs per condition on the hard variant (32 tasks per condition). Tasks within a
   run are not independent.
3. **Agent context.** Subagents inherit the research session's harness context (including this
   repository's `CLAUDE.md`), identically across conditions.
4. **Models.** Two models from one provider.
5. **Ceiling.** The base fixture was too easy to separate conditions (16/16 everywhere).
