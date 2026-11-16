module Echelon.KnowledgeStore.Bench.RetrievalBench

open System
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Bench.Corpus

/// A benchmark task with a gold answer set of record keys.
type Task =
    {
        Id: string
        Kind: string
        Target: string
        Query: string
        Gold: Set<string>
    }

/// What a strategy returned for a task.
type Outcome =
    {
        Found: Set<string>
        Candidates: int
        Tokens: int
        ToolCalls: int
    }

type Env =
    {
        Graph: KnowledgeGraph
        Files: SourceFile list
        FileTokens: Map<string * string, int>
        KeyOfFile: Map<string * string, string>
        FileOfKey: Map<string, string * string>
        Index: Bm25Index
        Count: string -> int
    }

let env (count: string -> int) (files: SourceFile list) (records: KnowledgeRecord list) =
    let g = Graph.build false records

    let chunks =
        records
        |> List.collect (fun r -> Retrieval.sections (Graph.key r) r.Title (r.Body))

    {
        Graph = g
        Files = files
        FileTokens = files |> List.map (fun f -> (f.Repository, f.Path), count f.Text) |> Map.ofList
        KeyOfFile =
            records
            |> List.map (fun r -> (r.Provenance.Repository, r.Provenance.Path), Graph.key r)
            |> Map.ofList
        FileOfKey =
            records
            |> List.map (fun r -> Graph.key r, (r.Provenance.Repository, r.Provenance.Path))
            |> Map.ofList
        Index = Retrieval.build chunks
        Count = count
    }

let private idOf (e: Env) (key: string) = e.Graph.Records[key].Id

// ---------------- Task generation (gold from authored typed metadata) ----------------

let tasks (e: Env) : Task list =
    let g = e.Graph
    let keys = g.Records |> Map.keys |> List.ofSeq |> List.sort

    let lookup =
        keys
        |> List.filter (fun k -> g.Records[k].Title <> "")
        |> List.indexed
        |> List.filter (fun (i, _) -> i % 10 = 0)
        |> List.map (fun (_, k) ->
            {
                Id = $"lookup:{k}"
                Kind = "lookup-by-id"
                Target = k
                Query = idOf e k
                Gold = Set.singleton k
            })

    let evidence =
        keys
        |> List.choose (fun k ->
            let ev =
                Graph.incoming g k
                |> List.filter (fun x -> x.Kind = Supports || x.Kind = Contradicts)
                |> List.map _.Source
                |> Set.ofList

            if ev.IsEmpty then
                None
            else
                Some
                    {
                        Id = $"evidence:{k}"
                        Kind = "find-evidence"
                        Target = k
                        Query = idOf e k
                        Gold = ev
                    })

    let impact =
        keys
        |> List.choose (fun k ->
            let imp = Graph.impactOf g 6 k |> Map.keys |> Set.ofSeq

            if imp.Count >= 2 then
                Some
                    {
                        Id = $"impact:{k}"
                        Kind = "change-impact"
                        Target = k
                        Query = idOf e k
                        Gold = imp
                    }
            else
                None)

    let supersession =
        keys
        |> List.choose (fun k ->
            let rec latest seen k =
                match
                    Graph.outgoing g k
                    |> List.filter (fun x -> x.Kind = SupersededBy)
                    |> List.map _.Target
                with
                | next :: _ when not (Set.contains next seen) -> latest (Set.add next seen) next
                | _ ->
                    match
                        Graph.incoming g k
                        |> List.filter (fun x -> x.Kind = Supersedes)
                        |> List.map _.Source
                    with
                    | next :: _ when not (Set.contains next seen) -> latest (Set.add next seen) next
                    | _ -> k

            let current = latest (Set.singleton k) k

            if current <> k then
                Some
                    {
                        Id = $"current:{k}"
                        Kind = "current-authority"
                        Target = k
                        Query = idOf e k
                        Gold = Set.singleton current
                    }
            else
                None)

    let byTitle =
        lookup
        |> List.map (fun t ->
            { t with
                Id = $"title:{t.Target}"
                Kind = "lookup-by-description"
                Query = g.Records[t.Target].Title
            })

    lookup @ byTitle @ evidence @ impact @ supersession

// ---------------- Strategies ----------------

let private fileTokens (e: Env) f =
    e.FileTokens |> Map.tryFind f |> Option.defaultValue 0

/// Grep the raw Markdown for the id, read every matching file whole, and chase
/// newly found ids up to `depth` times (manual reference search).
let grepWholeFiles (depth: int) (e: Env) (t: Task) : Outcome =
    let grep (id: string) =
        e.Files
        |> List.filter (fun f -> f.Text.Contains(id, StringComparison.Ordinal))
        |> List.map (fun f -> f.Repository, f.Path)

    let rec go d frontier (readFiles: Set<string * string>) calls =
        if d = 0 || List.isEmpty frontier then
            readFiles, calls
        else
            let hits =
                frontier
                |> List.collect grep
                |> List.distinct
                |> List.filter (readFiles.Contains >> not)

            let next =
                hits |> List.choose (fun f -> e.KeyOfFile |> Map.tryFind f) |> List.map (idOf e)

            go (d - 1) next (Set.union readFiles (Set.ofList hits)) (calls + frontier.Length + hits.Length)

    let target = e.FileOfKey |> Map.tryFind t.Target |> Option.toList |> Set.ofList
    let files, calls = go depth [ t.Query ] target 0
    let keys = files |> Seq.choose (fun f -> e.KeyOfFile |> Map.tryFind f) |> Set.ofSeq

    {
        Found = keys
        Candidates = files.Count
        Tokens = files |> Seq.sumBy (fileTokens e)
        ToolCalls = calls + 1
    }

/// Read only the target's own file and use the links it declares.
let targetFileOnly (e: Env) (t: Task) : Outcome =
    let r = e.Graph.Records[t.Target]

    let declared =
        Graph.outgoing e.Graph t.Target
        |> List.map _.Target
        |> List.append (
            KnowledgeRecord.inverseRelations r
            |> List.choose (fun (s, _) ->
                e.Graph.Records
                |> Map.tryFindKey (fun _ x -> x.Id = s && x.Provenance.Repository = r.Provenance.Repository))
        )
        |> Set.ofList
        |> Set.add t.Target

    {
        Found = declared
        Candidates = declared.Count
        Tokens = fileTokens e e.FileOfKey[t.Target]
        ToolCalls = 1
    }

/// BM25 over Markdown sections; read top-k excerpts.
let bm25Excerpts (k: int) (e: Env) (t: Task) : Outcome =
    let hits = Retrieval.search e.Index k t.Query

    {
        Found = hits |> List.map (fun (c, _) -> c.Key) |> Set.ofList
        Candidates = hits.Length
        Tokens = hits |> List.sumBy (fun (c, _) -> e.Count c.Text)
        ToolCalls = 1
    }

let private card (e: Env) k = e.Count(Context.card e.Graph k)

let private resolveQuery (e: Env) (t: Task) =
    e.Graph.Records
    |> Map.filter (fun _ r -> r.Id = t.Query)
    |> Map.keys
    |> List.ofSeq
    |> function
        | [] -> []
        | ks -> if List.contains t.Target ks then [ t.Target ] else ks

/// Typed-graph query answering the task's question directly, returning cards.
let graphQuery (e: Env) (t: Task) : Outcome =
    let g = e.Graph

    let answer (start: string) =
        match t.Kind with
        | "lookup-by-id"
        | "lookup-by-description" -> Set.singleton start
        | "find-evidence" ->
            Graph.incoming g start
            |> List.filter (fun x -> x.Kind = Supports || x.Kind = Contradicts)
            |> List.map _.Source
            |> Set.ofList
        | "change-impact" -> Graph.impactOf g 6 start |> Map.keys |> Set.ofSeq
        | "current-authority" ->
            let rec latest seen k =
                match
                    Graph.outgoing g k
                    |> List.filter (fun x -> x.Kind = SupersededBy)
                    |> List.map _.Target
                with
                | next :: _ when not (Set.contains next seen) -> latest (Set.add next seen) next
                | _ ->
                    match
                        Graph.incoming g k
                        |> List.filter (fun x -> x.Kind = Supersedes)
                        |> List.map _.Source
                    with
                    | next :: _ when not (Set.contains next seen) -> latest (Set.add next seen) next
                    | _ -> k

            Set.singleton (latest (Set.singleton start) start)
        | _ -> Set.empty

    let starts = resolveQuery e t
    let found = starts |> List.map answer |> Set.unionMany

    {
        Found = found
        Candidates = found.Count
        Tokens = found |> Seq.sumBy (card e)
        ToolCalls = 1
    }

/// Hybrid: BM25 to find seed records when no id is known, then the typed query.
let hybrid (e: Env) (t: Task) : Outcome =
    match resolveQuery e t with
    | [] ->
        let seeds =
            Retrieval.search e.Index 20 t.Query
            |> List.map (fun (c, _) -> c.Key)
            |> List.distinct
            |> List.truncate 3

        let outcomes =
            seeds
            |> List.map (fun s -> graphQuery e { t with Query = idOf e s; Target = s })

        {
            Found = outcomes |> List.map _.Found |> Set.unionMany
            Candidates = outcomes |> List.sumBy _.Candidates
            Tokens = outcomes |> List.sumBy _.Tokens
            ToolCalls = 1 + outcomes.Length
        }
    | _ -> graphQuery e t

let strategies: (string * (Env -> Task -> Outcome)) list =
    [
        "A1 grep + whole files (1 hop)", grepWholeFiles 1
        "A2 grep + whole files (chase 6 hops)", grepWholeFiles 6
        "A3 target file only", targetFileOnly
        "A4 BM25 excerpts (k=10)", bm25Excerpts 10
        "F1 typed graph query (cards)", graphQuery
        "F2 hybrid BM25 → graph (cards)", hybrid
    ]

type Score =
    {
        Strategy: string
        TaskKind: string
        Tasks: int
        Recall: float
        Precision: float
        Complete: int
        MeanTokens: float
        MeanToolCalls: float
    }

let score (e: Env) (ts: Task list) =
    strategies
    |> List.collect (fun (name, run) ->
        ts
        |> List.groupBy _.Kind
        |> List.map (fun (kind, group) ->
            let results =
                group
                |> List.map (fun t ->
                    let o = run e t
                    let hit = Set.intersect o.Found t.Gold |> Set.count
                    let recall = float hit / float t.Gold.Count

                    let precision =
                        if o.Candidates = 0 then
                            0.0
                        else
                            min 1.0 (float hit / float o.Candidates)

                    recall, precision, o)

            {
                Strategy = name
                TaskKind = kind
                Tasks = group.Length
                Recall = results |> List.averageBy (fun (r, _, _) -> r)
                Precision = results |> List.averageBy (fun (_, p, _) -> p)
                Complete = results |> List.filter (fun (r, _, _) -> r >= 1.0) |> List.length
                MeanTokens = results |> List.averageBy (fun (_, _, o) -> float o.Tokens)
                MeanToolCalls = results |> List.averageBy (fun (_, _, o) -> float o.ToolCalls)
            }))
