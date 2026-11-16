---
id: EKS-RESEARCH-2026-001-LIT-KR
title: Literature review - knowledge representation, docs/requirements tooling, provenance and argumentation
workstream: B
date: 2026-11-16
status: draft
sources: evidence/sources-kr.csv
---

# Literature review: knowledge representation for an LLM-agent-consumed org knowledge base

## 0. How to read this document

Every substantive statement carries one of three tags:

- **[E]** Established: supported by a source I retrieved in this session. The source id (SRC-KR-nnn) is given; see `evidence/sources-kr.csv` for URLs.
- **[I]** My inference: reasoning from [E] facts or general engineering judgement. Not evidence.
- **[B]** Background knowledge I did not retrieve in this session (for example, Prolog, ER models, semantic networks, Z). Treat as unverified until cited.

Retrieval caveats, stated up front so nobody over-trusts the citations:

- Some sources were read only through search-result snippets, not the full text (marked "search snippets only" in the CSV). These are SRC-KR-006, 007, 011, 012, 013, 017, 021, 023, 034, 035, 036, 037, 038.
- The OpenFastTrace user guide (SRC-KR-023) could not be fetched directly (HTTP 404 on three URL variants); its syntax is known from snippets of the project's own spec files. GitHub API access to tool repositories was denied in this session, so release dates and star counts come only from the GitHub HTML pages I could fetch.
- SRC-KR-017 (AGENTS.md study): the figures come from secondary summaries and the ICLR abstract; they must be checked against the PDF before use in any decision.
- I report no numbers except those in a source. Where a source gives a number, it is quoted with its source.

Scope assumptions: about 13 repositories, at most ~10k records, readers are LLM coding agents (and humans), content is requirements, ADRs, research reports, agent instructions, today stored as Markdown with YAML front matter (ids, status, supersedes, related_documents). [Given in task]

## 1. Knowledge representation formalisms

### 1.1 RDF, RDFS/OWL, JSON-LD, SHACL, SPARQL

**RDF.** Statements are subject-predicate-object triples; IRIs may appear in all three positions; many serializations (N-Triples, Turtle, TriG, N-Quads, JSON-LD, RDFa, RDF/XML) express the same graph [E: SRC-KR-001]. The RDF 1.1 Primer is a W3C Working Group Note (24 June 2014), informative and non-normative [E: SRC-KR-001].
- Strengths: global identifiers, merge-by-union of graphs, mature standards stack [I, grounded in SRC-KR-001].
- Costs: IRI minting and vocabulary choice; developers must learn a second data model next to their code [I].

**OWL 2.** An ontology language "with formally defined meaning"; OWL 2 DL is based on the SROIQ description logic; profiles EL, QL, RL trade expressivity for tractable reasoning (EL polynomial time; QL answerable with relational database technology; RL via rule-extended database technology) [E: SRC-KR-004]. W3C Recommendation, 11 December 2012 [E: SRC-KR-004].
- Fit for EKS: reasoning (subsumption, consistency) is rarely the bottleneck for a ~10k-record knowledge base whose main need is "is this id valid, what supersedes what, what is stale". [I] The open-world assumption of OWL makes "missing link" checks awkward (absence of a statement is not falsity); this is general knowledge I did not retrieve [B]; SHACL is the usual remedy for closed-world checks [I].

**SHACL.** W3C Recommendation, 20 July 2017; validates an RDF data graph against a shapes graph; Core constraints include cardinality (`sh:minCount/maxCount`), datatype, class, pattern, `sh:in`, logical combinators and `sh:closed`; SHACL-SPARQL adds SPARQL-based constraints and is optional; output is a validation report with focus node, path, value, source constraint and severity (Info/Warning/Violation) [E: SRC-KR-002].
- Strength: it is the closest standards-based analogue to "typed validation of knowledge records" [I]. The validation-report shape (focus node + path + severity) is a good design reference for any EKS validator output [I].
- Cost: requires converting records to RDF first, then a SHACL engine; error messages are about graph nodes, not source files/lines unless engineered [I].

**JSON-LD 1.1.** W3C Recommendation, 16 July 2020; serializes Linked Data in JSON; `@context` maps short terms to IRIs, `@id` identifies nodes, `@type` types them; design goal "zero edits, most of the time" for existing JSON [E: SRC-KR-003].
- Fit: gives a migration path for existing YAML/JSON front matter to a graph without rewriting records [I]. YAML front matter is not JSON-LD, but is mechanically convertible [I; B for YAML-JSON equivalence].

**SPARQL 1.1.** W3C Recommendation, 21 March 2013, query language for RDF [E: SRC-KR-005].

**Evidence for LLM consumption.**
- Sequeda et al. report GPT-4 zero-shot accuracy of 16% on enterprise questions asked directly over SQL schemas, versus 54% when the same questions were posed over a knowledge-graph representation (ontology plus mappings) of the same database [E: SRC-KR-014]. Caveats [I]: insurance-domain schema, SQL question answering, a single benchmark by authors from a KG vendor-adjacent community, and the gain comes from semantic context (ontology/mappings), not from RDF syntax as such. It does not show that a KG beats well-written Markdown for agent instruction retrieval.
- The Pan et al. roadmap frames LLMs and KGs as complementary: KGs store facts explicitly but are costly to build and keep current [E: SRC-KR-012, snippets].
- Net: [I] evidence supports "explicit semantics help LLMs answer questions over structured data"; there is no retrieved evidence that RDF/OWL specifically helps coding agents working in repositories.

### 1.2 Knowledge graphs and RAG over them

Microsoft's GraphRAG builds an LLM-derived entity graph and community summaries; the authors report gains over conventional RAG in comprehensiveness and diversity for *global* sensemaking questions over corpora of about 1 million tokens, while noting conventional vector RAG remains more direct for pinpointing individual facts [E: SRC-KR-013, snippets only; detailed numbers not verified].
- Fit: EKS questions are mostly lookup/traceability ("which ADR supersedes X?", "what requirements does this test cover?") rather than corpus-wide themes. [I] This argues that explicit typed links beat LLM-extracted graphs for the primary workload, and that GraphRAG-style summarisation is a possible secondary use for "what are the main themes across 13 repos".
- Cost: LLM-extracted graphs introduce extraction errors and indexing cost [I]; for ~10k records with hand-authored ids, extraction is unnecessary because the structure already exists in front matter [I].

### 1.3 Property graphs and query languages

ISO/IEC 39075:2024 (GQL) was published 12 April 2024 and targets property graphs; it is based on the Cypher/openCypher line; the standard itself is paid [E: SRC-KR-006, secondary sources/snippets].
- Strengths: edge properties (e.g. a `supersedes` edge with a date and reason) are first-class; path queries are concise [I].
- Costs: needs a database process or embedded engine; weak fit to git-as-source-of-truth unless the graph is rebuilt from files on each run [I].
- Evidence on LLM text-to-Cypher/SPARQL accuracy on small knowledge bases: not retrieved. [gap]

### 1.4 ER models

Entity-relationship modelling (Chen 1976) and relational schemas [B, not retrieved]. A relational/SQLite projection of records (tables for documents, links, statuses) gives cardinality, foreign keys and SQL queries. [I] For ~10k records this is the cheapest "queryable" form; it is a derived index, not necessarily the source of truth. No LLM-specific evidence retrieved for ER vs other forms except the SQL-vs-KG result above [E: SRC-KR-014], which indicates that raw SQL schemas without semantic context performed poorly in that benchmark.

### 1.5 Datalog, logic programming, Prolog, rule engines

Datalog is a database query language based on the logic-programming paradigm; the classic survey covers syntax, semantics, use for querying relational data and optimisation [E: SRC-KR-007, abstract via search]. Prolog and production-rule engines (Drools, CLIPS) [B].
- Strengths: transitive closure and "reachability" (e.g. full supersession chains, transitive trace coverage) are one-line rules; derivation is explainable [I].
- Costs: another language for contributors; rule engines add operational weight [I].
- LLM angle: Logic-LM pairs an LLM (formalising a problem) with a deterministic symbolic solver, with a self-refinement step that uses solver error messages; reported average gains of 39.2% over standard prompting and 18.4% over chain-of-thought on five logical-reasoning datasets [E: SRC-KR-018]. Caveat [I]: those are synthetic logical-reasoning benchmarks, not knowledge-base maintenance. The transferable idea is "deterministic checker returns machine-readable errors that the agent uses to repair its own output", which is exactly the role an F# validator would play.

### 1.6 Typed functional / algebraic data type (ADT) representations (the EKS proposal)

Records as discriminated unions and records with smart constructors, so illegal states (e.g. `Superseded` without a `supersededBy`) are unrepresentable and ids are distinct types [B/I]. I retrieved no peer-reviewed evidence on typed representations improving LLM agent performance. Closest retrieved evidence:
- Prompt/format choice changes LLM results: He et al. found GPT-3.5-turbo results swung by up to 40% on a code-translation task depending on template (plain text, Markdown, JSON, YAML); GPT-4-class models were more robust [E: SRC-KR-015]. Sclar et al. found up to 76 accuracy points of variation for LLaMA-2-13B from meaning-preserving formatting changes, persisting with scale and instruction tuning [E: SRC-KR-016].
- [I] These results concern formatting of prompts, not typed schemas; they justify testing the *serialisation the agent sees* as an independent variable, and warn that a single format comparison is fragile (Sclar recommends reporting a range across plausible formats).
- Key distinction [I]: "typed" can mean (a) a source-of-truth format that agents read/write, or (b) a validation layer behind Markdown. The literature on docs-as-code and requirements tools is entirely about (b)-like checking of text files. Hypotheses below separate these.

### 1.7 Event sourcing

Stores all changes as an ordered event log; state is derived by replay; benefits: audit, temporal queries, rebuilds, replay-based correction; costs: unfamiliar style, slow rebuilds without snapshots, difficulty handling side effects and code changes, hard to retrofit [E: SRC-KR-008].
- Fit: git already is an append-only event log of file changes [I]. For EKS, the question is whether *semantic* events ("Decision D-12 superseded by D-19 on date X by agent Y") add value over `git log` of Markdown diffs. [I] A cheap partial adoption: record status transitions as dated entries inside a record rather than adopting full event sourcing.

### 1.8 Semantic networks and ontologies

Semantic networks (Quillian, 1960s) are an older node-and-labelled-edge formalism, ancestors of KGs [B, not retrieved]. Ontologies in the OWL sense are covered in 1.1. [I] For EKS an ontology is simply the controlled vocabulary of record types and link types; its value is agreeing and enforcing it, not reasoning.

### 1.9 Formal specification languages (TLA+, Alloy, Z)

- **TLA+**: high-level language by Leslie Lamport for modelling programs and systems, especially concurrent/distributed; tools TLC (model checker) and TLAPS (proof system); used to find design errors early [E: SRC-KR-009]. AWS engineers reported using formal specification and model checking on 10 large real-world systems, with seven teams using TLA+ at the time of the 2015 article [E: SRC-KR-011, snippets].
- **Alloy**: relational logic (inspired by Z, Tarski's relational calculus and temporal logic) with an Analyzer that finds structures satisfying constraints and counterexamples [E: SRC-KR-010].
- **Z**: set-theory-based specification notation [B].
- Fit [I]: these specify *system behaviour or structure*, not an organisation's document corpus. Alloy could model the knowledge schema's invariants (e.g. supersession is acyclic) and be used once, at design time, to check the EKS schema itself; using them as the day-to-day representation for requirements/ADR prose is a poor fit. No evidence retrieved that formal specs improve LLM-agent task success.

### 1.10 Summary table (fit for a small, <=10k-record corpus read by LLM agents)

| Approach | Strength for EKS | Main cost | Retrieved LLM-specific evidence |
|---|---|---|---|
| RDF/JSON-LD + SHACL | Standard validation reports; interoperable ids | Second data model, tooling | KG-vs-SQL accuracy (SRC-KR-014); not vs Markdown |
| OWL | Formal semantics | Open-world awkwardness, reasoner weight | None for this size/use |
| Property graph/GQL | Rich edges, path queries | DB process, sync from files | None retrieved |
| Relational/ER | Cheap queries over derived index | Mapping layer | SQL baseline was weak without semantics (SRC-KR-014) |
| Datalog/Prolog | Transitive checks, explainable | New language | Logic-LM (SRC-KR-018) on synthetic reasoning |
| Typed ADTs (F#) | Illegal states unrepresentable; compile/validate-time errors | Authoring friction; agents must emit valid syntax | None direct; format sensitivity (SRC-KR-015/016) |
| Event sourcing | Audit, temporal queries | Complexity | None |
| TLA+/Alloy/Z | Design-time invariants | Wrong granularity | None |

## 2. Documentation and knowledge management

### 2.1 Markup languages and toolchains

- **Docs as code**: "writing documentation with the same tools as code": issue trackers, Git, plain-text markup, code review, automated tests [E: SRC-KR-039]. The page does not quantify benefits or discuss costs [E: SRC-KR-039]; any claim of ROI would be [I].
- **Markdown/MkDocs**: MkDocs builds a static site from Markdown with a single YAML config [E: SRC-KR-043].
- **MDX**: Markdown plus JSX components, compiled to JavaScript at build time [E: SRC-KR-042]. [I] Executable components inside docs are a liability for agent parsing unless the build is run.
- **reStructuredText / Sphinx**: Sphinx supports reStructuredText and MyST Markdown; cross-references to sections, figures, citations, glossary terms and code objects, within and across projects (intersphinx); extension mechanism [E: SRC-KR-044]. RST's directive/role extensibility is what makes sphinx-needs possible [I; directive use shown in SRC-KR-025].
- **AsciiDoc**: "defined by the Asciidoctor implementation" until a specification is ratified; an Eclipse AsciiDoc Language project is drafting one [E: SRC-KR-041].
- **DITA**: OASIS standard, topic-oriented information types with maps; DITA 1.3 Errata 02 dated 19 June 2018 [E: SRC-KR-040]. Specialization/conref details not retrieved.
- **DocBook**: XML schema; version 5.2 is an OASIS Standard dated 6 February 2024; RELAX NG normative, Schematron assertions required for conformance [E: SRC-KR-045].
- [I] DITA/DocBook offer schema validation but with XML authoring cost; for agent-authored small records the overhead is hard to justify. Markdown/RST with front matter is where the ecosystem and LLM training exposure lie (the latter is [B]/[I], not retrieved).

### 2.2 ADRs

- **Nygard (2011)**: short records, five parts (title with sequential number, context, decision, status, consequences); statuses proposed, accepted, deprecated/superseded; stored in the repo (`doc/arch/adr-NNN.md`), numbers never reused; the post argues that large documents are not kept up to date [E: SRC-KR-030].
- **MADR 4.0.0** (released 2024-09-17): Markdown template with optional YAML front matter fields `status`, `date`, `decision-makers`, `consulted`, `informed`; sections for context/problem, considered options, decision outcome, consequences, pros/cons, optional confirmation [E: SRC-KR-029].
- [I] The existing Echelon conventions (ids, status, supersedes) are an extension of exactly this lineage; ADR formats already contain a machine-readable status and a human-readable rationale. Typing them is a small step, but note neither ADR format defines a `supersedes` field in the retrieved pages (Nygard describes "references its replacement" in prose [E: SRC-KR-030]).

### 2.3 Requirements management and traceability tools (concrete investigation)

#### Doorstop [E: SRC-KR-019, 020, 021]
- Stores requirements as one file per item alongside source code in version control; items in a directory form a document; documents form a tree hierarchy; LGPLv3; Python 3.10+; the repository page showed about 697 stars, 175 forks and 2,853 commits on `develop` [E: SRC-KR-019].
- Item format: YAML, or Markdown with YAML front matter, mixable per document; UID is the filename sans extension (prefix + separator + number/name); fields include `active`, `derived`, `normative`, `level`, `header`, `text` (Markdown), `references` (array of file paths with optional keyword; missing referenced file causes an error unless disabled), `links`, `reviewed`; custom extended attributes allowed [E: SRC-KR-020].
- Change detection: `reviewed` stores a SHA256 hash (URL-safe Base64) of UID, `text`, `references` and link UIDs (plus configured extended attributes); each link stores the parent's fingerprint at last review; when a parent's fingerprint no longer matches, validation reports a suspect link; `doorstop review` and `doorstop clear` resolve them [E: SRC-KR-020, 021].
- Running `doorstop` with no arguments checks document-tree integrity; `doorstop publish all ./public` produces HTML [E: SRC-KR-019].
- Gaps relative to EKS [I]: no first-class claim/evidence/hypothesis types; hierarchy is a document tree with parent links (not arbitrary typed edges); status vocabularies are not enforced by a type system; no `supersedes` semantic (could be an extended attribute, unchecked unless configured).

#### OpenFastTrace (OFT) [E: SRC-KR-022, 023]
- Requirement tracing suite that shows whether specifications were implemented and helps find obsolete parts; Java 17+; GPL-3.0; CLI `trace`; Maven/Gradle plugins, JBang; about 210 stars, 1,406 commits on the repo page; version 4.10.0 appears in the README example [E: SRC-KR-022].
- Model: a specification item has an ID of artifact type, name and revision (e.g. `req~name~2`); optional title, status, description; `Covers` lists IDs it covers, `Depends` lists IDs that must be implemented first, `Needs` lists artifact types that must provide coverage; a revision bump marks existing coverage links as outdated; an item is covered when every needed type has at least one covering item; source code is tagged to mark coverage; custom artifact types allowed [E: SRC-KR-023, from search snippets of the project docs].
- [I] OFT's revision-in-ID mechanism is the cleanest existing solution to the "stale reference" problem in plain text: a link names the exact revision, so semantic change forces visible re-review. Its limit: it is coverage tracing between artifact *types*, not a general knowledge model; text lives in Markdown/RST (supported formats list not retrieved, so which formats it parses is unverified).

#### sphinx-needs [E: SRC-KR-024..028]
- Sphinx extension to define, link and analyse "needs" (requirements, specs, tests, features) in documentation, with custom types, import/export for sync with tools like Jira/GitHub, and use in safety-standard contexts (ISO 26262, DO-178B/C mentioned) [E: SRC-KR-024].
- Need directives (`.. req::` etc.) with `id` (must match `needs_id_regex` or the build fails; if omitted a hash of the title is generated), `status` (optionally restricted via `needs_statuses`), `tags`, `links`, custom typed link types, filter conditions on links (e.g. `REQ_001[status=="open"]`), custom fields [E: SRC-KR-025]. Hash-generated ids change when titles change [E: SRC-KR-025]; this is a known trap for stable identifiers [I].
- Validation: `needs_types` id_regex; `needs_links` `target_types` (link to a disallowed type gives a warning); schema validation based on JSON Schema with field types (string, boolean, integer, number and arrays), enums, patterns, length/range, required fields, link cardinality (`minItems/maxItems`), network rules over linked needs and back-links, nested up to depth 4, severities info/warning/violation, report in `schema_violations.json`; intended to replace legacy `needs_constraints`/`needs_warnings`; `-W` turns warnings into build failures [E: SRC-KR-026, 027].
- Export: `needs.json` containing all needs under a docs version, with a `needs_schema` describing each field's source; archiving the file in git gives history; other projects can import it (`needimport`, `needs_external_needs`) [E: SRC-KR-028]. Also a per-need JSON builder [E: SRC-KR-028].
- Gaps [I]: requires a Sphinx build; content is RST/MyST; a Python toolchain; the "type system" is configuration, not a programming-language type system; status transitions (state machines) are not mentioned in the pages retrieved; no first-class provenance model.

#### ReqIF [E: SRC-KR-031]
OMG Requirements Interchange Format, v1.2 (July 2016; 1.0.1 April 2011, 1.1 October 2013), for exchange between organisations that cannot share a repository; XML Schema files are provided (the XML nature is inferred from that) [E: SRC-KR-031]. [I] It is an interchange format for tool vendors, not a authoring format in git; relevant to EKS only as an export target if ever required.

### 2.4 Adversarial comparison and conclusions of section 2
See section 5.

## 3. Provenance, epistemic and argumentation models

### 3.1 W3C PROV
- PROV family: PROV-DM (data model), PROV-O (OWL), PROV-N, PROV-CONSTRAINTS are Recommendations; the Overview is a Working Group Note dated 30 April 2013 [E: SRC-KR-032].
- Core: Entity (physical, digital, conceptual thing with fixed aspects), Activity (occurs over time, acts on entities), Agent (bears responsibility); relations wasGeneratedBy, used, wasInformedBy, wasDerivedFrom, wasAttributedTo, wasAssociatedWith, actedOnBehalfOf; derivation subtypes Revision, Quotation, PrimarySource; alternateOf/specializationOf are listed but their definitions fell outside the portion I read [E: SRC-KR-033].
- [I] Mapping to EKS: record version = Entity; an agent session editing a record = Activity; the human or model = Agent; `supersedes` = a Revision-style derivation; "this report quotes that source" = Quotation; an agent-authored claim `wasAttributedTo` a model/session. PROV answers "who/what/when produced this", which is distinct from "is it true" (see below). A minimal PROV-inspired triple (agent, activity/session id, derived-from) is probably enough; full PROV-O is not justified at this scale [I].

### 3.2 Claims, evidence, hypotheses: argumentation models
- **Toulmin** (The Uses of Argument, 1958): claim, grounds, warrant, backing, qualifier, rebuttal [E: SRC-KR-037, secondary educational summaries]. Sources differ on which parts are essential [E: SRC-KR-037].
- **IBIS** (Kunz and Rittel, 1970): issues (questions), positions (candidate responses), arguments for/against positions; gIBIS (Conklin and Begeman, 1988) tool; Conklin's dialogue mapping (2005); Compendium implemented IBIS [E: SRC-KR-038, snippets]. [I] IBIS maps well onto ADRs: issue = problem statement, positions = considered options (MADR already has "Considered Options" and "Pros and Cons" [E: SRC-KR-029]), arguments = pros/cons, decision outcome = chosen position.
- **GSN / assurance cases**: graphical notation for goals, strategies, solutions (evidence) and context in a safety/assurance argument; originated at the University of York in the early 1990s; SCSC GSN Community Standard Version 3 issued May 2021; CC BY 4.0 [E: SRC-KR-036, snippets]. [I] The goal-strategy-evidence tree is the closest existing discipline to "claim vs evidence" with explicit support structure; its practice includes confidence arguments (listed among v3 changes [E: SRC-KR-036]).
- **Nanopublications**: three parts, assertion (atomic formally represented statement), provenance, and publication info; serialised with named graphs in RDF [E: SRC-KR-034, snippets].
- **Micropublications**: layered on full-text papers to represent claims, evidence, arguments, challenge and disagreement; OWL 2 vocabulary; compared to nanopublications, which model only the single statement [E: SRC-KR-035, snippets].
- [I] Shared lesson: separate (a) the assertion, (b) its evidentiary support, (c) its provenance, and (d) its challenges, and make each a first-class object with an identifier. In a Markdown research report these four are interleaved in prose; an agent cannot distinguish "measured" from "assumed". No retrieved source tests whether explicit claim/evidence typing improves *agent* decisions [gap]; that is exactly what EKS can test.

## 4. Evidence on whether any of this helps coding agents

- The only directly relevant retrieved study: Gloaguen et al. (Feb 2026) evaluated repository-level context files (AGENTS.md style) with four agents on SWE-bench Lite-derived tasks and a new benchmark (AgentBench). Per the ICLR abstract context files gave no improvement in task success and raised inference cost by over 20%; a secondary summary reports LLM-generated files lowering success by 0.5-2% and developer-written files giving about 4% gains; agents followed the instructions, causing more exploration/testing/reasoning; the authors recommend minimal requirements only [E: SRC-KR-017, snippets and secondary summaries; verify against PDF].
- Implication [I]: more knowledge in context is not automatically better; any EKS benefit must come from *precision* (correct, current, non-conflicting facts, retrieved selectively), and the cost side (tokens, extra exploration) must be measured as a first-class outcome.
- Format effects exist and can be large for weaker models, smaller for stronger ones [E: SRC-KR-015, 016]. [I] Because the target agents are strong current models, a null format effect on success is plausible; the benefit of typing may be in *write-time validation* rather than *read-time comprehension*.
- Gap: I found no study comparing Markdown+front matter vs typed schema as the knowledge substrate for coding agents. EKS would be producing novel evidence, not replicating it.

## 5. What existing tools already solve (adversarial)

Question: does sphinx-needs / Doorstop / OpenFastTrace already do what EKS proposes? EKS proposes (from the mission brief): typed (F#-validated) records with ids, status, supersedes, related_documents, validated mechanically, consumed by agents.

| EKS capability | Doorstop | OpenFastTrace | sphinx-needs | Verdict |
|---|---|---|---|---|
| Stable ids in text files in git | Yes (UID = filename) [E] | Yes (type~name~rev) [E] | Yes, regex-enforced; auto-hash ids fragile [E] | Solved |
| Typed links / link-target type checks | Parent links only, tree [E] | Covers/Depends/Needs [E] | Custom link types, target_types check [E] | Largely solved (sphinx-needs best) |
| Suspect/stale link detection | Yes, via fingerprints [E] | Yes, via revision bump [E] | Not found in retrieved pages (unknown) | Solved in 2 of 3 |
| Enumerated status / field validation | Not retrieved | Status field exists [E, snippet] | Yes, JSON-Schema enums, patterns, required [E] | Solved in sphinx-needs |
| Link cardinality, multi-hop rules | No evidence | Coverage rules [E] | Yes, depth up to 4 [E] | Solved in sphinx-needs |
| Machine-readable export | YAML files, REST/Python API [E] | CLI reports (formats not verified) | needs.json [E] | Solved |
| CI failure on violations | `doorstop` integrity check [E] | `trace` [E] | `-W` flag [E] | Solved |
| Code-to-requirement traceability | `references` to files [E] | Native code tags [E] | Via docs/extensions (not verified) | OFT strongest |
| Supersession semantics | Not built in [I] | Not built in [I] | Possible as custom link type [I] | Configurable, not native |
| Claim / evidence / hypothesis / confidence types | No [I] | No [I] | Could be custom types; no semantics [I] | Not solved |
| Provenance (agent/session/model attribution) | git only [I] | git only [I] | git only [I] | Not solved |
| Agent-oriented write path (agent emits valid record, gets structured errors) | CLI/Python API [E] | CLI [E] | Sphinx build + JSON report [E] | Partially; not designed for agents [I] |
| Language-level static types shared with the F# codebase | No | No | No | Not solved (and perhaps not needed) |

Adversarial conclusions [I, grounded in the table]:
1. For "ids + links + status validation + traceability in git text files + CI gate", the existing tools already solve most of it. sphinx-needs with JSON-Schema validation is the strongest baseline; Doorstop and OFT add change-detection mechanisms (fingerprint, revision-in-id) that EKS should copy, not reinvent.
2. The real delta of EKS is therefore narrow and must be argued on specific points: (a) first-class epistemic types (claim/evidence/hypothesis with status and provenance), (b) validation expressed in the same language/type system as Echelon's F# code, (c) an agent-facing read/write API with structured errors, (d) cross-repository (13 repos) scope, which none of the three tools target out of the box [I; cross-repo support exists in sphinx-needs via `needs_external_needs` importing needs.json [E: SRC-KR-028], so (d) is weaker than it first appears].
3. Costs the tools show: sphinx-needs and Doorstop impose a Python toolchain; OFT imposes Java; EKS would impose .NET. A pure-validator approach (keep Markdown, add schema validation in CI, e.g. via sphinx-needs-like rules or a small custom script) is the null hypothesis EKS must beat.
4. Because MADR/Nygard-style ADRs already have front matter, a lightweight JSON-Schema or SHACL-style check of the existing front matter would capture much of the value at minimal migration cost [I]. This is the strongest competitor to a typed re-representation.

## 6. Implications for EKS: falsifiable hypotheses

All thresholds below are proposals for pre-registration by the team, not claims from the literature. Each states what result would falsify it.

**H1 (existing-tool sufficiency; null).** A schema-validated Markdown+front-matter baseline (JSON-Schema or equivalent over existing fields, run in CI) detects at least as many seeded integrity defects (dangling ids, invalid status, missing supersession target, circular supersession, stale link after edit) as the F# typed representation. Falsified if the typed version catches a defect class the baseline cannot without custom code beyond N lines (team picks N in advance).

**H2 (defect rate in current corpus).** The current Markdown corpus contains a measurable rate of integrity defects (broken ids, inconsistent status vs supersedes, orphaned related_documents). Falsified if an audit of all ~13 repos finds near zero; then typing buys little. (Audit is cheap and informs everything else.) [Motivated by Nygard's claim that large documents go stale [E: SRC-KR-030]; no retrieved measurement for Echelon.]

**H3 (agent write-path correctness).** When agents author or edit records, the typed validator with structured errors yields a higher fraction of valid records after at most k repair attempts than Markdown with a linter producing text errors. Basis: Logic-LM's gain from solver-error self-refinement [E: SRC-KR-018], not directly transferable [I]. Falsified if validity after k attempts is statistically indistinguishable.

**H4 (agent read-path accuracy).** On a fixed question set (lookup, supersession-chain, "what is the current decision on X", traceability), agents answer more accurately given typed/structured serialisation than given the same content as Markdown. Basis: format sensitivity [E: SRC-KR-015, 016] and KG-context effect [E: SRC-KR-014], both weak analogues [I]. Falsified if the difference is within run-to-run noise across at least several serialisations (Sclar's recommendation to report a spread, not a single format [E: SRC-KR-016]). Expect to be falsified for frontier models [I].

**H5 (cost).** A typed/structured substrate reduces tokens and agent exploration steps per task relative to Markdown + AGENTS.md-style context. Basis: context files increased cost by over 20% in Gloaguen et al. [E: SRC-KR-017, unverified figures]. Falsified if token/step cost is equal or higher.

**H6 (staleness detection).** A content-fingerprint or revision-in-link mechanism (Doorstop/OFT style) catches stale downstream references that plain Markdown misses; measure on seeded edits to upstream records [mechanisms E: SRC-KR-020, 023]. Falsified if baseline reviewers/agents notice the same fraction without it.

**H7 (epistemic typing).** Representing claims, evidence and hypotheses as separate typed objects with status/confidence reduces agents' errors of treating a hypothesis or low-confidence finding as established fact, versus narrative research reports. Basis: argumentation/micropublication practice [E: SRC-KR-035, 036] with no agent-specific evidence [gap]. Falsified if misclassification rates (hypothesis treated as fact in agent outputs) do not differ.

**H8 (provenance sufficiency).** A minimal provenance triple (author agent/human, session/activity id, derived-from) answers "who asserted this and based on what" for at least a stated share of audit questions; full PROV is unnecessary [PROV mapping I]. Falsified if audit questions require relations outside the minimal triple.

**H9 (authoring friction).** Humans and agents can author typed records without a material increase in time or rejected commits compared with Markdown. Falsified if median authoring time or rejection rate exceeds a pre-set tolerance; also consider the maintenance cost of a bespoke toolchain versus adopting sphinx-needs [E: SRC-KR-027 shows available validation].

**H10 (scale).** At <=10k records, whole-corpus validation and graph derivation (e.g. loading to SQLite or in-memory) complete within a CI-acceptable time without a graph database. Basis: inference only [I]; no retrieved benchmark. Falsified by direct measurement.

Experiment-design cautions drawn from the literature [I]:
- Vary serialisation as a factor, test several models, report spreads [E basis: SRC-KR-016].
- Measure cost (tokens, steps) as well as success [E basis: SRC-KR-017].
- Include the "schema-validated Markdown" and "sphinx-needs" arms as baselines, not only raw Markdown.
- Separate read-path and write-path effects.

## 7. Gaps and unverified items

- No retrieved evidence on text-to-Cypher/SPARQL accuracy for small KBs, or on LLM comprehension of F#/ADT-style data vs Markdown.
- OFT supported input formats and release recency not verified; Doorstop and sphinx-needs release dates and licence of sphinx-needs not verified; GitHub API access was denied.
- Section 1.4, 1.5 (Prolog, rule engines), 1.8, Z notation and Chen's ER paper are background [B].
- SRC-KR-017 figures need verification against the PDF.
- Search snippets for Toulmin/IBIS are secondary sources; original works not retrieved.
