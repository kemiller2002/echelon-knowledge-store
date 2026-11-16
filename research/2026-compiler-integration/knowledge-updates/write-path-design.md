# Write-path design

## Options compared

| Approach | What it is | Evidence in this programme | Assessment |
|---|---|---|---|
| Direct Markdown editing | agent edits files with ordinary tools | Phase 1 write trial; Phase 2 evidence trial (20 runs) | Works; agents follow conventions. Produced 1 malformed record (Haiku, no validation) and 2 title-only fabrications in 20 runs. |
| Structured tools | an API that writes records from typed fields | not built | Would prevent malformed YAML; would not prevent fabrication (both fabrications were well formed). |
| Compiler-validated editing | edit, then `eks validate`, fix errors | Phase 1; Phase 2 conditions 2–5 | No validated run left a malformed record (0 of 16; the one malformed record came from an unvalidated run). Does not prevent title-only fabrication. |
| **Transactional change set + change manifest** | evaluate the whole diff base → head; reject on new errors; otherwise require review with attention items | built this phase (`Changes.evaluate`, `eks change-manifest`) | On the 20 trial results: 3 of 3 defective change sets caught (1 rejected, 2 with a "source cannot be located" item); the 17 correct ones carried no attention items. All 19 non-rejected sets get the same `requires-review` verdict, so detection here means an attention item, and the attention rule was written after seeing the two fabrications (post hoc, untested on new data). |
| Generated patches | agent proposes a patch; a person or reviewer applies it | evidence-trial condition 5 (reviewer edits in place) | Reviewer approved correct records; no defective record reached a reviewer, so detection is unmeasured. |
| Schema-aware editing | editor that knows record kinds and relations | not built | Subsumed by structured tools + validation for this corpus. |

## Recommended write path

1. Agents edit Markdown directly. It needs no new tooling, and agents already follow the
   conventions well.
2. Before proposing, the agent runs `eks validate` and fixes errors (cheap; reports malformed
   records).
3. The proposal is evaluated as a transaction with `eks change-manifest BASE HEAD`:
   - `rejected` → the change cannot be proposed;
   - `requires-review` → the manifest goes with the pull request. A person reviews the attention
     items (status changes, unlocatable sources, impacted records).
4. **No change is approved by the compiler.** Schema-valid is not correct. The mission's constraint
   ("do not automatically approve an edit because it passes schema validation") is enforced by
   construction: the verdict type has no approved case.
5. Praxis and Dokimos decide completion and verification through their own gates. EKS supplies the
   manifest as evidence; it does not gate.

## Not done

- No structured-edit tool. The evidence does not show it would help beyond validation.
- The manifest is not yet a Praxis evidence type. Praxis accepts free-text evidence types, so it
  could be attached without changing Praxis (`../architecture/praxis-integration.md` §6).
