---
title: Adversarial review of the emerging EKS recommendation
mission: EKS-RESEARCH-2026-001
role: Agent N (independent adversarial reviewer)
date: 2026-11-16
status: review
---

# Objections to the emerging EKS architecture

This review covers the emerging recommendation, which is not yet final:

- keep Markdown with YAML front matter as the canonical source;
- use F# as the schema, validation and compiler layer;
- derive an index/graph and compiled task-specific context packages;
- do not adopt a custom DSL or F# source as storage;
- add explicit typed references between repositories.

Every objection below has evidence I measured or cited myself, a severity, and a test that would settle it.
The reproduction scripts are in the reviewer's scratchpad and are described inline.
The simple linter's source is in the appendix of `complexity-analysis.md`.
I did not modify the audited corpus or any existing file in this repository.

Severity scale:

- **critical**: the recommendation, or the claim of evidence behind it, does not hold unless this is resolved.
- **major**: a significant flaw in an experiment or in the design.
- **minor**: worth fixing, but the conclusion does not change.

Working-tree note: other agents edited `AblationBench.fs`, `Program.fs` and the ablation results while this review ran.
Line counts were taken at 2026-11-16 during the review.

---

## 1. The recommended architecture already exists in-house, and the research never uses it as a baseline (critical)

**Claim.** "Markdown canonical, index generated, validator in F#, CI gate" is already an accepted and deployed Praxis decision.
EKS is measured against plain Markdown and grep. It is never measured against the system it would actually replace.

**Evidence.**

- The decision already exists.
  - `praxis/research/decisions/DF-ROS-2026-A001--canonical-artifacts-and-generated-registries.md:22-29` (status `accepted`, 2026-07-24) reads: "Plain-text artifact files are canonical. JSON registries are deterministic, replaceable indexes generated from artifact front matter … Manual registries: rejected because drift is inevitable."
- An F# validator already exists.
  - `praxis/src/Praxis.Domain/Artifacts/Policy.fs` has 166 lines. It checks:
    - the identifier pattern (:89);
    - the filename convention (:111);
    - per-prefix status enums (:120);
    - confidence labels (:125);
    - duplicate ids (:134);
    - broken references across 16 reference fields (:145);
    - self-supersession (:148);
    - supersession reciprocity (:155, :163).
- JSON Schemas already exist.
  - `schemas/hypothesis.schema.json` (status enum, required `confidence`) is present in 10 of the 13 audited repositories: aegis, arca, dokimos, edf, fides, forma, mercatus, percepta, praxis and signal (`ls */schemas/hypothesis.schema.json`).
- CI already enforces both.
  - `praxis/.github/workflows/praxis-validation.yml:84-85` runs `./praxis registry check` and `./praxis validate`.
- The generated registries are not stale. I compared `registries/{hypotheses,evidence,decisions}.json` with the `research/*` files in 10 repositories:
  - 0 records were missing from a registry;
  - 0 registry entries had no file;
  - 0 status values differed.
  - The comparison was a Python script that walks `registries/*.json` and greps `^id:`/`^status:`.
- The research does not reference this validator.
  - `grep -rn "ArtifactPolicy\|Policy.fs\|hypothesis.schema" research/` returns 0 hits outside this folder.
  - The baseline mentions the registries only as a write target and as "not used as an index" (`baseline/current-agent-workflows.md:64, :337` C5).

**Consequence.** EKS's real contribution is the delta over Praxis:

- derived inverse links;
- transitive impact traversal;
- evidence-symmetry rules;
- typed edges between repositories;
- compiled packages.

None of these needs a new canonical format, a new store or a new repository.

**Resolving test.**

1. Add a `praxis-registry` condition: front matter plus the existing `registries/*.json`, plus the Praxis validator.
2. Report EKS results only as the marginal gain over that condition.
3. Put the recommendation as "extend `praxis registry build` and `ArtifactPolicy`" against "build EKS", and compare them on cost.

---

## 2. The agent trials cannot tell the recommendation apart from the status quo (critical)

**Evidence.**

- `experiments/results/trial-scores.md`: the conditions that score at the ceiling are
  - `md` (raw files with front matter, grep) at 16/16 on both Sonnet and Haiku;
  - `md-index`, `dsl` and `compiled` at 16/16;
  - `jsonl` at 15/16.
- The only condition that loses is `plain` (7/16 and 8/16).
- At n = 16, the Wilson 95% lower bound for 16/16 is 0.81. The data are therefore consistent with every front-matter condition being identical.
- The pre-registered hypothesis H-M7 (`literature/agent-memory.md:110`) says a flat-file baseline would come within a margin of any bespoke arm. **It was not falsified.** On this evidence, the existing front matter plus grep already reaches the ceiling.

**Consequence.**

- No trial result supports the compiled packages or the index on accuracy.
- The ranking `compiled ≥ md` rests on retrieval simulations (objection 12), not on agents.

**Resolving test.**

1. Build a harder question set of at least 60 questions with no wording leakage (objection 4). Include:
   - multi-hop impact questions of depth 3 or more;
   - questions that cross repositories;
   - questions that need the inverse direction without being told to look for it.
2. Run at least 3 runs per condition and report confidence intervals.
3. Pre-register a minimum effect size (for example, at least 10 points of exact-match accuracy) that justifies building the compiler.

---

## 3. The `plain` condition is a strawman, and part of its loss comes from the scorer (major)

**Evidence.**

- `Trials.fs:239` deletes the front matter in the `plain` condition.
- The questions then ask for "the *recorded* status and confidence" (epistemic questions) and for "typed metadata relations (depends_on, …)" (`Trials.fs:81`). Both are defined by the fields that were deleted. The condition tests whether facts that were deleted can be recovered, not whether Markdown is sufficient.
- Some `plain` losses are formatting differences in the scorer:
  - **Q05 (plain-b1).** The answer `confidence=very low` scored 0.667. The scorer does not normalise "very low" to "very-low" (`Trials.fs:289`), even though `Confidence.parse` does (`Model.fs`).
  - **Q15 (plain-b2).** The answer `status=refuted` scored as wrong. The prose of HY-FORMA-2026-0002 says "Refuted for this design…" (body line 55), while the front matter says `rejected`. The agent reported what the text it was given says.
- Rescored with synonym normalisation, plain-r1 rises from 0.721 to about 0.763.

**Resolving test.**

- Score epistemic answers with vocabulary normalisation.
- Add a "plain Markdown with conventions" condition that keeps relations as prose lines (`Supports: HY-…`). That is the realistic alternative to YAML, not deletion.

---

## 4. The question wording tells the agent the method (major)

**Evidence.**

- `Trials.fs:81`, change-impact questions: "Use only typed metadata relations (depends_on, derived_from, supports, contradicts, supporting_evidence, contradicting_evidence), not related_documents or prose mentions." This gives a grep-capable agent the exact field names to search for.
- `Trials.fs` (find-evidence template): "Include evidence declared in either direction (in X's own metadata or in the evidence record's metadata)."
  - This instruction is exactly the inverse-link feature whose value the ablation measures: removing inverse links drops find-evidence recall to 0.679 (`ablation.md`).
  - In the trials, the question supplies that feature to every condition. The `md` condition then reaches 100% partly because it was told to do what the index does.

**Resolving test.** Re-run with natural wording, for example:

- "What else should I re-check if EX-ROS-2026-A021 changes?"
- "What is the evidence for HY-FE-2026-B006?"

Measure how often the `md` condition misses inverse-direction evidence. That miss rate is the real value of inverse links.

---

## 5. The gold sets are circular, and the gold inherits the EKS parser's defects (major)

**Evidence.**

- 13 of the 16 trial questions take their gold from `Graph` functions in EKS:
  - `Trials.fs:71` (evidence);
  - `:83` (`Graph.impactOf`);
  - `:95` (supersession);
  - `:137` (unsupported hypotheses);
  - the contested flag in the epistemic questions.
- All typed retrieval tasks also take their gold from that graph. `benchmark-specification.md:75` concedes that perfect scores for typed strategies are "definitional".
- Independent check: my 108-line Python linter, which does not use F# or the EKS graph, combined with a 30-line traversal, reproduces the EKS gold exactly:
  - **56/56** find-evidence;
  - **66/66** change-impact;
  - **12/12** current-authority;
  - command: `python3 fe.py` against `results/retrieval-tasks.json`.
- So the gold measures a property of the authored front matter, not of EKS. Anything that reads front matter scores 1.0.
- The gold is also parser-dependent. Two real Praxis records are missing from the EKS graph because YamlDotNet rejects their front matter:
  - `EX-ROS-2026-A022` (unquoted colon in the title);
  - `DF-ROS-2026-A075` (block-mapping error).
- Both records are present in the Praxis registries, which shows that the corpus's own tooling accepts them.

**Resolving test.**

- Have a human (the owner) adjudicate a random 20% sample of the gold.
- Report agreement between independent implementations (already done above: 134/134).
- Report every record that one parser accepts and the other rejects.

---

## 6. Eight of the nine "blocking" validation findings are parser false positives (major)

**Evidence.**

- `validation.md:32` reports 9 blocking `dangling-reference` findings.
- Eight of them point to `EX-ROS-2026-A022`, which exists at `praxis/research/experiments/EX-ROS-2026-A022--affinity-by-execution-mode.md`.
- EKS raises them because:
  - `FrontMatter.split` → `Yaml.parse` fails with "While scanning a plain scalar value, found invalid mapping" (title: "Work affinity by execution mode: grouped versus …");
  - the record is then silently dropped;
  - every reference to it becomes "dangling".
- `Validation.all` has no parse-error rule, so the actual defect is never reported against the file that contains it.
- The only real dangling reference is `dokimos:RQ-DOKIMOS-REL-001 → RQ-SDE-2026-0002`, which points to a repository that was not audited.
- If EKS were made a CI gate, Praxis CI would be blocked on files that Praxis's own validator accepts.
- Measured: `perr.fsx` lists 4 non-fixture Praxis files that EKS rejects. Two of them, `EX-ROS-2026-A022` and `DF-ROS-2026-A075`, are research records that `./praxis registry build` has indexed. The other two are starter-template documents.

**Resolving test.**

- Report parse failures as file-attributed blocking violations.
- Run a differential parse of all 2,149 Markdown files with EKS and with the Praxis parser, and resolve every disagreement before any gating.

---

## 7. The "lossless migration 568/568" claim is false for `confidence` (major)

**Evidence.**

- `Model.fs:192`: `Confidence.parse` maps any unknown label to `Unstated`.
- `medium-high` is:
  - a legal label in Praxis (`Policy.fs` `confidenceLabels`);
  - used in 4 corpus records, including `praxis:DF-ROS-2026-A027`, `DF-ROS-2026-A008`, `DF-ROS-2026-A009` and `edf:RP-EDF-2026-002`.
- Measured with `loss.fsx` on DF-ROS-2026-A027:
  - the parsed value is `Unstated`;
  - the original contains `confidence: medium-high`;
  - the canonical Markdown has **no confidence line at all**.
- The "lossless" check (`Formats.fs:202`) compares the parsed record with the re-parsed record. It never compares against the original text, so a loss at the first parse is invisible.

**Resolving test.** Round-trip the original YAML key by key (text → text) and keep unknown enum values verbatim, or reject them.

---

## 8. Most typical agent editing mistakes pass the EKS validator undetected (critical)

**Method.** I applied 26 single edits to 3 real records:

- `praxis:HY-FE-2026-B006`;
- `praxis:DF-ROS-2026-A031`;
- `forma:HY-FORMA-2026-0002`.

The edits are the kinds of mistakes agents make. 25 of them are real mistakes; one, writing `superseded_by` as a scalar, is harmless. For each edit I re-ran `Validation.all` over the full Praxis and Forma corpus (248 records) and compared the result with the baseline.

- Harness: `mut.fsx`, using the compiled `Echelon.KnowledgeStore.Core.dll` from a scratch copy of the repository.
- I ran the same edits through a minimal Python linter (`simple/mut.py`).
- For Praxis `ArtifactPolicy`, I inferred the result from its code; I did not execute it.

| # | Mutation | EKS validator | 108-line linter | Praxis Policy.fs (code reading) |
|---|---|---|---|---|
| 1 | `supporting_evidence` → `supporting_evidences` (edge silently lost) | **undetected** | blocking | undetected (schema `additionalProperties: true`, line 64) |
| 2 | status typo `propsed` | **undetected** | blocking | blocking (:120) |
| 3 | status → `supported` while 2 of 3 evidence records contradict | **undetected** | advisory | undetected |
| 4 | confidence `hgih` | **undetected** (becomes `Unstated`) | blocking | blocking (:125) |
| 5 | confidence `0.8` | **undetected** | blocking | probably undetected (not text) |
| 6 | confidence raised from very-low to high with no new evidence | undetected | undetected | undetected |
| 7 | evidence id typo, B003 → B030 | blocking | blocking | blocking (:145) |
| 8 | evidence id replaced by a real but wrong id | advisory | advisory | undetected |
| 9 | evidence id lower-cased | **undetected** | blocking | undetected (filtered by the id regex) |
| 10 | duplicate `status:` key appended (first value wins, `Yaml.fs:95`) | **undetected** | blocking | depends on the parser |
| 11 | unquoted colon in title | indirect (record dropped; dangling references reported on *other* records) | blocking | accepted (lenient parser) |
| 12 | `id` deleted | blocking (indirect) | blocking | blocking |
| 13 | one id removed from `contradicting_evidence` (edge lost) | **undetected** | undetected | undetected |
| 14 | relation written in prose only | undetected | undetected | undetected |
| 15 | `superseded_by` cleared while status is `superseded` | blocking | blocking | blocking (reciprocity) |
| 16 | status `accepted` while `superseded_by` is kept | advisory | advisory | undetected |
| 17 | `superseded_by` points to a nonexistent id | blocking | blocking | blocking |
| 18 | `superseded_by` points to self | advisory only | blocking | blocking (:148) |
| 19 | list indentation broken | blocking | undetected | depends on the parser |
| 20 | `Superseded_By` (case) | blocking | blocking | blocking (reciprocity) |
| 21 | `derived_from` → `derive_from` (edge silently lost) | **undetected** | blocking | undetected |
| 22 | `derived_from` → `dependsOn` | **undetected** | blocking | undetected |
| 23 | status `rejected` → `refuted` | **undetected** | blocking | blocking (:120) |
| 24 | contradicting evidence moved into the supporting list | advisory | advisory | undetected |
| 25 | copied file with the id unchanged | blocking | blocking | blocking (:134) |

**Totals over the 25 real mistakes.**

| Validator | Blocking | Advisory | Undetected |
|---|---|---|---|
| EKS | 7 + 1 indirect | 4 | **13 (52%)** |
| 108-line linter | 17 | 4 | 4 |

**Consequence.**

- "F# as the validation layer" is not yet shown to add safety. The F# validator that ships today is weaker than both the existing F# validator in Praxis and a short script.
- The design choice to keep unknown fields losslessly in `Extra` (`Model.fs:287`) is exactly why misspelled relation fields vanish without an error.

**Resolving test.**

1. Make this mutation suite an acceptance test.
2. Require at least 90% of mutations 1–25 to be blocking.
3. Add these rules:
   - closed vocabularies per kind;
   - unknown-key detection near relation names;
   - duplicate-key detection;
   - id-case checks;
   - self-supersession;
   - "status supported but contradictions outnumber support".

---

## 9. Most measured problems are organisational, not format problems (major)

| Problem (measured in the EKS reports) | Root cause, from my measurement | Format change needed? |
|---|---|---|
| 580 duplicate file copies, 868k tokens (`baseline.md`) | 578 extra copies in 94 sets; 74 sets span repositories. The top paths are installed framework material: `docs/` 62, `templates/research` 54, `.sde/method` 54, `docs/00-governance` 54, `framework/standards` 45, `.sde/templates` 45, `.visual-engineering` 42 (sha256 over all `.md`). | No. This is vendoring by copy. The fix is to reference packages or submodules and exclude them from retrieval. |
| 205 broken relative links (`baseline.md`) | 160 of 205 (78%) are in two private repositories: echelon-organization-administration (134; 0% front-matter adoption) and clarity-framework (26; 13% adoption). Neither is governed by Praxis. | No. A link checker in CI, for example lychee or markdown-link-check, would catch them. |
| 28 evidence-link asymmetries | The corpus convention asks authors to write both directions (`supports` on the evidence record and `supporting_evidence` on the claim). Praxis's validator checks reciprocity only for supersession. | Convention: write one direction and derive the other. EKS must still decide which side is canonical. |
| Drift in hand-maintained indexes (5 documents) | `context/DECISIONS.md` is a hand log of the kind DF-ROS-2026-A001 already says should be generated. **2 of the 5 flagged entries are false positives**: `docs/migrations/fsharp/TRACEABILITY.md` says "Closed (2026-09-28, DF-ROS-2026-A049) … historical ledger". The generated registries show 0 drift (objection 1). | No. Generate the index or delete the hand log. |
| 0 typed edges between repositories (`dependencies.md:5`, M1 recall 0.00) | Nobody wrote any. YAML already allows `depends_on: [fides:DF-FIDES-2026-0003]`, and `Graph.resolve` already parses `repo:id`. | No. This needs a convention and a rule, not a format. |
| Epistemic staleness (objection 16) | Status is not updated after the evidence arrives. | No. This needs a review-due rule and a workflow. |

**Resolving test.** Code every violation in `validation.json` and every baseline problem by root cause (convention, enforcement, distribution, or format/tooling). Report the share that would remain after "enforce existing Praxis conventions in all 13 repos plus a link checker".

---

## 10. Explicit typed references between repositories would duplicate the manifests and add another place to drift (major)

**Evidence.**

- 44 of the 54 hard-dependency evidence instances in `baseline/cross-repo-edges.json` already come from manifests: `.fsproj`, `package.json`, `conditor.json`, `Directory.Packages.props`, workflow files and `vendor/nuget/*.lock`.
- The baseline already shows the failure mode of restating the same fact. In `current-agent-workflows.md:345` (D3), the Praxis version in signal appears in 4 files with 3 different values.
- An authored `depends_on: fides:…` would be a fifth copy.
- The naive manifest method M3 already reaches precision 0.77 and recall 0.69 on hard dependencies (`dependencies.md`). Its file list (`DependencyBench.fs` `isManifest`) omits `vendor/**/*.lock` and test files that pin versions.

**Resolving test.**

1. Extend manifest extraction to lock files, and to test files that pin another repository, and measure recall.
2. Allow authored edges between repositories only for conceptual dependencies that no manifest can express. Clarity is the example: "conceptual only" (`cross-repository-dependency-inventory.md:69`).

---

## 11. The dependency gold was found by keyword search, which favours the keyword method (major)

**Evidence.**

- Candidate edges were discovered with "a case-insensitive, word-bounded grep for each target repo's name, package name, release URL and requirement-ID prefix" (`cross-repository-dependency-inventory.md:39`).
- M2, prose keyword search with essentially the same patterns, then scores recall 0.98 (`dependencies.md:6`).
- An edge that no keyword reveals cannot be in the gold. So the recall of keyword search is inflated by construction, and the size of the problem the typed edges must solve is unknown.

**Resolving test.** Build an independent gold for at least 2 scenarios from owner interviews, build-graph analysis (`dotnet list package --include-transitive`, `npm ls`) and runtime calls (HTTP clients). Then score M2 against it.

---

## 12. Token savings from the retrieval simulation do not appear in the real agent trials (major)

**Evidence.**

- `retrieval.md` compares *scripted* strategies. "A2 grep + whole files (chase 6 hops)" costs 485k tokens per change-impact task. No real agent behaves that way.
- In the real trials (`trial-usage.json`), mean tokens per agent session were:

| Model | md | compiled | md-index | jsonl | plain |
|---|---|---|---|---|---|
| Sonnet | 77.5k | 75.4k (**−2.7%**) | 75.9k | 70.5k | 94.3k |
| Haiku | 106.9k | 116.7k (**+9.1%**) | – | – | 130.8k |

- `benchmark-specification.md:79` states that about 65–70k of each session is fixed overhead. The task-dependent part for `md` is therefore roughly 7–12k tokens, which is far below the simulation's figures.
- The real differences are in tool calls and wall time:
  - Sonnet, md: 8.5 calls and 44 s;
  - Sonnet, compiled: 7 calls and 22 s;
  - this is one pair of runs per condition.

**Resolving test.**

- Run at least 5 runs per condition per model.
- Subtract a per-model empty-task overhead.
- Report the net task tokens, tool calls and duration with confidence intervals.
- Drop the scripted A2 numbers from any cost claim.

---

## 13. The compiled condition gets the task router and the answer universe for free (major)

**Evidence.**

- `Trials.fs:172-183`: `taskKind` maps the *gold* question kind to the context recipe. This is oracle routing.
- `Trials.fs:193-203`: for missing-evidence questions, the compiled context is exactly "all hypotheses in repo X, with their evidence links". That is the candidate set the answer is drawn from.
- The explicit id is also extracted from the question text.
- A deployed compiler must classify free text into a task type. Its routing errors are not measured.

**Resolving test.** Route on the question text alone, using either a classifier or the agent choosing a recipe. Measure routing accuracy and end-to-end accuracy with the router included.

---

## 14. The trials are small: one model family, one run per cell, and isolation by instruction only (major)

**Evidence.**

- n = 16 questions.
- One Sonnet run per condition. Only md, plain and compiled were repeated with Haiku.
- Both models are Claude (`benchmark-specification.md`, threats).
- "Agents restricted by instruction (not sandbox) to their condition directory" (`benchmark-specification.md:72`). The original corpus with full front matter was readable at `/home/user/kemiller2002` during the `plain` runs.
- The trial prompt given to the subagents is not recorded in the repository. `trial-usage.json` was "recorded by hand".

**Resolving test.**

- Use sandboxed per-condition directories.
- Commit the prompts.
- Run at least 3 runs per cell and at least one non-Claude model.
- Randomise question order.

---

## 15. Epistemic metadata already goes stale, and no rule detects it (major)

**Evidence.** Measured with `stale.py` over the hypotheses in the 10 public governed repositories (24 hypotheses):

- 23 of 24 hypotheses have no `updated` field, so the metadata cannot show whether a status was ever revisited. The repositories are shallow clones (1 commit each), so git history cannot show it either.
- 5 hypotheses have evidence created *after* the hypothesis's created/updated date.
- 6 hypotheses are still `proposed`/`active` although evidence exists.
- **Confirmed drift.** `praxis:HY-ROS-2026-A030` has `status: active`.
  - Its own provenance reason reads: "Recorded the first test … not supported (EV-ROS-2026-A075)".
  - Its body (line 133) says "Not supported (EV-ROS-2026-A075)."
  - The front matter was never updated.
- `forma:HY-FORMA-2026-0002` stores the status twice: `rejected` in front matter and "Refuted" in the prose. This is the duplication that confused the plain trial (objection 3).
- Neither EKS nor Praxis has a rule that compares the status with the evidence, or the confidence with anything at all.
- The EKS `stale-evidence` rule only fires on `retrieved` older than 180 days, which is irrelevant for a corpus created between July and November 2026.

**Resolving test.** Add these rules:

- **review-due**: evidence created later than the claim's `updated` date;
- **status-contradicts-evidence**: `supported` with net contradiction, or `active` with only contradicting evidence.

Also consider Doorstop-style fingerprints, where a link stores the hash of its target at review time and becomes "suspect" when the target changes. The literature review already recommends this (`literature/knowledge-representation.md:203`) but the prototype does not implement it. Measure the suspect rate on the next month of commits.

---

## 16. Generated views stay correct only with CI enforcement, so the compiler adds a synchronisation surface (minor)

**Evidence.**

- Generated registries are 100% in sync where CI runs `registry check` (objection 1). Hand-maintained views are not (objection 9).
- The proposal adds new generated views (index, compiled packages) and an index across repositories.
- The index across repositories cannot be checked in each repository's CI without credentials for the other repositories. The security model requires separate compile credentials and forbids private-derived artifacts in public repositories (`architecture/security-model.md` C-13, C-23). So the index of the whole organisation becomes a separate deployment that is checked outside per-repository CI. That is a new way to go stale, which T-20 already lists as a threat.

**Resolving test.** Specify where the index across repositories is rebuilt, by what trigger, and with what freshness SLO. Measure the lag between a source commit and index rebuild over 2 weeks.

---

## 17. Unqualified ids resolve silently across repositories (minor)

**Evidence.**

- `Graph.fs:59` (`| _, [ k ] when qualifiedRepo.IsNone -> Resolved k`) resolves an unqualified id to the unique record in *any* repository.
- `validation.md` reports 29 id collisions across repositories. Adding a record to repository B can therefore silently change an old reference in repository A from resolved to ambiguous, or redirect it.
- A typo that happens to match an id in another repository resolves without any warning.

**Resolving test.** Require the `repo:` qualifier for any target outside the current repository. Report every unqualified reference that resolves outside the repository.

---

## 18. The relation vocabulary is narrower than the corpus (minor)

**Evidence.**

- Corpus front-matter usage outside archive, templates and tests:
  - `tests_hypotheses` 15 files;
  - `inputs` 14;
  - `outputs` 15;
  - `evidence_ids` 7;
  - `hypothesis_ids` 4;
  - `theory_ids` 4.
- Praxis's validator treats `tests_hypotheses`, `evidence_ids`, `hypothesis_ids` and `theory_ids` as references (`Policy.fs` `referenceFields`).
- EKS's `RelationKind.fields` does not, so experiment → hypothesis links become untyped `Extra`.
- Strict typing then reports "experiment without a hypothesis (4)" (`ablation.md`) and has to fall back to *prose mentions* (`Strict.fs:166-169`) to find hypotheses.

**Resolving test.** Derive the relation vocabulary from the union of existing schemas and validators, not from scratch. Report coverage.

---

## 19. The DSL and F#-source experiments confirm the rejection, but their code is still being carried (minor; I agree with the recommendation)

**Evidence** (`formats.md`, o200k_base tokens):

| View | YAML | F# records | DSL |
|---|---|---|---|
| Metadata only | 70,434 | 121,946 (**1.73×**, `:17`) | 63,295 (0.90×) |
| Full records | 914,040 | – | 907,487 (−0.7%) |

- Bodies dominate the full view, so a DSL buys under 1% of tokens and costs a parser, a writer and a new syntax for agents to learn.
- `Codec.fs`, `Dsl.fs` and `FSharpSource.fs` hold 583 lines of non-blank, non-comment code, 32% of the core, for formats the recommendation rejects.

**Resolving test.** None is needed for the decision. Delete or archive these modules before production work begins so they do not become maintained surface (see `complexity-analysis.md`).

---

## 20. The architecture is sized for a larger problem than the measured one (major)

**Evidence.**

- One owner (`architecture/security-model.md` §4.5 "One-owner reality").
- About 570 typed records.
- Record creation dates show 90, 6, 257, 155 and 22 new records per month (July to November 2026).
- The security model proposes 23 threats and 25 controls.
- The versioning research proposes schema registries and compatibility modes.
- Against that, a 108-line linter with a 30-line traversal:
  - reproduces 134/134 of the typed retrieval gold;
  - blocks 17 of 25 agent mistakes, against 8 of 25 for EKS;
  - generates an index with inverse links: 0.62 s to read and parse 10 repositories, then 0.04 s to lint and index.
  - EKS reports 63 ms to compile its index (`ablation.md`), so both are effectively instant at this scale.
- See `complexity-analysis.md` for the full comparison.

**Resolving test.** Pre-register the decision rule. Build the compiler only if, on the harder trial set of objection 2, the compiled condition beats `praxis-registry` with inverse links by at least X points of accuracy or at least Y% of net task tokens. Choose X and Y before running.

---

## 21. Drift over time cannot be measured on this corpus (minor)

**Evidence.**

- Every audited repository is a shallow clone with one commit (`git -C <repo> rev-parse --is-shallow-repository` returns `true`).
- So claims about how fast links rot, how often status changes, or how often indexes drift are not measured; they are inferred from a single snapshot.

**Resolving test.** Unshallow at least praxis, forma and fides. Replay the history and compute the asymmetry, broken-link and status-change rates per week.

---

## 22. `KnowledgeId` is dead code (minor)

**Evidence.**

- `Identifiers.fs` (35 code lines) defines `KnowledgeId`.
- Nothing in `src/` or `benchmarks/` uses it; only `IdentifierTests.fs` does (`grep -rn KnowledgeId src benchmarks`).
- `KnowledgeRecord.Id` is a `string`.
- This is a small sign that the type design is not yet in sync with the implementation.

**Resolving test.** Use it in `Model` and `Graph`, or delete it.

---

## What I could not refute

- **Inverse links and transitive traversal have real value** for impact and evidence questions:
  - ablation: −inverse links gives find-evidence recall 0.679;
  - −transitive traversal gives impact recall 0.489.
- **Typed relations beat prose mentions** on precision (0.257 vs 1.000 for change-impact).
- **Generated indexes do not drift under CI**, and hand-maintained ones do.
- **Markdown with front matter is the right canonical form.** Every alternative format offers at most about 1% savings, and agents already read it at ceiling accuracy.

All four findings support **adding inverse links, traversal and stricter rules to the existing Praxis pipeline**. None of them, on the evidence here, requires a separate EKS store, compiler or security perimeter.
