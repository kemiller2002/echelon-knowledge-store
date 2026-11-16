# Compilation pipeline

1. **Load** Markdown sources of the selected repositories (vendor, archive,
   tests, fixtures, templates, starter excluded). Hash each file.
2. **Parse** (pure, cached): front matter → `Yaml` (aliases, anchors, tags and
   duplicate keys rejected; event limit) → `KnowledgeRecord`. Field checks.
   Files without an id but with an `ID--slug.md` name become identity-only records.
3. **Resolve** relation targets: same repository → unique id elsewhere → path suffix;
   unresolved targets kept with their relation for diagnostics.
4. **Build graph**; derive inverse edges from `supporting_evidence`/`contradicting_evidence`.
5. **Validate** (corpus rules) and map to EKS codes; sort diagnostics deterministically.
6. **Partially evaluate** impact, evidence, current authority, dependency closure.
7. **Emit** artefacts + manifest (input and output hashes, counts, status), or
   answer a query (task package, evidence bundle, impact, contract, explain, diff).

Determinism: inputs sorted; diagnostics and links sorted; no clock reads except the
configured `AsOf`; verified by unit test (input order permuted → identical outputs)
and by the benchmark (warm = cold outputs).

Failure behaviour: compile always completes and reports; `eks validate` exits 4 on
errors; query commands exit 2 on stale artefacts and 3 on incomplete outputs
unless `--allow-incomplete` is given.
