# Unresolved risks carried into Phase 2

| Risk | Status at start of Phase 2 | Status at end of Phase 2 |
|---|---|---|
| R1 Private paths in public history | Redacted in the tree; history retained (no force-push). | Mitigated going forward by a denylist check in CI. Owner accepted the historical exposure on 2026-11-17; history is not rewritten (`../security/privacy-review.md`). |
| R2 Validation rules that exist but do not run | One instance found (EKS0026). | Rule runs are recorded in every compilation and manifest; a skipped mandatory rule blocks COMPLETE (tested). A second, subtler instance was found: the source-existence rule ran but silently skipped every `../`-relative path. Fixed and regression-tested. |
| R3 Packages marked COMPLETE while incomplete | Completeness was assigned in code paths. | Completeness is now derived from listed conditions; tests assert status ⇔ all conditions. Work-item packages add checks for missing links, non-accepted dependencies and missing acceptance criteria. |
| R4 Stale caches | Parse cache keyed by path + content hash only. | Found that parsing also depends on compiler version and schema configuration; the key now includes both (regression test). Package memoisation measured: naive memo is stale on every step; a dependency-keyed memo is correct but slower than recompiling. |
| R5 Fabricated or misrepresented evidence | Compiler validation alone did not prevent it. | Re-measured across five conditions and two models (`../evidence-integrity/experimental-results.md`). |
| R6 Agents leaving their sandbox / using session tools | Not measured. | Audited every trial transcript: no sandbox escapes succeeded; several Haiku agents made stray session-tool calls, all denied or no-ops (`../security/adversarial-results.md`). |
| R7 Fixture contamination | n/a | Hidden tests are committed to a public repository, so future models may train on them (`../benchmarks/held-out-policy.md`). |
| R8 Dokimos runs on .NET 8 | n/a | Run under roll-forward on .NET 10; not validated on .NET 8. |
