# Versioning, Identity, Merge Behavior and Migration for EKS Records

Mission EKS-RESEARCH-2026-001, workstream J. Status: research draft. Date: 2026-11-16.

Conventions. **[F]** fact retrieved from a cited source (`SRC-SEC-nnn`, see `../evidence/sources-security.csv`). **[E]** result of a local experiment I ran (git 2.43.0, fixtures described in section 6; SRC-SEC-040). **[L]** observed in local Echelon repos. **[I]** inference or proposal. **[U]** unverified.

---

## 1. Recommendations at a glance

| Topic | Recommendation | Basis |
|---|---|---|
| Schema version scheme | `MODEL-REVISION-ADDITION` (SchemaVer) per record type, carried in each record as `schema: <type>/<M>-<R>-<A>` | SchemaVer defines the three levels by effect on historical data [F, SRC-SEC-021]; SemVer's patch concept has no analog for schemas per its critics [F, SRC-SEC-021]; fit to records **[I]** |
| Compatibility target | The current EKS validator must read **all** historical record versions still in git (BACKWARD_TRANSITIVE in Confluent terms) | Records persist in git indefinitely **[I]**; term from SRC-SEC-024 |
| Unknown fields | Within the same MODEL, tools preserve unknown fields on rewrite; authority-bearing fields never tolerate unknowns | Protobuf preserves unknown fields and warns copy/JSON conversion can drop them [F, SRC-SEC-025]; security rationale **[I]** |
| Identifiers | Opaque, immutable, never reused, namespaced by owning repo, stored in front matter (not derived from path or title) | Protobuf field-number discipline [F, SRC-SEC-025]; Echelon repos already use stable-looking IDs [L]; rest **[I]** |
| Lifecycle | Never delete. `status` + `superseded_by` / `supersedes` + tombstones. Cross-repo supersession is effective only when the target's owner acknowledges | **[I]**; security reasoning in security-model.md T-05/T-11 |
| Layout for concurrency | **One record per file** (Markdown with front matter, or one JSON file) as the default unit; JSONL only for append-only logs with `merge=union` and unique-ID dedup | Experiment results in section 6 **[E]** |
| Agent writes | PR-only, branch per agent per task, random-suffix IDs, no shared counters; compare-and-swap (blob sha or `--force-with-lease`) for any direct API write | SRC-SEC-029, SRC-SEC-030 [F]; the rest **[I]** |
| Migration | Expand, migrate, contract; migrations are pure data-to-data functions with identity round-trip tests and a corpus dry run; Markdown body bytes untouched | SRC-SEC-031 [F]; the rest **[I]** |
| Rollback | `git revert` of the migration PR plus keeping old-version validators until contract | **[I]** |

---

## 2. Schema versioning schemes

### 2.1 What the sources say

- **Semantic Versioning 2.0.0** [F, SRC-SEC-022]: MAJOR for incompatible API changes, MINOR for backward-compatible functionality **or marking a public API feature deprecated**, PATCH for backward-compatible bug fixes. Requires a declared public API. Released versions must not be modified; any change needs a new version. 0.y.z is unstable.
- **SchemaVer** (Snowplow Iglu) [F, SRC-SEC-021, from search-result snippet; the primary page failed to fetch via redirect loop, so quote-level detail is unverified]: **MODEL** for a breaking change that blocks interaction with any historical data; **REVISION** for a change that may block interaction with some historical data; **ADDITION** for a change compatible with all historical data. Starts at 1-0-0. The authors concluded SemVer did not fit because "there is no such thing as bug fixes for a JSON Schema" [F, SRC-SEC-021, snippet]. A community thread notes enforcement is in Snowplow's UI rather than the server because some compatibility cannot be detected from the schema alone [F, SRC-SEC-021, snippet].

### 2.2 Compatibility rules in schema-bearing systems

**Avro** [F, SRC-SEC-023]: Resolution compares *writer* schema (used to encode) with *reader* schema (what the app expects). Record fields match by name; writer fields missing from the reader are ignored; a reader field absent from the writer takes its default, and if it has no default resolution errors. Enum symbols unknown to the reader use the reader default if one exists, otherwise error. Promotions: int to long/float/double, long to float/double, float to double, string and bytes interchangeable. Aliases rewrite the writer schema using the reader's aliases. `doc` is ignored.

**Confluent Schema Registry compatibility modes** [F, SRC-SEC-024]: BACKWARD (default; new-schema consumers read old data), BACKWARD_TRANSITIVE (all earlier versions), FORWARD (old consumers read new data), FORWARD_TRANSITIVE, FULL (both, against previous only), FULL_TRANSITIVE, NONE. Upgrade order: BACKWARD means upgrade consumers first; FORWARD means upgrade producers first; FULL allows independent upgrades. Adding a field is backward compatible only if it has a default; removing a field under BACKWARD requires that it was optional or defaulted.

**Protobuf proto3** [F, SRC-SEC-025]: Changing a field number is not safe (equivalent to delete plus add). Adding fields is safe; removing is safe only if the number is never reused, so use `reserved` for numbers and names. Adding enum values is safe on the wire but can break exhaustive switches. Some type changes (int32/uint32/int64/uint64/bool) are conditionally compatible with truncation. Proto3 **preserves unknown fields** during parse; converting to JSON can drop them and field-by-field copying can drop them.

**JSON Schema** [F, SRC-SEC-026]: Objects are open by default ("any additional properties are allowed"); `additionalProperties: false` closes them but only sees properties in its own subschema; `unevaluatedProperties` (2019-09+) counts properties across composed subschemas. Implication stated by the page's examples, not explicitly: a closed schema makes adding fields a breaking change for existing validators; an open one accepts newer fields.

### 2.3 Applying to EKS records [I]

EKS is closer to **reader-supports-all-writers** than to a streaming topic: records written under schema 1-0-0 stay in git forever; the current validator must still read them (BACKWARD_TRANSITIVE). Forward compatibility (an old validator reading a new record) matters less because EKS validators are released and pinned centrally, but it matters at the edges: a source repo's CI pinned to an older EKS release will see records written under a newer schema.

Proposed mapping:

| Change | SchemaVer level | Examples | Migration needed? |
|---|---|---|---|
| ADDITION | Backward and forward compatible with existing records | Add an optional property; widen an enum with a default reader behavior; loosen a `maxLength` | No |
| REVISION | Some existing records may no longer validate | Make a field required with a default the migrator can supply; narrow a pattern; tighten a limit | Yes, mechanical migration; dry-run reports violators |
| MODEL | Existing records cannot be read as-is | Rename or re-type a field; change an enum's meaning; change ID grammar; split a type | Yes, with a conversion tool and expand-migrate-contract window |

Rules:

1. A released schema version is **immutable** (SemVer's rule [F, SRC-SEC-022]). Fixes ship as a new ADDITION or REVISION; the registry stores every version.
2. Closed vs open: use closed schemas (`unevaluatedProperties: false`) for authority-bearing fields so unexpected fields fail closed, and allow a namespaced extension object (for example `x-<repo>:`) for forward-tolerant data. Round-tripping tools must preserve the extension object and any unknown field within the same MODEL (the protobuf lesson [F, SRC-SEC-025]).
3. Deprecation is a MINOR-style signal in SemVer terms [F, SRC-SEC-022]; for schemas, mark a field `deprecated` in an ADDITION release, then remove it in a MODEL release after the migration window.
4. Version in records: `schema: eks.decision/1-2-0`. Version is a *claim* used to select the validator; it can never lower the minimum acceptable version for the type (security-model.md C-20).
5. Never re-use a removed field name with a different meaning inside the same MODEL (protobuf `reserved` analog [F, SRC-SEC-025]); keep a `reserved` list in the schema.
6. The checkable part of "is this ADDITION or REVISION" should be tooling: a schema-diff step that classifies a PR's schema change and fails the build when the declared level is lower than the computed one. SchemaVer itself notes some changes cannot be detected from the schema alone [F, SRC-SEC-021 snippet], so a human judgment stays in the loop. **[I]**

### 2.4 Alternative considered: SemVer for schemas

SemVer's PATCH level has no clear schema meaning (the SchemaVer authors' point [F, SRC-SEC-021 snippet]), and its MINOR conflates "new optional data" with "deprecation" [F, SRC-SEC-022]. If EKS wants a single scheme across code and schema, SemVer works for the EKS *release* version while records carry SchemaVer-style schema versions; the release notes map one to the other. **[I]**

---

## 3. Stable identifiers

Observations from local repos [L]: Echelon repos already use ID families such as `DF-AEGIS-2026-DBBD`, `DF-FIDES-2026-0010`, `WI-0022`, `RQ-ROS-2026-A001`, `EXE-20261109T142541284Z-a3f0d18d`. These mix per-repo namespaces, year, sequential numbers, hex tokens, and timestamp+random. (These are observations of file names and headers I read, not a documented standard.)

Requirements for EKS IDs **[I]**:

1. **Immutable**: never changes for the life of the record, across renames, moves, edits and supersession.
2. **Never reused**, even after retraction (the protobuf field-number discipline [F, SRC-SEC-025]). Retired IDs live in a tombstone list.
3. **Independent of path and title**: stored in front matter or the JSON body; path is location, title is display. A file move keeps the ID.
4. **Namespaced by owning repo** so that two repos cannot collide and so that location-to-ID binding can be validated (spoofing control, security-model.md C-07). Cross-repo moves keep the old ID as a tombstone with `moved_to`.
5. **Collision-free under concurrent creation without coordination**: sequential counters (`0010`, `0011`) collide when two branches each allocate "the next number"; they only fail at merge time and renumbering breaks references. Prefer a random or time-sortable component (hex token, ULID/UUID-style) for machine-created records; allow human-friendly aliases separately. This follows from how git merges (both branches add different files, merge is clean, but both claim the same number); the collision is semantic and git will not flag it. **[I]**; the "adds different files merge cleanly" part is **[E]** (section 6).
6. **Strict grammar**: ASCII, bounded length, no path separators or whitespace, case-folded comparison rules defined once (security: homoglyph and traversal resistance, security-model.md C-05).
7. **Version-addressable references** when meaning matters: `id` for "the record", `id@<content-hash or commit>` for "this exact text" (compiled packages and cross-repo refs record the pinned form). Cross-repo mutable references are a poisoning vector (security-model.md T-11).
8. **Human alias layer**: a short slug or number for conversation, resolved via the index; never used as the key.

Not decided **[U]**: the exact ID format and generator; a spike should compare readability against collision risk using actual repo volumes (not measured here).

---

## 4. Deprecation and supersession

Lifecycle states (proposal **[I]**): `draft` → `proposed` → `accepted` → (`deprecated` | `superseded` | `retracted`). Only forward transitions plus `retracted` from any state; transitions are validated by the type's schema and require review (authority derives from review, not from the claim; see security-model.md section 4).

Fields: `status`, `supersedes: [id@hash]`, `superseded_by: id`, `deprecated_reason`, `retracted_reason`, `effective_from`.

Rules:

1. **Never delete** accepted records; supersede or retract. Git history keeps the text, but compiled packages and indices must be able to show "this was decided, then replaced by X". Tombstone file when a file must be removed for legal or secret reasons: keeps ID, drops content.
2. **Reciprocity check**: if B says `supersedes: A`, the validator requires A's repo's current record to say `superseded_by: B` (or an acknowledged proposal in A's owner repo). Otherwise B's claim is only a proposal. Prevents one repo from silently voiding another's decisions. **[I]**
3. **Graph hygiene**: no cycles; a chain resolves to exactly one head; the compiler follows to the head and shows the chain.
4. **Query default**: "current" means head of chain with status `accepted`; retrieval of superseded text is an explicit request.
5. **Schema deprecation** uses the same pattern at the type level: expand (introduce new schema/field alongside old), migrate (move records), contract (remove old) [F, SRC-SEC-031]. The supplier carries two versions during migration, so the contract phase must be scheduled and tracked [F, SRC-SEC-031].

---

## 5. Format choices and merge behavior

### 5.1 Facts about git merging relevant here

- Git's default is a 3-way text merge; `merge=union` takes lines from both sides without conflict markers but "tends to leave added lines in random order" and the manual warns "Do not use this if you do not understand the implications" [F, SRC-SEC-027]. Custom merge drivers can be defined in config (not `.gitattributes` itself) [F, SRC-SEC-027], which means they do not travel with the repo and are a poor fit for agents' fresh clones **[I]**.
- JSON Lines: UTF-8, one JSON value per line, newline-terminated, no blank lines [F, SRC-SEC-028].
- `git push --atomic` updates all refs or none; `--force-with-lease` overwrites a ref only if it still has the expected value [F, SRC-SEC-029].

### 5.2 Experiment [E]

I ran these in a scratch repo (git 2.43.0). Each case: two branches from the same base make a change, then merge one into the other. Results:

| Scenario | Result |
|---|---|
| JSONL: both branches append a line at end of file | **CONFLICT** |
| Same, with `.gitattributes` `merge=union` | Clean; both lines present (here ordered by branch merge order) |
| One big YAML list: both branches append an item at end | **CONFLICT** |
| One record per file: each branch adds a different new file | Clean |
| One record per file: edits to different non-adjacent lines of the same record (title line 3 vs owner line 5) | Clean |
| Same record: edits to adjacent lines (title line 3 vs status line 4) | **CONFLICT** |
| Same record: front-matter edit vs body edit | Clean |
| Big YAML: edits to different entries on non-adjacent lines | Clean |
| Big YAML: edits to adjacent lines of neighboring entries | **CONFLICT** |
| Rename of a record file on one branch, body edit of the same file on the other | Clean (git followed the rename; edit applied to renamed file) |

Caveat: tiny fixtures; rename detection in git is similarity-based and could fail on larger edits **[I]**; "adjacent" means within git's hunk context, which is why lines 3 and 4 collided but 3 and 5 did not. Not tested: reordering, formatting-only changes, very large files, `git rebase` flows, GitHub's merge queue **[U]**.

### 5.3 Format comparison for concurrent writes **[I]** (derived from [E])

| Layout | Concurrent adds | Concurrent edits to different records | Concurrent edits to same record | Notes |
|---|---|---|---|---|
| **One record per file** (MD+front matter or JSON) | No conflicts | No conflicts | Conflicts only if edited lines are adjacent; front matter vs body edits merge cleanly | Best unit for review (PR diff = records), CODEOWNERS granularity, per-record history, rename tracking |
| **JSONL, append-only** | Conflict at EOF without `merge=union`; union merges cleanly but loses order guarantees and cannot dedupe | Clean if non-adjacent | Line-level | Fine for event/audit logs where order is a timestamp field and IDs make duplicates detectable; poor for CODEOWNERS (single file) and review |
| **One big YAML/JSON document** | Conflict at end; hot-spot at insertion points | Conflicts when adjacent | Same | Worst: single merge hot-spot; any parse error corrupts every record (blast radius) |

Security tie-in: per-record files let CODEOWNERS and push rules apply per path [F: CODEOWNERS patterns, SRC-SEC-016; push rulesets restrict paths, SRC-SEC-015], and a malformed record rejects only itself (quarantine) rather than the whole file. **[I]**

Recommendation: records as one file each. If JSONL is used, restrict it to derived or log data (for example telemetry), keep `.gitattributes` `merge=union` for those paths **in the repo**, require unique IDs, and have the reader dedupe and sort by an explicit key rather than file order.

---

## 6. Concurrent agent writes and atomic updates

Principles **[I]** unless cited:

1. **Never write to the default branch directly.** Each agent task works on its own branch and opens a PR. Git's merge, plus per-record files, makes most concurrent work conflict-free (section 5.2). Conflicts that remain are real semantic collisions and should go to a human or to a rebase-and-revalidate step.
2. **No shared mutable counters or index files in source repos.** IDs are allocated locally with a random component; derived index/graph is rebuilt from git, so it is never a write hotspot.
3. **Optimistic concurrency for API writes.** GitHub's contents API requires the blob `sha` of the file being replaced when updating [F, SRC-SEC-030], which makes lost updates detectable. The page does not explain the 409 behavior and warns against running update in parallel with delete [F, SRC-SEC-030]; so serialize per-repo writes in the agent harness **[I]**. For ref-level compare-and-swap in git itself, `--force-with-lease=<ref>:<expect>` is the documented mechanism, though the explicit form is described as experimental [F, SRC-SEC-029].
4. **Atomicity within a repo = one commit/PR.** A multi-record change (for example supersede A with B: create B, edit A) is a single commit so the repo is never observed half-updated on the protected branch **[I]**; the validator must check the invariant (reciprocity, section 4) on the merged tree, not per file. `git push --atomic` helps when pushing several refs (branch plus tag) as a unit [F, SRC-SEC-029].
5. **No atomicity across repos.** Cross-repo supersession or reference changes are two PRs in two repos. Design for eventual consistency: reference by pinned `repo@commit`, validate reciprocity after both merge, and have a "dangling acknowledgement" state that is reported, not silently honored. **[I]**
6. **Validation gate = required status check**, run from a pinned EKS release against the merged result (not the PR's own validator), so that two individually valid concurrent PRs that conflict semantically fail the second one after rebase. Whether GitHub's merge queue is available and helpful here is **[U]** (not researched).
7. **Idempotent agent operations**: operations keyed by record ID and content hash so a retried run does not duplicate. (Aegis documents the same idea for its event store: replay of an identical record succeeds without writing, and a different record at the same path is a conflict [L, SRC-SEC-033]; that is a precedent for the pattern, not a component EKS can reuse for records.)

---

## 7. Rollback

**[I]** unless cited.

- **Records**: `git revert` of the PR (history preserved; no force push). For a supersession, revert restores the old head. Because the index is derived, rebuild from the reverted commit.
- **Compiled packages**: pinned to source commits and the schema release, so a rollback is "recompile from the earlier pin"; packages are never edited in place.
- **Schemas/validators**: a schema release is immutable [F, SRC-SEC-022]; rollback means pinning the previous release. This is why REVISION/MODEL changes use expand-migrate-contract [F, SRC-SEC-031]: during the window both versions validate, so rolling back the migration PR does not strand records.
- **Migrations**: each migration PR touches only the records it migrates plus the `schema:` field; revert is mechanical. If a migration is not reversible by revert (information dropped), it must not run in one step: first an ADDITION-level expand, then contract only after a retention period.
- **Security rollback**: a poisoned accepted record is retracted by a new commit (status `retracted`, content may be replaced by tombstone) and the compile step re-run; packages already delivered to agents are not recallable, so keep a package-ID log to identify sessions that consumed it (security-model.md T-13).

---

## 8. Round-trip conversion and lossless migration

### 8.1 What is lost where

| Format | Loss risks | Basis |
|---|---|---|
| YAML | Anchor names are a serialization detail discarded after composing; repeated nodes become aliases or duplicates depending on emitter [F, SRC-SEC-011]. Implicit typing differences between YAML 1.1 and 1.2: 1.2 "removed many of the problematic implicit typing recommendations" [F, SRC-SEC-011], so the same text may parse to different types in different tools. Comments, key order and quoting style are presentation, not data model **[I]**; libraries exist that preserve comments and key order specially (ruamel.yaml's stated feature [F, SRC-SEC-032]), implying ordinary parse/emit does not |
| JSON | No comments; number precision and key order depend on the library **[I]** |
| JSONL | One value per line; no comments [F, SRC-SEC-028] |
| Markdown | Body is free text; re-rendering through a Markdown AST may normalize whitespace, list markers and link styles **[I]** |
| Protobuf-style typed layers | Unknown fields dropped when converting through JSON or copying field by field [F, SRC-SEC-025] |

### 8.2 Practices **[I]**

1. **Edit, do not re-serialize.** For front-matter changes during migration, modify the minimal text span (text-level patch) or use a comment- and order-preserving round-trip editor; keep the Markdown body **byte-identical**. A migration that reformats whole files creates noisy diffs, hides malicious edits from reviewers (security-model.md T-02) and causes merge conflicts.
2. **Identity migration test.** For every schema version, `parse → emit → parse` must be semantically equal and a no-op "migrate" must produce **zero diff** on the corpus.
3. **Pure migration functions.** `migrate_n_to_n+1 : Record -> Record` on the typed/validated structure, data-only, no I/O, no script execution (security-model.md C-17). Each has: pre-condition validator (version n), post-condition validator (n+1), a property test for idempotence, and where possible an inverse or a retained original in git.
4. **Corpus dry run.** Run migrations across all source repos in a read-only job; produce a report (migrated, unchanged, rejected with reason) before opening any PR. Rejected records go to quarantine, not silent skip.
5. **Semantic hash.** Compute a hash over a canonical typed form of the record data (excluding presentation) before and after migration; mismatch beyond the declared field changes fails the build. JSON canonicalization standards exist but I did not retrieve one, so choice of canonical form is **[U]**.
6. **Preserve unknowns and extensions** (section 2.3 rule 2); migrate only the fields it knows.
7. **Pin the YAML dialect** (one version, one parser) and test with the "surprising scalar" cases (booleans, nulls, dates, octal-looking numbers) because 1.1 vs 1.2 rules differ [F, SRC-SEC-011]. Specific examples of surprising scalars are **[I]** (not enumerated in the pages I read).
8. **Version bump last.** The migrator changes `schema:` only after the post-condition validation passes.
9. **Batch size and review.** One PR per repo per migration; CODEOWNERS review applies (security-model.md C-08); the migration tool's own version is recorded in the PR body.

---

## 9. Open items and experiments suggested

1. Prototype the schema-diff classifier (ADDITION/REVISION/MODEL) on a JSON Schema pair, measure false negatives on a seeded set. **[U]**
2. Re-run the merge experiment on realistic record volume and edit patterns, including rebase, squash merge and GitHub's rebase-and-merge (which changes commits [F, SRC-SEC-017]). **[U]**
3. Decide ID format via a collision/readability spike (section 3). **[U]**
4. Determine whether GitHub's merge queue and rulesets apply to the owner's private repos (plan dependent per SRC-SEC-015). **[U]**
5. Pick and test the F# YAML library for: alias rejection, tag restriction, depth limits, comment-preserving edits (all **[U]**).
6. Confirm SchemaVer wording directly from the primary Snowplow page (fetch failed; snippet only).
