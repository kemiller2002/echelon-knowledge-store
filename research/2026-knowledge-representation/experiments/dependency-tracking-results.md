# Cross-repository dependency tracking results

Gold: 100 cross-repository edges verified by the audit workstream (file, line and
snippet re-checked), collapsed to 47 repository pairs; 29 pairs for "hard"
dependencies (depends-on, vendors, validates, implements). Gold is independent of
the typed graph. It is **not** independent of keyword search: the auditor
partly used keyword search to find edges, so M2's recall is optimistic.
Raw: `results/dependencies.md`.

| Method | All edges P / R | Hard deps P / R |
|---|---|---|
| M1 typed front-matter graph | – / **0.00** | – / **0.00** |
| M2 prose keyword search | 0.54 / 0.98 | 0.34 / 1.00 |
| M3 manifests (packages, workflow uses, Praxis install) | 0.77 / 0.43 | 0.77 / 0.69 |
| M5 keyword ∪ manifests | 0.54 / 0.98 | 0.34 / 1.00 |

Mandatory scenarios (target repository; all-edge recall for M2 / M3):

| Scenario | Gold pairs | Keyword | Manifests | Notes from audit |
|---|---|---|---|---|
| Fides requirement change | 4 | 0.75 | 0.25 | Signal is the only hard consumer (NuGet 0.2.0, cites FID-DATA-009) |
| Forma design-standard change | 10 | 1.00 | 0.50 | five npm consumers on four versions |
| Clarity methodology change | 3 | 1.00 | 0.00 | conceptual only; no path, id or URL references exist |
| EDF diagnostic model change | 6 | 1.00 | 0.00 | no hard dependency exists; Dokimos explicitly declines one |
| Dokimos validation rule change | 9 | 1.00 | 0.33 | only aegis and fides run a Dokimos gate, pinned to different SHAs |

## Conclusions

* Today no structured knowledge supports cross-repository impact analysis. The
  typed graph has zero cross-repository edges; keyword search over-reports
  (precision 0.34 for hard dependencies, largely from vendored framework prose
  that names Praxis and Dokimos in every repository).
* Machine-readable manifests are the most precise existing signal, but they miss
  dependencies that are not packages (methodology, validation contracts).
* Value of EKS here is **prospective**: explicit, versioned, typed
  cross-repository references (`fides:FID-DATA-009@0.2.0`) would make impact
  analysis exact. Whether repositories would maintain them is untested (see
  `findings/unresolved-questions.md`).
* Dependency tracking offers more value than serialisation efficiency, which
  offers almost none (`comparative-results.md`).
