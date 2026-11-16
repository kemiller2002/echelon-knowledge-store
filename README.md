# Echelon Knowledge Store

Research into how Echelon repositories should store and deliver knowledge to
autonomous AI agents — and whether anything should replace Markdown at all.

**Status:** research phase (mission `EKS-RESEARCH-2026-001`). No stable schema,
no released packages.

## Question

Does a typed, F#-validated knowledge representation let agents complete more
correct work per unit of context, time and cost than Markdown — once retrieval,
organisation and agent behaviour are controlled for?

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
