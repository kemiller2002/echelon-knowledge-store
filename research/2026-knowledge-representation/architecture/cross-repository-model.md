# Cross-repository model

## Decision (EXPERIMENTAL)

**Source repositories own their knowledge.** EKS provides schemas, validators,
the compiler and retrieval; it does not hold a central copy of organisational
knowledge.

Reasons (measured or verified):
* Ownership already follows repositories (ids namespaced per repository, e.g. DF-FIDES-*).
* Centralising copies is exactly what produced 25% duplicated tokens and 29
  cross-repository id collisions (vendored governance).
* Two repositories are private; a central public store would leak them (security model).

## Mechanisms

| Need | Mechanism |
|---|---|
| Stable cross-repo reference | `repo:ID` (implemented in `Graph.resolve`); optional `@version` pin (proposed) |
| Discovery | each repository's compiled index (`INDEX.md`/JSONL) as a build artefact; a consumer fetches indexes of repositories it may read |
| Federated index | built per reader, from the repositories that reader can access; never committed to a less private place than its most private input |
| Conflicting definitions | same id in two repositories is an error unless one is a declared vendored copy of the other (provenance `vendored_from`) |
| Deleted records | tombstone (`status: retired`, keep id); ids are never reused |
| Versioning | pins on references to released artefacts; unpinned references follow the target's main branch |
| Access | GitHub repository permissions (verified: the only enforced boundary; Fides docs state it authenticates, not authorises) |

## Centralised alternative

A central repository holding all knowledge was rejected: it would duplicate
owned knowledge (drift: 5 of 6 hand-maintained views are already stale), cross
privacy boundaries, and break the per-repository review rule that gives records
their authority. A central **index of public indexes** remains possible and cheap.
