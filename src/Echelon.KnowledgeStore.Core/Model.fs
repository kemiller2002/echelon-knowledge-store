namespace Echelon.KnowledgeStore

open System
open System.Text.RegularExpressions

/// Record categories found in Echelon knowledge (validated against the corpus,
/// see research/2026-knowledge-representation/architecture/knowledge-taxonomy.md).
type KnowledgeKind =
    | Requirement
    | Decision
    | Hypothesis
    | Evidence
    | Experiment
    | Theory
    | ResearchPackage
    | Journal
    | ResearchQuestion
    | Governance
    | Method
    | Verification
    | Document
    | Other of string

/// Typed relationships between records.
type RelationKind =
    | Supports
    | Contradicts
    | DependsOn
    | Supersedes
    | SupersededBy
    | DerivedFrom
    | RelatedTo
    | Mentions

type Relation = { Kind: RelationKind; Target: string }

/// Qualitative confidence. Numeric scores are not used: the corpus records
/// confidence as words and nothing calibrates a number.
type Confidence =
    | VeryLow
    | Low
    | Medium
    | High
    | VeryHigh
    | Unstated

type Provenance =
    {
        Repository: string
        Path: string
        Author: string option
        SourceUri: string option
        Created: DateOnly option
        Updated: DateOnly option
        Retrieved: DateOnly option
    }

/// A knowledge record. `Extra` keeps unrecognised front-matter fields so that
/// conversion to and from Markdown is lossless.
type KnowledgeRecord =
    {
        Id: string
        Kind: KnowledgeKind
        Title: string
        Status: string option
        Confidence: Confidence
        Relations: Relation list
        Provenance: Provenance
        Body: string
        Extra: (string * Yaml) list
    }

[<RequireQualifiedAccess>]
module KnowledgeKind =

    let private byDirectory =
        [
            "hypotheses", Hypothesis
            "evidence", Evidence
            "experiments", Experiment
            "theories", Theory
            "packages", ResearchPackage
            "journals", Journal
            "journal", Journal
            "requirements", Requirement
            "decisions", Decision
            "verification", Verification
            "methods", Method
            "00-governance", Governance
        ]

    let private byPrefix =
        [
            "HY", Hypothesis
            "EV", Evidence
            "EX", Experiment
            "TH", Theory
            "RP", ResearchPackage
            "JR", Journal
            "RQ", Requirement
            "DF", Decision
            "ADR", Decision
            "GV", Governance
            "SDE-METHOD", Method
            "SDE-DOCTRINE", Governance
            "VR", Verification
            "REQ", Requirement
            "DEC", Decision
        ]

    /// Infers the kind from the path's directories, then the id prefix.
    let infer (path: string) (id: string) =
        let dirs = path.Split('/') |> Array.rev |> Array.skip 1 |> List.ofArray

        dirs
        |> List.tryPick (fun d -> byDirectory |> List.tryFind (fst >> (=) d) |> Option.map snd)
        |> Option.orElse (
            byPrefix
            |> List.sortByDescending (fst >> String.length)
            |> List.tryFind (fun (p, _) -> id.StartsWith(p + "-"))
            |> Option.map snd
        )
        |> Option.defaultValue Document

    let name =
        function
        | Other s -> s
        | k -> (sprintf "%A" k).ToLowerInvariant()

[<RequireQualifiedAccess>]
module RelationKind =

    let name =
        function
        | Supports -> "supports"
        | Contradicts -> "contradicts"
        | DependsOn -> "depends_on"
        | Supersedes -> "supersedes"
        | SupersededBy -> "superseded_by"
        | DerivedFrom -> "derived_from"
        | RelatedTo -> "related_documents"
        | Mentions -> "mentions"

    /// Front-matter fields that carry relations, and the relation each encodes.
    /// `supporting_evidence: [E]` on X means E supports X: stored as an inverse edge.
    let fields =
        [
            "supports", Supports, false
            "contradicts", Contradicts, false
            "depends_on", DependsOn, false
            "supersedes", Supersedes, false
            "superseded_by", SupersededBy, false
            "derived_from", DerivedFrom, false
            "related_documents", RelatedTo, false
            "related_theories", RelatedTo, false
            "supporting_evidence", Supports, true
            "contradicting_evidence", Contradicts, true
        ]

    /// Direction in which a change propagates along a relation `source -kind-> target`.
    /// `X depends_on Y`: a change to Y requires review of X (towards the source).
    /// `E supports C`: a change to E requires review of C (towards the target).
    type Propagation =
        | ToSource
        | ToTarget
        | NoPropagation

    let propagation =
        function
        | DependsOn
        | DerivedFrom
        | Mentions -> ToSource
        | Supports
        | Contradicts -> ToTarget
        | Supersedes
        | SupersededBy
        | RelatedTo -> NoPropagation

[<RequireQualifiedAccess>]
module Confidence =

    let parse (s: string option) =
        match s |> Option.map (fun s -> s.Trim().ToLowerInvariant()) with
        | Some "very-low"
        | Some "very low" -> VeryLow
        | Some "low" -> Low
        | Some "medium"
        | Some "moderate" -> Medium
        | Some "high" -> High
        | Some "very-high"
        | Some "very high" -> VeryHigh
        | _ -> Unstated

    let name =
        function
        | VeryLow -> "very-low"
        | Low -> "low"
        | Medium -> "medium"
        | High -> "high"
        | VeryHigh -> "very-high"
        | Unstated -> ""

[<RequireQualifiedAccess>]
module KnowledgeRecord =

    /// Identifier-shaped tokens such as `HY-FE-2026-B006`, `RQ-ROS-2026-A030`, `PRX-QUAL-001`.
    let idPattern =
        Regex(@"\b[A-Z][A-Z0-9]{1,9}(?:-[A-Z0-9]{1,12}){0,4}-(?:[A-Z]?\d{2,4}[A-Z]?)\b", RegexOptions.Compiled)

    let private recognised =
        [
            "id"
            "title"
            "status"
            "confidence"
            "author_agent"
            "created_by_agent"
            "source_uri"
            "created"
            "updated"
            "retrieved"
        ]
        @ (RelationKind.fields |> List.map (fun (f, _, _) -> f))

    let private date (s: string option) =
        s
        |> Option.bind (fun s ->
            match DateOnly.TryParse(s.Trim().Substring(0, min 10 (s.Trim().Length))) with
            | true, d -> Some d
            | _ -> None)

    /// Body mentions of other identifiers (implicit, untyped links).
    let mentions (selfId: string) (body: string) =
        idPattern.Matches body
        |> Seq.map _.Value
        |> Seq.filter ((<>) selfId)
        |> Seq.distinct
        |> Seq.map (fun t -> { Kind = Mentions; Target = t })
        |> List.ofSeq

    /// Builds a record from a parsed Markdown document. Requires an `id` field.
    let ofDocument (repository: string) (path: string) (doc: MarkdownDocument) : Result<KnowledgeRecord, string> =
        match doc.FrontMatter with
        | None -> Error "no front matter"
        | Some fm ->
            let str k =
                Yaml.field k fm |> Option.bind Yaml.text

            let list k =
                Yaml.field k fm |> Option.map Yaml.strings |> Option.defaultValue []

            match str "id" with
            | None -> Error "front matter has no id"
            | Some id ->
                let declared =
                    RelationKind.fields
                    |> List.filter (fun (_, _, inverse) -> not inverse)
                    |> List.collect (fun (field, kind, _) ->
                        list field |> List.map (fun t -> { Kind = kind; Target = t.Trim() }))

                let fields =
                    match fm with
                    | YMap kvs -> kvs
                    | _ -> []

                Ok
                    {
                        Id = id.Trim()
                        Kind = KnowledgeKind.infer path id
                        Title = str "title" |> Option.defaultValue ""
                        Status = str "status"
                        Confidence = Confidence.parse (str "confidence")
                        Relations = declared
                        Provenance =
                            {
                                Repository = repository
                                Path = path
                                Author = str "author_agent" |> Option.orElse (str "created_by_agent")
                                SourceUri = str "source_uri"
                                Created = date (str "created")
                                Updated = date (str "updated")
                                Retrieved = date (str "retrieved")
                            }
                        Body = doc.Body.Trim('\n').TrimEnd()
                        Extra =
                            fields
                            |> List.filter (fun (k, _) -> not (List.contains k recognised))
                            |> List.append (
                                [ "supporting_evidence"; "contradicting_evidence" ]
                                |> List.choose (fun k -> Yaml.field k fm |> Option.map (fun v -> k, v))
                            )
                    }

    /// Inverse relations implied by `supporting_evidence` / `contradicting_evidence`
    /// on a record: each listed evidence id supports (contradicts) the record.
    let inverseRelations (r: KnowledgeRecord) =
        RelationKind.fields
        |> List.filter (fun (_, _, inverse) -> inverse)
        |> List.collect (fun (field, kind, _) ->
            r.Extra
            |> List.tryFind (fst >> (=) field)
            |> Option.map (snd >> Yaml.strings)
            |> Option.defaultValue []
            |> List.map (fun source -> source.Trim(), { Kind = kind; Target = r.Id }))

    /// Explicit plus implicit (mention) relations.
    let allRelations (r: KnowledgeRecord) = r.Relations @ mentions r.Id r.Body
