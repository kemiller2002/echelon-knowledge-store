namespace Echelon.KnowledgeStore

open System

/// Agent tasks for which a context package can be compiled.
type TaskKind =
    | Implement
    | ImpactAnalysis
    | EvidenceReview
    | DecisionHistory

type ItemRole =
    | Target
    | Dependency
    | SupportingEvidence
    | ContradictingEvidence
    | Supersession
    | Impacted
    | Neighbour

type Detail =
    | Full
    | Card

type ContextItem =
    {
        Key: string
        Role: ItemRole
        Detail: Detail
    }

/// A task-specific context package. Anything dropped for budget is listed in
/// `Omitted`: truncation is never silent.
type ContextPackage =
    {
        Task: TaskKind
        Target: string
        Items: ContextItem list
        Omitted: string list
        Text: string
    }

[<RequireQualifiedAccess>]
module Context =

    /// First paragraph of a body, at most `n` characters.
    let summary (n: int) (body: string) =
        body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
        |> Array.map _.Trim()
        |> Array.tryFind (fun p -> p.Length > 0 && not (p.StartsWith "#"))
        |> Option.map (fun p -> p.Replace('\n', ' '))
        |> Option.map (fun p -> if p.Length > n then p.Substring(0, n) + "…" else p)
        |> Option.defaultValue ""

    /// One-record compact card: type, id, title, status, typed links, location, summary.
    let card (g: KnowledgeGraph) (key: string) =
        match Map.tryFind key g.Records with
        | None -> $"[missing {key}]"
        | Some r ->
            let links =
                Graph.outgoing g key
                |> List.filter (fun e -> e.Kind <> Mentions && e.Kind <> RelatedTo)
                |> List.groupBy _.Kind
                |> List.sortBy (fst >> RelationKind.name)
                |> List.map (fun (k, es) ->
                    $"{RelationKind.name k} {String.Join(' ', es |> List.map _.Target |> List.distinct |> List.sort)}")

            let evidence =
                Graph.incoming g key
                |> List.filter (fun e -> e.Kind = Supports || e.Kind = Contradicts)
                |> List.groupBy _.Kind
                |> List.sortBy (fst >> RelationKind.name)
                |> List.map (fun (k, es) ->
                    let label = if k = Supports then "supported_by" else "contradicted_by"
                    $"{label} {String.Join(' ', es |> List.map _.Source |> List.distinct |> List.sort)}")

            let fields =
                [
                    yield! r.Status |> Option.map (sprintf "status %s") |> Option.toList
                    if r.Confidence <> Unstated then
                        yield $"confidence {Confidence.name r.Confidence}"
                    yield! links
                    yield! evidence
                ]

            let joined = String.Join(" | ", fields)
            $"[{KnowledgeKind.name r.Kind} {key}] {r.Title}\n  {joined}\n  source {r.Provenance.Repository}/{r.Provenance.Path}\n  {summary 280 r.Body}"

    let private full (g: KnowledgeGraph) (key: string) =
        match Map.tryFind key g.Records with
        | None -> $"[missing {key}]"
        | Some r -> card g key + "\n\n" + r.Body

    let private rank =
        function
        | Target -> 0
        | Dependency -> 1
        | SupportingEvidence
        | ContradictingEvidence -> 2
        | Supersession -> 3
        | Impacted -> 4
        | Neighbour -> 5

    /// Selects items for a task (before budgeting).
    let select (g: KnowledgeGraph) (task: TaskKind) (target: string) : ContextItem list =
        let item role detail key =
            {
                Key = key
                Role = role
                Detail = detail
            }

        let follow kinds k =
            Graph.outgoing g k
            |> List.filter (fun e -> List.contains e.Kind kinds)
            |> List.map _.Target

        let rec closure kinds depth frontier seen =
            if depth = 0 || List.isEmpty frontier then
                seen
            else
                let next =
                    frontier
                    |> List.collect (follow kinds)
                    |> List.distinct
                    |> List.filter (fun k -> not (List.contains k seen))

                closure kinds (depth - 1) next (seen @ next)

        let evidence detail =
            Graph.incoming g target
            |> List.choose (fun e ->
                match e.Kind with
                | Supports -> Some(item SupportingEvidence detail e.Source)
                | Contradicts -> Some(item ContradictingEvidence detail e.Source)
                | _ -> None)

        let supersession =
            closure [ Supersedes; SupersededBy ] 10 [ target ] []
            @ (Graph.incoming g target
               |> List.filter (fun e -> e.Kind = Supersedes || e.Kind = SupersededBy)
               |> List.map _.Source)
            |> List.distinct
            |> List.filter ((<>) target)
            |> List.map (item Supersession Card)

        let items =
            match task with
            | Implement ->
                [ item Target Full target ]
                @ (closure [ DependsOn; DerivedFrom ] 3 [ target ] []
                   |> List.map (item Dependency Card))
                @ evidence Card
                @ supersession
            | EvidenceReview -> [ item Target Full target ] @ evidence Full @ supersession
            | DecisionHistory ->
                [ item Target Full target ]
                @ (supersession |> List.map (fun i -> { i with Detail = Full }))
            | ImpactAnalysis ->
                [ item Target Card target ]
                @ (Graph.impactOf g 6 target
                   |> Map.toList
                   |> List.sortBy snd
                   |> List.map (fst >> item Impacted Card))

        items |> List.distinctBy _.Key |> List.sortBy (_.Role >> rank)

    /// Compiles a package within `budget` tokens (counted by `count`). Lowest-priority
    /// items are first reduced to cards, then dropped and listed as omitted.
    let compile (count: string -> int) (budget: int) (g: KnowledgeGraph) (task: TaskKind) (target: string) =
        let render (i: ContextItem) =
            match i.Detail with
            | Full -> full g i.Key
            | Card -> card g i.Key

        let text items omitted =
            let header = $"# Context: {task} {target}\n"

            let omittedNote =
                match omitted with
                | [] -> ""
                | xs ->
                    let names = String.concat " " xs
                    $"\n\nOMITTED (budget): {names}"

            header + String.concat "\n\n" (items |> List.map render) + omittedNote

        let rec fit (items: ContextItem list) omitted =
            let t = text items omitted

            if count t <= budget || List.length items <= 1 then
                {
                    Task = task
                    Target = target
                    Items = items
                    Omitted = omitted
                    Text = t
                }
            else
                match
                    items
                    |> List.rev
                    |> List.tryFindIndex (fun i -> i.Detail = Full && i.Role <> Target)
                with
                | Some ri ->
                    let idx = items.Length - 1 - ri
                    fit (items |> List.mapi (fun j i -> if j = idx then { i with Detail = Card } else i)) omitted
                | None ->
                    let last = List.last items
                    fit (items |> List.take (items.Length - 1)) (last.Key :: omitted)

        fit (select g task target) []
