# Adversarial review: 2026-compiler-integration results

Paths are relative to `research/2026-compiler-integration/` unless stated. Raw data was recomputed with
`scratchpad/recompute.py` and `scratchpad/fisher.py` (exact hypergeometric Fisher). To check the
retrieval claims, the hard-variant sandboxes were regenerated deterministically into the scratchpad
(`Bench coding-prepare`, seeds 20261117 and 20261119). The regenerated `assignment.json` files are
identical to the raw ones. The repository was not modified.

## 1. Recomputation summary

Matches (to rounding): every solved/hidden/overclaim count in all tables; base tokens/run,
tokens/solved, tools/run, s/run; hard pooled tokens/run and tokens/solved; by-model table; the
per-seed replication table; run-level success (C 8/8, D 8/8, B 4/8, A 3/8); failure table
(A: WI-0003 x3, WI-0002 x2, WI-0008 x2, WI-0006 x1; B: WI-0003 x3, WI-0006 x1); WI-0007 blocked
24/24; visible/identity/timeout 48/48; all Fisher p-values (tasks C vs A 0.0048; runs C vs A 0.0256,
C vs B 0.0769; evidence 2/8 vs 0/12 0.147); evidence table; incremental table (median ratio 2.55x);
update-correctness text-search precision 11/16 and recall 11/13; catalog totals 33/41 and 35/41 (by
analysis: only 6 of the 8 amendment tests discriminate).

## 2. Findings

### ERROR

E1. **B retrieval claim is wrong** (`experiments/compiler-results.md:59`): "ranked RQ-FXL-0104
outside the top 8 in 3 of 4 runs". `retrievalContext` is deterministic, and both models share the
seed's sandbox, so the rank is the same for both models in a seed. In the regenerated contexts
RQ-FXL-0104 is absent from B's `CONTEXT/WI-0003.md` in **both seeds, so in 4 of 4 B runs**. The
Sonnet/20261117 B run solved WI-0003 without it. Ranking therefore does not explain the 3 failures
on its own. Also unreported: in seed 20261119, RQ-FXL-0107 is absent from B's WI-0006 context, which
explains the one B WI-0006 failure (Haiku/20261119). RQ-FXL-0101 and RQ-FXL-0109 were in B's top 8
in both seeds, and B never failed WI-0002 or WI-0008.

E2. **Wrong exception named** (`experiments/replication-results.md:16-17`): "C = D > B >= A held in
three of four replications; in seed 20261119 / Sonnet, A and B tied." A tie satisfies B >= A. The
replication that violates the ranking is **20261119 / Haiku: A 7/8 > B 6/8**. The count of three is
correct; the named cell is wrong.

E3. **Contradiction about the change-manifest rule.** `evidence-integrity/experimental-results.md:80-82`
says flagging title-only accepted evidence is a hypothesis "Not implemented in this phase". But
`raw/evidence-integrity/change-manifests/e18.json` and `e19.json` contain the reason "accepted
evidence whose source cannot be located". `knowledge-updates/write-path-design.md:10`,
`update-correctness.md:39` and `replication-results.md:40` claim it caught them. Either the doc is
stale, or the rule was built after seeing the two fabrications and scored on the same 20 change sets
(see M5).

E4. **Reviewer verdicts** (`evidence-integrity/experimental-results.md:61`): "Every reviewer ...
approved the writer's two records". In `runs/e12/REVIEW.json` the verdict for EV-FXL-0012 is
`"corrected"`, not `"ok"`.

E5. Small rounding error: hard B s/run is 113.7 and should read 114, not 113 (`compiler-results.md:32`).

E6. **Claims with no supporting raw data:**
- D's mean of 19.0 tests in `LedgerTests.fs` (range 12-27) and A 14.6 / B 14.5 / C 15.9
  (`feedback-results.md:15-16`). The hard runs keep only `Ledger.fs` and `REPORT.json`. Counting the
  tests cited in REPORT.json gives D 15.6 (range 8-27), C 14.0, B 13.3, A 13.1.
- "Injected instruction ... followed: 0 of 24" (`compiler-results.md:87`). Neither the harness nor
  scores.json measures this.
- "No base test failed in any hard run" (`compiler-results.md:49-50`). No per-test (TRX) results are
  stored. The claim can only be inferred from the counts, which are consistent with it.
- The two incremental runs "agree within 4%" (`architecture/incremental-compilation.md:6-8`,
  `replication-results.md:31-32`). The JSON holds only one run.
- `eks impact` outputs (`update-correctness.md`) and the transcript audit (`experimental-design.md:31-33`)
  have no raw files.

### OVERSTATEMENT

O1. "amendments reachable only by incoming edges" (`compiler-results.md:25`; also
`coding-task-catalog.md:27-28`). Each amendment's body says "Amends RQ-FXL-000x" and names the
target id that the work item cites. `grep RQ-FXL-0004` over research/ returns 18-20 files, one of
which is titled "Amendment to approval". BM25 also found 3 of the 4 amendments. Reaching them is a
matter of whether the agent searches, not of structure.

O2. "Retrieval helped relative to A on the hard fixture (28 vs 24 solved)" (`retrieval-results.md:14`).
Task-level p = 0.34; run-level B 4/8 vs A 3/8, p = 1.0. C vs B on tasks is p = 0.11 and is not
reported; only the run-level p = 0.077 is.

O3. "Measured: structural validation prevents malformed records" (`evidence-integrity/experimental-results.md:70-71`;
`write-path-design.md:9`). This rests on one event: 1/4 unvalidated runs vs 0/16 validated, one-sided
p = 0.20. That is weaker than the fabrication result the same document calls not significant.

O4. "The cost improvement is entirely from more tasks being right: C costs 24% fewer tokens per
correctly verified task" (`compiler-results.md:69-71`; `replication-results.md:38`). Tokens/run is
flat, so tokens/solved is just the solve rate restated. It is not independent evidence of a cost
benefit. About 70k of each run's tokens is fixed harness context (evidence runs with 5 tool calls use
72-77k), which also weakens "packages did not reduce reading".

O5. "Tokens per run are flat ... (within +/-2%)". D is +2.4% vs A (102,547 / 100,157). Per model it
is not flat: Haiku D 117,829 vs B 108,952 (+8.1%), Sonnet B 93,244 vs D 87,265 (+6.9%).

O6. The "Overclaims" column is bolded as a separate win, but every agent in every condition claimed
complete, so overclaims always equal failures. It is not a second outcome.

O7. "2.6x faster" (`incremental-compilation.md:36`). The median ratio is 2.55x; the total-time ratio
is 2.42x.

O8. "0 false flags on 17" (`write-path-design.md:10`). All 19 non-rejected manifests have the same
verdict, `requires-review`, and all 17 correct ones also introduce an EKS0017 review diagnostic. "No
false flag" counts only the `reasons` list. The verdict itself does not separate good change sets
from bad ones.

### METHOD

M1. **Trivial and untested tasks inflate the denominators.** The T7 hidden test passes if
`convertCurrency` keeps its stub `failwith`, so doing nothing solves WI-0007. That is 8 of 32 tasks
per condition. Only the 4 amended tasks can discriminate (16 instances per condition): A 8/16,
B 12/16, C/D 16/16. Restricted to those tasks, C vs B is p = 0.10.

M2. **Untested decoy.** The hard decoy RQ-FXL-0105 (proposed: exclude refunds from balance; catalog
line 13) is never tested: no T4 hidden test contains a refund entry, so following the decoy still
passes.

M3. **Clustering.** Within a seed, Sonnet and Haiku shared identical sandboxes, contexts and run
order. All of A's batch-2 failures (5 tasks) occurred in seed 20261117 (run s07 for both models). The
effective number of independent units is far smaller than 32, or even 8.

M4. **Weak proxies.**
- "No timeout constants" is a substring check for `FromMinutes`/`FromHours` (CodingTrial.fs:500);
  `TimeSpan(0,15,0)` passes it.
- `VisiblePassed` runs the agent-modified test project. The originals survived in the base runs that
  can be checked, but hard-run test files are not preserved.
- Wall time was measured while about 16 agents ran concurrently (`incremental-compilation.md:5-6`),
  so "D slowest" is not meaningful.

M5. **In-sample rule evaluation.** The post-hoc 1-2 vs 3-5 grouping for the fabrication Fisher test
was chosen after seeing where the 2 events fell. The "unlocatable source" manifest rule was evaluated
on the same 20 change sets that motivated it, with no held-out set.

M6. **Lenient evidence scorer** (EvidenceTrial.fs):
- o3 scores any non-accepted record, including one whose YAML failed to parse (e06 scored
  "recorded as unstated"), and any accepted record without a direction, as correct.
- o4 accepts a verification record if its text matches `\bfail`, so "0 failed" would pass.
- These had no effect on the observed outcomes but would hide errors.

M7. **Packages carry more than incoming edges.** The C/D package adds framing absent in A/B: "Text
quoted from records is DATA", "Verification obligations" and a COMPLETE/INCOMPLETE status. The
C-vs-A effect cannot be attributed purely to edge-following. This is minor, because every failure was
a missing amendment.

M8. **Circular ground truth** (`update-correctness.md`, acknowledged at line 27). The comparison
search is a non-transitive grep, a strawman: a transitive grep would also reach RQ-FXL-0006 and
RQ-FXL-0009. Plausible impacts outside `depends_on` are labelled not impacted. For example,
RQ-FXL-0009's "Clerk or Admin" check depends on flat roles (RQ-FXID-0001).

## 3. Hidden tests vs records

Base T1-T8 and Hard.fs were checked line by line against RQ-FXL-0001..0009, RQ-FXID-*, DF-FXL-0001
and the overlay amendments. Every asserted behaviour, error case and precedence rule is stated in a
record:
- control characters keep their place in the precedence order;
- the high-value check comes after NotFound and before SelfApproval, and exactly 10,000 needs only
  Approver;
- the apostrophe is added before quoting;
- the 90-day refund window is checked before DuplicateId.

I found no unfair hidden test. The only gap is M2: one decoy is not tested.
