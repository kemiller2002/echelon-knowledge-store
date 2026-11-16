# F# as a knowledge representation: records, types and source-as-data

Mission EKS-RESEARCH-2026-001, workstream C. Status: research analysis, not final results.
Labels: **[F]** = fact verified from a retrieved source (SRC-FS-xxx in `../evidence/sources-fsharp.csv`); **[I]** = inference or reasoning by the author; **[P]** = prior knowledge not re-verified in this session. No performance numbers are asserted; all quantitative questions are deferred to `experiments/`.

## 1. Question and short answer

Options: (A) F# source (`.fs`/`.fsx`) as the canonical storage format; (B) F# as implementation language of a typed knowledge engine; (C) F# as schema language of a compact declarative format; (D) F# as validation layer over structured Markdown; (E) combination.

Short answer **[I]**: **B is clearly right and cheap** (Echelon writes all code in F# anyway). **A is rejected** for knowledge records: it makes data and code indistinguishable, and the only safe reader is a parser-only subset, which is then a DSL in disguise with worse diffs and worse LLM ergonomics than JSON/YAML/Markdown. **D and C collapse into B**: F# types are the schema and the validator, while the stored bytes are a non-F# format. Recommended: **E = B + D (+ C only if experiments justify a custom compact format)**; storage stays inert text (Markdown with structured front matter, or JSON/YAML), and F# domain types plus a total validator are the single source of truth for invariants.

## 2. Language feature review (what each gives a knowledge engine)

| Feature | What it gives | Limits / caveats |
|---|---|---|
| Records | Immutable, structural equality and comparison by default, `with` copy-update. Natural shape for a record with fields. **[P]** | Equality includes all fields; need custom equality if identity is by ID only. |
| Discriminated unions (DU) | Closed sum types: `Status = Proposed \| Accepted \| Superseded of replacedBy: RecordId`. Exhaustiveness checking on match gives compile-time completeness when a new variant is added. **[P]**; technique per SRC-FS-017 **[F]** | Exhaustiveness is warning-level by default (can be made error via warnaserror) **[P]**. DUs model the closed set of record kinds; open extension by third parties needs a separate escape hatch. |
| Units of measure | Compile-time-only tags on numerics (`float<ms>`). **[P]** | Relevant only to numeric measurements inside experiment results; erased at runtime, so they do not survive serialization. Marginal value for a knowledge store. **[I]** |
| Type providers | Types generated from external schema/data at compile time. | See section 4: runs code at design/compile time; adds trust surface; no benefit for a growing corpus because every data change would affect compilation. **[I]** |
| Computation expressions | Builders for `result {}`, `validation {}`, `async`, `task`. Good for composing validation steps. FsToolkit.ErrorHandling provides Result helpers and CEs, including `Result<'a,'b list>` shapes (SRC-FS-015) **[F]**. | The retrieved page did not confirm a dedicated applicative Validation CE; check in an experiment before relying on error accumulation via `and!`. **[I]** |
| Active patterns | Named, reusable views over data (`(\|Superseded\|_\|)`, `(\|ActiveRequirement\|_\|)`). Complete patterns give exhaustiveness. **[P]** | Not serializable; only an in-engine convenience. |
| Immutability | Snapshots of the corpus are values; safe concurrent validation; trivial "before/after" diffs for migration. **[P]** | Large persistent maps cost allocation; fine for corpora of the expected size, to be measured. |
| Pattern matching | Concise, exhaustive dispatch on record kind and state. **[P]** | None significant. |
| Modules | Namespacing and encapsulation: private constructors plus a module of smart constructors. **[P]** | Private DU/record constructors are enforceable inside the assembly boundary only; reflection can bypass them (see Reflection). |
| Smart constructors / validation | Parse-don't-validate: `RecordId.create : string -> Result<RecordId, Error list>`; wrapped single-case DUs for `RecordId`, `NonEmptyString`, `NonEmptyList<'a>`. | Only covers per-record invariants; see section 8. |
| Reflection | Used by serializers and by FSharp.Reflection to enumerate union cases. | Also bypasses private constructors, so record values built via reflection-based deserializers must be re-validated. **[I]** |
| Quotations | Code-as-data for F# expressions. | Irrelevant to storage; if knowledge were quotations it would still be code. Do not use. **[I]** |

### 2.1 Making illegal states unrepresentable (Wlaschin style)

The core technique is one union case per valid state so invalid combinations do not exist as values; the compiler then forces handling of every case and flags breakage when rules change (SRC-FS-017) **[F]**. Applied to knowledge records **[I]**:

```fsharp
type RecordId = private RecordId of string
type NonEmptyList<'a> = { Head: 'a; Tail: 'a list }
type Evidence = { Source: SourceRef; Quote: string option }

type Conclusion =
  { Id: RecordId; Statement: string; Evidence: NonEmptyList<Evidence> }  // evidence required by type

type Status =
  | Proposed
  | Accepted
  | Superseded of replacedBy: RecordId   // no replacement => not representable

type Requirement =
  { Id: RecordId; Title: string
    Acceptance: NonEmptyList<Criterion>; Status: Status }

type Experiment = { Id: RecordId; Hypothesis: Hypothesis; Outcome: Outcome }   // hypothesis non-optional
```

## 3. Serialization

- **System.Text.Json + FSharp.SystemTextJson**: supports records (incl. struct and anonymous), DUs (incl. struct), lists/maps/sets, and options (plain value or null by default since v0.6); union encoding is configurable via `JsonUnionEncoding` (SRC-FS-002) **[F]**. The retrieved page showed no release version; pin a version only after checking NuGet. Deserialization of records through reflection can produce values that skip smart constructors **[I]**, so decode to a DTO and convert via smart constructors.
- **Thoth.Json**: type-safe decoders/encoders/codecs; targets .NET (over Newtonsoft or System.Text.Json) plus Fable JS/Python; its own benchmark reports roughly 2.7x the time of plain System.Text.Json (SRC-FS-016) **[F]**. Explicit decoders give good, path-aware error messages **[P]**, which suits LLM-facing error feedback; Fable portability is not needed for Echelon **[I]**.
- **FsToolkit.ErrorHandling**: Result helpers (`map`, `bind`, `apply`, `traverse`, `sequence`) and CEs (SRC-FS-015) **[F]**.
- **Recommended pattern [I]**: DTO (loose, serializer-friendly) -> validator (total, accumulates all errors) -> domain type (strict). Never let a serializer construct the domain type directly.

## 4. Type providers and the security of compile-time data reading

- Type provider design-time components run inside the compiler/tooling process (`fsc`, `dotnet fsi`, IDEs) (SRC-FS-001, which lists those host processes) **[F]**.
- Microsoft's security page (seen via search snippet only) states that provider DLLs execute in the IDE at design time, even when merely browsing code; the IDE asks the user to trust a provider, and a malicious provider could compromise the machine; declining yields compile errors (SRC-FS-004) **[F, snippet-level]**.
- Sandboxing: a community forum post (search result, not fetched) said restricted AppDomain/process isolation is "best effort" only **[F, snippet-level]**.
- Implications **[I]**: (1) a provider that reads a data file is trusted code reading untrusted data; a parser bug in the provider (e.g. path traversal via `file:` or URL includes, unbounded recursion) runs with build-time privileges; (2) knowledge records edited by agents or contributors become build inputs, so a poisoned record can influence the build of the engine itself; (3) every data change invalidates compilation. Verdict: do not use type providers for the knowledge corpus. A first-party, minimal provider over a closed schema could in theory be audited, but it gains nothing over loading data at runtime into the same types.

## 5. Option A in depth: knowledge stored as `.fs` / `.fsx`

Example of what a record would look like **[I]**:

```fsharp
let req0042 =
  Requirement.create "REQ-0042" "Title"
    [ Criterion "..." ] (Depends [ "REQ-0018" ])
```

### 5.1 Reading it without executing it

| Approach | Executes code? | Notes |
|---|---|---|
| `dotnet fsi script.fsx` / `#load` / `#r` | **Yes.** Compiles and runs arbitrary F#, including `System.Diagnostics.Process`, file IO, network, and `#r "nuget: ..."` package restore. **[P]** | Disqualified for untrusted or agent-written content. |
| Compile to assembly and reflect | **Yes** (static initializers, module-level `do`, top-level values run on load) **[P]**; even compile-time only, type providers and analyzers/MSBuild targets referenced by the project run code. | Disqualified. |
| FSharp.Compiler.Service **untyped AST** (`FSharpChecker.ParseFile` -> `ParseTree`) | **No**, parsing only; no type-checking and no evaluation (SRC-FS-003: FSharpChecker with `ParseFile` and the `ParseTree` property) **[F]**. | Then walk the syntax tree and accept only a whitelisted subset (literals, list/record expressions, a fixed set of constructor function names). Everything else is rejected. This is a **restricted-subset interpreter written by Echelon**. |

Consequences **[I]**:
- The result is a de facto DSL with F# surface syntax. Anything not in the whitelist (let-bound computed values, helper functions, string interpolation, `#if`, `#load`, active patterns) must be refused, so most of what makes F# attractive is unavailable. What remains offers few advantages over a purpose-built format.
- FCS API instability: the guide warns the API changes between package versions, and namespaces were reorganized (`SourceCodeServices` removed in 31.0.0) (SRC-FS-003, snippet) **[F]**. Pin a version and own an adapter layer. FCS tracks F# language versions, so whitelisting must be re-audited at each upgrade (new syntax can create new ways to hide expressions that the whitelist ignores) **[I]**.
- Parser as attack surface: FCS is a large compiler front-end, not designed for hostile input (deep nesting, huge literals); resource limits would need to be applied around it (input size, timeout, process isolation) **[I]**.
- Two-phase flow needed: parse+whitelist (safe) -> build domain values by interpreting the AST in the engine. Types are not checked on the data files unless the full compiler is run, and running the compiler on untrusted input is itself the risk. So the compile-time guarantees that motivate option A (type system enforces invariants) are **not available** without executing/compiling the content **[I]**. This is the central argument against A: the safe read path discards the benefit.

### 5.2 Other dimensions of A

| Dimension | Assessment |
|---|---|
| Build cost / compile time as corpus grows | F# compile time is superlinear-ish in practice for large files with heavy type inference; large literal lists and records are a known pain point **[P, unmeasured]**. Every knowledge edit would trigger recompilation of the project containing it. Requires measurement (to be added under `experiments/`). Parse-only avoids it but forfeits typing (5.1). |
| Git diffs | Line-oriented code diffs are fine for single-field edits, but offside-rule reindentation and list/commas produce noisier diffs than key-per-line formats; merges across `let` ordering are fragile. **[I]** |
| Agent generation reliability | LLMs write F# less reliably than JSON/YAML/Markdown (smaller training share, whitespace sensitivity, type-inference errors). **[I]**, not measured here; a generation-and-validate experiment is recommended. A failing compile gives a rich error but also executes nothing, so retries are safe only if compile is sandboxed. |
| Security | `.fsx` is arbitrary code by design; a single record could exfiltrate secrets or alter other records at build time. Violates the mission rule that knowledge is inert data. **[I]** |
| Tooling for non-F# consumers | Other tools (grep, docs sites, non-.NET scripts, other agents) cannot parse it without FCS. **[I]** |
| Schema evolution | Changing a type breaks every data file at compile time (good for finding breakage, bad because old records cannot be read until migrated; no way to read multiple versions side by side except keeping old types). **[I]** |

Verdict on A: **reject** as canonical storage. Retain F# *syntax* only as a possible inspiration for a deliberately non-executable DSL (see `fsharp-dsl.md`).

## 6. Quantities, schema evolution and versioned contracts

- Put `schema_version` (integer or semver string) in every stored record **[I]**. The engine keeps one DTO type per supported version (`RequirementV1`, `RequirementV2`) and a pure upgrade chain `V1 -> V2 -> ... -> Current` composed as functions; unknown or future versions are a validation error, never a best-effort parse **[I]**.
- DUs help: adding a case forces all matches to be revisited, and `Obsolete` conversions are explicit **[I]**.
- Tolerant reader policy: unknown fields rejected or captured in an `Extensions: Map<string,string>` bag? Choose "reject unknown fields in strict mode, warn in migration mode" **[I]** (design decision to test).
- Contract tests: golden files per version, round-trip tests (`decode >> encode = id` on canonical form), and property-based tests over generators **[I]**.

## 7. Option D: validation layer over structured Markdown

Markdown with a front-matter block (YAML or similar) for typed fields and a prose body for narrative; F# provides: parse front matter to DTO -> validate -> domain type; body kept as opaque text. Benefits **[I]**: storage and diffs are in the formats LLMs know best; human-readable; F# does all enforcement. Costs: front-matter parsers must themselves be safe (YAML parser features such as custom tags/anchors are a hazard: configure a safe, data-only mode; unverified here **[P]**), and cross-record rules still need a corpus pass. Also require that Markdown rendering never executes embedded HTML/scripts (render sanitized, or not at all).

## 8. Invariant enforcement table

"Type system" = enforceable by F# types alone for a *single, already-constructed record value*. "Corpus validator" = needs a pass over all records (needs the set of IDs, the graph, or other versions).

| Invariant | Type system (per record) | Corpus runtime validation | Notes **[I]** |
|---|---|---|---|
| Conclusion requires evidence | **Yes**: `Evidence: NonEmptyList<Evidence>` | Only to check each evidence reference resolves | Presence by type; validity of the reference is runtime. |
| Requirement requires acceptance criteria | **Yes**: `NonEmptyList<Criterion>` | No | Pure shape rule. |
| Superseded decision requires replacement | **Yes**: `Superseded of replacedBy: RecordId` | Yes: replacement must exist, not be itself superseded in a cycle, be of compatible kind | Type enforces presence; existence is corpus-level. |
| Experiment requires hypothesis | **Yes**: non-optional `Hypothesis` field | No | Trivial. |
| Reference to nonexistent record | Partially: `RecordId` well-formed (smart constructor) and kind-tagged `Ref<Requirement>` phantom type prevents wrong-kind refs at construction | **Required**: existence check against the corpus index | Types cannot know what is in the store without dependent typing. |
| Incompatible schema version | Partially: version as DU/closed set so unsupported versions fail to decode into any known DTO | Needed for migration-state checks (e.g. mixed-version corpus, forward-incompatibility policy) | Decoder rejects unknown version; corpus rules about mixes are runtime. |
| Invalid dependency cycle | **No** (a cycle is a global graph property; F# cannot express acyclicity of an arbitrary graph by type) | **Required**: graph pass (topological sort / SCC) | Immutability allows validating a snapshot; incremental checks on changed nodes are an optimization to measure. |
| ID uniqueness, ID matches filename | No | Required | |
| Status transitions legal (e.g. Proposed -> Accepted only) | Partly via transition functions over distinct state types | Required against history when files are edited out-of-band | Git history makes this a diff-level check. |

Summary: 3 of the 7 requested invariants (conclusion-needs-evidence, requirement-needs-acceptance, experiment-needs-hypothesis) are fully enforceable by types as presence rules; supersession is split (replacement presence by type, replacement existence by corpus); 3 (dangling references, dependency cycles, mixed or incompatible versions) need a corpus pass, with version incompatibility partly caught at decode time.

## 9. Recommendation for this workstream

1. **Reject A.** Knowledge stays inert text; F# source is code, not data.
2. **Adopt B** unconditionally: F# domain model with smart constructors, DTO decoders, a total corpus validator returning accumulated errors (no exceptions).
3. **Adopt D** as the default storage experiment (structured Markdown + typed front matter), and treat a compact custom DSL (**C**) as an experimental alternative only if measured token/readability savings justify the maintenance cost (see `fsharp-dsl.md`).
4. Final form **E = B + D, optionally + C**.
5. Experiments to request: agent generation success rate and error rates per format; validator throughput vs corpus size; FCS-whitelist prototype only as a negative control for A.

## Measured results

Measured (`experiments/results/formats.md`, 568 records):

* F# record source for full records: 921,924 tokens (1.00× original Markdown);
  metadata only: 121,946 tokens, **+75% vs YAML front matter** (69,866) and the
  most expensive format measured.
* Writer implemented (`FSharpSource.fs`); no safe reader without the F# compiler's
  parser, and evaluating the source would execute code.
* Not trialled with agents as a corpus format (equivalent information to JSONL/DSL,
  which scored 0.98–1.00).
* Every gain of typing (strict per-kind invariants) was obtained **without**
  storing knowledge as F#: `Strict.classify` derives the typed view from Markdown.
  Coverage: 100% for most kinds, 64% of requirements, 73% of experiments.

Verdict: F# as storage — REJECT. F# as schema, validator and compiler — ADOPT.
