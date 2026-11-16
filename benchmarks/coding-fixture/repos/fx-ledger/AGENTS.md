# Agent contract (fixture)

Knowledge lives in `research/` as Markdown records with YAML front matter:
requirements `research/requirements/RQ-*`, decisions `research/decisions/DF-*`,
evidence `research/evidence/EV-*`. Accepted records are authoritative; a record
with `superseded_by` is history only. This repository depends on `../fx-identity`
for sessions and roles.

Build and test with `dotnet test tests/FxLedger.Tests`.
