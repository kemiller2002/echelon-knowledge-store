# Portfolio integration requirements (KSI)

Status: **proposed requirements and hypotheses — nothing here is measured.**
Origin: echelon-registry work item GOV-TRN-001 (organizational transparency and portfolio
governance), 2026-11-25. Classification: public.

Related records:
- echelon-registry `portfolio/repositories.json`, `portfolio/initiatives.json`
  (`echelon.portfolio-repositories/v1`, `echelon.portfolio-initiatives/v1`), `spec/repository-manifest.md`
  (`echelon.repository/v1`), `portfolio/analysis/`.
- This repository's findings: keep Markdown with front matter canonical and compile it
  (`research/2026-knowledge-representation/strategy/executive-report.md`); task packages helped when
  requirements were amended by records the work item does not mention, on a synthetic benchmark
  (`research/2026-compiler-integration/README.md`, measured there, not here).

## 1. Division of responsibility

| Concern | Owner |
|---|---|
| What repositories, systems, capabilities, initiatives exist; their status | Echelon Registry (authoritative records) |
| Policies, controls | Organization administration (policy text), echelon-assurance (controls) |
| Representation of relationships, compilation, retrieval, task packages | **Knowledge Store** |
| When agents must consult or update the registry | Praxis (`requirements/REGISTRY-MAINTENANCE.md`) |

- **KSI-01** The Knowledge Store MUST NOT become a second portfolio authority. It ingests registry
  records read-only and derives representations; corrections go to the authoritative record.
- **KSI-02** Every derived node and edge MUST carry the source record, its path and its content digest,
  so a stale projection is detectable.

## 2. Entities and relationships to represent

**KSI-03** Represent at least these node kinds, each keyed by the authoritative identifier:

| Node | Identifier source |
|---|---|
| Repository | `portfolio/repositories.json` `id` |
| System | `registry/systems-v2.json` `id` |
| Capability | `provides[].id`, `plannedCapabilities[].id` |
| Product | initiative or system flagged `product` (provisional: no product registry exists yet) |
| Initiative | `INIT-NNN` |
| Policy / standard | `POL-*`, `STD-*`, `SEC-*` |
| Requirement | repository requirement ids (for example `RM-*`, `CA-*`, `SAF-URL-*`) |
| Control | echelon-assurance control ids |
| Evidence | evidence ids (`EV-*`) and evidence envelope ids |
| Research finding | `HY-*`, `TH-*`, research package ids |
| Decision | `DF-*`, `OD-*` |
| Risk | risk register ids |

**KSI-04** Represent at least these edges: `hosts` (repository→system), `provides` / `plansToProvide`
(system→capability), `participatesIn` (repository|system→initiative), `dependsOn` with `time`
(`run-time` | `repository-time` | `build`) and `strength` (`hard` | `optional`), `governedBy`
(any→policy), `implements` (system→requirement), `satisfies` (control→requirement), `evidences`
(evidence→control|claim), `decides` (decision→any), `supersedes`, `overlapsWith` (from the duplication
analysis, with finding id), `relatedTo` (initiative↔initiative).

**KSI-05** Distinguish *registered*, *implemented*, *proposed* and *unowned* capability status exactly
as the capability ownership matrix does; never collapse them.

## 3. Task-specific projections

Agents must not load the whole organizational knowledge base. **KSI-06** The compiler MUST produce
bounded, task-specific packages from a typed query, for example:

| Task type | Package contents (closure rule) |
|---|---|
| Security investigation of system *S* | *S*; policies `governedBy` *S* (Secure Construction, classification, disclosure, VDP); `SEC-*` rules and their owners; controls and current evidence status for *S*; initiatives INIT-001/003/006 records; open decisions touching *S* |
| Proposing a new product or system | capability ownership matrix rows for the proposed capabilities; systems that `provide` or `plansToProvide` them; architectural standards (F#, WebAssembly-first, secure construction); strategic initiatives and their status; duplication findings; relevant public commercial research |
| Changing responsibilities of system *S* | *S*'s manifest; dependents (`dependsOn` inbound) with time and strength; initiatives; RM-* obligations |
| Answering a due diligence question *Q* | *Q*; mapped controls; evidence status and claim status; policies; **public fields only** unless the requester is authorized |

**KSI-07** A package MUST state what it omitted (counts by node kind) so the agent knows the package is
partial.

**KSI-08** A package MUST respect classification: packages built for public outputs MUST be compiled
only from Public records (STD-ORG-PUB-001 §5). The compiler MUST refuse to mix classifications above
the requested ceiling, and MUST NOT include Restricted content in any package (Restricted material is
not in Git, per STD-ORG-CLS-001, so it should be absent by construction; the check is defensive).

## 4. Efficiency

**KSI-09** Retrieval MUST work from the registry records alone (two JSON files plus systems-v2 and the
analysis documents), with no service or database (AGENTS rule 6).

**KSI-10** Incremental compilation: a change to one repository manifest re-derives only its nodes and
edges.

## 5. Hypotheses to test (not claims)

| ID | Hypothesis | Measure | Falsified if |
|---|---|---|---|
| HY-KSI-01 | A portfolio task package lets an agent identify the correct existing owner of a capability faster and with fewer errors than reading `portfolio/` documents | time, tokens, correct-owner rate on a set of "where should this capability live" tasks | no improvement in correct-owner rate at equal or lower token cost |
| HY-KSI-02 | Packages reduce duplicate-system proposals | proposals that ignore an existing owner, before/after | no reduction |
| HY-KSI-03 | Digest-carrying projections let a reviewer detect stale portfolio facts | staleness detected / staleness seeded | below 90% detection |

Baselines are **unknown**; collection method: the benchmark harness in `benchmarks/` with seeded
stale records.

## 6. First increment (smallest useful)

1. Read `portfolio/repositories.json`, `portfolio/initiatives.json` and `registry/systems-v2.json`
   from a registry clone; derive KSI-03/04 nodes and edges with KSI-02 provenance.
2. Add one package type: "capability owner lookup" (supports RM-01 in Praxis).
3. Unit tests for derivation and a property test that every derived edge cites an existing source node.

Not started. Requires a work item in this repository.
