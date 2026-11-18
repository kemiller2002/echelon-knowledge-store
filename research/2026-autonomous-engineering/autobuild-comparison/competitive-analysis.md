# Competitive analysis: Obvious AutoBuild

Mission EKS-RESEARCH-2026-003 · sources accessed 2026-11-18 · detailed notes and source list:
`data/autobuild-research-notes.md`; capability table: [`capability-matrix.md`](capability-matrix.md).

## What it is (documented)

A hosted, spec-driven, multi-agent "software factory":
- **Intake:** work comes in from Slack, tickets, triggers or schedules.
- **Planning:** an agent drafts a "Blueprint" (product / UX / technical spec) that a human
  approves.
- **Execution:** an orchestrator splits the work into initiative > feature > executable
  ("one task, one agent, one PR"). Workers run in per-repository cloud sandboxes and
  carry PRs through CI and an automated rubric review to merge. Human-merge and human-PR
  gates are available.
- **Pricing:** Standard $199 and Pro $499 per seat per month, plus usage credits.
- **Models:** routed per step ("Fusion"). Listed models include Claude Sonnet, Opus and
  Haiku 5.5, GPT-6.1 Sol and GLM 5.3.

## Evidence quality

- **Independently verified:** none of the 14 capabilities. No third-party review,
  benchmark or technical discussion was found.
- **Demonstrated through public GitHub activity:** 6. About 5,706 bot PRs were found:
  5,007 merged, 201 closed unmerged and 498 open.
- **Documented by the vendor:** 7.
- **Inferred:** 1.

The vendor's 96% decided-merge rate is consistent with the public count (96.1%), but
merge rate is not correctness.

The often-quoted Storybook figure ("87 of 143 merged") counts still-open PRs as
unmerged, so it is **not** comparable with the 96% decided-merge rate. This was corrected
after review. Every other quantitative claim is unverified: 6.9×
velocity, "3 in 4 merged PRs", and "six months in 48 hours", which the same page also
gives as 24 hours.

## Where AutoBuild is ahead

1. **Product surface and attention routing:** intake, inbox and build drawer, with mobile
   apps. Echelon is CLI-first.
2. **Operational scale:** thousands of PRs in public repositories. Echelon has no
   comparable throughput evidence.
3. **Spend controls and cost-per-PR dashboards** exist as product features. Echelon
   records cost telemetry but has no budget controls and no measure of human attention.
4. **Skill loading discipline:** skills load only from the default branch, so an
   unmerged branch cannot change agent instructions.

## Where Echelon is ahead (in design, not deployment)

1. **Verification independence.**
   - Praxis can require claim-bound validation results produced by Praxis itself, or
     receipts signed by a host-held verifier whose key is pinned in protected policy.
     The policy is read from the protected merge base, so the completing agent cannot
     weaken it.
   - AutoBuild documents a rubric reviewer and QA evidence, but not how the reviewer is
     kept independent of the author.
   - **However**, Echelon's verified completion is enabled in 0 of 13 repositories
     (`verification-audit.md` V3). Today the advantage exists only on paper.
   - AutoBuild PRs, by contrast, already pass through each customer's own CI and review
     bots. Those checks are deployed and independent of the authoring agent. Echelon has
     nothing equivalent deployed. Calling Echelon "ahead" here compares Echelon designs
     with AutoBuild documentation; this mission did no hands-on AutoBuild trial.
2. **Governed learning.**
   - AutoBuild consolidates memory nightly without approval. It requires approval for
     rules, but documents no counterfactual or regression evaluation, no version history
     and no diff.
   - Echelon now has a contract (praxis#302) that refuses memorised, regressing or
     self-approved lessons. It has no runner yet.
3. **Provenance and telemetry discipline:**
   - every actor's identity is recorded;
   - provider-reported and estimated costs are kept separate;
   - unavailable data is never recorded as zero.

## Where neither has evidence

Neither has evidence of higher correctness than a single strong agent on the same task.
This program's controlled experiment found that a current Opus-class agent alone solved
every trap task (`clarity-edf-experiment-results.md`). The value of either factory
therefore has to come from throughput, safety and attention routing, not from per-task
correctness on small tasks.

## Bottom line

AutoBuild competes on throughput and product, Echelon on verifiable correctness and
governance. Echelon's position only holds if it:
1. turns its verification design into a default (deploy `complete-verified`);
2. measures human attention;
3. ships a thin control plane over existing data.

Building more analysis frameworks into the loop is not supported by evidence.
