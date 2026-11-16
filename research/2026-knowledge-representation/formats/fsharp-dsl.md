# A compact, non-executable, F#-inspired declarative DSL: design considerations

Mission EKS-RESEARCH-2026-001, workstream C. Status: research analysis, not final results.
Labels: **[F]** verified from a retrieved source (SRC-FS-xxx in `../evidence/sources-fsharp.csv`); **[I]** inference; **[P]** prior knowledge not re-verified now. No measurements here.

Sketch of the target syntax:

```
requirement REQ-0042
  title "Cache invalidation must be explicit"
  depends REQ-0018
  accept "Stale reads are impossible after a write"
```

## 1. Do existing formats already suffice?

| Format | What it offers (retrieved facts) | Executes code? | Fit for Echelon **[I]** |
|---|---|---|---|
| TOML | Minimal config format mapping to a hash table; strict table and array-of-tables rules, inline tables single-line, mixed-type arrays allowed; v1.1.0 listed as newer than the 1.0.0 page (SRC-FS-009) **[F]** | No (data only) **[P]** | Poor for deeply nested records with lists of objects (array-of-tables restrictions); good for flat metadata. |
| KDL | Node name + arguments + properties + children; type annotations `(u8)10`; slashdash comments; KDL 2.0.0 finalized; .NET parsers KdlSharp (not marked v1-only) and Kadlet (v1 only) (SRC-FS-007) **[F]** | No (document language) **[P]** | **Closest match to the sketch**: `requirement REQ-0042 { title "..."; depends REQ-0018 }` is already KDL-shaped (braces rather than indentation). Needs schema validation done by F#. Library maturity for .NET to be checked. |
| HCL | Native syntax plus JSON variant sharing one model; attribute values may be expressions with variables and functions; APIs documented are Go (SRC-FS-014) **[F]** | Expression evaluation is part of the language (application-supplied variables/functions) **[F]** | Expressions conflict with "inert data"; no confirmed .NET implementation from sources retrieved. |
| EDN | Values-only notation, no schemas; extensible tagged elements handled by reader-supplied handlers; unknown tags may error or be kept generic (SRC-FS-008) **[F]** | The text defines no evaluation; code risk lies in handlers **[F]** | Good data model (sets, keywords, tags) but little .NET ecosystem and low LLM familiarity relative to JSON **[I]**. |
| Dhall | "JSON + functions + types + imports"; total, non-Turing-complete; goal of safely evaluating untrusted code; semantic hashes/diffs (SRC-FS-012) **[F]** | Evaluates functions but is total by design; imports are an I/O vector **[I]** | Overpowered; no first-class .NET implementation confirmed in this session. |
| CUE | Data validation language, types and values in one lattice, constraints combine order-independently (SRC-FS-010) **[F]** | Page mentions a scripting layer, so CUE tooling is not purely inert **[F]** | Excellent for validating YAML/JSON-like data **[I]**, but the toolchain is Go-based **[P]** and would be a second validation language beside F#. |
| Pkl | Configuration-as-code language with rich validation and tooling (SRC-FS-011) **[F]**; page retrieved says nothing about sandboxing **[F]** | A language with evaluation; resource reads and imports need checking in its Security docs (not retrieved) | Needs separate runtime; unverified safety. |
| Nickel | Merge, contracts, optional static typing; page does not state Turing-completeness (SRC-FS-013) **[F]** | Programming language **[F]** | Same objection as Pkl. |
| JSON / YAML / Markdown+front matter | Not re-researched here; heavy LLM training exposure **[P]** | Data only if safe-loaded **[P]** | The baseline any DSL must beat. |

Assessment **[I]**: none of the programmable languages (Dhall, CUE, Pkl, Nickel, HCL) is attractive as storage since each adds evaluation semantics Echelon would then have to constrain, and all but Dhall/CUE would need a foreign runtime. KDL is the only existing format that matches the *shape* of the sketch with no evaluation, so **before building anything, the experiment should include KDL as a candidate** (and plain YAML/JSON). A custom DSL is justified only if it beats KDL/YAML on tokens, diff quality, and agent write-accuracy by a margin worth its maintenance cost. That test is not yet run.

## 2. Grammar ambiguity pitfalls for an indentation-based language

- **Offside rule**: significant indentation needs a defined policy: spaces only, fixed step (2), tabs rejected. Mixed indentation and dedent-to-nonexistent-level must be hard errors with line/column.
- **Multiline strings**: a continuation line indented deeper than the key versus a new child; resolve by requiring strings to be delimited (`"..."` with escapes, or `"""` blocks) and never inferring.
- **Keyword vs value collisions**: `title depends` (a bare word that is also a keyword). Fix: values are always quoted strings, numbers, or syntactically distinct ID tokens (`REQ-0018` matches an ID regex); keywords only in key position.
- **ID token vs identifier vs number**: `REQ-0018` must lex as one token; `2026-11-16` as a date; avoid `-`ambiguity with negative numbers by lexing IDs by pattern first.
- **Lists**: repeated keys (`depends X`, `depends Y`) vs inline lists `depends [X, Y]`; supporting both causes ambiguity about merge vs replace. Pick one canonical form (repeat key) **[I]**.
- **Blank lines and comments**: define whether comments may sit between a node and its children and whether they are preserved (needed for lossless formatting).
- **Unicode and line endings**: normalize CRLF, reject BOM or NUL, forbid bidirectional control characters in strings to avoid "Trojan source" style confusion **[I]** (attack class known from **[P]** background).
- **Duplicate keys**: error, not last-wins.
- **Extensibility vs strictness**: unknown keywords must be an error in strict mode, preserved in "unknown" bag only in tooling mode.

## 3. Parsing approaches in F#

| Approach | Pros | Cons |
|---|---|---|
| FParsec | Mature combinators; positions and error messages built in; docs at quanttec.com, BSD-2 license; NuGet listing showed 1.1.1 (SRC-FS-005/006, snippet-level, latest status unconfirmed) **[F]** | Indentation sensitivity requires user state or a separate layout pass; reported error messages are good for generic grammars but custom recovery is manual; library is lightly maintained **[P]**; dependency adds supply-chain surface **[I]**. |
| Hand-written recursive descent over a pre-lexed token stream with INDENT/DEDENT tokens | Full control over error recovery, no dependency, easy to guarantee totality and resource bounds, straightforward in F# with `Result` and discriminated-union tokens **[I]** | More code to test; ad hoc error message quality depends on effort. |
| Parse via FCS (reuse F# parser, whitelist) | Free syntax | Rejected: see `fsharp-records.md` section 5 (large front-end, API churn, not hostile-input hardened). |

Recommendation **[I]**: the grammar is tiny and line-oriented (a two-pass lexer-with-layout, then recursive descent), so **hand-written** is preferable: no dependency, controllable limits, and error recovery by line.

**Totality requirements** (non-throwing): the public entry is `parse : string -> ParseResult` where `ParseResult = { Document: Document; Diagnostics: Diagnostic list }`; never raises. Enforce **input size cap, max nesting depth, max token length, max node count**; loops consume at least one character per iteration (termination argument); use `Result`/DU for errors and treat any internal exception as a bug caught at the boundary to produce a diagnostic (still total). Test with fuzzing / property tests (parse never throws; `print >> parse` round-trips on canonical form) **[I]**.

## 4. Error recovery

- Line-based synchronization: on error, skip to the next line whose indent is at or below the enclosing node's indent and continue; collect all diagnostics rather than stopping at the first. Particularly valuable for LLM-authored files where a single feedback round should list every problem **[I]**.
- Diagnostics carry: code (stable string), severity, line/column span, a "expected X, found Y" message, and a fix hint where deterministic.
- Partial document: return the nodes that parsed plus error nodes (`Error of span`) so downstream validation can still report corpus-level issues without cascading false positives.
- Separate **syntax diagnostics** (parser) from **semantic diagnostics** (validator in F# types, see `fsharp-records.md`).

## 5. Maintenance cost of a custom language

| Cost | Detail **[I]** |
|---|---|
| Spec and versioning | Language itself needs a version, a changelog, a conformance test-suite. |
| Tooling | Formatter (canonical printer), linter, LSP/diagnostics, syntax highlighting grammar (TextMate), diff-friendly canonicalization; each is work that YAML/JSON/Markdown already have for free. |
| Editor support | No highlighting, folding or schema-aware completion out of the box; Markdown/YAML get these natively. |
| Third-party consumers | Other languages need a parser; every integration (docs site, search, GitHub rendering) loses default support. |
| LLM familiarity | Models are trained heavily on JSON/YAML/Markdown; a new DSL has zero pre-training exposure, so correctness relies on in-context examples and validator feedback loops. KDL and TOML have some exposure; a bespoke DSL has none **[I]; not measured**. Risk: higher generation error rate and more tokens in prompts for the grammar. Quantification must come from `experiments/`. |
| Bus factor / drift | Grammar changes break every stored file; needs migration tool per grammar version. |
| Security | A new parser is new attack surface (mitigated by totality and size limits above). |

Mitigations: keep it a **projection** rather than the system of record: canonical storage in a well-known format, DSL as an optional compact rendering generated from and parsed back to the same typed model, with round-trip tests. This confines the cost to a view layer **[I]**.

## 6. Proposed minimal grammar (EBNF, for experiment)

Lexical layer produces tokens after layout processing: `INDENT` and `DEDENT` are emitted when leading spaces increase/decrease by exactly one level (2 spaces); tabs are lexical errors; blank lines and comment-only lines are ignored for layout.

```ebnf
document    = { node } ;
node        = keyword , [ id ] , { argument } , NEWLINE , [ INDENT , { node } , DEDENT ] ;
keyword     = lower , { lower | digit | "-" | "_" } ;
id          = upper , { upper } , "-" , digit , { digit } ;     (* e.g. REQ-0042 *)
argument    = string | number | boolean | date | id ;
string      = '"' , { char | escape } , '"' ;
escape      = "\\" , ( '"' | "\\" | "n" | "t" | "u" , hex , hex , hex , hex ) ;
number      = [ "-" ] , digit , { digit } , [ "." , digit , { digit } ] ;
boolean     = "true" | "false" ;
date        = digit4 , "-" , digit2 , "-" , digit2 ;
comment     = "#" , { any-char-except-newline } ;              (* ignored, whole-line or trailing *)
lower       = "a".."z" ; upper = "A".."Z" ; digit = "0".."9" ;
digit2      = digit , digit ; digit4 = digit2 , digit2 ;
hex         = digit | "a".."f" | "A".."F" ;
char        = any Unicode scalar except '"', "\\", control characters ;
```

Semantics, kept out of the grammar **[I]**: the first node of a file must be a record keyword (`requirement`, `conclusion`, `decision`, `experiment`, ...) with its id; allowed child keywords and argument arities per record kind come from the F# schema (DU of record kinds), not from the grammar, so the parser stays generic and the schema lives in one place. Repeated child keywords form lists. No expressions, no references-by-evaluation, no includes, no tags/macros: **there is nothing to execute by construction**. Example mapping:

```
requirement REQ-0042
  title "Cache invalidation must be explicit"
  depends REQ-0018
  accept "Stale reads are impossible after a write"
```
-> `Requirement { Id = REQ-0042; Title = ...; DependsOn = [REQ-0018]; Acceptance = [..] }` after validation.

Known open issues to test: whether to allow multiline strings (`"""`), inline lists, and quoted vs bare identifiers for tags; whether trailing comments are permitted; whether KDL (with braces) removes the need for the layout pass.

## 7. Conclusion for this workstream

- F# as a DSL **implementation** language (parser, validator): yes (option B).
- F# **syntax** as storage (option A): no; F#-*inspired* indentation only if experiments show a clear win over KDL and YAML.
- Priority order for experiments **[I]**: (1) YAML/JSON/Markdown front matter baseline, (2) KDL, (3) custom DSL above; compare tokens per record, diff stability, agent write-and-validate success rate, parse throughput, and validator error quality.

## Measured results

_Pending: see experiments/._
