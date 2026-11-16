# Echelon Knowledge Store

Research into how Echelon repositories should store and deliver knowledge to
autonomous AI agents — and whether anything should replace Markdown at all.

**Status:** research phase (mission `EKS-RESEARCH-2026-001`). No stable schema,
no released packages.

## Question

Does a typed, F#-validated knowledge representation let agents complete more
correct work per unit of context, time and cost than Markdown — once retrieval,
organisation and agent behaviour are controlled for?

## Findings (research phase)

Start with [`research/2026-knowledge-representation/strategy/executive-report.md`](research/2026-knowledge-representation/strategy/executive-report.md).
In short: keep Markdown with front matter canonical (as Praxis already does), and
compile it, deriving links, closures and authority, validating agent edits and
delivering task-specific packages. Do not replace it with a new format.

## Try the compiler

```bash
dotnet run --project tools/Echelon.KnowledgeStore.Cli -- validate --root <dir with repository clones> --repos praxis,fides
dotnet run --project tools/Echelon.KnowledgeStore.Cli -- context DF-ROS-2026-A059 --root <dir> --repos praxis
```

## Build

Requires the .NET 10 SDK (`global.json`).

```bash
dotnet tool restore
dotnet test Echelon.KnowledgeStore.sln -c Release
```

## Contributing

See [`AGENTS.md`](AGENTS.md) (agents and humans) and
[`docs/standards/governance.md`](docs/standards/governance.md).

## License

[PolyForm Noncommercial 1.0.0](LICENSE), consistent with the other Echelon
Foundry repositories (see `docs/decisions/0001-license.md`).
