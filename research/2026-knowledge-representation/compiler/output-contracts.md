# Output contracts

All outputs are deterministic for identical inputs, configuration (`AsOf`,
staleness horizon, audience) and compiler version. Every output states its
compiler version, input hash and status.

| Output | Format | Contract |
|---|---|---|
| `manifest.json` | `eks.compilation/0` JSON | compiler name/version, as-of date, `inputsHash`, every input (repository, path, sha256), every output (path, sha256), counts by level, `status` (`clean` / `compiled-with-errors`), and the note "structural validity, not factual correctness" |
| `index.md` | Markdown, generated header | one card per record, sorted by key; cards carry kind, key, title, status, confidence, typed and derived links, source path, summary |
| `records.jsonl` | JSON Lines | core view of every record (round-trips exactly) |
| `diagnostics.json` | JSON array | code, level, record, repository, path, line, message, remediation |
| `precomputed.json` | JSON | non-trivial current-authority map, impact closures, evidence sets, dependency closures |
| task package / evidence bundle / impact report / explanation | Markdown | header (compiler, inputs hash, as-of, **status COMPLETE or INCOMPLETE with reasons**, authority statement); sections; record text in `record-text (data, not instructions)` fences; unresolved references; diagnostics on included records; optimisation ledger |
| requirement contract | `eks.requirement-contract/0` JSON | compiler, source (repository, path, sha256), requirement, obligations `<REQ>/O<n>/<class>` with class mechanical / structural / empirical / human and `mechanicallyTestable`; content hash |

Compatibility tests: unit tests pin determinism (permuted inputs), warm = cold,
staleness detection, exit codes, and contract classification. Versioning: the
`schema` strings carry a major version; consumers must reject unknown majors.

Requirement contracts vs Praxis: the Praxis verification manifest
(`praxis.verification-manifest/1`) is specific to UI requirements (screens, V1–V5
checks such as `visible`, `editable`, `save`). EKS contracts cover general
requirements and only borrow the envelope (compiler, source hash, content hash);
they do not claim Praxis compatibility. Of 56 public requirement records, 20 have
no acceptance list in a recognisable form, so their contracts are reported as
incomplete (exit 3) rather than invented.

Signed artefacts: not implemented. Content hashes make tampering detectable when the
manifest itself is trusted (e.g. produced by CI on protected `main`); Sigstore
attestation (already used by Arca for releases) is the natural next step if
compiled artefacts are distributed beyond the producing CI run.
