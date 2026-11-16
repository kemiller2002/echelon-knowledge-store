---
id: EKS-BASELINE-XREPO-001
title: Cross-repository knowledge dependency inventory (verified)
status: draft
mission: EKS-RESEARCH-2026-001
workstream: A/H
created: 2026-11-16
related_documents:
  - cross-repo-edges.json
  - current-agent-workflows.md
---

# Cross-repository knowledge dependency inventory

This inventory covers 13 repositories, read from shallow clones at
`/home/user/kemiller2002/<repo>`. The audit was read-only. The repositories and
their HEAD commits are:

- aegis 2c28b6e
- arca 8cfa064
- clarity-framework 8cd4801
- dokimos 6ea8624
- echelon-diagnostic-framework (EDF) 41fea6f
- echelon-engineering-system (EES) 4ca2f98
- echelon-organization-administration (EOA) 9ee7c93
- fides f9b7291
- forma 3887eec
- mercatus d67feaf
- percepta 4a53688
- praxis 12bf995
- signal e5530d3

The machine-readable edge list is [`cross-repo-edges.json`](cross-repo-edges.json).
It has 100 edges, 47 distinct (from, to) repository pairs, and 0 verification
failures.

## Method and verification

1. **Candidate discovery.** I ran a case-insensitive, word-bounded grep for each target repo's name, package name, release URL and requirement-ID prefix across all other repos. Examples of the search terms: `\bfides\b`, `@echelon-foundry/design-system`, `kemiller2002/forma`, `FORMA-[A-Z]+-\d+`, `FID-[A-Z]+-\d+`, `\bEDF\b`, `\bClarity\b`, `\bdokimos\b`, `"id": "praxis"`. The search excluded `.git`, `node_modules`, `.ros/` (event logs) and `telemetry/`.
2. **Manual triage.** I read each hit in context and kept it only if it states a real relationship. I discarded marketing copy, generic words and coincidental ID shapes. The false positives are listed in the "Rejected" section below.
3. **Mechanical re-verification.** Each kept edge is stored as `(from_repo, from_path, regex, to_repo, to_artifact, kind)`. A script (`edges.py`, run with python3) opens the file, finds the first line that matches the regex, and writes `evidence = "<repo>/<path>:<line>: <snippet>"`. If the file or the match is missing, the script fails. Final run: `edges 100 failures 0`.
4. **Target existence.** I checked that cited target artifacts exist in the target repo:
   - `fides/docs/requirements/FIDES-REQUIREMENTS.md:1499` defines FID-DATA-009, and line 543 defines FID-CLI-001.
   - These Forma files exist: `forma/requirements/WHITE-LABEL-AND-SKINNING.md`, `forma/requirements/AEGIS-FAULT-PRESENTATION.md`, `forma/docs/MARKETING-SITES.md` and `forma/src/workflow/Forma.Workflow/RequirementIdentity.fs`.
   - `praxis/requirements/CODE-QUALITY-HARDENING.md:316` defines PRX-QUAL-023.
   - **One exception:** FORMA-NS-001 is cited by mercatus and signal, but its only definition is an intake inbox file, `forma/.praxis/inbox/documents/2026-11-09-FORMA-NS-001-namespace-rename.md`, plus forma's work queue. No canonical requirement record exists for it.

**Kinds used.** These are the values of the `kind` field in the JSON:
- `vendors`: a copy or a pinned binary artifact is committed in the consumer.
- `depends-on`: a build, package, configuration or normative MUST relationship.
- `implements`: code or configuration that realises the target's contract.
- `validates`: tests or CI that check against the target.
- `cites`: a specific ID, file or issue in the target.
- `references`: prose relationship with no machine binding.

The JSON adds one field beyond the requested shape: `scenario`.

**Scope.** Only edges between the 13 audited repositories are included. The
`conditor.json` files also declare components outside the audited set (ordo,
limen, limen-fsharp, folio, visual-engineering, communication-engineering,
echelon-theme, tutela). These are counted in the summary but are not edges.

## Summary by mandated scenario

| Scenario | Edges | Consuming repos | Strength |
|---|---|---|---|
| Depends on **Fides** requirements/APIs | 12 | signal (7), arca (3), praxis (1), dokimos (1) | **One hard consumer.** Signal builds against the Fides 0.2.0 NuGet API and cites FID-DATA-009. Arca's links are a designed *non-dependency*: a token-provider seam enforced by a test. Praxis only notes possible future integration, and dokimos only references a Fides issue. |
| Consumes **Forma** design standards | 29 | signal (5), mercatus (5), EDF (4), praxis (3), fides (3), dokimos (3), EES (2), percepta (2), arca (1), EOA (1) | **Strongest knowledge hub.** 7 repos pin a Forma release: npm tarball, `forma.lock` or a vendored `.tgz`. Fides requirements cite Forma requirement files by path. 4 repos validate Forma pins in tests or CI. |
| Depends on **Clarity** methodology | 9 | EDF (4), EOA (3), mercatus (2) | **Conceptual only.** No repo cites a clarity-framework file path, ID or URL (grep for `clarity-framework/[private path withheld]`, `kemiller2002/clarity` and Clarity file names outside clarity-framework: 0 hits). EDF positions Clarity as the "Decide" stage. EOA's business-plan agent prompt says "Apply Clarity". Mercatus treats Clarity as optional decision support and as a product to sell. |
| Depends on **EDF** diagnostic models | 8 | clarity-framework (3), dokimos (1), forma (1), mercatus (1), praxis (1), EOA (1) | **No repo depends on EDF's diagnostic model.** Clarity's README describes EDF modes (EDF-0) conceptually. Dokimos explicitly declines to depend on EDF internals (`dokimos/docs/architecture/ARCHITECTURE.md:77`). Forma cites the EDF *website* as a bug reporter. Mercatus and EOA mention EDF as a commercial initiative. The praxis edge is boilerplate copied into 10 repos. |
| Depends on **Dokimos** validation rules | 14 | fides (4), aegis (2), praxis (2), percepta, mercatus, signal, arca, EES, forma (1 each) | **Two installed gates.** aegis and fides have `.dokimos/policy.json` and a SHA-pinned `quality-gate` action. Praxis consumes Dokimos evidence at work completion (PRX-QUAL-023, DF-ROS-2026-A052), and percepta implements a Dokimos contract. Mercatus has planned checks that cannot run yet. Signal and arca record that Dokimos is not installed or not adopted. |
| **Praxis** framework vendored into others | 20 | aegis, arca, dokimos, EDF, fides, forma, mercatus, percepta, signal (2 each), EOA, EES (1 each) | **Universal vendoring.** All 9 Praxis-governed repos carry a 100–103-artifact Praxis payload managed by `.echelon/ros.json`, and all 9 pin praxis in `conditor.json`. The content-hash data is in the "Praxis vendoring" section. |
| Other edges among audited repos | 8 | fides→arca, fides→percepta, signal→arca, signal→EOA | Arca NuGet vendoring. Signal deploys its production data into EOA. |

Totals by kind: depends-on 34, references 27, vendors 14, cites 12, validates 7,
implements 6.

### Dependency direction (from the edge list)

```
praxis  <── vendored by 9 repos (aegis arca dokimos EDF fides forma mercatus percepta signal)
forma   <── pinned by dokimos EDF EES mercatus percepta praxis signal; cited by fides
praxis  ──> forma (vendor/forma/forma.lock)      forma ──> praxis (installs Praxis 3.7.2)   [cycle]
fides   <── signal (API)          arca <── fides, signal (NuGet)       fides ──> arca (implements port)
dokimos <── aegis, fides (gate)   praxis (evidence consumer)           percepta (contract)
EDF     <──> clarity-framework (prose only, both directions)
signal  ──> echelon-organization-administration (production data store)
```

## Scenario detail

### 1. Fides requirements and APIs

| From | Evidence | Kind |
|---|---|---|
| signal | `Directory.Packages.props:23` `<PackageVersion Include="EchelonFoundry.Fides" Version="0.2.0" />` | depends-on |
| signal | `src/Echelon.Signal.Application/Echelon.Signal.Application.fsproj:45` `PackageReference Include="EchelonFoundry.Fides.Client"` | depends-on |
| signal | `vendor/nuget/fides.lock:6` `repository kemiller2002/fides` (4 sha256-pinned nupkgs) | vendors |
| signal | `conditor.json:31` `"id": "fides"` (0.2.0) | depends-on |
| signal | `src/Echelon.Signal.Application/Identity.fs:17` `open Fides.Client` | implements |
| signal | `research/decisions/DF-SIGNAL-2026-0001--…md:69` "Administrator credentials come from Fides" | references |
| signal | `research/decisions/DF-SIGNAL-2026-0027--…md:61` "(Fides FID-DATA-009)" | cites |
| arca | `docs/requirements/ARCA-STORAGE-REQUIREMENTS.md:118` "ARCA-D-002 \| Arca does not depend on Fides" | references (negative) |
| arca | `tests/Arca.Tests/ArchitectureTests.fs:153` "no Arca package depends on Fides … (ARCA-ARCH-005)" | validates (absence) |
| arca | `.ros/work/queue.json:162` "Fides supplies an implementation (Fides FID-CLI-001)" | cites |
| praxis | `docs/ecir-host-integration.md:75` "Fides integration may be added later" | references |
| dokimos | `.ros/work/queue.json:566` "Found on kemiller2002/fides#16" | references |

Only **2 Fides requirement IDs** are cited outside the fides repo. FID-DATA-009 is
cited in a signal decision, and FID-CLI-001 is cited in arca's work queue, which
is a JSON event store and not a Markdown record. Mercatus does **not** depend on
Fides. Its `.conditor/authority/resolved-release-set.json:530` lists fides only
because that file is a copied registry catalog of every system. The same file is
present in aegis, arca, percepta, signal and others.

### 2. Forma design standards

Consumers by mechanism:

- **npm tarball pin.** These packages pin `@echelon-foundry/design-system`. The
  versions differ:
  - `dokimos/package.json:25`: 0.3.0
  - `percepta/package.json:29`: 0.4.1
  - `echelon-engineering-system/package.json:14`: 0.9.0
  - `mercatus/package.json:12`: 1.0.1
  - `signal/package.json:18`: 1.0.1

  So **five consumers are on four different Forma versions** (0.3.0, 0.4.1,
  0.9.0, 1.0.1). Two vendored lock files add more: 0.13.0 in EDF and 0.4.1 in
  praxis, against Forma's own `package.json` at 1.0.2.
- **Vendored lock file and tarball.** `echelon-diagnostic-framework/forma.lock:4`
  (0.13.0 CSS assets) and `praxis/vendor/forma/forma.lock:7` (0.4.1 `.tgz`).
  Praxis also embeds the lock file in its own binary:
  `src/Praxis.Infrastructure/Praxis.Infrastructure.fsproj:216`.
- **Normative requirements.**
  - `mercatus/requirements/UI-REQUIREMENTS.md:22`: "MUST be built with Forma".
  - `fides/docs/requirements/FIDES-REQUIREMENTS.md:1701` (FID-UX-003), which
    also cites Forma requirement files by path at `:1707` and `:1771`.
- **Validation.**
  - `echelon-diagnostic-framework/tests/forma-site.test.mjs:43`
  - `mercatus/tools/modernization/ui-conformance.mjs:12`
  - `signal/tests/Echelon.Signal.Tests/FoundationsConformanceTests.fs:19`
  - `praxis/tests/Praxis.Tests/FormaPresentationTests.fs:27`
- **Code mirroring a Forma type.** `dokimos/src/Dokimos.Core/Verification/RequirementJudging.fs:18`
  "Mirrors Forma.Workflow.RequirementIdentity". The type is duplicated, not
  imported, so it can drift silently.
- **Agent instruction.** `percepta/AGENTS.md:321` "Use **Forma** … Do not copy Forma CSS into Percepta."
- **Weak references.** `arca/docs/requirements/ARCA-STORAGE-REQUIREMENTS.md:120`
  and `echelon-organization-administration/[private path withheld]`.

Not a consumer: `aegis`. Its only Forma mention is the copied
`.conditor/authority/resolved-release-set.json`. `clarity-framework` has 0
word-bounded Forma hits.

### 3. Clarity methodology

| From | Evidence | Kind |
|---|---|---|
| EDF | `docs/edf/README.md:89` "## Relationship To Clarity" | references |
| EDF | `docs/edf/specification.md:194` "## Relationship To Clarity" | references |
| EDF | `docs/edf/operating-cycle.md:18` "\| Decide \| Evaluate options and choose a path \| Clarity \|" | depends-on (conceptual) |
| EDF | `docs/edf/specification-v1-draft.md:214` "Clarity governs reasoning depth and reconsideration." | depends-on (conceptual) |
| mercatus | `requirements/PRODUCT-REQUIREMENTS.md:220` "Clarity MAY be used for market selection…" | references |
| mercatus | `requirements/PORTFOLIO-GTM-SCENARIOS.md:10` | references (commercial) |
| EOA | [private path and quote withheld] | depends-on (agent method) |
| EOA | [private path and quote withheld] | references |
| EOA | [private path withheld] | references |

**Explicit finding:** no repository links to any clarity-framework artifact.
Every consumer restates Clarity in its own words, for example EOA's
`02.03-clarity-operating-model.md`. There is no path link, ID citation or
version pin, so a change to Clarity cannot be propagated or detected.
Clarity's methodology is not machine-addressable: clarity-framework has 17 of
131 Markdown files with front matter and no `AGENTS.md`.

### 4. EDF diagnostic models

**Explicit finding: no repository depends on EDF's diagnostic model**, meaning
its modes, lenses or Diagnostic Sufficiency. The 8 edges are:
- conceptual cross-references from clarity-framework (`README.md:41`, `:51`; `docs/progressive-commitment.md:15`);
- an explicit refusal to depend on it, at `dokimos/docs/architecture/ARCHITECTURE.md:77`: "EDF/Ordo research is still evolving. Dokimos therefore depends on narrow state/evidence principles rather than copying experimental framework internals";
- Forma citing the EDF *site* as a bug reporter (`forma/CHANGELOG.md:106`);
- commercial mentions (`mercatus/requirements/PORTFOLIO-GTM-SCENARIOS.md:1`, `echelon-organization-administration/[private path withheld]`);
- one boilerplate sentence in the vendored `docs/agent-provenance.md`, recorded once from its praxis source (`praxis/docs/agent-provenance.md:446`). Identical copies are in 9 more repos at line 420 or 446.

EDF's own ID families appear outside EDF only incidentally:
- `EX-EDF-001` in a Forma test fixture;
- `EV-EDF-2026` in a Praxis unit-test fixture;
- `EX-EDF-2026` in an EOA audit.

### 5. Dokimos validation rules

| From | Evidence | Kind |
|---|---|---|
| aegis | `.github/workflows/dokimos.yml:41` `uses: kemiller2002/dokimos/actions/quality-gate@2923d18…` | validates |
| aegis | `.dokimos/policy.json:2` `"$schema": "https://echelonfoundry.com/dokimos/schemas/policy"` | implements |
| fides | `.github/workflows/dokimos.yml:41` `quality-gate@2be0cf4…` (a **different** pinned SHA from aegis) | validates |
| fides | `.dokimos/policy.json:2` | implements |
| fides | `docs/requirements/FIDES-REQUIREMENTS.md:817` "**FID-TEST-014** Requirement coverage MUST be evaluated by Dokimos" | depends-on |
| fides | `conditor.json:26` `"id": "dokimos"` (0.2.0) | depends-on |
| praxis | `research/decisions/DF-ROS-2026-A052--consume-quality-evidence-at-completion.md:3` | depends-on |
| praxis | `requirements/CODE-QUALITY-HARDENING.md:316` "PRX-QUAL-023 — Integrate Dokimos quality evidence" | depends-on |
| percepta | `src/Percepta.Core/Specification.fs:52` "`dokimos.verification-refusals/1`" | implements |
| mercatus | `docs/modernization/DOMAIN-UI-CONTRACTS.md:207` CHK-DOK-01..03 "defined, not executable" | references |
| signal | `docs/modernization/SYSTEM-INVENTORY.md:52` "Dokimos \| not installed \| 0.11.1 (required by the channel)" | references |
| arca | `docs/measurement/baseline-sources.md:37` "Dokimos checks \| not adopted" | references |
| EES | `requirements/ENGINEERING-SYSTEM-WEBSITE-v0.1.md:40` REQ-EVID-001 | references |
| forma | `research/requirements/RQ-FORMA-2026-A002--public-semantic-color-contract.md:55` FORMA-COLOR-001.2 "usable by … Dokimos CI" | references |

All 10 Praxis-governed repos also carry a vendored `docs/work-protocol.md` (line
71 or 81) that names the `dokimos-ratchet` evidence type, and a vendored
`schemas/praxis-completion-readiness.schema.json:5` that cites PRX-QUAL-023. I
recorded these through the Praxis vendoring edges, not as separate Dokimos
edges.

### 6. Praxis framework vendored into others

Each of the 9 adopters has two edges:
- `.echelon/ros.json:4` `"package": "@echelon-foundry/repository-operating-system"`, with 100 managed artifacts (103 in mercatus and signal), split as 69–72 tool-owned, 16 shared, 7 user-owned and 8 generated;
- a `conditor.json` `"id": "praxis"` pin.

Two further edges: EOA cites PRX-QUAL-001, and EES names Praxis as an
authoritative source.

Content hashing shows how much is copied. The command is in Appendix A of `current-agent-workflows.md`. **Every
file below is byte-identical in all 10 Praxis-governed repos** (the 9 adopters
plus praxis itself):

| Vendored set | Files | Bytes per copy | Copies | Redundant bytes |
|---|---|---|---|---|
| `docs/00-governance/*` (constitution, Agent Operating Manual, Engineering Standards, REP spec, Decision Log, index) | 6 | 68,740 | 10 | 618,660 |
| `framework/**` (REP spec, policies, protocols, standards) | 11 | ~10,357 | 10 | 93,213 |
| `templates/**` (research + mission templates) | 7 | ~7,148 | 10 | 64,332 |
| `BOOTSTRAP.md`, `CLAUDE.md` | 2 | 3,152 | 10 | 28,368 |
| `.sde/**` (State-Directed Engineering 1.5.0, a separate package) | 20+ | ~92,267 | 10 | 830,403 |
| `schemas/**` | 28 groups | – | ≤10 | 578,529 |

Version-cohort drift in files that share a path:
- `docs/work-protocol.md`, `docs/development-telemetry.md` and `docs/remote-protocol.md` each have **3 variants**:
  - 7 repos on Praxis 3.7.2 share one;
  - mercatus and signal on 3.25.0 share another;
  - praxis HEAD has the third.
- `AGENTS.md` has 9 variants in 10 repos (aegis and EDF are identical). Each is
  76–94% line-identical to `praxis/AGENTS.md`.

Cross-repo links inside vendored files point to **tag-pinned GitHub blob URLs**,
for example `aegis/docs/remote-protocol.md:9` →
`github.com/kemiller2002/praxis/blob/v3.7.2/research/decisions/DF-ROS-2026-A041-…`.
I counted 96 cross-repo `praxis/blob` links in adopters, 8–16 per repo. This is
the only cross-repo link convention that is version-safe.

There is also a **cycle**. Forma installs Praxis (`forma/conditor.json:16`,
`forma/.echelon/ros.json:4`), and Praxis vendors a Forma release
(`praxis/vendor/forma/forma.lock:7`).

## Rejected candidate relationships (false positives)

- **`EV-CLARITY-2026-0044`** appears in 10 repos, but only as the example in the
  vendored `framework/standards/NAMING-STANDARD.md:15`. It is not a citation of
  Clarity evidence.
- **`RFR-001` … `RFR-010`** are defined in both clarity-framework and EDF
  (`research/frontier/records/`) with **different content**. Both repos use the
  same "research frontier" layout, which I count as a shared convention. There
  are no citations in either direction, so this is an ID collision, not an
  edge.
- **`.conditor/authority/resolved-release-set.json`** lists every system (fides,
  forma, …) in aegis, arca, mercatus, percepta, signal and others. It is a
  copied global catalog, not evidence that the holding repo depends on those
  systems.
- **Fides in mercatus.** The only hit is that catalog
  (`mercatus/.conditor/authority/resolved-release-set.json:530`).
- **Clarity and EDF in marketing copy.** `forma/examples/marketing-site/index.html:70`
  and the Forma catalog cards (`forma/catalog/components/card-grid.mjs:72`, "Dokimos") are sample text.
- **The word "clarity"** in percepta and other repos. Word-bounded
  capital-C hits outside EDF, EOA and mercatus were sample text, a
  completed-mission non-goal (`praxis/missions/completed/MS-FE-2026-B001…md:63`),
  or the vendored governance index line "no … Clarity materials"
  (`aegis/docs/00-governance/README.md:67`, identical in 10 repos).
- **Name-only substring hits.** For example, "forma" in "format" or
  "information", and "signal" as an ordinary word. These were excluded by the
  word-bounded regexes.

## Implications for the knowledge store

1. **The only cross-repo knowledge edges that are versioned and verifiable are
   package and lock pins.** These are Conditor components, npm tarballs,
   `forma.lock` and `vendor/nuget/*.lock`. Prose relationships, such as
   Clarity↔EDF and the Clarity "operating model" in EOA, carry no version, path
   or ID. They will drift without anything detecting it.
2. **Requirement-level traceability across repos is nearly absent.** Only 2
   Fides IDs, 2 Forma IDs (one of them, FORMA-NS-001, has no canonical record)
   and a handful of PRX-QUAL IDs are cited outside their owning repo. Most
   consumers cite a *package version*, not the requirement that justifies it.
3. **Governance knowledge is duplicated rather than referenced.** The ~0.6 MB
   governance corpus is copied byte-for-byte 10 times. Its global IDs
   (GV-START-001, GV-INDEX-001, …) are therefore not globally unique.
   GV-START-001 names 9 different `AGENTS.md` texts.
