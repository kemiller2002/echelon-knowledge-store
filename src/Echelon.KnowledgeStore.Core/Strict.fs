namespace Echelon.KnowledgeStore

open System
open System.Text.RegularExpressions

/// A list with at least one element.
type NonEmpty<'a> = private NonEmpty of 'a * 'a list

[<RequireQualifiedAccess>]
module NonEmpty =
    let ofList =
        function
        | [] -> None
        | x :: xs -> Some(NonEmpty(x, xs))

    let toList (NonEmpty(x, xs)) = x :: xs

/// Lifecycle of normative records. A superseded record always names its replacement.
type Lifecycle =
    | Active of status: string
    | Superseded of replacements: NonEmpty<string>
    | Retired of status: string

/// Epistemic standing of a claim, derived from evidence links. `Supported`
/// cannot exist without evidence; `Contested` needs both directions.
type Standing =
    | Unassessed
    | Supported of supporting: NonEmpty<string>
    | Contested of supporting: NonEmpty<string> * contradicting: NonEmpty<string>
    | Refuted of contradicting: NonEmpty<string>

/// Where evidence came from: a structured source or linked records.
type EvidenceSource =
    | External of uriOrTitle: string
    | Linked of NonEmpty<string>

/// The strict model: per-kind types that make the single-record invariants
/// unrepresentable. Corpus-wide invariants (references exist, no cycles,
/// symmetric links) stay in `Validation`.
type StrictRecord =
    | StrictRequirement of id: string * title: string * acceptance: NonEmpty<string> * Lifecycle
    | StrictDecision of id: string * title: string * grounds: NonEmpty<string> * Lifecycle
    | StrictHypothesis of id: string * title: string * Standing * Confidence * declaredStatus: string
    | StrictEvidence of id: string * title: string * source: EvidenceSource * observed: DateOnly option
    | StrictExperiment of id: string * title: string * hypotheses: NonEmpty<string> * outcome: string option
    | StrictOther of id: string * kind: KnowledgeKind * title: string

[<RequireQualifiedAccess>]
module Strict =

    let private acceptanceHeading =
        Regex(@"^(#{1,6}\s*|\*\*)?Acceptance( criteria)?\b.*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)

    /// Acceptance criteria: list items following an "Acceptance" heading or label.
    let acceptanceCriteria (body: string) =
        let lines = body.Split('\n')

        lines
        |> Array.tryFindIndex acceptanceHeading.IsMatch
        |> Option.map (fun i ->
            lines
            |> Array.skip (i + 1)
            |> Array.skipWhile String.IsNullOrWhiteSpace
            |> Array.takeWhile (fun l -> not (l.StartsWith "#"))
            |> Array.map _.Trim()
            |> Array.filter (fun l -> l.StartsWith "- " || l.StartsWith "* " || Regex.IsMatch(l, @"^\d+\. "))
            |> Array.map (fun l -> Regex.Replace(l, @"^(- |\* |\d+\. )", ""))
            |> List.ofArray)
        |> Option.defaultValue []

    let private targets kinds (r: KnowledgeRecord) =
        r.Relations
        |> List.filter (fun x -> List.contains x.Kind kinds)
        |> List.map _.Target

    let private lifecycle (r: KnowledgeRecord) =
        let status = (defaultArg r.Status "").Trim().ToLowerInvariant()

        match status, NonEmpty.ofList (targets [ SupersededBy ] r) with
        | "superseded", Some reps -> Ok(Superseded reps)
        | "superseded", None -> Error "superseded without replacement"
        | ("deprecated" | "retired" | "rejected" | "withdrawn"), _ -> Ok(Retired status)
        | "", _ -> Error "no status"
        | s, _ -> Ok(Active s)

    /// Classifies a record into the strict model. `evidence` gives the record's
    /// (supporting, contradicting) evidence ids from the graph (both link directions).
    let classify (evidence: string list * string list) (r: KnowledgeRecord) : Result<StrictRecord, string list> =
        let supporting, contradicting = evidence

        match r.Kind with
        | Requirement ->
            match NonEmpty.ofList (acceptanceCriteria r.Body), lifecycle r with
            | Some ac, Ok lc -> Ok(StrictRequirement(r.Id, r.Title, ac, lc))
            | ac, lc ->
                Error(
                    [
                        if ac.IsNone then
                            "requirement without acceptance criteria"
                        match lc with
                        | Error e -> e
                        | Ok _ -> ()
                    ]
                )
        | Decision ->
            let grounds = targets [ Supports; DerivedFrom; DependsOn; RelatedTo ] r @ supporting

            let hasRationale =
                Regex.IsMatch(
                    r.Body,
                    @"^#{1,6}\s*(Rationale|Context|Why|Reasons?|Consequences)\b",
                    RegexOptions.Multiline ||| RegexOptions.IgnoreCase
                )

            let grounds =
                if hasRationale then
                    "rationale-section" :: grounds
                else
                    grounds

            match NonEmpty.ofList grounds, lifecycle r with
            | Some g, Ok lc -> Ok(StrictDecision(r.Id, r.Title, g, lc))
            | g, lc ->
                Error(
                    [
                        if g.IsNone then
                            "decision without grounds (no rationale section, evidence or links)"
                        match lc with
                        | Error e -> e
                        | Ok _ -> ()
                    ]
                )
        | Hypothesis
        | Theory ->
            let standing =
                match NonEmpty.ofList supporting, NonEmpty.ofList contradicting with
                | Some s, Some c -> Contested(s, c)
                | Some s, None -> Supported s
                | None, Some c -> Refuted c
                | None, None -> Unassessed

            let status = (defaultArg r.Status "").Trim().ToLowerInvariant()

            let claimsSupport =
                List.contains status [ "supported"; "accepted"; "confirmed"; "validated" ]

            match standing, claimsSupport with
            | (Unassessed | Refuted _), true ->
                Error [ $"status '{status}' asserts support that the evidence does not give" ]
            | _ when status = "" -> Error [ "no status" ]
            | s, _ -> Ok(StrictHypothesis(r.Id, r.Title, s, r.Confidence, status))
        | Evidence ->
            let structured =
                r.Provenance.SourceUri
                |> Option.orElse (
                    r.Extra
                    |> List.tryFind (fst >> (=) "source_title")
                    |> Option.bind (snd >> Yaml.text)
                )

            match structured, NonEmpty.ofList (targets [ DerivedFrom; RelatedTo ] r) with
            | Some s, _ -> Ok(StrictEvidence(r.Id, r.Title, External s, r.Provenance.Retrieved))
            | None, Some links -> Ok(StrictEvidence(r.Id, r.Title, Linked links, r.Provenance.Retrieved))
            | None, None -> Error [ "evidence without any source" ]
        | Experiment ->
            let hyps =
                (r.Relations |> List.map _.Target)
                @ (KnowledgeRecord.mentions r.Id r.Body |> List.map _.Target)
                |> List.filter (fun t -> t.StartsWith "HY-")
                |> List.distinct

            let outcome =
                Regex.Match(
                    r.Body,
                    @"^#{1,6}\s*(Result|Results|Outcome|Conclusion|Findings)\b",
                    RegexOptions.Multiline ||| RegexOptions.IgnoreCase
                )
                |> fun m -> if m.Success then Some m.Value else None

            match NonEmpty.ofList hyps with
            | Some h -> Ok(StrictExperiment(r.Id, r.Title, h, outcome))
            | None -> Error [ "experiment without a hypothesis" ]
        | k -> Ok(StrictOther(r.Id, k, r.Title))

    /// Supporting and contradicting evidence keys for a record, from the graph.
    let evidenceFor (g: KnowledgeGraph) (key: string) =
        let ofKind k =
            Graph.incoming g key
            |> List.filter (fun e -> e.Kind = k)
            |> List.map _.Source
            |> List.distinct

        ofKind Supports, ofKind Contradicts
