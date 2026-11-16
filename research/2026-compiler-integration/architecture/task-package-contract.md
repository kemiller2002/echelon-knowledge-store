# Task package contract (work item → compiled knowledge package)

Implementation: `src/Echelon.KnowledgeStore.Compiler/WorkItems.fs`; CLI `eks work-item REPO WI`
(exit 3 when incomplete, unless `--allow-incomplete`) and `eks work-item-manifest REPO WI`.
Tests: `tests/Echelon.KnowledgeStore.Tests/WorkItemTests.fs`.

## Input (verified against Praxis at `12bf995`, see `praxis-integration.md`)

A Praxis backlog row from `REPO/.ros/work/queue.json` (`schemaVersion` "1.0.0"), parsed as data
with `System.Text.Json`. EKS reads `id`, `title`, `description`, `tags`, `status`, `source`,
`sourceReference`. Unknown fields are ignored; nothing in the file is executed. Praxis has no
typed requirement field, so record identifiers are taken from `sourceReference` (basis
`structured`) and from title and description (basis `prose`). The basis is kept on every link and
shown in the package; prose links are never upgraded to structured ones.

Not yet read (documented in `praxis-integration.md` §6): the live item in
`.ros/context/current.json` (`evidence[].path`, `telemetryExecutionIds`, `semanticState`).

## The twelve required fields

| # | Required content | Where it appears | Source of truth |
|---|---|---|---|
| 1 | Work-item identifier | header target `REPO:WI-…`, section "Work item" | queue row |
| 2 | Applicable requirements | "Target" and "Requirements" sections | cited records + incoming requirement edges (amendments) |
| 3 | Acceptance criteria | quoted record text; "Validation obligations" | `## Acceptance` lists |
| 4 | Relevant constraints | "Governance" section; dependencies | governance records linked or sharing tags |
| 5 | Architectural decisions | "Decisions" section | 1-hop decision neighbours |
| 6 | Dependencies | "Dependencies" section | transitive `depends_on` closure, across repositories |
| 7 | Supporting evidence | "Supporting / Contradicting evidence" | typed evidence relations |
| 8 | Unresolved questions | "Unresolved questions and missing information" | failed checks, missing bindings, review-level diagnostics, prose-only links |
| 9 | Verification obligations | "Verification obligations"; `eks dokimos-obligations` | acceptance criteria + declared `verified_by` |
| 10 | Source provenance | `provenance … sha256:` line per record; manifest `inputs` | file hashes |
| 11 | Source revision identifiers | "Source revisions" section; manifest `sourceRevisions` | `.git` HEAD read as data; `unknown` when absent (content hashes still identify inputs) |
| 12 | Compilation status | header `status:`; manifest `completeness` + `conditions` | derived, see below |

## Completeness is derived, never assigned

A package is COMPLETE only if every condition holds. The manifest lists each condition with its
value, so a reader can check the derivation.

Package-level conditions (all packages): every included record resolves; no unresolved typed
references; no error diagnostics on included records; every mandatory validation rule executed;
optimisation accounted for every item.

Work-item conditions (added here): the work item cites at least one record; every cited record
exists; every cited record and every dependency in its closure is authoritative (`accepted`,
`canonical`, `active`); every requirement in scope has acceptance criteria.

Tested properties: completeness ⇔ all conditions (over every work item in a fixture); a draft
dependency and a missing record each make the package INCOMPLETE; a work item with no links is
never COMPLETE; a disabled mandatory rule makes every package INCOMPLETE.

What COMPLETE does **not** mean: that the requirements are right, that the evidence supports
them, or that anything is verified. The header states the authority order and that quoted record
text is data.

## Example: WI-0007 (currency conversion, depends on an undecided draft)

`status: INCOMPLETE — non-authoritative record(s) in scope: fx-ledger:RQ-FXL-0008 is proposed;
fx-ledger:DF-FXL-0009 is draft; requirement(s) without acceptance criteria: fx-ledger:RQ-FXL-0008`

In all 24 coding-trial runs that received WI-0007 (every condition, both models), the agent
reported it blocked and did not invent exchange rates. The package's INCOMPLETE status was not
needed for that outcome on this fixture; it is what makes the outcome checkable by a machine.

## Known limitations

- Links depend on identifiers appearing in the work item. A work item that names no record gets an
  INCOMPLETE package that says so; EKS does not guess relevant records.
- Incoming edges are included for requirements (amendments). This is what made the difference in
  the hard coding trials (`../experiments/compiler-results.md`); it also means a package can grow
  with unrelated amendments if records over-link.
- Status vocabulary for "authoritative" is fixed (`accepted`, `canonical`, `active`); repositories
  with other vocabularies need configuration.
