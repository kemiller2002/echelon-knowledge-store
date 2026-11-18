# Final report: EKS-RESEARCH-2026-003

Echelon Foundry autonomous-engineering improvement program, with Obvious AutoBuild as
the competitive reference. Executed 2026-11-18 by one Claude Code session with parallel
subagents. **Completed implementation, proposed work, experimental findings and
hypotheses are labelled throughout.** Evidence IDs resolve in
[`evidence-register.md`](evidence-register.md).

This report was revised after an independent adversarial review (Phase 11, EV-AE-025).
Its corrections are listed at the end.

## The central question

> Can Echelon Foundry produce more correct, independently verified engineering outcomes
> with less total cost, less human attention, and fewer downstream failures?

**Answer, on present evidence: not demonstrated. The route the program assumed
(mandatory decision analysis) is not supported.**

1. **More analysis did not buy measurable correctness.**
   - Mandatory Clarity, EDF or combined analysis gave no measured gain over a baseline
     agent in 72 controlled runs (EV-AE-011, EV-AE-012).
   - It added 30–124% wall time, about 6–27 seconds on these small tasks.
   - With n=6 per arm per round this is "no evidence of benefit", not "evidence of no
     benefit".
2. **Independent verification is Echelon's real differentiator, and it is not deployed.**
   - Praxis already resists forged and foreign evidence when `complete-verified` is
     enabled (EV-AE-007).
   - It is enabled in 0 of 13 repositories (EV-AE-005).
   - The default path accepted any existing path, including `/etc/hostname` and a file
     in a destroyed agent container, until this program's fix (EV-AE-003, EV-AE-004).
3. **The one failure that plausibly matters most is undisclosed incomplete work.**
   - In 72 runs it occurred once: the baseline, on the weaker model.
   - Every analysis arm that finished either fixed the root cause or disclosed the
     residual. That is a hypothesis to test, not a finding.
4. **Less human attention cannot be shown or refuted.** Echelon does not measure it
   (`economic-analysis.md`).
5. **Against AutoBuild:**
   - Echelon's verification and learning *designs* are stricter than AutoBuild's
     *documentation*.
   - AutoBuild has deployed throughput (about 5,700 public PRs) and its PRs pass through
     customers' own independent CI and review bots. Echelon has nothing equivalent
     deployed.
   - No AutoBuild capability is independently verified, and no hands-on trial was done
     (EV-AE-015).

## Repositories examined

| Repo | Role | How |
|---|---|---|
| praxis | governance / repository OS | read, built, full suite ×3, 2 PRs |
| dokimos | verification / quality evidence | read, built, full suite, 1 PR |
| echelon-knowledge-store | knowledge, this research | built (78/78), research stored |
| ordo, aegis, fides, arca, vigila | orchestration, operational recovery, identity, persistence, follow-ups | read, built, tested (EV-AE-018) |
| forma, folio, limen, echelon-registry, echelon-engineering-system, echelon-foundry | UI, reporting, browser/WASM boundary, registry, docs, site | read, partially built and tested (EV-AE-018) |
| clarity-framework, clarity-service, echelon-diagnostic-framework | decision and diagnostic methods | read; clarity-service built (23/23) |
| echelon-organization-administration | portfolio governance inventory | read (historical defect register) |

Inventory: `data/baseline-inventory.json` (17 systems).

## Changes implemented (all draft PRs, none merged)

| Change | PR | Tests | Notes |
|---|---|---|---|
| Repeated test identities keep their worst outcome. This fixes a false verification where a failed test followed by a pass verified the obligation. | dokimos#71 | 5 adversarial tests; 602/602 locally; self-ratchet passes | Two further commits fix CI toolchain drift: FS3262 nullness on four `nonNull` sites in untouched files, made SDK-independent. Known limits: rerun protection relies on the caller-written manifest listing every run; a deliberately skipped data row now blocks verification. |
| Completion evidence must resolve inside the repository, including through symlinked parent directories. Frozen golden `test6` was changed openly. | praxis#303 | Adversarial test (7 cases); full suite shows no failure absent from main (EV-AE-008); final run status in the PR | Still accepted: untracked or ignored files, and `.` or an empty path |
| Governed improvement proposals: counterfactual, held-out and regression gates; every change needs a human approver who is not the proposer; agent-governing files count as governance; state bound to the evaluated proposal | praxis#302 | 18/18; architecture, naming and internal guards pass | Known limit: actor kind is self-reported. Bind approval to the owner-signed `RunAuthorization` before unattended use. |
| Research package, experiment harness, raw data (72 runs with final reports), scripts, adversarial review | echelon-knowledge-store (this branch) | analysis scripts reproduce every number; independently re-run | The harness is Python under `research/`; flagged for owner decision |

Every code change went through the repository's Praxis work protocol:
- Dokimos WI-0018;
- Praxis WI-0108 and WI-0109 (learning);
- Praxis WI-0110 (confinement; captured with an explicit id to avoid colliding with the
  learning branch).

## Tests executed

- **Dokimos:** baseline 597/597; after the fix 602/602.
- **Praxis:**
  - Full suite before the golden update: 2,514 tests, 2,485 passed, 29 failed.
  - After it: 2,486 passed, 28 failed.
  - All 28 also fail on untouched main in this container; they spawn real subprocesses
    (EV-AE-019).
  - Learning: 18/18 plus guards.
- **EKS:** 78/78.
- **Others (subagents):** Ordo 806 passed in 5 of 6 projects (95 failures need an npm
  build); Aegis 347/347; Fides 331/331; Arca 415/415; Vigila 285/285; Limen 873 passed,
  10 skipped; Forma 399 subtests passed before a time cap; registry conformance 5/5.
- **Experiment:** 72 agent runs graded by deterministic hidden oracles. Reference
  solutions were verified first. An independent reviewer reproduced every grade.

## Experimental results (Phase 6, measured)

See [`clarity-edf-experiment-results.md`](clarity-edf-experiment-results.md).

- Round 1 (Opus-class): every arm 6/6 (a ceiling).
- Round 2 (Haiku): A 5/6, B 6/6, C 6/6, D 4/6, E 6/6, N 5/6, F 5/6 (derived).
- No arm beat baseline under the pre-registered rule.
- Universal at baseline:
  - T6: refusing to delete a used function;
  - T3: following recorded decisions over a conflicting issue;
  - never weakening tests.
- **T4's hidden criterion is contestable**, so T4 is reported in two views: oracle, and
  disclosure.

## Outstanding blockers and limits

- **Owner decisions:**
  - merge the three PRs, including the `test6` contract change and the Dokimos rerun
    consequence;
  - enable verified completion in a pilot repository (R1);
  - whether the Python research harness is acceptable.
- **Not exercised:** prompt-injection and unmerged-branch instruction attacks;
  whole-suite nondeterminism.
- **Not instrumented:** human attention, billed tokens, escaped defects.
- **Harness artifacts:** the subagent runtime refused analysis files and stopped 2 runs.
  Smaller-model subagents made unrequested session-tool calls; they were denied, and no
  session was created or updated (EV-AE-013, EV-AE-014).
- **Pre-registration evidence is container file times only** (EV-AE-026).
- **Privacy.** The repository's `privacy-check` found 8 references in an earlier commit
  of this branch: ephemeral agent scratchpad paths and file paths inside private
  repositories.
  - They are redacted forward, following the `.privacy-denylist` convention.
  - `privacy-check` and `privacy-check-private` now report no findings (1,379 private
    names checked).
  - The earlier branch commits still contain them, because AGENTS.md forbids
    force-pushing. **Squash-merge** keeps them out of `main`.
- **No real-repository task experiment yet.**

## Next recommended actions

1. Review the three draft PRs (R0a–c).
2. Owner decision: pilot `requireVerifiedCompletion` with pinned verifier keys in
   Dokimos (R1).
3. Pin evidence by digest and require it to be tracked at the completion commit (R2).
   This closes the remaining confinement gaps.
4. Instrument human attention and escaped-defect links (R3).
5. Run EXP-AE-002 on 20+ real tasks, with disclosure as an explicit outcome and
   pre-registration by commit (R6), before any further investment in mandatory analysis.

## Adversarial review (Phase 11)

An independent reviewer, given the raw data and code but not this report's
conclusions, was asked to break the findings (`adversarial-review/review.md`, with
reproductions).

**What stood:**
- the "no measured benefit from mandatory analysis" headline;
- universal T3/T6 compliance;
- no weakened tests;
- the Dokimos defect;
- 0/13 deployment as the key governance fact;
- byte-for-byte reproducibility of all grades.

**What was wrong, and what was done:**

| # | Finding | Response |
|---|---|---|
| 1 | The claim that EDF's ranking "steered away from the root cause and produced a false completion" over-read one run. D-T4 disclosed the residual and asked; in round 1, EDF ranked the other way. | **Withdrawn.** T4 relabelled into undisclosed / disclosed / behaviour-change classes; recommendation I5 and the learning worked case rewritten; all 72 final reports archived so this is checkable |
| 2 | T4's hidden criterion punishes a scoped, disclosed fix while T6 rewards asking | **Acknowledged.** T4 reported in oracle and disclosure views |
| 3 | The Praxis confinement fix could be bypassed through a symlinked parent directory; the PR was cited before it existed; an evidence file was missing | **Fixed** (every path component resolved; regression test); praxis#303 opened; EV-AE-008 file written; remaining gaps (untracked files, `.`) documented |
| 4 | The learning contract was gamed six ways | **Fixed** (PRX-LEARN-002, 6 regression tests); self-reported actor kind documented as a known limit |
| 5 | The economic ranking is driven by assumptions; "under all assumption sets" was false | **Corrected:** break-even R published (4.4–21.8 min); alternative countings shown |
| 6 | Dokimos rerun protection depends on the caller-written manifest; "explicitly excepted" named a mechanism that does not exist | **Documented** in the audit and the PR |
| 7 | Storybook "87/143 merged" counts open PRs; Echelon's independence is undeployed while AutoBuild's customer-side checks are deployed | **Corrected** in the competitive analysis and capability matrix |
| 8 | Pre-registration timestamps were later than the runs | **Corrected:** they were hand-entered and wrong. The true file times precede every run output; noted that no commit proves it |

**Is Echelon's complexity justified?**
- On this evidence, the verification core (claims, receipts, protected policy) is
  justified, because it targets the failure that matters. It is not yet in use.
- Mandatory decision frameworks in the agent loop are not justified by any measurement
  so far.
- A simpler system, a single strong agent plus deployed independent CI and verified
  completion, would likely match everything this program measured.
