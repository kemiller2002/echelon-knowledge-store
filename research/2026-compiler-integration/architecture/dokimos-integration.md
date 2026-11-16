# Dokimos integration: verified contracts EKS could feed

Mission: EKS-RESEARCH-2026-002, Workstream B.
Subject: Dokimos at commit `6ea86246cb13f141201bc5cae5d84ebe9831d02b` (source version
`0.11.1`, `dokimos/Directory.Build.props:11`), read-only, at `<workspace>/dokimos`.
Also read: `aegis/.github/workflows/dokimos.yml`, `fides/.github/workflows/dokimos.yml`,
`praxis/requirements/CODE-QUALITY-HARDENING.md`, `praxis/docs/requirement-verification-manifest.md`.

All paths below are relative to `<workspace>/` unless they start with `src/` of EKS
(`<eks>/`).

## Evidence status (AGENTS.md rule 5)

- **Verified by reading source and docs.** Every statement with a `file:line` citation was read at
  the commit above. No Dokimos command was executed in this workstream, so nothing here is a
  *measured* runtime result. Where Dokimos docs report their own measurements (for example the
  Rehearsal 8 replay), they are quoted as Dokimos's claims, not re-measured.
- **Hypothesis.** Everything in "Recommended minimal integration" that describes what EKS
  *would* emit is a design proposal. It has not been run against Dokimos.

## Verdict (short)

An existing Dokimos input already expresses "requirement R has obligation O, which must be
verified by executable check T". It is **`dokimos.verification-obligations/1`**, consumed by
`dokimos verification compile --obligations F` and `dokimos verification coverage --obligations F`
(`dokimos/src/Dokimos.Core/Verification/Obligations.fs:7-63`,
`dokimos/src/Dokimos.Cli/VerificationCommand.fs:136-181`). EKS can target it without a new
Dokimos contract. Three things are missing:

1. **No JSON Schema file.** `dokimos/schemas/` has no `dokimos-verification-obligations.schema.json`.
   The contract is defined only by the F# decoder (`Obligations.fs:151-210`) and the doc table
   (`dokimos/docs/verification-compiler.md:9-15`).
2. **EKS has no test bindings.** Dokimos refuses an obligation with no bound test identity
   (`Obligations.fs:129-130`). EKS obligations have no such field
   (`src/Echelon.KnowledgeStore.Compiler/Packages.fs:25-33`), and the EKS record model has no
   "verified by" relation (`src/Echelon.KnowledgeStore.Core/Model.fs:24-35`).
3. **No frozen-plan check.** Dokimos carries `planDigest` through to the coverage report without
   checking it (`dokimos/src/Dokimos.Core/Verification/Coverage.fs:296`; no other reader in `src/`).
   It ignores unknown members (`dokimos/src/Dokimos.Core/Json.fs:86-105`), so EKS provenance
   fields would be dropped from the `compile` output (`Obligations.fs:219-240`).

The construction/adapter oracles do not fit (domain-specific state-machine vocabulary). The
completion authority is a host (Praxis) input. The Praxis manifest path is UI-only and its
manifest is written by the Praxis compiler only. Details below.

---

## 1. Dokimos inputs and outputs (`dokimos/schemas/`)

Legend: **O** = describes what must be verified (obligation, oracle, policy, required set);
**R** = what was verified (result, receipt, evidence); **I** = other input (claims,
observations, configuration).

| Schema file | Contract id | Kind | Purpose (one line) | Required fields (top level) |
|---|---|---|---|---|
| `dokimos-policy.schema.json` | policy 1.0.0/1.1.0/1.2.0 | **O** (aggregate) | Ratchets, thresholds, regression and introduced-finding rules, `requiredEvidence` metrics for `dokimos evaluate` (desc. line 5) | `schemaVersion`, `baseline` (line 7) |
| `dokimos-construction-inputs.schema.json` | `oneOf` 4 docs (line 6) | **O** + I | Inputs to `construction verify`: frozen oracle, host observations, claims, authorization (desc. line 5) | per `$defs` (line 20): oracle `schema, oracleId, frozenAt, producer, subject, binding, derivedFrom, cases`; hostObservations `schema, host, observations` (line 278); claims `schema, source` (line 393); authorization `schema, authority` (line 454) |
| `dokimos-adapter-oracle.schema.json` | `dokimos.adapter-oracle/1` | **O** | Frozen behavioural oracle driven through a declared adapter; unscored cases reported, never judged (line 5) | `schema, oracleId, frozenAt, producer, subject, binding, derivedFrom, cases` (lines 7-16) |
| `dokimos-completion-authority.schema.json` | `dokimos.completion-authority/1` | **O** (required sets) + I (host observations) | Host-supplied expected commit and required requirement/decision/validation sets plus independent evidence observations (line 5) | `SchemaVersion, WorkItemId, ClaimToken, FencingGeneration, ExpectedCommit, ExpectedVerificationRevision, SupportedOrdoContractVersions, ExpectedOrdoContractDigest, RequiredRequirementIds, RequiredDecisionIds, RequiredConflictResolutionIds, RequiredValidationNames, Evidence, ConsumedPacketDigests` (line 8) |
| `praxis-completion-packet.schema.json` (in Dokimos `schemas/`) | Praxis packet | I (untrusted claim) | The completion claim Dokimos checks against the authority (`dokimos/docs/completion-verification.md:3-15`) | `SchemaVersion … RequirementIds, DecisionIds, ConflictResolutionIds, Validations, Evidence` (line 8) |
| `dokimos-evidence-reuse-request.schema.json` | `dokimos.evidence-reuse-request/1` | I | Tests with test-to-input mappings and freshness policies, plus recorded prior runs (line 5) | `SchemaVersion, Revision, Tests, PriorEvidence` (line 8); each test `TestId, Freshness` (line 28) |
| `dokimos-quality-exceptions.schema.json` | `dokimos.quality-exceptions` 1.0.0 | I (waivers) | Owned, expiring exceptions to one ratchet rule in one scope (line 5) | `contract, schemaVersion, exceptions` (line 7) |
| `dokimos-ratchet-baseline.schema.json` | `dokimos.ratchet-baseline` 1.0.0 | **O** (accepted limits) | Accepted per-rule, per-scope values; only lowered (line 5) | `contract, schemaVersion, repository, acceptedAt, configuration, rules` (line 7) |
| `dokimos-snapshot.schema.json` | snapshot 1.0.0/2.0.0 | **R** | Immutable evidence for one analysis of one revision (line 5) | per version, `$defs` `v1`/`v2` (line 17) |
| `dokimos-comparison.schema.json` | `dokimos.comparison` 1.0.0 | **R** | Snapshot-to-snapshot metric and finding changes (line 5) | `Contract, SchemaVersion, DokimosVersion, Before, After, Summary, MetricChanges, FindingChanges` (line 7) |
| `dokimos-evaluation.schema.json` | `dokimos.evaluation` 1.0.0 | **R** | Policy disposition; exit 0/3/4 (line 5) | `Contract … Disposition, ExitCode, Reason, Baseline, Current, Policy, Summary, OutcomeCounts, Outcomes` (line 7) |
| `dokimos-results.schema.json` | `dokimos.results` 1.0.0 | **R** | Application-facing projection; consumers never recompute (line 5) | `Contract … Overview, Current, Baseline, Outcomes, Improvements, Regressions, Findings, Hotspots, Trends, Provenance, Unavailable` (line 7) |
| `dokimos-history.schema.json` | `dokimos.history` 1.0.0 | **R** | Store query over snapshots and series (line 5) | `Contract … Store, Repository, Query, Baseline, Snapshots, Series, Findings, Notes` (line 7) |
| `dokimos-ratchet.schema.json` | `dokimos.ratchet` 1.0.0 | **R** | Ratchet report: pass 0, regression 4, invalid-exceptions 6, unavailable 3 (line 5) | `Contract … Verdict, ExitCode, Reasons, Baseline, Exceptions, Summary, Rules, Generated, Findings` (line 7) |
| `dokimos-construction-verification.schema.json` | `dokimos.construction-verification` 1.0.0-1.2.0 | **R** | Construction verdict verified 0 / rejected 4 / incomplete 3 (line 5) | `… Verdict, ExitCode, Reasons, Inputs, SourceConservation, StructuralConformance, BehavioralAcceptance, Containment, Dependencies, Requirements, Counts, Exceptions, Findings, Limitations` (line 8) |
| `dokimos-behavior-verification.schema.json` | `dokimos.behavior-verification` 1.0.0 | **R** | Adapter run verdict passed 0 / failed 4 / unknown 3 / refused 4 (line 5) | `… FirstPassVerdict, ExitCode, Reasons, Oracle, Candidate, Compile, Adapter, Cases, Counts, ConflictObservations, Requirements, Run, Findings, Limitations` (line 8) |
| `dokimos-completion-verification.schema.json` | `dokimos.completion-verification/1` | **R** | Completion receipt verified 0 / rejected 4 (line 5) | `… Verdict, ExitCode, WorkItemId, Commit, VerificationRevision, PacketDigest, RequirementCount, EvidenceDigests, ReusedEvidenceIds, Diagnostics` (line 8) |
| `dokimos-evidence-reuse-plan.schema.json` | `dokimos.evidence-reuse-plan/1` | **R** (decision) | Per test, run or reuse, with reasons; "Reuse is not acceptance" (line 5) | `Contract … Revision, Acceptance, Summary, ChangedPaths, UnmappedChangedPaths, Tests` (line 8) |
| `dokimos-store-result.schema.json` | `dokimos.store-result` 1.0.0 | R (receipt) | Outcome of a store write: `stored` / `already-stored` | `Contract, SchemaVersion, Store, Outcome, Subject` (line 6) |
| `dokimos-diagnostic.schema.json` | `dokimos.diagnostic` 1.0.0 | — | stderr diagnostic when no canonical output is possible (line 5) | `Contract, SchemaVersion, State, Code, Message` (line 7) |

`dokimos/schemas/` also holds non-Dokimos schemas (Praxis/ROS: `evidence.schema.json`,
`mission.schema.json`, `ros-*.schema.json`, `work-*.schema.json`). They are out of scope here.

**Contracts that exist only in F# and docs, with no schema file.** These are the ones that matter most for EKS:

| Contract | Direction | Defined at |
|---|---|---|
| `dokimos.verification-obligations/1` | **O** (input) | `dokimos/src/Dokimos.Core/Verification/Obligations.fs:105-210` |
| `dokimos.verification-refusals/1` | R (compile refusal) | `dokimos/src/Dokimos.Cli/VerificationCommand.fs:152-158` |
| `dokimos.verification-results/1` | I (results manifest) | `dokimos/src/Dokimos.Core/Verification/TestResults.fs:134-157` |
| `dokimos.verification-coverage/1` | **R** | `dokimos/src/Dokimos.Core/Verification/Coverage.fs:268-323` |
| `dokimos.verification-work/1` | R (scaffold work) | `dokimos/docs/verification-compiler.md:13` |
| `dokimos.fault-probes/1`, `dokimos.fault-probe-results/1` | I / R | `dokimos/docs/verification-compiler.md:15,76-85` |
| `praxis.verification-matrix/1` (written by Dokimos for Praxis) | R | `Coverage.fs:328-357` |
| `praxis.verification-manifest/1` (Praxis-owned, read by Dokimos) | **O** (input) | `dokimos/src/Dokimos.Core/Verification/RequirementEvidence.fs:144,229-275`; schema in `praxis/schemas/praxis-verification-manifest.schema.json` |
| `dokimos.requirement-observations/1`, `dokimos.requirement-verification/1` | I / R | `dokimos/docs/requirement-verification.md:5,15` |

### Obligation-side vs result-side summary

- **What must be verified:** `verification-obligations/1` (generic, test-identity bound);
  `praxis.verification-manifest/1` (UI requirements, V1-V5 checks); construction oracle and
  `adapter-oracle/1` (state-machine behaviour cases with `requirementKeys`); completion-authority
  `Required*` sets; policy `requiredEvidence` and ratchets (aggregate metrics, not requirements).
- **What was verified:** `verification-coverage/1`, `requirement-verification/1`,
  `construction-verification`, `behavior-verification`, `completion-verification/1`, snapshot,
  comparison, evaluation, results, history, ratchet, evidence-reuse-plan.

## 2. Requirement ids, traceability and acceptance criteria

Dokimos has requirement ids and requirement-to-test traceability in four separate mechanisms:

1. **Verification compiler (generic).** `Obligation = { Id; Requirement; Priority; Criterion;
   Categories; Dependencies }` (`Obligations.fs:20-26`). `BoundTest = { Id (test identity); Suite;
   Kind; Obligations; Categories }` (`Obligations.fs:28-34`) binds tests to obligations. The
   acceptance criterion is the free text `Criterion`. Dokimos never executes or interprets it, and
   refuses an empty one: "no criterion text: expected behaviour would have to be invented"
   (`Obligations.fs:127-128`).
   - **Test discovery.** Dokimos does not discover tests to satisfy an obligation. It looks up the
     *declared* identity. Supported identities are `dotnet:<className>.<method>` (TRX),
     `playwright:<file> > <titles> [<project>]`, and any other `scheme:` string, which counts as
     record evidence that Dokimos never judges (`Obligations.fs:11-18,79-97`;
     `dokimos/docs/verification-compiler.md:17-21`). Source files under each suite's `paths` are
     scanned lexically (`VerificationCommand.fs:52-86`). A test is `invalid` if it is skipped, a
     placeholder, has no or only trivial assertions, is not compiled by its `.fsproj`, is
     outside the runner paths or `--filter`, or does not reach `requiredBoundary`
     (`dokimos/docs/verification-compiler.md:30`). Near-miss names are reported as drift
     (`Coverage.fs:146-158`).
   - **Satisfaction.** An obligation is `verified` only when every bound identity `passed` at the
     candidate SHA. Precedence is `failed > invalid > not-generated > blocked > not-run`
     (`Coverage.fs:182-190`; `dokimos/docs/verification-compiler.md:36`). A requirement is verified
     when all of its obligations are (`Coverage.fs:276-277`). A surviving fault probe withdraws
     `verified` (`Coverage.fs:237-238`).
2. **Manifest-driven requirement verification (UI).** This reads `praxis.verification-manifest/1`.
   Requirement ids are rendered as `data-requirement-id` in the application, and obligations are
   `<REQ>/V<n>/<check>` (`dokimos/docs/requirement-verification.md:14,159-161`). A requirement
   takes the worst status of its obligations (`requirement-verification.md:45-46`). The manifest
   is compiled from requirements alone (`requirement-verification.md:7-8`), and its `contentHash`
   is recomputed (`RequirementEvidence.fs:220-275`).
3. **Construction / behaviour oracles.** Each case carries `requirementKeys` and a `citation`.
   `derivedFrom[]` records `requirementKey`, `excerpt`, `contentDigest` and `coverage`
   (`full|partial|none`) (`dokimos-construction-inputs.schema.json:131-164,165-275`;
   `dokimos-adapter-oracle.schema.json:150-183`). A requirement is `verified` when all of its scored
   cases passed and coverage is full, and `partial` when they passed with partial coverage
   (`dokimos/docs/behavior-verification.md:148-150`). Construction reports per-requirement
   `Represented … IndependentlyVerified … Waived` (`dokimos/docs/construction-verification.md:70-75`).
4. **Completion verification.** The host lists `RequiredRequirementIds` and
   `RequiredValidationNames`. Missing and extra references both fail closed
   (`dokimos-completion-authority.schema.json:18-21`; `dokimos/docs/completion-verification.md:24-31`).

The aggregate quality gate that aegis and fides run has **no** requirement notion. It counts
`tests.total/passed/failed/skipped` from TRX (`dokimos/src/Dokimos.Core/Collection.fs:67`) and
ratchets `tests.failed` to 0 (`aegis/.dokimos/policy.json`, `fides/.dokimos/policy.json`).

## 3. CLI surface and CI invocation

Commands are dispatched in `dokimos/src/Dokimos.Cli/Program.fs:470-497`: `version`, `capabilities`,
`compare`, `ecir audit|recognition-audit`, `construction verify`, `behavior verify`,
`completion verify`, `evidence plan|record`, `evaluate`, `snapshot`, `measure`, `analyze`,
`store …`, `history`, `init`, `results`, `status|verify|doctor`, `upgrade`, `identity`,
`verification compile|scaffold|coverage|probe|requirements`, `ratchet …`.

Global exit codes (`Program.fs:466`; constants `dokimos/src/Dokimos.Core/Evaluation.fs:106-121`,
`Ratchet.fs:9`): 0 continue, 1 unexpected fault, 2 invalid invocation, 3 evidence
unavailable/invalid, 4 policy failure, 5 store conflict, 6 invalid exceptions.

Verification compiler (`VerificationCommand.fs:381-387`; `dokimos/docs/verification-compiler.md:9-15`):

| Command | Inputs | Output | Exit |
|---|---|---|---|
| `verification compile --obligations F [--out F]` (or `--indy-execution-plan F --indy-verification-plan F`) | obligation set | validated `verification-obligations/1` | 0; 4 with `verification-refusals/1`; 3 unreadable (`VerificationCommand.fs:136-158`) |
| `verification coverage --obligations F [--root .] [--results MANIFEST] [--candidate SHA] [--probes F]… [--scope REQ,…] [--require-verified true] [--out F] [--praxis-matrix F --work-item ID --dimensions D,…]` | set, source tree, `verification-results/1`, probe results | `verification-coverage/1` (+ optional `praxis.verification-matrix/1`) | 0; 4 if `--require-verified true` and not all verified; 3 unreadable (`VerificationCommand.fs:162-181`) |
| `verification scaffold --obligations F …` | set, tree | `verification-work/1`, optional skeletons | 0 (`:186-213`) |
| `verification probe --spec F --sha SHA …` | `fault-probes/1` | `fault-probe-results/1` | 0 all detected, 4 otherwise (`:257-288`) |
| `verification requirements --manifest F --observations F|DIR --results PW.json --root . --candidate SHA …` | Praxis manifest, observations, Playwright JSON | `requirement-verification/1` | 0 accepted, 4 not, 3 manifest unavailable, 2 bad args (`:295-379`; `requirement-verification.md:28-33`) |

**How CI invokes Dokimos (aegis and fides).** Both run only the aggregate quality gate. No cloned
repository workflow invokes `dokimos verification` (a search of `*.yml` under `kemiller2002/` found
none). Aegis, `aegis/.github/workflows/dokimos.yml:38-48`:

```yaml
      - name: Test (TRX captured as evidence)
        continue-on-error: true
        run: dotnet test Aegis.sln --configuration Release --no-build --logger trx --results-directory artifacts/dokimos/test-results
      - uses: kemiller2002/dokimos/actions/quality-gate@2923d18917ed06601d996febd1793ab7a1fcd8e8
        with:
          version: 0.1.0
          package-sha256: 9fcc30e71e843a20c8b69fcc204e574d590c077b25899c2808e59a9173dba536
          source: src
          policy: .dokimos/policy.json
          build-log: artifacts/dokimos/build.log
          test-results: artifacts/dokimos/test-results
          evidence-branch: dokimos-evidence
```

Fides, `fides/.github/workflows/dokimos.yml:38-48`, is the same with
`quality-gate@2be0cf4e8cbba482ab481614f99c7c88b49354ec` and `version: 0.2.0`, with no
`package-sha256`. The action runs `snapshot → store → compare → evaluate` and exits with the
evaluation code (`dokimos/actions/quality-gate/gate.sh:3,66-71,102-103`;
`dokimos/actions/quality-gate/action.yml:126-138`).

**Praxis.** PRX-QUAL-023 consumes `dokimos.ratchet` 1.0.0 via `--evidence dokimos-ratchet=PATH`.
Unavailable evidence is never a pass (`praxis/requirements/CODE-QUALITY-HARDENING.md:316-340`).
Praxis also gates `work complete` on `praxis.verification-matrix/1` (facet
`verification-matrix-satisfied`, `CODE-QUALITY-HARDENING.md:309-314`). Dokimos `coverage
--praxis-matrix` writes that contract, "so no Praxis change is needed"
(`dokimos/docs/verification-compiler.md:58-74`).

## 4. Evidence semantics

- **Exists vs passed.** Identity statuses are kept apart: `not-generated` (absent), `invalid`
  (present but cannot count), `generated` (valid, suite has no result), `not-executed` (suite ran,
  identity has no result), `passed`/`failed`, and `blocked` (`Coverage.fs:7-21,140-174`;
  `dokimos/docs/verification-compiler.md:25-34`). A runtime skip is `invalid`
  (`Coverage.fs:168`). A placeholder skeleton cannot verify even when a result claims it passed
  (test `VerificationTests.fs:226-227`). For UI verification, `implemented` (V1 passed) and
  `verified` are counted separately, and `verifiedPercent` uses verified only
  (`requirement-verification.md:54-60`).
- **Staleness (by commit).** A result run is admitted only when `sourceSha` equals the candidate
  (case-insensitive, at least 7 characters). Other runs are rejected with a reason and never
  carried forward (`Coverage.fs:78-93`; `dokimos/docs/verification-compiler.md:43`). If a suite
  has only other-SHA results, its identities are `blocked` (`Coverage.fs:171-172`). In UI
  verification, observations from another manifest hash or commit are BLOCKED, and an edited
  manifest is detected (`RequirementVerification.fs:140-147`). In completion verification,
  `ClaimAcquiredAt`/`LeaseExpiresAt` give `EVIDENCE_STALE`/`STALE_LEASE`
  (`completion-verification.md:49-61`).
- **Evidence reuse.** Reuse happens only in `dokimos evidence plan`. It uses the
  content-addressed identity `dokimos.evidence-identity/1` over sources, dependencies, test
  contract, configuration and tool version (`dokimos/docs/evidence-reuse.md:67-79`). Freshness
  policies are `content-addressed | per-commit | live` (live is never reused)
  (`evidence-reuse.md:87-93`). Security and independent-acceptance categories are never reused
  (`:129-144`). The `release` checkpoint forces every test to run (`:146-164`). "Reuse is not
  acceptance": completion rejects undeclared, stale-commit or live reuse (`:252-267`). The
  verification compiler itself performs **no** reuse (`Coverage.fs:78-79`).
- **Contradictory results.** If one identity has differing outcomes across attempts or retries,
  it is `Failed` (`TestResults.fs:70,130-131`; `VerificationCommand.fs:328-334`). A failing
  record at the current identity blocks reuse (`REUSE.CONFLICTING_PRIOR`). Rewriting a stored
  record is a store conflict, exit 5 (`evidence-reuse.md:212-224`). In requirement aggregation the
  worst status wins (`Coverage.fs:182-190`; `requirement-verification.md:45-46`).

## 5. Feasibility: can EKS emit a file an existing Dokimos input accepts?

| Candidate input | Fits "R must be verified by executable check T"? | Why |
|---|---|---|
| **`dokimos.verification-obligations/1`** | **Yes** | Generic. Requirement id, criterion text, obligation id, exact test identity, suite runner (`dotnet-test`, `playwright`, `evidence-record`) (`Obligations.fs:20-63,140-210`). EKS is F#/xUnit, so `dotnet:` identities via TRX fit directly (`TestResults.fs:41-68`). |
| `praxis.verification-manifest/1` | Only indirectly, and only for UI | Checks are UI facts (`exists`, `visible`, `enter`, `save` …) judged from browser observations (`requirement-verification.md:62-74`). The manifest is "Written by the compiler only" (Praxis) (`praxis/docs/requirement-verification-manifest.md:22`). EKS could at most supply a `praxis.requirement-catalog/1` for Praxis to compile. |
| construction oracle (`dokimos.construction-oracle/1`) | No | `binding` requires `stateType, commandType, transitionModule, portsInterface`, and case kinds are `decide`/`sequence`/`port-surface` (`dokimos-construction-inputs.schema.json:80-130,180-185`). It is a state-machine oracle for Ordo-generated F#. |
| `dokimos.adapter-oracle/1` | No | Expectations are hard-coded to the timer experiment: `next ∈ {NoTimer, Running, Paused}`, `effects ∈ {complete-entry}`, `recorded-minutes` (`dokimos-adapter-oracle.schema.json:256-267,389`). The producer must not be `generator` or `ordo` (`:36-46`). |
| `dokimos.completion-authority/1` | Not EKS's role | `RequiredRequirementIds` exists (`:18`), but the document is *host-supplied authority* bound to a claim token and fencing generation (`completion-verification.md:23-31`). Praxis, not EKS, is the host. |
| `dokimos.evidence-reuse-request/1` | Complementary only | Describes reuse inputs per `TestId`, not obligations. `TestContract` is a path or digest of the test's specification (`dokimos-evidence-reuse-request.schema.json:36-40`). An EKS contract file could be named there (hypothesis). |
| policy / ratchet baseline | No | These are aggregate metrics, with no requirement identity (`dokimos-policy.schema.json:138`; `Collection.fs:67`). |

**What does not exist (verified absent):**

- No JSON Schema for `verification-obligations/1`, `verification-results/1` or
  `verification-coverage/1` in `dokimos/schemas/`. A search of `schemas/` for these ids found
  nothing; they appear only in `src/` and `docs/verification-compiler.md`.
- No verification of `planDigest`/`planId`. They are echoed into coverage (`Coverage.fs:294-296`)
  but never compared to a pinned value. The construction path does pin plan digests
  (`dokimos/src/Dokimos.Core/ConstructionChecks.fs:70-73`); the verification compiler does not.
- No strict intake for obligations. Unknown members are ignored by `Json.field`/`fieldOr`
  (`Json.fs:86-105`), and `toJson` writes only known fields (`Obligations.fs:219-240`), so
  EKS-specific provenance (record hash, contract hash) does not survive `compile`.
- No per-obligation freshness or reuse in the compiler: SHA equality only (`Coverage.fs:78-93`).
- No runner other than TRX, Playwright JSON and Playwright list (`dokimos/docs/verification-compiler.md:117`).
  Shell or CLI checks (for example `eks validate` exit 0) are not executable identities. They
  could only be `scheme:` record evidence, which Dokimos always reports as `blocked`
  (`Coverage.fs:144`).
- No inference of tests from requirement text. Dokimos refuses unbound obligations
  (`Obligations.fs:129-130`), and its docs state "The compiler never invents expected behaviour"
  (`Obligations.fs:9-10`).
- No EKS-side test binding. EKS obligations carry `Id, Class, Criterion, Mandatory,
  MechanicallyTestable` (`src/Echelon.KnowledgeStore.Compiler/Packages.fs:25-33,225-242`). EKS
  `RelationKind` has no verification relation (`src/Echelon.KnowledgeStore.Core/Model.fs:24-35`).

## 6. Recommended minimal integration (hypothesis, not run)

Principle: EKS supplies obligations and their declared bindings, and Dokimos alone decides
satisfaction. EKS never writes a status.

**Smallest verified path (all Dokimos parts exist today):**

1. **EKS emits `eks-obligations.json`** in `dokimos.verification-obligations/1` shape (fields per
   `Obligations.fs:192-210`):

   ```json
   { "schema": "dokimos.verification-obligations/1",
     "planId": "<EKS compilation id>", "planDigest": "sha256:<EKS contract hash>",
     "suites": [ { "id": "SU-EKS-DOTNET", "runner": "dotnet-test",
                   "command": "dotnet test Echelon.KnowledgeStore.sln -c Release",
                   "paths": ["tests"], "requiredBoundary": [] } ],
     "namespaceRoots": [ { "namespace": "Echelon.KnowledgeStore.Core.Tests", "directory": "tests/Echelon.KnowledgeStore.Core.Tests" } ],
     "obligations": [ { "id": "RQ-X/O1/mechanical", "requirement": "RQ-X",
                        "criterion": "<acceptance criterion text, verbatim>", "priority": "", "categories": [], "dependencies": [] } ],
     "tests": [ { "id": "dotnet:<Ns.Module>.<test name>", "suite": "SU-EKS-DOTNET",
                  "obligations": ["RQ-X/O1/mechanical"] } ] }
   ```

   The obligation `id`, `requirement` and `criterion` map 1:1 from the existing EKS `Obligation`
   (`Packages.fs:236-242`). The `tests` bindings must come from knowledge that a person declared;
   EKS must never guess them.
2. **Gate the plan:** `dokimos verification compile --obligations eks-obligations.json`. Exit 4
   lists every unbound or criterion-less obligation (`VerificationCommand.fs:147-158`). This
   makes EKS's missing bindings visible immediately.
3. **Run tests with TRX** (as aegis and fides already do) and write a
   `dokimos.verification-results/1` manifest: `{schema, candidate, runs:[{suite, format:"trx",
   path, sourceSha, runId}]}` (`TestResults.fs:137-157`).
4. **Decide:** `dokimos verification coverage --obligations eks-obligations.json --root .
   --results results-manifest.json --candidate $GITHUB_SHA --require-verified true`. Optionally add
   `--praxis-matrix … --work-item … --dimensions …` for Praxis completion
   (`VerificationCommand.fs:162-181`).
5. **Non-mechanical obligations** (EKS classes `human`/`empirical`, `Packages.fs:218-223`) should
   be bound to a record identity such as `praxis:independent-review`. Dokimos reports these
   `blocked` and never judges them (`Coverage.fs:144`), so they stay visible and fail closed.
   Use `--scope` per requirement to gate only the requirements that are fully mechanical
   (`Coverage.fs:230`).

**Missing pieces, in order of need:**

1. **EKS (in scope for EKS).** A declared obligation-to-test binding in knowledge records, for
   example a `verifiedBy` field holding exact `dotnet:`/`playwright:` identities. Also an
   `eks dokimos-obligations` emitter in `tools/Echelon.KnowledgeStore.Cli`. Both need unit tests
   (EKS AGENTS.md rule 4).
2. **Dokimos (needs a Dokimos change, not EKS).** A published JSON Schema for
   `verification-obligations/1` and `verification-results/1`. A `--expected-plan-digest`
   check on `coverage` so a frozen EKS plan cannot be swapped. Strict intake that rejects unknown
   members, as completion already does (`completion-verification.md:18-20`).
3. **CI (consumer repositories).** No current workflow runs `dokimos verification`. aegis and fides
   pin Dokimos 0.1.0 and 0.2.0, and use only the quality gate
   (`aegis/.github/workflows/dokimos.yml:41-43`; `fides/.github/workflows/dokimos.yml:41-43`). Which
   published Dokimos release first contains the verification compiler was not established here;
   the source at this commit is 0.11.1.
4. **Not recommended.** Emitting construction or adapter oracles, completion authorities, or
   Praxis manifests from EKS. Each either has domain-fixed vocabulary or belongs to another
   system's authority (see section 5).

## 7. Measured after this document was written: EKS → Dokimos round trip

The "hypothesis, not run" status above was superseded by a measured run in the same phase.

- `src/Echelon.KnowledgeStore.Compiler/WorkItems.fs` (`WorkItems.dokimosObligations`) emits
  `dokimos.verification-obligations/1` for the requirements a Praxis work item cites. Obligations
  come from `## Acceptance` criteria. Tests come **only** from a declared `verified_by` list on the
  requirement record (`dotnet:Namespace.Class.Method`). Suites are supplied by the caller
  (`--suite-id`, `--suite-command`, `--suite-path`), because EKS does not know how a repository
  runs its tests and must not invent it.
- The CLI refuses to emit without a suite command, and warns for each cited requirement with no
  declared binding.
- Run: `eks dokimos-obligations fx-ledger WI-0004 --suite-id fx-ledger-unit --suite-command "dotnet test tests/FxLedger.Tests" --suite-path tests/FxLedger.Tests`
  on the synthetic fixture, then `dokimos verification coverage --obligations F --root .` with the
  Dokimos CLI built from `6ea8624` (run under `DOTNET_ROLL_FORWARD=Major` on .NET 10).
- Result (`runs/wi-0004.obligations.json`, `runs/wi-0004.dokimos-coverage.json`): Dokimos parsed
  the file without refusal, located both bound tests in source (`LedgerTests.fs:14`, `:19`), and
  reported both obligations **`not-run`** ("generated, no result at the candidate"). A test that
  exists is not a verified obligation; Dokimos enforces this without any EKS logic.

Findings from the run:

1. **Declared bindings can be wrong.** RQ-FXL-0005's `verified_by` names the two visible tests, but
   neither exercises the cut-off boundary the first criterion is about. If both pass, Dokimos will
   report the obligation verified. Binding correctness is a review question; neither EKS nor
   Dokimos can decide it. (In the coding trials the boundary bug was fixed only because hidden
   tests checked it.)
2. **Coarse binding.** A requirement-level `verified_by` binds every obligation of that requirement
   to every listed test. Per-criterion bindings would need a schema for criteria identities, which
   EKS does not have yet.
3. **Category quality.** EKS's keyword classifier labelled both RQ-FXL-0005 criteria `human`
   (not mechanically testable); they are plainly testable. The classifier is a heuristic and should
   not be sent to Dokimos as fact; the field is informational there.
4. **planDigest.** EKS fills it with a hash of obligation ids and criteria. Dokimos carries it
   unchecked (§ Verdict, item 3), so it provides traceability, not protection.
