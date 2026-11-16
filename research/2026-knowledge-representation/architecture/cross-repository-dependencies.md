# Cross-repository dependencies

Inventory: `baseline/cross-repository-dependency-inventory.md` (100 verified
edges, 47 repository pairs). Experiment: `experiments/dependency-tracking-results.md`.

## Findings

* No typed cross-repository edge exists in any record. Dependencies live in
  package manifests (NuGet `EchelonFoundry.*`, npm `@echelon-foundry/*`), the
  Praxis install manifest (`ros.json`), workflow pins (Dokimos gate SHAs) and prose.
* Forma ↔ Praxis is a dependency cycle (Forma installs Praxis; Praxis vendors a
  Forma release).
* Several "dependencies" are deliberate non-dependencies recorded in prose
  (Arca enforces by test that it does not depend on Fides; Dokimos declines to
  depend on EDF). These are knowledge too: a `does_not_depend_on` assertion with
  a test reference would make them checkable.

## Proposed design

1. **Harvest** manifest dependencies automatically (implemented as M3; precision
   0.77) into the compiled index as `depends_on` edges with `source: manifest`.
2. **Declare** knowledge-level dependencies in front matter:
   `depends_on: [fides:FID-DATA-009@0.2.0]`, `validates: [dokimos:RULE-...]`.
3. **Validate** in CI: unresolved cross-repository targets warn (target may be
   private or unavailable); pinned versions older than the target's latest release
   are reported for review.
4. **Impact query** across indexes the reader can access (`Graph.impactOf`).

Effort to adopt: one front-matter field per real dependency; the audit found 29
hard repository-pair dependencies, so the initial backfill is small.
