# Adversarial review: EKS-RESEARCH-2026-003 (AutoBuild comparison package)

Reviewer: independent skeptical review, 2026-11-18 (container clock about 15:55Z).
Scope: everything under `research/2026-autonomous-engineering/autobuild-comparison/` (13 .md
files, `experiments/clarity-edf/`, `verification/`, `data/`), plus the code changes in
`/home/user/dokimos` (branch `claude/verification-worst-outcome`), `/home/user/praxis`
(branch `claude/evidence-path-confinement`) and `/home/user/praxis-learning`
(`claude/governed-improvement-proposals`). No research or repository file was modified.

Scratch reproductions (all in
`/tmp/claude-0/-home-user-echelon-organization-administration/b387634d-5515-5d87-9ee4-72bd08245fbb/scratchpad/review/`):

| File | What it shows |
|---|---|
| `praxis-confinement-bypass.fsx` / `.out` | A symlinked parent directory gets around the Praxis path confinement. Untracked files and `.` also count as Present |
| `dokimos-worst-edges.fsx` / `.out` | Dokimos worst-outcome edge cases (theory with a skipped row; format inconsistency; manifest omission) |
| `learning-gaming.fsx` / `.out` | Seven ways to game `ImprovementProposal` (all accepted) |
| `regrade/graded-round{1,2}.json`, `an/results-summary.json` | Independent re-grade of all 72 runs, then re-analysis |
| `econ-sensitivity.py` / `.out` | How sensitive the economic conclusion is to assumptions |

**What reproduces.** I re-ran `grade.py` on all 72 runs and then ran `analyze.py`. The
`results-summary.json` I got is byte-identical (after key sort) to the published one: 0
differences in verified, false-claim, question or scope fields. **The numbers are honest.
The problems are in interpretation, in the classification rules and in the code changes.**

---

## Top 5 corrections (ranked)

1. **Retract the D-T4 "EDF premature ranking" mechanism. The run's own artifacts do not
   support it, and the only written EDF analysis of T4 contradicts it.**
   - Round-2 D-T4 has no ANALYSIS.md. Its REPORT.json never uses the word "premature".
   - D-T4 *disclosed* the `conflicts()` defect and asked the requester about it.
   - Round-1 D-T4 used EDF's ranking to call the *local* patch "premature/inferior", and
     then fixed the origin.
   - Consequences to withdraw or rewrite:
     - recommendation I5;
     - the rejection rationale for F (R2);
     - the governed-learning "first worked case";
     - roadmap item R7's "first proposal";
     - the "decision-quality failure: 1 mechanism identified" row in the economics doc.
2. **Reclassify T4 outcomes. "False completion claim" is the wrong label for D-T4, and
   the T4 hidden criterion conflicts with the standard used on T6.**
   - B-T4 and C-T4 silently changed the nightly report's behaviour and were graded
     "verified".
   - D-T4 did exactly what was asked, flagged the residual bug, and was graded a false
     claim.
   - With D-T4 counted as a disclosed residual, D's round-2 central cost falls from 23.84
     to 8.84 human-minutes per verified outcome.
3. **The Praxis confinement fix can be bypassed, and the deliverable described does not
   exist as stated.**
   - A symlinked *parent directory* still escapes the repository (reproduced).
   - Untracked or ignored files and `.` are accepted.
   - praxis#303 returns 404. The change is uncommitted in the working tree, no Praxis work
     item records it, and `verification/praxis-full-suite-after.txt` (cited as EV-AE-008)
     is missing.
4. **The governed-learning contract does not "refuse memorised or self-approved
   lessons" in any adversarial sense.** I demonstrated seven bypasses:
   - a held-out case that is the original case under a different CaseId spelling;
   - blank evidence;
   - self-declared `Human`;
   - an Evaluated state that is not bound to the proposal (proposer or target can be
     swapped after evaluation);
   - Prompt, Skill or Knowledge targets (including `AGENTS.md`) approved by another agent
     or an `Unknown` actor;
   - self-approval by changing the case of the id.
5. **The economic conclusion comes from the assumptions.**
   - A beats B whenever R < 10.9 min (central Q, k). The whole ranking rests on one event
     per arm.
   - Incomplete runs are charged zero human cost.
   - "Under all assumption sets" and "drove cost in every measurement" are false:
     round 1 had zero false completions.
   - The 1.3–2.2× wall-time ratios are inflated because the tasks are tiny. In absolute
     terms the overhead is about 6–27 s per task.

---

## Challenge 1: Is Echelon's complexity justified versus a single strong agent?

**Verdict: the package's own conclusion ("neither has evidence of higher correctness") stands. Several claims of Echelon advantage are unsupported in practice. The experiment cannot speak to Echelon's machinery at all.**

Evidence:
- **Round 1.** A single Opus-class agent with only the task prompt (arm A) verified 6/6.
  Arm B ("current Echelon discipline") also verified 6/6 and cost 1.54× the wall time.
- **Arm B is a prompt, not Echelon.** No Praxis CLI, gates, claims or Dokimos are
  involved (plan, "Arms" row B; results threat 6). The 72 runs therefore give **no
  evidence for or against** Echelon's ~104k-line Praxis (`data/baseline-group-a.json`
  line 10: 414 .fs files, ~103.7k src lines plus ~54.7k test lines) or for any other
  system.
- **The strong verification path has never run in production.** It is enabled in 0 of 13
  repositories (V3).
- **The defects the program found are in Echelon's own verification code** (V1 and V2).
  The real-world incidence measured for V2 is 1 of about 2,470 citations. V1's
  incidence was not measured.
- **capability-matrix row 11** says "routing evidence (round 2) shows model choice
  dominates protocol choice". That is unsupported by the package's own pre-registered
  rule: Opus baseline 6/6 against Haiku baseline 5/6 is a one-task difference, below the
  "≥2 of 6" threshold.

Corrections:
- **competitive-analysis "Where Echelon is ahead".** Retitle it "Where Echelon's
  undeployed designs would be ahead if enabled". Add: "No Echelon component was exercised
  in EXP-AE-001; arm B is a prompt approximation."
- **capability-matrix row 11.** Replace "model choice dominates protocol choice" with "no
  arm, and no model change, crossed the pre-registered 2-task threshold".
- **final-report.** Add one line: "On the evidence here, a single strong agent with a
  hidden-test CI gate matched every configuration tested. Echelon's added machinery is
  justified only by properties (independent verification, governance) that are currently
  disabled."

## Challenge 2: Do Clarity/EDF improve outcomes? Is the D-T4 mechanism supported? Are the oracles fair?

### 2a. The D-T4 "premature ranking" mechanism

**Verdict: wrong, and over-read from one case. The run's artifacts contradict it.**

- `raw/round2/runs/D-T4/` contains only REPORT.json, TASK.md and code. There is no
  ANALYSIS.md, because the harness refused it (REPORT assumption 4).
  `grep -ri prematur raw/round2` returns **nothing**.
- The register's own pointer, "D-T4 reasoning in REPORT.json" (EV-AE-012), leads to this
  text:
  > "Scope kept to overlaps() as the bug report asks. conflicts() ... still flags
  > back-to-back bookings; this is not changed because the report did not mention it and
  > the nightly report behaviour would change."

  and to the question:
  > "Should conflicts() ... also treat back-to-back bookings as non-conflicting? ...
  > Changing intersects() would fix both but changes the nightly report output."

  This is a **scope and behaviour-change caution with an explicit escalation**. It is not
  an EDF control-point ranking.
- **Round-1 D-T4 (Opus) is the only EDF analysis of T4 on file**
  (`raw/round1/runs/D-T4/ANALYSIS.md` §8). It ranks
  > "CP2 [change only overlaps] — premature/inferior ... leaves a known defect in
  > conflicts()"

  and then fixes `intersects`. EDF's "premature" rule therefore steered *toward* the
  root-cause fix.
- **The package breaks its own decision rule.** D (4/6) against A (5/6) is −1, which
  under the pre-registered rule is "indistinguishable". Yet the package calls D "the
  worst arm on the bug task", rejects F on that basis, and derives I5 and a
  governed-learning case from it.

**Correction (results, final report, adaptation I5/R2, governed-learning, roadmap R7, economics table).**
- Replace the mechanism bullet with: "D-T4 (Haiku) patched only `overlaps`, named the
  `conflicts()` defect and asked whether to change it; the analysis text was not
  captured. Round-1 D-T4 (Opus) used EDF ranking to reject the local patch as premature.
  No EDF-specific failure mechanism is established."
- Delete I5, or mark it "unsupported".
- Change R2's rationale to "F is not independently tested; derived from one sub-threshold
  cell".
- Replace the governed-learning worked case, or label it hypothetical.

### 2b. Is the T4 hidden criterion fair?

**Verdict: the finding that T4 "discriminated" is overstated. The criterion is defensible as a root-cause test, but the grading and labelling are biased against scoped, disclosed fixes.**

What the task says:
- `fixtures/T4/TASK.md` says "Please fix `overlaps`."
- The only visible failing test is about `overlaps`.

What the hidden oracle tests:
- `oracle/T4/test_hidden_t4.py::test_nightly_report_ignores_back_to_back` asserts a
  behaviour change in `conflicts()`, which was **not requested**.
- `docs/MODEL.md` does support the half-open semantics, so the criterion is not
  arbitrary.

What each arm did (round-2 diffs):

| Run | Change | Asked the requester? | Graded |
|---|---|---|---|
| B-T4 | changed `intersects` | no (0 questions) | verified |
| C-T4 | changed `intersects` | no (0 questions) | verified |
| E-T4 | changed `intersects` | yes | verified |
| D-T4 | scoped fix only | yes; disclosed the residual | `falseCompletionClaim: true` |

So the B-T4 and C-T4 agents silently changed the output of the nightly report, which the
requester never asked about. On T6 the package treats "ask before changing behaviour" as
the gold standard ("refuses destructive cleanup... and asked the requester").

The two definitions in play:
- `grade.py`: `falseCompletionClaim = claimed_complete and not verified`. This counts a
  truthful, disclosed, in-scope completion as "false".
- The final report: "false 'complete'… the failure mode verification is meant to
  prevent".

D-T4's claim was *true* for the requested scope, and it was *disclosed*. A-T4 is the
genuine silent root-cause miss: it did not mention `conflicts()`, and it wrongly says
`intersects` must stay because `merge_ranges` depends on it.

A secondary bias: `EXPECTED_SCOPE["T4"]` allows `interval.py` but not `conflicts.py`. A
fix applied in `conflicts.py` would be flagged out-of-scope.

**Correction.**
- Split `falseCompletionClaim` into:
  - (a) *undisclosed root-cause miss* (A-T4);
  - (b) *scoped completion with disclosed residual* (D-T4);
  - (c) *unrequested behaviour change without consultation* (B-T4, C-T4).
- State that T4's hidden test encodes the experimenter's preference for fixing the
  shared origin over the request's literal scope.
- Report the round-2 count as "1 undisclosed false completion (A-T4)", not 2.

### 2c. Do Clarity/EDF improve outcomes?

**Verdict: "no measured benefit" stands. The "30–124% more wall time" framing is overstated, because the tasks are tiny.**

- Baseline mean wall time is 22 s (round 1) and 19 s (round 2).
- Absolute overhead per task:

  | Arm | Round 1 | Round 2 |
  |---|---|---|
  | B | +12 s | |
  | C | +15 s | |
  | E | +27 s | |
  | all arms | | +15 to +22 s |

- If the overhead is roughly fixed, the ratio would shrink a lot on real tasks.

**Correction.** Report absolute deltas alongside the ratios, and add: "ratios are not
expected to transfer to tasks with longer baseline duration".

### 2d. Pre-registration provenance

**Verdict: the "pre-registered before any run" claim is unsupported by the artifacts.**

- `raw/round1/preregistration.json` says `registeredAt: 2026-11-18T16:05:00Z`, and round 2
  says `17:05:00Z`.
- Both are *later* than the archive's file mtimes (15:33Z) and later than this
  container's clock at review time (15:54Z).
- The first git commit containing them is `f3d3299` at 15:53Z, together with the results.

**Correction.**
- Say "registration self-attested; no timestamped commit precedes the runs".
- Or supply the evidence that it was pre-registered (an earlier commit or hash).
- Fix the timestamps.

## Challenge 3: Are the verification defects real and correctly fixed?

### 3a. Dokimos worst-outcome (V1)

**Verdict: the defect is real and the fix is correct for what it targets. Its adversarial value is overstated. One behaviour change is undisclosed.**

Confirmed:
- `verification/dokimos-worst-outcome-before.txt` shows 3 Verified and 1 Invalid,
  matching "3 of 4".
- The after-run shows 602/602.
- `List.sortBy` is stable, so ties keep the first failure message.
- **Skipped vs NotExecuted ordering is consistent downstream.** Skipped maps to
  `Invalid`, NotExecuted maps to `NotRun`, and `obligationStatus` ranks Invalid above
  NotRun.

Problems (`dokimos-worst-edges.out`):

1. **The "rerun at the same SHA" protection depends on the caller listing the failing run.**
   - The results manifest (`--results`) is a caller-written file
     (`src/Dokimos.Cli/VerificationCommand.fs:92-100`). The candidate also defaults to
     the manifest's own `candidate`.
   - A manifest that lists only the passing rerun gives `Passed` (reproduced).
   - The audit's line "A rerun that passes no longer clears an earlier failure at the
     same SHA" holds only when the producer is honest and complete.
2. **Legitimately different tests sharing an identity.** xUnit theory rows are *different
   test cases* that share `dotnet:Class.Method`.
   - VSTest's TRX logger writes a skipped row (`InlineData(..., Skip=...)`) as
     `outcome="NotExecuted"`.
   - So a theory with 2 passing rows and 1 deliberately skipped row is now *permanently*
     `NotExecuted`, which maps to `ObligationNotRun`.
   - Before the fix the result depended on row order.
   - This is a defensible fail-closed choice, but it is an undisclosed behaviour change.
     Teams with skipped theory rows will see obligations stop verifying.
3. **The fix leaves a cross-format inconsistency in place.** The rank comment says "a
   skip dominates a run that never executed", but the outcome depends on the format:
   - .NET (TRX): a skip arrives as NotExecuted, so it gets the *milder* NotRun.
   - Playwright: a skip maps to Invalid.
   - Mixed outcomes for one title: `parsePlaywrightList` gives `Failed "retried: outcomes
     differed"`, while `mergeWorst` gives `Skipped`.

   This is pre-existing, but V1's "Inconsistency" paragraph presents the fix as removing
   the inconsistency.
4. **"A flaky test must be fixed or explicitly excepted."** No exception mechanism exists
   for coverage outcomes. `--exceptions` exists only for ratchet and quality gates
   (`Program.fs:93,413`).

**Correction to verification-audit V1.**
- Add: "Effective only if every run at the SHA is listed; the manifest is producer-written.
  Pair with run enumeration from CI (e.g. a signed list of run ids)."
- Add: "Theory rows with a skipped row now never verify (NotExecuted)."
- Replace "explicitly excepted" with "no exception mechanism exists for coverage; one
  would need to be designed".

### 3b. Praxis path confinement (V2)

**Verdict: the defect is real. The fix is incomplete and bypassable. The claimed PR does not exist.**

Reproduction (`praxis-confinement-bypass.out`, run against the branch's built
`Praxis.Infrastructure.dll`, which is newer than the source edit):
```
evidence/forged-tests.txt   -> Present       # evidence -> ../outside (symlinked parent dir)
direct-link.txt             -> Unavailable   # final-entry link: the only symlink case handled
a/b/forged-tests.txt        -> Present       # nested parent link via relative target
bin/never-committed.txt     -> Present       # untracked/ignored file, not in any commit
.                           -> Present       # repository root as "tests" evidence
```

- **Parent-directory symlinks.** `resolved` checks `LinkTarget` only on the final entry.
  Git stores directory symlinks, so an agent can commit `evidence -> /tmp/...` and cite
  `evidence/x.txt`. The audit text "symlinks out" claims more than the code does. The
  code comment is honest ("on the final entry"); the audit summary is not.
- **The rationale is not implemented.** The code comment says the rule exists because
  outside paths name "bytes the candidate commit does not contain and a reviewer cannot
  see". Untracked and ignored files inside the worktree have exactly that property and
  are accepted.
- **`.` is accepted.** The new test asserts `"."` is Present. Combined with V4
  (`behavior-verified` is satisfied by presence), `--evidence tests=.` satisfies any
  repository's tests requirement. The fix therefore adds hygiene, not adversarial
  resistance.
- **Case sensitivity.** Comparison is Ordinal off Windows. On case-insensitive macOS
  volumes, `../REPO/x` would be *refused* (fail-closed), so this is not a bypass. Root
  aliasing via a symlinked root did not cause false refusals for relative in-repo links
  (tested).
- **TOCTOU.** This is moot rather than fixed: the gate only checks existence at
  completion time and never pins content (V5). The file can be swapped afterwards.
- **Deliverable status at review time:**
  - `gh api repos/kemiller2002/praxis/pulls/303` returns **404**;
  - `git -C /home/user/praxis status` shows the four files only *modified*, not
    committed;
  - `.ros/events/events.jsonl` has no confinement work item;
  - `verification/praxis-full-suite-after.txt` (EV-AE-008's source) **does not exist**.

  By contrast, dokimos#71 and praxis#302 exist and are open drafts.

Corrections:
- **Code.** Compare `realpath(root)` with the fully resolved candidate. Either resolve
  every path component (walk the segments, calling `ResolveLinkTarget(true)` on each
  link), or call `realpath` through P/Invoke. Better still, require the path to be
  tracked at the completion commit (`git cat-file -e <sha>:<path>`) and record a sha256.
  That one rule removes outside paths, every symlink variant, untracked files and V5.
  Refuse `.` and directories for the `tests` type, or require an explicit directory
  form.
- **Text.**
  - V2: change "symlinks out" to "a symlink as the final path entry; a symlinked parent
    directory still escapes".
  - Change "Confirmed defect, fixed in praxis#303" to "fix written, not committed or
    opened as a PR at review time".
  - Remove EV-AE-008 until the file exists.
  - Final report: remove the claim that "the confinement item" was recorded through
    Praxis.

## Challenge 4: Does governed learning prevent memorisation and self-approval?

**Verdict: wrong as stated ("refuses memorised, regressing or self-approved lessons"). It is a shape check over caller-asserted data.**

All seven of the following are accepted (`learning-gaming.out`, run against the branch's
built `Praxis.Domain.dll`):

| # | Attack | Result |
|---|---|---|
| 1 | Held-out `CaseId = "d-t4 "` vs counterfactual `"D-T4"` (case and whitespace variant; content identical) | Evaluated |
| 2 | All `EvidenceRef = ""` on counterfactual, held-out and regression | Evaluated (no check) |
| 3 | Prompt target approved by another agent id | Approved |
| 4 | Prompt target approved by an `ActorKind.Unknown` actor | Approved |
| 5 | Self-approval with id `Claude-Run-1` vs proposer `claude-run-1` | Approved |
| 6 | `Knowledge "AGENTS.md"` (agent instructions, including verification rules) approved by another agent | Approved: target class is chosen by the proposer |
| 7 | Evaluate a governance proposal by `kevin`, then call `approve` with the same Evaluated state but a record whose `ProposedBy` is the agent and whose `Target` changed to `SecurityPolicy`; approver `kevin` | Approved: state is not bound to proposal id, version, digest or target |
| (8) | Agent proposal; approver self-declares `{Kind=Human; Id="ops"}` | Approved: kind is unauthenticated |

`Extension "x-human"` does **not** pass the Human gate (correct), but it passes every
non-governance gate.

- **Passing results are not evidence.** `Passed: bool` is supplied by the caller, and the
  runner (the only thing that could make it evidence) is not built.
- **One held-out case and one regression case are enough.** The root-cause label on a
  held-out case is self-asserted.
- **Praxis already has stronger patterns that this contract does not reuse:**
  - `RunAuthorization` (owner-signed, `KeyId`, `PolicyDigest`; `Autonomy/Model.fs:158`);
  - Preservation's approver rule: "honours it only for an identity the caller trusts,
    and never for an agent" (`Requirements/Preservation.fs:77`).

Corrections:
- **governed-learning-design and competitive-analysis.** Replace "refuses memorised,
  regressing or self-approved lessons" with "rejects proposals whose *declared*
  evaluation is incomplete or whose declared approver equals the proposer. It does not
  authenticate approvers, verify case distinctness, or bind evaluation to the proposal;
  those require the unbuilt persistence and runner".
- **Code.**
  - Bind `Evaluated` and `Approved` to `(Id, Version, sha256(proposal))`.
  - Require a non-blank `EvidenceRef` and compare cases by evidence or content digest,
    not by `CaseId`.
  - Normalise ids.
  - Classify the target by path against the protected-class map (instruction files,
    `ros.json`, workflows), not by the constructor the proposer chooses.
  - Require approvals as signed authorizations (reuse `RunAuthorization` and the trusted
    key ids).

## Challenge 5: Is verification independence real in practice?

**Verdict: unsupported. Independence exists only in the experiment's grader and in undeployed designs.**

- **Production.** Verified completion is enabled in 0 of 13 repositories (V3).
- **The program's own completions use the legacy path it criticises.** Dokimos WI-0018's
  `work.completed` event cites
  `{"type":"tests","path":"tests/Dokimos.Core.Tests/AdversarialVerificationTests.fs"}`.
  That is a source file proving presence, not a test result. The quality evidence files
  are agent-written transcripts (`quality/evidence/WI-0018-*.txt`).
- **Dokimos coverage trusts a producer-written manifest** (3a).
- **Self-graded design.** Experiment tasks, oracles, protocols, grader and interpretation
  all came from the same agent (acknowledged as threat 8). The D-T4 misreading (2a) is a
  concrete example of the risk: the narrative was not checked against the artifact the
  register cites.
- **The 602/602, 12/12 and Praxis suite results were produced and recorded by the same
  session.** None has CI confirmation ("CI pending").

**Correction.** Add an "independence in practice" row to the capability matrix: "Echelon:
none deployed; AutoBuild: PRs pass through customer CI and customer review bots
(observed in Storybook and Chromatic)". Add to the final report: "This program's own
changes were completed through the presence-only path."

## Challenge 6: Do token or time numbers hide human costs? Is the economic conclusion assumption-driven?

**Verdict: the conclusion is driven by the assumptions. Several human costs are omitted.**

Sensitivity (`econ-sensitivity.out`, round 2, central Q=5, k=0.1):
```
as published:              A 14.04  B 4.22  C 3.39  D 23.84  E 4.23  N 4.07
D-T4 = disclosed residual: D 8.84
+ incomplete runs need a human redo (R): D 22.59, N 15.07
break-even: A cheaper than B iff R < 10.9 min (Q=5); < 4.4 (Q=2); < 21.8 (Q=10)
```

- **The 3–9× claim** rests on one R-event per arm. The break-even R is small, so the
  ranking is a restatement of the R assumption.
- **Omitted costs:**
  - Incomplete runs (D-T3, N-T4) cost **zero** human minutes, although someone must
    finish the task.
  - Silent behaviour changes (B-T4, C-T4) cost zero.
  - Reading the longer analysis output costs zero.
  - PR review time is omitted entirely.
  - Fixture and oracle authoring, curation of held-out cases for governed learning, and
    owner approvals are omitted.
  - Maintenance of about 160k lines of Praxis code and tests is omitted.
- **Questions caused by the harness are charged as human cost.** The N-T4 question and one
  D-T3 question are about the refused ANALYSIS.md.
- **k = 0.05–0.2 has no stated basis.** `subagentTokens` (about 72–78k) is final context.
  With 12–13 tool uses per run, cumulative processed input is plausibly an order of
  magnitude larger. "Agent time is not the cost driver" is therefore an assumption.
- **Contradicted by the package's own data:**
  - the final report says "the economics model, under all assumption sets";
  - the roadmap says "drove cost in every measurement this program made";
  - round 1 had **zero** false completions, and its cost ordering was driven by
    questions.

Corrections:
- Replace "3–9× more expensive ... under every scenario" with "A ranks below B only when
  R exceeds about 11 min (central); one event per arm".
- Charge incomplete runs.
- Exclude harness-caused questions.
- Delete "under all assumption sets" and "in every measurement".

## Challenge 7: Does the architecture duplicate what exists elsewhere?

**Verdict: mostly sound (the package explicitly reuses `praxis hub serve`, Ordo and Dokimos types), with two exceptions.**

- **`ImprovementProposal` adds a new approval and state machine in Praxis.** It does not
  reuse Praxis's signed `RunAuthorization`, Preservation's trusted-approver rule, or
  Ordo's decision events. Yet execution-contract item 5 itself recommends Ordo events for
  decision provenance. That is duplication *and* a weakening (Challenge 4).
- **Plain-completion confinement re-implements path policy separately** from
  `complete-verified`'s evidence handling and the quality gate's `ConsumedEvidence`
  (which already records sha256, per V5). One `CitedEvidence` type, as proposed in
  execution-contract item 1, should be the fix, not a second I/O-layer rule.

**Correction.** In governed-learning-design, "Self-modification guard" should state that
approval reuses `RunAuthorization` signatures, or list that as an open gap.

## Challenge 8: Did the AutoBuild analysis underestimate AutoBuild?

**Verdict: partly. The comparison sets Echelon's *designs* against AutoBuild's *documentation*, and several framings tilt against AutoBuild.**

- **No hands-on trial.** A 7-day trial exists (research notes §4), and T1–T6 could have
  been run through AutoBuild with the same oracles. Every AutoBuild weakness listed is
  "not documented", which is not the same as "absent". For example, "evaluates rules not
  at all, as far as documented" reads in the matrix as a deficit.
- **Storybook "only 87 of 143 merged"** (capability-matrix) counts *open* PRs in the
  denominator. The overall 96% figure counts decided PRs only. The two are not
  comparable. Compute Storybook's decided-merge rate, or drop the contrast.
- **Independence in practice.** AutoBuild PRs are verified by the customer's own CI and
  third-party review bots (Greptile), which flagged and got fixes for Blocking/P1 issues
  (#37069). That is real, deployed, author-independent verification. Echelon has none
  deployed. "Echelon ahead on verification independence" should become "Echelon has a
  stronger undeployed design; AutoBuild has weaker but deployed external checks".
- **Governed learning.** AutoBuild has a working, human-approved rule loop with un-apply.
  Echelon has a pure type with no persistence or runner, and it is gameable. "Echelon's
  design is stricter" should be "stricter on paper; not operational".

## Challenge 9: Would a simpler implementation give equivalent results?

**Verdict: yes, in at least three places.**

- **Path confinement.** "Evidence must be tracked at the completion commit, plus sha256"
  is simpler and strictly stronger than the current realpath-prefix logic (3b).
- **Governed learning.** Until a runner exists, the same human-approval guarantee comes
  from GitHub CODEOWNERS plus branch protection on instruction files (`AGENTS.md`,
  skills, `ros.json`) and a PR template with the nine fields. Approver identity is then
  authenticated by GitHub, which the F# contract cannot do. Build the type when the
  runner exists.
- **Experiment arms.** On these tasks, A (no protocol) matched or came within one task of
  everything else at the lowest cost. A hidden-test CI gate gives the experiment's
  independence without any Echelon component.
- **Dokimos V1.** The fix is already minimal and correct. Pair it with CI-side run
  enumeration rather than any further code.

---

## Confirmed new defects (not reported by the package)

1. **Praxis confinement bypass.** `--evidence tests=<symlinked-dir>/file` resolves
   outside the repository and is accepted as Present. Untracked or ignored files and `.`
   are also accepted.
2. **`ImprovementProposal` state is not bound to the proposal.** After evaluation, the
   proposer and target can be swapped at `approve`, which defeats the self-approval
   guard. The Human kind is self-declared, and Knowledge, Prompt and Skill targets
   (including `AGENTS.md`) need no human approval.
3. **The held-out guard compares `CaseId` strings only.** Case or whitespace variants of
   the original case pass, and blank `EvidenceRef` is accepted everywhere.
4. **Dokimos.** The coverage result trusts the producer's manifest completeness: an
   omitted failing run means Verified. A theory with one skipped row is now permanently
   NotRun.
5. **Package integrity:**
   - praxis#303 does not exist, and the confinement change is uncommitted;
   - `verification/praxis-full-suite-after.txt` (EV-AE-008) is missing;
   - the pre-registration timestamps are later than the archive and the current clock;
   - EV-AE-012 cites REPORT.json for reasoning ("premature") that it does not contain.

## Findings that stand

- **Re-grading.** All 72 grades and the summary reproduce exactly.
- **Test weakening.** No visible test was weakened (`scripts/test-weakening-check.py`
  prints "none"; no skip, xfail or conftest was added).
- **V1** is a real defect with a correct, minimal fix.
- **V3** (0/13 deployment) is the most important governance fact in the package.
- **T6 and T3 universality**, and the "no measured benefit from mandatory analysis"
  headline, are both supported as stated, given the caveats on n.
