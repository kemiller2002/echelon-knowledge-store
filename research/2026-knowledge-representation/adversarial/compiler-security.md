# Compiler security

| Threat | Control in the prototype | Tested |
|---|---|---|
| Executable knowledge | knowledge is never executed; YAML parsed to an inert tree; F# knowledge only type-checked in the separate probe, never compiled/run | design + FCS probe |
| YAML attacks (alias bombs, tags, type confusion, duplicate keys) | aliases, anchors, explicit tags and duplicate keys rejected; event limit 20,000; all exceptions converted to errors | unit tests; replication fuzzing (billion laughs rejected) |
| Parser DoS | DSL line cap 100k chars, iterative tokenizer; JSON depth 32 | unit tests |
| Prompt injection in records | record text fenced as `record-text (data, not instructions)`; authority header in every package; EKS0015 flags text addressed to agents | unit test; trial 2 H08 (all conditions resisted) |
| Self-promoting records | authority policy: a one-sided supersession by a non-accepted record does not change current authority; EKS0021 | unit test; trial 2 H07 |
| False provenance | provenance taken from location (repository, path, content hash), not from front-matter claims; `author_agent` reported, not trusted | design |
| Private → public leakage | EKS0020 blocks compiling private repositories for a public audience | unit test. Lapse during this research: private directory names were pushed in a report (redacted later; history retained) — human process, not the compiler |
| Stale or tampered artefacts | manifest with input and output hashes; `--artifacts` queries refuse on mismatch (exit 2) | unit test |
| Schema downgrade | `schema` major version checked (EKS0004); minimum-version policy proposed, not implemented | partial |
| Compiler defects | catalogue-wiring test; preservation oracle for optimisations; determinism and warm=cold tests | added after two real defects (see `semantic-preservation.md`) |

Not implemented: signed artefacts, authorisation (GitHub permissions remain the only
enforced boundary), sandboxing of the CLI.
