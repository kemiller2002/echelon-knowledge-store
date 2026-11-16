namespace Echelon.KnowledgeStore

/// A directed, typed edge between record keys (`repository:id`).
type Edge =
    {
        Source: string
        Kind: RelationKind
        Target: string
    }

/// How a raw relation target was resolved.
type Resolution =
    | Resolved of key: string
    | Ambiguous of keys: string list
    | Unresolved of target: string

/// Immutable knowledge graph with forward and reverse adjacency.
type KnowledgeGraph =
    {
        Records: Map<string, KnowledgeRecord>
        Edges: Edge list
        Unresolved: (string * Relation) list
        Outgoing: Map<string, Edge list>
        Incoming: Map<string, Edge list>
    }

[<RequireQualifiedAccess>]
module Graph =

    /// The unique key of a record: `repository:id`.
    let key (r: KnowledgeRecord) = $"{r.Provenance.Repository}:{r.Id}"

    let private normalisePath (p: string) =
        p.Replace("\\", "/").TrimStart('.', '/').ToLowerInvariant()

    /// Resolves a relation target from `fromRepo`: same-repository id, then a
    /// unique id elsewhere, then a path suffix match.
    let resolve
        (byId: Map<string, string list>)
        (byPath: Map<string, string list>)
        (fromRepo: string)
        (target: string)
        =
        let t = target.Trim()

        let qualifiedRepo, local =
            match t.Split(':') with
            | [| repo; local |] when not (repo.Contains "/") -> Some repo, local
            | _ -> None, t

        let candidates = byId |> Map.tryFind local |> Option.defaultValue []

        let sameRepo =
            candidates
            |> List.filter (fun k -> k.StartsWith((defaultArg qualifiedRepo fromRepo) + ":"))

        match sameRepo, candidates with
        | [ k ], _ -> Resolved k
        | _, [ k ] when qualifiedRepo.IsNone -> Resolved k
        | _, (_ :: _ :: _ as ks) -> Ambiguous ks
        | _ ->
            let p = normalisePath t

            byPath
            |> Map.toList
            |> List.filter (fun (path, _) -> p.Length > 3 && (path.EndsWith p || p.EndsWith path))
            |> List.collect snd
            |> List.filter (fun k -> k.StartsWith(fromRepo + ":"))
            |> function
                | [ k ] -> Resolved k
                | [] -> Unresolved t
                | ks -> Ambiguous ks

    /// Builds the graph. `includeMentions` adds untyped body-mention edges.
    let build (includeMentions: bool) (records: KnowledgeRecord list) : KnowledgeGraph =
        let keyed = records |> List.map (fun r -> key r, r)

        let index f =
            keyed
            |> List.groupBy (snd >> f)
            |> List.map (fun (k, rs) -> k, rs |> List.map fst |> List.distinct)
            |> Map.ofList

        let byId = index _.Id
        let byPath = index (fun r -> normalisePath r.Provenance.Path)

        let relationsOf (k, r: KnowledgeRecord) =
            let explicitRelations =
                (if includeMentions then
                     KnowledgeRecord.allRelations r
                 else
                     r.Relations)
                |> List.map (fun rel -> k, r.Provenance.Repository, rel)

            let inverse =
                KnowledgeRecord.inverseRelations r
                |> List.map (fun (source, rel) -> source, k, r.Provenance.Repository, rel)

            explicitRelations, inverse

        let pairs = keyed |> List.map relationsOf
        let resolveIn repo target = resolve byId byPath repo target

        let direct =
            pairs
            |> List.collect fst
            |> List.map (fun (src, repo, rel) -> src, rel, resolveIn repo rel.Target)

        let inverse =
            pairs
            |> List.collect snd
            |> List.map (fun (source, targetKey, repo, rel) ->
                match resolveIn repo source with
                | Resolved s -> s, { rel with Target = targetKey }, Resolved targetKey
                | other -> targetKey, { rel with Target = source }, other)

        let all = direct @ inverse

        let edges =
            all
            |> List.choose (fun (src, rel, res) ->
                match res with
                | Resolved t when t <> src ->
                    Some
                        {
                            Source = src
                            Kind = rel.Kind
                            Target = t
                        }
                | _ -> None)
            |> List.distinct

        let unresolved =
            all
            |> List.choose (fun (src, rel, res) ->
                match res with
                | Unresolved _
                | Ambiguous _ -> Some(src, rel)
                | Resolved _ -> None)

        let group f = edges |> List.groupBy f |> Map.ofList

        {
            Records = Map.ofList keyed
            Edges = edges
            Unresolved = unresolved
            Outgoing = group _.Source
            Incoming = group _.Target
        }

    let outgoing (g: KnowledgeGraph) k =
        g.Outgoing |> Map.tryFind k |> Option.defaultValue []

    let incoming (g: KnowledgeGraph) k =
        g.Incoming |> Map.tryFind k |> Option.defaultValue []

    /// Records directly affected by a change to `k`, following each relation's
    /// propagation direction. `include` filters the relation kinds considered.
    let affected (g: KnowledgeGraph) (include': RelationKind -> bool) (k: string) =
        let fromIncoming =
            incoming g k
            |> List.filter (fun e -> include' e.Kind && RelationKind.propagation e.Kind = RelationKind.ToSource)
            |> List.map _.Source

        let fromOutgoing =
            outgoing g k
            |> List.filter (fun e -> include' e.Kind && RelationKind.propagation e.Kind = RelationKind.ToTarget)
            |> List.map _.Target

        fromIncoming @ fromOutgoing |> List.distinct

    /// Records that must be reviewed when `start` changes: transitive closure of
    /// `affected`, with the hop distance of each. Excludes `start`. Bounded by `maxDepth`.
    let impactWith
        (include': RelationKind -> bool)
        (g: KnowledgeGraph)
        (maxDepth: int)
        (start: string)
        : Map<string, int> =
        let rec bfs frontier depth seen =
            if List.isEmpty frontier || depth > maxDepth then
                seen
            else
                let next =
                    frontier
                    |> List.collect (affected g include')
                    |> List.distinct
                    |> List.filter (fun k -> k <> start && not (Map.containsKey k seen))

                bfs next (depth + 1) (next |> List.fold (fun m k -> Map.add k depth m) seen)

        bfs [ start ] 1 Map.empty

    /// Impact over typed relations only (body mentions excluded).
    let impactOf (g: KnowledgeGraph) (maxDepth: int) (start: string) =
        impactWith (fun k -> k <> Mentions) g maxDepth start

    /// Undirected neighbourhood up to `depth`, including `start`.
    let neighbourhood (g: KnowledgeGraph) (depth: int) (start: string) : Set<string> =
        let step k =
            (outgoing g k |> List.map _.Target) @ (incoming g k |> List.map _.Source)

        let rec go frontier d seen =
            if d = 0 || Set.isEmpty frontier then
                seen
            else
                let next =
                    frontier |> Seq.collect step |> Set.ofSeq |> fun s -> Set.difference s seen

                go next (d - 1) (Set.union seen next)

        go (Set.singleton start) depth (Set.singleton start)
