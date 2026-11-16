---
id: EKS-BASELINE-WORKFLOWS-001
title: How agents currently locate and consume knowledge across Echelon repositories
status: draft
mission: EKS-RESEARCH-2026-001
workstream: A/H
created: 2026-11-16
related_documents:
  - cross-repository-dependency-inventory.md
  - cross-repo-edges.json
---

# Current agent knowledge workflows (baseline)

This is a read-only audit of 13 shallow clones under
`/home/user/kemiller2002/`. The commit SHAs are listed in
[`cross-repository-dependency-inventory.md`](cross-repository-dependency-inventory.md).
All numbers come from scripts run against those clones. The commands are in
Appendix A.

**Token estimates are bytes ÷ 4**, a rough heuristic for English and Markdown
with a tokenizer. They are not tokenizer counts.

## 1. Three classes of repository

| Class | Repos | Agent entry point | Knowledge layout |
|---|---|---|---|
| **A. Praxis-governed** (Repository Operating System, "ROS", renamed Praxis in DF-ROS-2026-A050) | aegis, arca, dokimos, echelon-diagnostic-framework (EDF), fides, forma, mercatus, percepta, signal, and praxis itself (10) | `CLAUDE.md`, `GEMINI.md` and `.github/copilot-instructions.md` are 1-paragraph pointers to `AGENTS.md`. AGENTS.md is the canonical contract, `id: GV-START-001`. | Uniform skeleton: `context/`, `framework/`, `registries/`, `missions/`, `research/{decisions,evidence,experiments,hypotheses,journals,packages,theories}`, `docs/00-governance/`, `templates/`, `schemas/`, plus hidden tool payloads `.echelon/`, `.ros/`, `.sde/`, `.visual-engineering/`, `.communication-engineering/`, `.conditor/`, `.praxis/` |
| **B. Content repo, no agent contract** | clarity-framework (private) | None found (details withheld: private repository). | 131 `.md` files; layout withheld (private repository). |
| **C. Business/site repos, no agent contract** | echelon-engineering-system (EES), echelon-organization-administration (EOA, private) | README only (EES 2,983 B; EOA 1,630 B). EOA layout withheld (private repository). | EES: `requirements/`, `docs/`, `evidence/`. EOA: [private layout withheld]. |

### Entry files present (command: `python3 inv.py`, Appendix A)

| Repo | AGENTS | CLAUDE | GEMINI | copilot | BOOTSTRAP | HANDOFF | SDE-MAP | CHARTER | ros.json | conditor.json |
|---|---|---|---|---|---|---|---|---|---|---|
| aegis, dokimos, EDF, mercatus, percepta, signal | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ |
| arca, fides, forma | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| praxis | ✓ | ✓ | ✓ | ✓ | ✓ | **–** | ✓ | – | ✓ | – |
| clarity-framework, EES, EOA | – | – | – | – | – | – | – | – | – | – |

## 2. The startup chain an agent follows (class A)

1. The provider auto-loads its pointer file:
   - Claude Code loads `CLAUDE.md` (262 B, byte-identical in 10 repos): "Read and follow `AGENTS.md`, the canonical Praxis agent contract."
   - `GEMINI.md` and `.github/copilot-instructions.md` say the same thing in two variants. The variants differ only in the words "Praxis" and "ROS".
2. **`AGENTS.md`** is 20.3–26.7 KB. It opens with a 46-line YAML front-matter block of 1,218 B (1,605 B in forma). That block is mostly `provenance.contributions` naming `openai/codex`, `model: unknown`.

   Its "Start Here" section tells the agent to:
   - read `docs/00-governance/README.md`;
   - locate canonical domain records;
   - run `./praxis sync check --start`;
   - run `./praxis work begin --id … --occurred-at …`;
   - follow `docs/work-protocol.md`, `docs/development-telemetry.md` and the Agent Operating Manual.

   Tool-managed blocks are appended to AGENTS.md:
   - `<!-- BEGIN echelon:visual-engineering -->` requires 4 reads from `.visual-engineering/` before UI work;
   - `<!-- echelon:communication-engineering:start -->` requires 5 reads before "consequential communication".
3. **`BOOTSTRAP.md`** is 2,890 B and identical in 10 repos. It sets this read order:
   - 6 `framework/` files;
   - 4 `context/` files;
   - "Read only the mission and source records needed… Do not load the entire repository";
   - then execute.

   Results go to the `research/*` canonical roots. After that the agent runs `./praxis registry build` and `./praxis validate`, because registries are generated and must not be hand-edited.
4. **`HANDOFF.md`** (875 B – 48,612 B) and `context/CURRENT-STATE.md` hold the continuation state.
5. **Run-time retrieval goes through the Praxis CLI, not files.** The commands are `./praxis status`, `./praxis work context ID`, `./praxis work ready` and `./praxis add`. The CLI reads `.ros/` state: `.ros/work/queue.json`, `.ros/events/events.jsonl`, and telemetry. That state is large: praxis 28 MB, aegis 12 MB, signal 8.8 MB, forma 7.6 MB.

   The `praxis` and `ros` launchers (`aegis/praxis:2` cites DF-ROS-2026-A049/A050) download a pinned native CLI on first use.

What is **not** in the startup chain:
- `.sde/` (State-Directed Engineering 1.5.0, 20 files, ~92 KB, installed in all 10 class-A repos). No `AGENTS.md` mentions it. Outside `.sde/` and `.ros/` it is referenced by 0 files in aegis, arca, EDF, fides, forma and percepta (`grep -rl "\.sde/"`).
- `SDE-MAP.md` (arca, fides, forma, praxis). Only `.sde/README.md` routes to it.
- `registries/*.json`. Only `BOOTSTRAP.md` mentions them, and only as a write target.

There is **no cross-repository retrieval mechanism.** No index, registry or
link convention spans repos. The only cross-repo catalog is Conditor's copied
`.conditor/authority/resolved-release-set.json`, which lists releases, not
knowledge. Cross-repo knowledge is reached through package pins and GitHub
URLs. In 9 adopters there are 96 tag-pinned `praxis/blob/vX.Y.Z/...` links,
all inside vendored docs, and 12 `forma/issues/N` links.

## 3. Startup instruction volume (bytes ÷ 4 ≈ tokens)

Tiers are defined from the instructions themselves:

- **T0** = what the provider loads plus the one file it is told to read: `CLAUDE.md` and `AGENTS.md`.
- **T1** = the mandatory "Start Here" and BOOTSTRAP reads: T0, `docs/00-governance/README.md`, `BOOTSTRAP.md`, the 6 BOOTSTRAP framework files, 4 `context/` files and `HANDOFF.md`.
- **T1+cond** = T1 plus the reads required by the managed blocks before UI or communication work. `.visual-engineering/RESEARCH-INDEX.md` (86,108 B) is excluded because its wording is only "consult".
- **T2** = T1+cond plus the linked "detailed rules" in AGENTS.md: the Agent Operating Manual, constitution, Engineering Standards, REP spec, `docs/work-protocol.md`, `docs/development-telemetry.md`, `SDE-MAP.md` and `PROJECT-CHARTER.md`.

Command: `python3 budget2.py` (Appendix A).

| Repo | T0 | T1 mandatory | T1 + conditional | T2 full governance | T1 bytes byte-identical to another repo |
|---|---|---|---|---|---|
| aegis | 21,579 B (~5.4k tok) | 38,392 B (~9.6k) | 97,953 B (~24.5k) | 218,384 B (~54.6k) | 90% |
| arca | 20,831 (~5.2k) | 40,494 (~10.1k) | 76,998 (~19.2k) | 200,056 (~50.0k) | 33% |
| dokimos | 23,062 (~5.8k) | 47,756 (~11.9k) | 70,813 (~17.7k) | 191,040 (~47.8k) | 28% |
| EDF | 21,579 (~5.4k) | 38,530 (~9.6k) | 98,091 (~24.5k) | 218,614 (~54.7k) | 90% |
| fides | 23,476 (~5.9k) | 53,855 (~13.5k) | 113,416 (~28.4k) | 241,560 (~60.4k) | 24% |
| forma | 24,368 (~6.1k) | 68,524 (~17.1k) | 128,085 (~32.0k) | 252,292 (~63.1k) | 19% |
| mercatus | 22,781 (~5.7k) | 48,847 (~12.2k) | 108,408 (~27.1k) | 269,330 (~67.3k) | 27% |
| percepta | 26,653 (~6.7k) | 43,484 (~10.9k) | 103,045 (~25.8k) | 224,031 (~56.0k) | 30% |
| praxis | 20,621 (~5.2k) | 40,094 (~10.0k) | 40,094 (~10.0k) | 203,115 (~50.8k) | 33% |
| signal | 23,328 (~5.8k) | 89,228 (~22.3k) | 148,789 (~37.2k) | 314,330 (~78.6k) | 15% |
| clarity-framework | 0 | README 2,281 (~0.6k) | – | – | – |
| EES | 0 | README 2,983 (~0.7k) | – | – | – |
| EOA | 0 | README 1,630 (~0.4k) | – | – | – |

How to read the table:
- Class-A agents must read **~9.6k–22.3k tokens before touching the task**, ~17.7k–37.2k for UI work, and up to ~78.6k if they follow every "detailed rules" link.
- Classes B and C give an agent **<1k tokens** of orientation and no routing.
- The "identical" column understates sharing. `AGENTS.md` differs in hash across 9 of 10 repos but is **76–94% line-identical to `praxis/AGENTS.md`** (non-blank lines).
- Signal's T1 is dominated by `HANDOFF.md` (48,612 B, 536 lines). It is an accumulating platform-upgrade log, not a short handoff (see §7, problem D2).
- **The repo-specific part of T1 is small.** The 4 `context/` files total 1–7 KB per repo. Most of the startup budget is generic, Praxis-wide process text that is identical across repos.

## 4. Front-matter conventions

Command: `python3 inv.py`. Columns count `.md` files whose front matter contains each key.

| Repo | .md | with front matter | % | id | status | supersedes | superseded_by | related_documents | provenance |
|---|---|---|---|---|---|---|---|---|---|
| aegis | 91 | 48 | 52% | 36 | 33 | 17 | 17 | 26 | 4 |
| arca | 99 | 52 | 52% | 45 | 42 | 30 | 30 | 35 | 21 |
| clarity-framework | 131 | 17 | 12% | 11 | 15 | 0 | 0 | 0 | 0 |
| dokimos | 130 | 38 | 29% | 31 | 28 | 15 | 15 | 21 | 6 |
| EDF | 207 | 72 | 34% | 59 | 50 | 17 | 17 | 21 | 4 |
| EES | 7 | 0 | 0% | – | – | – | – | – | – |
| EOA | 188 | 0 | 0% | – | – | – | – | – | – |
| fides | 115 | 72 | 62% | 60 | 57 | 31 | 31 | 49 | 36 |
| forma | 274 | 76 | 27% | 64 | 44 | 26 | 22 | 27 | 16 |
| mercatus | 111 | 48 | 43% | 36 | 32 | 21 | 21 | 25 | 11 |
| percepta | 171 | 57 | 33% | 45 | 42 | 24 | 24 | 26 | 8 |
| praxis | 458 | 255 | 55% | 253 | 248 | 102 | 102 | 197 | 129 |
| signal | 167 | 73 | 43% | 61 | 58 | 44 | 44 | 51 | 34 |

What the table shows:

- **Coverage is partial everywhere.**
  - Even class-A repos give front matter to only 27–62% of Markdown files.
  - The vendored governance and SDE records (GV-\*, SDE-\*) make up a large
    share of the front-matter files in small repos: 19 per repo, and 24 of aegis's 36 IDs.
  - EOA (188 files) and EES (7 files) have none.
  - clarity-framework has 12%, and that is only `RFR-*` frontier records.
- **Status vocabulary is not controlled.** Values seen include:
  - `canonical`, `accepted`, `draft`, `proposed`, `candidate`, `active`;
  - `complete` *and* `completed`;
  - `supported`, `rejected`, `implemented`, `verified`, `review`;
  - `superseded` *and* `Superseded`;
  - `Open` and `Canonical`, capitalised, in clarity-framework and EDF frontier records.
- **supersedes and superseded_by are mostly empty lists, written on templates.**
  Only 8 records in praxis and 1 each in fides, percepta and signal have
  `status: superseded`.
- **`related_documents` has three incompatible path bases.** Example
  resolution in aegis: 23 entries resolve file-relative, 28 repo-root-relative,
  and 34 only relative to the `.sde/` package root. In every adopter, 4 remain
  dangling: `AGENTS.md → docs/cli.md`, `docs/installation.md`,
  `docs/upgrading.md`, which exist only in praxis, and
  `Governance-Decision-Log.md → ../../prompts/Codex-Prompt-ROS-Phase-1-Governance-Foundation.md`.
- **`provenance` lives in front matter.** It totals 164,231 B across the corpus
  (82,389 B in praxis). Agents that read AGENTS.md pay for it at every startup.
  Front matter overall is 485,919 B of the 15,205,331 B Markdown corpus (3.2%).

## 5. ID schemes

**The declared standard** is `framework/standards/IDENTIFIERS.md`, vendored
identically in 10 repos: `<PREFIX>-<AREA>-<YYYY>-<TOKEN>`, with PREFIX ∈
{RP, JR, EV, HY, TH, EX, DF, CN, GL, MS} and TOKEN as 4 digits or 4 hex
characters, for example `EV-ROS-2026-A7F2`. Praxis's own decisions follow it
(`DF-ROS-2026-A050`).

**Actual practice.** Front-matter `id` values were checked against that regex
(Appendix A, `ids-conformance`):

| Repo | front-matter ids | conform to standard | other prefixes in use |
|---|---|---|---|
| praxis | 250 | 149 (59%) | RQ, GV, SDE; 11 IDs duplicated *within* praxis |
| signal | 61 | 29 (47%) | GV, SDE, SIG, PROJECT |
| forma | 64 | 25 (39%) | ADR (6), RQ, GV, SDE |
| arca / percepta | 45 / 45 | 15 / 15 (33%) | GV, SDE, RQ |
| fides | 59 | 16 (27%) | FIDES, GV, SDE |
| mercatus | 36 | 7 (19%) | GV, SDE |
| EDF | 59 | 8 (13%) | RFR, NHEA, GV, SDE; HY-EDF-001-style legacy IDs |
| aegis / dokimos | 36 / 31 | 2 / 1 | GV, SDE |
| clarity-framework | 11 | 0 | RFR, RFA |

Other ID families:

- **Requirement IDs follow no common scheme.**
  - Fides uses `FID-<AREA>-NNN`: 2,295-line `FIDES-REQUIREMENTS.md`, with `FID-AUZ`, `FID-TEST`, `FID-DATA` and others.
  - Arca uses `ARCA-<AREA>-NNN` and `ARCA-D-NNN`.
  - Praxis uses `PRX-QUAL-NNN`, `PRX-PLAN-NNN`, `RQ-ROS-2026-ANNN` and `PRAXIS-GROUP-NN`.
  - Signal uses unprefixed `ADM-NNN`, `SCS-NNN`, `ARX-NNN`, `CORE-NNN` and `AUT-NNN`.
  - Mercatus uses `PRD-NN`, `FND-NN`, `UIR-NN` and `DSM-NN`.
  - Forma uses `FORMA-COLOR-NNN`, `FORMA-NS-NNN` and `MOT-NNN`, plus `ADR-NNNN` and `DF-FORMA-2026-NNNN`.
  - Dokimos uses `DOK-OPS-NNN`, `DOK-R###`, `APR-NNN` and `EVD-NNN`.
  - Intake items add dotted sub-IDs: `DOK-URL-001.1`, `SIG-FORMA1-002.1`.
- **Distinct ID "shapes" per repo**, counted by replacing digits with 9 across
  .md, .json, .fs, .yml, .js and .mjs files: praxis 330, forma 253, dokimos
  229, mercatus 192, signal 191, EDF 147, fides 137, percepta 123, aegis 115,
  EOA 93, arca 88, clarity 35, EES 23.
- **Requirements are declared in four Markdown syntaxes.** These counts are
  ID-led lines in `requirements/`, `docs/` and `research/`:
  - bold paragraph `**FID-UX-003** …`: fides 131, arca 70;
  - heading `### PRX-QUAL-023 — …`: forma 305, praxis 92, mercatus 74, EDF 65;
  - table row `| ADM-017 | …`: praxis 590, signal 418, fides 187, mercatus 150;
  - list item `- REQ-EVID-001: …`: praxis 510, EES 32.

  Only the heading form yields a stable Markdown anchor.
- **Global ID collisions.** Two cases:
  - 37 front-matter IDs are defined in more than one repo. 26 of them are
    identical copies, such as GV-INDEX-001 and the template IDs
    `EV-AREA-YYYY-0001`. **11 have divergent content.** `GV-START-001` names
    **9 different `AGENTS.md` texts** in 10 repos. `RFR-001…RFR-010` name
    different research-frontier records in clarity-framework and EDF.
  - Nothing qualifies an ID by repository.

## 6. How requirements, decisions, research, evidence and experiments are recorded

| Repo | Requirements location | DF decision records | Other decision stores | `research/evidence` files | experiments | `missions/` files | `.ros/work/queue.json` items |
|---|---|---|---|---|---|---|---|
| aegis | `requirements/` | 2 | `docs/decisions/README.md` | 1 (.gitkeep) | 1 | 3 × .gitkeep | 192 |
| arca | `docs/requirements/` | 15 | – | 1 | 1 | 3 × .gitkeep | 39 |
| dokimos | `requirements/` | 1 | context/DECISIONS has 10 rows | 1 | 122 (top-level `experiments/`) | 3 × .gitkeep | 54 |
| EDF | none | 0 | `research/registries/*.json` *and* `registries/*.json` | 1 | 43 | 3 × .gitkeep | 15 |
| fides | `docs/requirements/` | 16 | context/DECISIONS has 17 rows | 1 | 1 | 3 × .gitkeep | 45 |
| forma | `requirements/` **and** `research/requirements/` | 2 | **`docs/decisions/ADR-*` (8 files)** | 16 | 1,531 | 3 × .gitkeep | 79 |
| mercatus | `requirements/` | 6 | – | 2 | 1 | 3 × .gitkeep | 52 |
| percepta | `requirements/` | 4 | – | 3 | 29 | 3 × .gitkeep | 23 |
| praxis | `requirements/` **and** `research/requirements/` (37 RQ) | 54–57 | – | 55 | 203 | 1 | (553 files in `.ros`) |
| signal | `docs/requirements/` | 29 | – | 1 | 1 | 3 × .gitkeep | 157 |
| clarity-framework | none | 0 | `decision-record.md` etc. at root (method, not records) | – | – | – | – |
| EES | `requirements/` (2) | 0 | – | `evidence/` | – | – | – |
| EOA | none | 0 | [private path withheld] | [private path withheld] | – | – | – |

Observations:

- **`missions/` is unused.** `BOOTSTRAP.md` step 3 says "Read the assigned
  mission", but every class-A adopter has only three `.gitkeep` files there.
  Real work lives in `.ros/work/queue.json`, with 15–192 items (WI-NNNN). That
  file is a JSON event-store projection read through `./praxis work context`.
  Agents that follow BOOTSTRAP literally find nothing.
- **Research and evidence directories are mostly empty in product repos.**
  `research/evidence/` holds only `.gitkeep` in 7 of 10 class-A repos. Evidence
  ends up in ad-hoc places instead, for example:
  - `signal/docs/modernization/evidence/`
  - `mercatus/research/evidence/EV-MERCATUS-UI-2026-0001-…md`
  - `forma/experiments/RLP-001/`
  - `echelon-organization-administration/[private path withheld]`
- **Decisions are stored in more than one place.**
  - Forma keeps `ADR-0001…ADR-0030` in `docs/decisions/` and also
    `DF-FORMA-2026-000N` in `research/decisions/`. Every other adopter's
    `docs/decisions/README.md` says it "must not become a second canonical
    decision store".
  - The `context/DECISIONS.md` navigation views are out of date. Rows versus
    DF records: signal 3 vs 29, praxis 3 vs 54, arca 1 vs 15.
- **Registries are generated JSON** (`registries/decisions.json` etc., 404,749
  B total) and are a rebuild target, not a retrieval index named in the
  startup chain. EDF has a second, parallel set in `research/registries/`.
- **External intake is staged before triage.** Owner directives arrive in
  `.praxis/inbox/documents/*.md` and move to
  `.praxis/processed/INPUT-<hash>/source/`. Some cross-repo requirement IDs
  exist *only* there. FORMA-NS-001 is the example: mercatus and signal cite it,
  and Forma defines it only in `.praxis/inbox/`.

## 7. Duplicated and vendored content

Method: SHA-256 of every non-empty, non-symlink file outside `.git`,
`node_modules`, `bin` and `obj`, in all 13 repos. Command: `python3 dup.py`.

- 10,403 files hashed; 8,753 unique contents.
- **150 contents occur in 2 or more repos, as 1,234 copies, with 6,708,757 redundant bytes.**
- Markdown alone: **74 shared contents, 626 copies, 3,834,691 redundant bytes (~959k tokens)**. That is **25% of all 15.2 MB of Markdown** in the 13 repos.

| Shared-content group (top dir) | Groups | Redundant bytes | Source package (`.echelon/*.json`) |
|---|---|---|---|
| `.visual-engineering/` | 8 | 1,553,034 | visual-engineering 1.0.1 (8 repos) |
| `docs/*` (work-protocol, telemetry, provenance, remote protocol…) | 14 | 1,153,014 | Praxis (`@echelon-foundry/repository-operating-system`) |
| `vendor/` (nupkg / tgz) | 5 | 1,073,877 | Conditor NuGet feeds |
| `.sde/` | 22 | 830,403 | `@echelon-foundry/sde` 1.5.0 (10 repos) |
| `docs/00-governance/` | 6 | 618,660 | Praxis |
| `schemas/` | 28 | 578,529 | Praxis |
| `.communication-engineering/` | 5 | 255,528 | communication-engineering 1.0.0 (8 repos) |
| `framework/` | 11 | 93,213 | Praxis |
| `templates/` | 7 | 64,332 | Praxis |

- **Every class-A repo carries the same 10 Praxis-governed copies** of the
  constitution, Agent Operating Manual, REP spec, framework standards,
  templates and BOOTSTRAP. These are byte-identical even though repos are
  pinned to Praxis 3.7.2 (7 repos) or 3.25.0 (mercatus, signal).
- **Version cohorts drift for files at the same path:**
  - `docs/work-protocol.md` has 3 variants: {aegis, arca, dokimos, EDF, fides, forma, percepta}, {mercatus, signal}, {praxis};
  - `docs/agent-provenance.md` has 2 variants;
  - `GEMINI.md` and `copilot-instructions.md` have 2 variants each, split on "Praxis" vs "ROS".
- **Copied text states false local facts.** The vendored
  `docs/00-governance/README.md:67`, identical in 10 repos, says: "The
  repository contained only the Phase 1 execution prompt… there were no domain
  REPs, registries, architecture standards, Clarity materials…". That is
  Praxis's own history, now asserted about fides, forma, signal and the others.
- **Every `AGENTS.md` "## Mission" section describes Praxis, not the repo.**
  The text "Praxis, Echelon Foundry's repository operating system, makes
  research…" is identical in aegis, fides, signal, forma and percepta. An agent
  learns nothing about the product it is in until it reaches `context/` or the
  README.

## 8. Problem classification

Each problem is assigned to its primary cause. The paths are concrete examples.

### A. Caused by Markdown syntax

| # | Problem | Evidence |
|---|---|---|
| A1 | Requirements declared as bold paragraphs or table rows have no stable anchor, so they cannot be deep-linked or chunked as a unit. | `fides/docs/requirements/FIDES-REQUIREMENTS.md:1701` `**FID-UX-003** …` (131 bold-led IDs in fides). `signal/docs/requirements/implementation-gap-analysis.md` is 153,327 B in 344 lines, longest line **3,363 chars**, all table rows. |
| A2 | YAML front matter is optional and free-form: status casing and vocabulary vary, and `supersedes` is sometimes a list and sometimes empty. | `Superseded` vs `superseded`, `complete` vs `completed` (§4). clarity `research/frontier/records/RFR-001.md` uses `status: Open` and `frontier_score:`, keys used nowhere else. |
| A3 | Managed regions inside `AGENTS.md` use two different HTML-comment marker syntaxes. | `aegis/AGENTS.md:306` `<!-- BEGIN echelon:visual-engineering -->` vs `:327` `<!-- echelon:communication-engineering:start -->` |
| A4 | Relative links in `related_documents` have no declared base, so a resolver must try three bases. | `.sde/method/*.md → architecture/STRUCTURAL-LOCALITY.md` (resolves only from `.sde/`); `docs/00-governance/README.md → ../../AGENTS.md` (file-relative); `AGENTS.md → docs/work-protocol.md` (repo-root) |
| A5 | Provenance metadata is embedded in prose files and inflates every read. | `aegis/AGENTS.md:1-46`: 1,218 B front matter, mostly `provenance.contributions`. 164 KB of provenance in front matter corpus-wide. |

### B. Caused by repository organisation

| # | Problem | Evidence |
|---|---|---|
| B1 | Governance is copied into each repo instead of referenced, so 10 copies must be kept in sync, and IDs collide. | `docs/00-governance/*` ×10 identical. GV-START-001 has 9 variants. 3.83 MB of duplicated Markdown (§7). |
| B2 | Declared canonical roots are empty while real content lives elsewhere. | `aegis/missions/{active,backlog,completed}/.gitkeep` vs 192 items in `aegis/.ros/work/queue.json`. `signal/research/evidence/` is empty, while evidence sits in `signal/docs/modernization/evidence/`. |
| B3 | There are parallel stores for the same record type. | `forma/docs/decisions/ADR-0001…` and `forma/research/decisions/DF-FORMA-2026-0001…`. `echelon-diagnostic-framework/registries/` and `research/registries/`. `forma/requirements/` and `forma/research/requirements/`. |
| B4 | Repos outside Praxis have no structure for agents. | `clarity-framework/[private path withheld]` root holds 20+ loose `.md` files with misspelt names (file names withheld: private repository) and further loose structure (details withheld: private repository) |
| B5 | Installed method packs are not connected to the routing. | `.sde/` (10 repos, ~92 KB each) is never mentioned in `AGENTS.md`. `arca/SDE-MAP.md` says "Accepted requirement artifacts: none declared" although `arca/docs/requirements/ARCA-STORAGE-REQUIREMENTS.md` exists. |

### C. Caused by retrieval strategy

| # | Problem | Evidence |
|---|---|---|
| C1 | Startup is "read a fixed list" rather than "query for what the task needs". Generic process text dominates. | T1 is 9.6k–22.3k tokens (§3). The repo-specific `context/` part is 1–7 KB. `.visual-engineering/RESEARCH-INDEX.md` is 86 KB × 8 copies. |
| C2 | No cross-repo index exists. Cross-repo knowledge is reachable only through package pins and URLs, and prose relationships are invisible. | Clarity↔EDF↔Mercatus links are prose only, with 0 path or ID citations to clarity-framework (inventory §3). The only cross-repo catalog is a release list (`.conditor/authority/resolved-release-set.json`). |
| C3 | Work state is behind a CLI and a JSON event store, not in addressable documents. | `./praxis work context ID`, `.ros/events/events.jsonl` (praxis `.ros` = 28 MB). BOOTSTRAP still points to `missions/`. |
| C4 | Cross-repo IDs can point at non-canonical records. | `mercatus/research/decisions/DF-MERCATUS-FND-2026-0003…md:55` and `signal/tests/fixtures/forma-zero-names.json:2` cite FORMA-NS-001. Its only definition is `forma/.praxis/inbox/documents/2026-11-09-FORMA-NS-001-namespace-rename.md`. |
| C5 | Some generated registries exist but are not used as an index. | `registries/decisions.json` etc. are not named in AGENTS.md's Start Here. Agents are pointed at directories to grep. |

### D. Caused by agent behaviour

| # | Problem | Evidence |
|---|---|---|
| D1 | Continuation state is not maintained. The `context/` views remain at their initial templates. | `signal/context/CURRENT-STATE.md`: "Newly initialized with Repository Operating System 1.2.1-main.16.1… No vertical slice has been selected", while signal has 29 DF records, 157 work items and a production sign-in (PR #144). `context/DECISIONS.md` rows vs DF records: signal 3/29, praxis 3/54, arca 1/15. |
| D2 | Handoffs accumulate instead of being rewritten. | `signal/HANDOFF.md`: 48,612 B, 536 lines of dated platform history (Praxis 3.11.0, echelon-current 1.22.0, …). `forma/HANDOFF.md`: 21,294 B. |
| D3 | The same fact is restated with conflicting values across files that agents updated independently. | Praxis version in signal: `ros.json` `rosVersion: 1.2.1-main.16.1`; `.echelon/ros.json:5` `3.25.0`; `HANDOFF.md` "Praxis 3.11.0"; `conditor.json` praxis 3.25.0. In aegis: `ros.json` 3.0.0 vs `.echelon/ros.json` 3.7.2, and `HANDOFF.md` tells agents to run `./ros validate` although AGENTS.md names `./praxis`. |
| D4 | A rename was applied only partly. | `forma/CLAUDE.md` says "canonical Praxis agent contract", and `forma/GEMINI.md` and `forma/.github/copilot-instructions.md` say "canonical ROS agent contract" (also in percepta and signal). |
| D5 | Provenance is incomplete. | AGENTS.md front matter in every adopter records `actor: openai/codex, model: unknown`. |
| D6 | Types are mirrored by hand across repos instead of referenced. | `dokimos/src/Dokimos.Core/Verification/RequirementJudging.fs:18` "Mirrors Forma.Workflow.RequirementIdentity (kemiller2002/forma)" |

## 9. Key numbers

- 13 repos. Total Markdown 15,205,331 B (~3.8M tokens); 25% of it is cross-repo duplicate copies.
- 10 repos are Praxis-governed. 3 repos (clarity-framework, EES, EOA) have no agent contract.
- Mandatory startup read is ~9.6k–22.3k tokens, ~17.7k–37.2k with the UI and communication blocks, and ~47.8k–78.6k with full governance.
- Front matter covers 0–62% of Markdown per repo. Front-matter ID conformance to the declared standard is 0–59%.
- 37 front-matter IDs are defined in more than one repo; 11 of those differ in content.
- Verified cross-repo edges: 100. See [`cross-repository-dependency-inventory.md`](cross-repository-dependency-inventory.md).

## Appendix A. Commands

All commands were run against `/home/user/kemiller2002`. The scripts were kept
in the session scratchpad and are reproduced here in condensed form.

```bash
# Entry files, directories, front-matter key counts (inv.py)
python3 - <<'EOF'
import os,re,collections; R='/home/user/kemiller2002'
for r in sorted(os.listdir(R)):
  md=[os.path.join(d,f) for d,dn,fn in os.walk(f'{R}/{r}') if '.git' not in d.split(os.sep) and 'node_modules' not in d for f in fn if f.endswith('.md')]
  k=collections.Counter(); fm=0
  for p in md:
    m=re.match(r'^---\n(.*?)\n---\n',open(p,errors='ignore').read(),re.S)
    if m: fm+=1; k.update(re.findall(r'^([A-Za-z_]+):',m.group(1),re.M))
  print(r,len(md),fm,{x:k[x] for x in ['id','status','supersedes','superseded_by','related_documents','provenance']})
EOF

# Duplication (dup.py): sha256 every non-empty file excluding .git/node_modules/bin/obj,
# group by hash, keep groups spanning >=2 repos, sum (copies-1)*size.
# Output used: "contents shared across >=2 repos: 150 total copies: 1234 redundant bytes: 6708757"
#              ".md contents: 74 copies 626 redundant md bytes 3834691"

# Variant cohorts for one path
for r in aegis arca dokimos echelon-diagnostic-framework fides forma mercatus percepta praxis signal; do
  echo "$(sha256sum $r/docs/work-protocol.md | cut -c1-8) $r"; done | sort

# Startup budgets (budget2.py): sum file sizes for the tier lists in §3; tokens = bytes//4.

# AGENTS.md overlap with praxis/AGENTS.md: share of non-blank lines present verbatim in praxis/AGENTS.md.

# Front-matter ID conformance
#   std = ^(RP|JR|EV|HY|TH|EX|DF|CN|GL|MS)-[A-Z0-9-]+-\d{4}-([0-9]{4}|[0-9A-F]{4})$
# Cross-repo ID collisions: group front-matter `id` across repos; divergent = >1 distinct sha1.

# Requirement declaration styles: count lines in requirements/|docs/|research/ .md matching
#   bold ^\*\*ID\*\*, heading ^#{2,5} ID, table ^\| ID \|, list ^\s*[-*] ID[:—-]

# .sde routing check
for r in */; do grep -rl "\.sde/" $r --exclude-dir=.git --exclude-dir=.sde --exclude-dir=.ros -I | wc -l; done

# Context vs DF decision records
grep -cE '^\| [0-9]{4}-' $r/context/DECISIONS.md; ls $r/research/decisions | grep -c '^DF-'

# Cross-repo GitHub links in Markdown (excluding self-links)
grep -rnoE "github\.com/kemiller2002/(<repos>)/(blob|tree|issues|pull|releases)" --include=*.md --exclude-dir=.ros .
```
