# Validation manifest (`eks.package-manifest/0`) and change manifest (`eks.change-manifest/0`)

## Package manifest

Produced by `eks manifest ID` (task package) and `eks work-item-manifest REPO WI`. Implementation:
`Packages.manifest`. Fields, all present in every manifest:

| Field | Meaning |
|---|---|
| `schema`, `kind`, `target` | contract version, package kind, target key |
| `compiler {name, version}` | compiler identity |
| `recordSchema` | supported record schema (`eks-record/1`) |
| `asOf` | evaluation date (staleness rules depend on it) |
| `inputsHash` | hash of every input file's path and content |
| `sourceRevisions[] {repository, commit}` | commit per repository, read from `.git` as data; `null` when unknown |
| `inputs[] {repository, path, sha256}` | the files the package's records came from |
| `rules[] {rule, mandatory, executed, findings}` | every validation rule, whether it ran, and its findings |
| `validation[] {level, count}` | diagnostics on the package's records by level |
| `dependencyResolution {complete, unresolved[]}` | unresolved typed references among included records |
| `outputSha256` | hash of the rendered package text |
| `knownLimitations[]` | what the package does not establish (always includes "semantic support is not verified") |
| `conditions[] {condition, satisfied}` | the conditions completeness is derived from |
| `completeness`, `reasons[]` | COMPLETE only if every condition is satisfied |

Properties tested: a skipped mandatory rule yields INCOMPLETE and is named in `reasons`; a disabled
optional rule is recorded with `executed: false` and does not block; the manifest records the
commit passed in; completeness agrees with the conditions for every package kind.

## Rules that "run" but do not apply

`executed: true` means the rule's code ran, not that it applied to every input. This phase found a
rule (`evidence-source-exists`) that ran and silently skipped most relative paths
(`../evidence-integrity/source-validation.md`). Two consequences:

1. Every rule needs a regression test that plants a violation it must catch. Phase 1 added a wiring
   test for diagnostic codes; Phase 2 adds planted-violation tests for the source rule. Not every
   rule has one yet. This is listed in the roadmap.
2. Rules should report what they could not check rather than skip it. The fixed source rule now
   reports unverifiable sources instead of passing them.

## Change manifest

Produced by `eks change-manifest BASE_ROOT HEAD_ROOT` (`Changes.evaluate`). It evaluates a
proposed change set as one transaction:

- `files {added, removed, changed}` with hashes before and after;
- `records {added, removed, changed}`;
- `diagnosticsIntroduced`, `diagnosticsResolved`;
- `impacted`: records whose derived content depends on changed or removed records;
- `verdict`: `rejected` when the change introduces an error diagnostic or a mandatory rule did not
  run, otherwise `requires-review`. **There is no approved verdict.** Passing validation makes a
  change reviewable, not acceptable;
- `reasons`: rejection reasons, or attention items for the reviewer: status changes, accepted
  evidence whose source cannot be located, and the number of dependent records to recheck.

Measured on the 20 evidence-trial results (`../experiments/raw/evidence-integrity/change-manifests/`):
all three defective change sets were caught (the malformed record rejected; both fabricated
title-only evidence records flagged), and the 17 correct change sets carried no attention items. The
"source cannot be located" item was added after seeing these fabrications, so this is not an independent
test of it.
