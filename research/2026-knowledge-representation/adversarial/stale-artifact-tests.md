# Stale-artefact and incomplete-output tests

| Case | Behaviour | Evidence |
|---|---|---|
| Source edited after compile | `Artifacts.freshness` returns `Stale [path]`; CLI with `--artifacts` exits 2 and names the changed sources | unit test `stale artefacts are detected` |
| Source added or removed | listed as changed (set difference of input keys) | same function |
| Package references an unresolved record | status INCOMPLETE with reason; `eks context` exits 3 unless `--allow-incomplete` | unit test |
| Unknown target id | INCOMPLETE, "record … not found", exit 1 at the CLI | unit test |
| Optimisation drops an item without a ledger entry | package becomes INCOMPLETE ("optimisation lost n item(s)") | invariant checked in `Packages.task`; 0 occurrences over 587 packages |
| Warm (incremental) build diverges from cold | benchmark compares output hashes; unit test | equal in all scenarios |
| Concurrent updates | not applicable to a stateless compiler: each run reads one snapshot; concurrent git edits are handled by git (one file per record) | versioning workstream merge test |
| Rule defined but not executed | caught only after a trial failure; now a unit test asserts every catalogued code is wired | `CompilerTests.fs` |

Residual risk: a compiler bug that produces a *plausible* COMPLETE output (as the
unwired EKS0026 did) is only caught by tests or by independent recomputation.
Recommended mitigation: keep raw sources reachable for agents (D + A access), and
run a periodic independent replication script against compiler outputs.
