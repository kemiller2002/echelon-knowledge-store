# Compiler-first architecture

Prototype: `src/Echelon.KnowledgeStore.Compiler` (F#, ~1,100 lines) and the `eks`
CLI (`tools/Echelon.KnowledgeStore.Cli`). Tests: `tests/.../CompilerTests.fs`.

## Principle

Move deterministic knowledge work out of the language model. Parsing, reference
resolution, inverse links, transitive closure, supersession, staleness and
structural validation are computed once by software. The agent receives the
result plus explicit statements of what is unknown.

## Two layers

| Layer | Role | Status |
|---|---|---|
| A — F# compiler | types the *knowledge model* (records, unions, exhaustive matches) and the compiler's own code; optionally type-checks knowledge expressed as F# | measured (`fsharp-compiler-integration.md`); not recommended as the path for knowledge data |
| B — EKS knowledge compiler | parses inert Markdown/YAML, validates, resolves, precomputes, emits artefacts and task outputs | prototype built and benchmarked |

## Pipeline (layer B)

```
sources (Markdown + front matter, per repository)        content hash per file
  │ parse (data-only, cached by hash)       ──► EKS0001, 0004, 0011, 0015, 0022–0025
  ▼
typed IR (KnowledgeRecord)                 ──► per-record rules
  │ resolve references (repo-local → unique cross-repo → path)
  ▼
graph (typed edges, derived inverse links) ──► corpus rules: 0002, 0006, 0007, 0008, 0010, 0012–0014, 0017–0021, 0026
  │ partial evaluation
  ▼
precomputed: impact closure, evidence sets, current authority, dependency closure
  │ emit (deterministic)                       │ on demand
  ▼                                            ▼
index.md · records.jsonl · diagnostics.json · task package · evidence bundle · impact report
precomputed.json · manifest.json (hashes)       · requirement contract · explain · diff
```

## Mechanically provable vs needs review

| Provable (Error/Warning) | Suspected (RequiresReview) |
|---|---|
| unresolved or mis-cased references, unknown/misspelled fields, invalid status for a schema-backed kind, duplicate ids, cycles, superseded-without-replacement, unsupported schema version, private→public flow, claim "supported" with no evidence | contested claims, status lagging evidence, one-sided supersession, conflicting copies of an id, text that addresses agents, stale evidence |

The compiler never states that a record is true. Manifests say
"Compilation success is structural validity, not factual correctness".

## Relationship to Praxis (verified)

Praxis already decides "Markdown canonical, registries generated"
(DF-ROS-2026-A001, accepted 2026-07-24), generates per-kind JSON registries
(front matter + path), runs `praxis validate` / `praxis registry check` in CI in
the Praxis-governed repositories, and has a UI-requirements compiler
(`praxis-verification-manifest`, levels V1–V5). The EKS compiler is therefore an
*extension* of an existing pipeline, not a new one: it adds derived links,
closure, diagnostics with codes and locations, task-specific outputs and staleness
checks to what `praxis registry build` already emits (`../adversarial/architecture-objections.md` O1).
