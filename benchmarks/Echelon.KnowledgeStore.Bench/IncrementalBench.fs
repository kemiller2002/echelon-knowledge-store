/// Incremental compilation strategies compared against a full compile after
/// every edit of a mutation sequence. A strategy that returns a package that
/// differs from the full compile's is stale; speed does not excuse that.
module Echelon.KnowledgeStore.Bench.IncrementalBench

open System
open System.Diagnostics
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Compiler

type Mutation =
    {
        Step: int
        Kind: string
        Detail: string
    }

type StrategyResult =
    {
        Strategy: string
        Steps: int
        StalePackages: int
        StaleSteps: int
        MedianMs: float
        TotalMs: float
        FirstStale: string
    }

type private Outputs = Map<string, string>

/// A strategy: given its state, the configuration, the files and targets, returns
/// package texts and the new state.
type private Strategy<'s> =
    {
        Name: string
        Init: 's
        Step: 's -> CompileConfig -> SourceFile list -> string list -> Outputs * 's
    }

let private packages (c: Compilation) (targets: string list) =
    targets |> List.map (fun t -> t, (Packages.task c t).Text) |> Map.ofList

let private full: Strategy<unit> =
    {
        Name = "full"
        Init = ()
        Step = fun () cfg files targets -> packages (Pipeline.compile cfg Map.empty files |> fst) targets, ()
    }

let private incremental: Strategy<Cache> =
    {
        Name = "incremental (parse cache)"
        Init = Map.empty
        Step =
            fun cache cfg files targets ->
                let c, cache' = Pipeline.compile cfg cache files
                packages c targets, cache'
    }

/// Common shortcut: reuse a package while the files of its items are unchanged.
/// Misses new incoming edges, new records and collisions in other files.
let private naiveMemo: Strategy<Cache * Map<string, (string * string) list * string>> =
    {
        Name = "package memo keyed by item files (naive)"
        Init = Map.empty, Map.empty
        Step =
            fun (cache, memo) cfg files targets ->
                let hashOf =
                    files |> List.map (fun f -> $"{f.Repository}/{f.Path}", f.Hash) |> Map.ofList

                let valid t =
                    memo
                    |> Map.tryFind t
                    |> Option.filter (fun (deps, _) -> deps |> List.forall (fun (p, h) -> hashOf.TryFind p = Some h))
                    |> Option.map snd

                if targets |> List.forall (valid >> Option.isSome) then
                    targets |> List.map (fun t -> t, (valid t).Value) |> Map.ofList, (cache, memo)
                else
                    let c, cache' = Pipeline.compile cfg cache files

                    let fresh =
                        targets
                        |> List.map (fun t ->
                            match valid t with
                            | Some text -> t, (text, None)
                            | None ->
                                let p = Packages.task c t

                                let deps =
                                    p.Items
                                    |> List.choose (fun i -> c.Graph.Records |> Map.tryFind i.Key)
                                    |> List.map (fun r -> $"{r.Provenance.Repository}/{r.Provenance.Path}")
                                    |> List.distinct
                                    |> List.map (fun p -> p, defaultArg (hashOf.TryFind p) "")

                                t, (p.Text, Some deps))

                    let memo' =
                        fresh
                        |> List.fold
                            (fun m (t, (text, deps)) ->
                                match deps with
                                | Some d -> Map.add t (d, text) m
                                | None -> m)
                            memo

                    fresh |> List.map (fun (t, (text, _)) -> t, text) |> Map.ofList, (cache', memo')
    }

/// Compiles only the files reachable from the target by outgoing relations
/// (identifier → file by name). Misses incoming edges by construction.
let private onDemand: Strategy<unit> =
    {
        Name = "on-demand (outgoing closure only)"
        Init = ()
        Step =
            fun () cfg files targets ->
                let byId =
                    files
                    |> List.choose (fun f ->
                        let name = f.Path.Split('/') |> Array.last
                        let m = KnowledgeRecord.idPattern.Match name

                        if m.Success && m.Index = 0 then
                            Some($"{f.Repository}:{m.Value}", f)
                        else
                            None)
                    |> List.groupBy fst
                    |> List.map (fun (k, xs) -> k, xs |> List.map snd)
                    |> Map.ofList

                let outgoing (f: SourceFile) =
                    KnowledgeRecord.idPattern.Matches f.Text
                    |> Seq.map _.Value
                    |> Seq.map (fun id -> if id.Contains ":" then id else $"{f.Repository}:{id}")
                    |> List.ofSeq

                let rec closure (seen: Set<string>) frontier =
                    match frontier with
                    | [] -> seen
                    | k :: rest when seen.Contains k -> closure seen rest
                    | k :: rest ->
                        let fs = byId |> Map.tryFind k |> Option.defaultValue []
                        closure (seen.Add k) (rest @ (fs |> List.collect outgoing))

                targets
                |> List.map (fun t ->
                    let keys = closure Set.empty [ t ]

                    let subset =
                        keys
                        |> Set.toList
                        |> List.collect (fun k -> byId |> Map.tryFind k |> Option.defaultValue [])

                    let c = Pipeline.compile cfg Map.empty subset |> fst
                    t, (Packages.task c t).Text)
                |> Map.ofList,
                ()
    }

/// Parse cache plus a package memo keyed by everything the package reads:
/// the hashes of its item files, every edge touching an item, diagnostics on
/// items, the rule runs, the configuration and the inputs of the compiler itself.
let private hybrid: Strategy<Cache * Map<string, string list * string * string>> =
    {
        Name = "hybrid (parse cache + dependency-keyed package memo)"
        Init = Map.empty, Map.empty
        Step =
            fun (cache, memo) cfg files targets ->
                let c, cache' = Pipeline.compile cfg cache files

                // Everything a package reads about its items. The item set is the
                // previous package's items plus the target: a new item can only
                // enter through an edge touching one of them, a changed item
                // file or a changed diagnostic, all of which change this key.
                let fingerprint (items: string list) =
                    let set = Set.ofList items

                    let paths =
                        items
                        |> List.choose (fun k -> c.Graph.Records |> Map.tryFind k)
                        |> List.map (fun r -> $"{r.Provenance.Repository}/{r.Provenance.Path}")
                        |> Set.ofList

                    [
                        yield Pipeline.parseKey cfg
                        yield $"{cfg.AsOf}|{cfg.Audience}|{cfg.StaleAfterDays}"
                        yield! c.Rules |> List.map (fun r -> $"{r.Rule}:{r.Executed}")
                        yield!
                            c.Units
                            |> List.filter (fun u -> paths.Contains $"{u.File.Repository}/{u.File.Path}")
                            |> List.map (fun u -> $"{u.File.Path}:{u.File.Hash}")
                            |> List.sort
                        yield!
                            c.Graph.Edges
                            |> List.filter (fun e -> set.Contains e.Source || set.Contains e.Target)
                            |> List.map (fun e -> $"{e.Source}>{e.Kind}>{e.Target}")
                            |> List.sort
                        yield!
                            c.Graph.Unresolved
                            |> List.filter (fun (s, _) -> set.Contains s)
                            |> List.map (fun (s, r) -> $"{s}?{r.Target}")
                            |> List.sort
                        yield!
                            c.Diagnostics
                            |> List.filter (fun d -> d.Record |> Option.exists set.Contains)
                            |> List.map Diagnostic.format
                            |> List.sort
                        yield!
                            items
                            |> List.map (fun k -> if c.Graph.Records.ContainsKey k then k else k + ":missing")
                    ]
                    |> String.concat "\n"
                    |> Pipeline.sha256

                let results =
                    targets
                    |> List.map (fun t ->
                        let cached =
                            memo
                            |> Map.tryFind t
                            |> Option.filter (fun (items, key, _) -> fingerprint items = key)

                        match cached with
                        | Some(items, key, text) -> t, (items, key, text)
                        | None ->
                            let p = Packages.task c t
                            let items = t :: (p.Items |> List.map _.Key) |> List.distinct |> List.sort
                            t, (items, fingerprint items, p.Text))

                results |> List.map (fun (t, (_, _, text)) -> t, text) |> Map.ofList, (cache', Map.ofList results)
    }

let private inputsLine = Text.RegularExpressions.Regex @"inputs [0-9a-f]{12}"

let private median (xs: float list) =
    match List.sort xs with
    | [] -> 0.0
    | s -> s[s.Length / 2]

/// Deterministic mutation sequence over `files` that touches the targets'
/// neighbourhoods. Returns the file list after each step.
let mutations (seed: int) (steps: int) (targets: string list) (c0: Compilation) (files: SourceFile list) =
    let rng = Random seed

    let pick (xs: 'a list) = xs[rng.Next xs.Length]

    let fileOf (key: string) (fs: SourceFile list) =
        c0.Graph.Records
        |> Map.tryFind key
        |> Option.bind (fun r ->
            fs
            |> List.tryFind (fun f -> f.Repository = r.Provenance.Repository && f.Path = r.Provenance.Path))

    let neighbours t =
        Graph.neighbourhood c0.Graph 1 t |> Set.toList |> List.filter ((<>) t)

    let replaceFile (fs: SourceFile list) (f: SourceFile) text =
        fs
        |> List.map (fun x ->
            if x.Repository = f.Repository && x.Path = f.Path then
                Pipeline.sourceFile f.Repository f.Path text
            else
                x)

    let step (fs: SourceFile list) (n: int) =
        let t = pick targets
        let repo, id = t.Split(':')[0], t.Split(':')[1]

        match rng.Next 6 with
        | 0 ->
            match fileOf (pick (t :: neighbours t)) fs with
            | Some f ->
                replaceFile fs f (f.Text + $"\n\nEdited in step {n}.\n"),
                {
                    Step = n
                    Kind = "edit-body"
                    Detail = f.Path
                }
            | None -> fs, { Step = n; Kind = "noop"; Detail = t }
        | 1 ->
            match neighbours t |> List.choose (fun k -> fileOf k fs) with
            | [] -> fs, { Step = n; Kind = "noop"; Detail = t }
            | xs ->
                let f = pick xs

                let text =
                    Text.RegularExpressions.Regex.Replace(
                        f.Text,
                        @"(?m)^status:.*$",
                        "status: superseded",
                        Text.RegularExpressions.RegexOptions.None,
                        TimeSpan.FromSeconds 1.0
                    )

                replaceFile fs f text,
                {
                    Step = n
                    Kind = "status-change"
                    Detail = f.Path
                }
        | 2 ->
            let path = $"research/evidence/EV-MUT-2026-{n:D4}--added.md"

            let text =
                $"---\nid: EV-MUT-2026-{n:D4}\ntitle: Added evidence {n}\nstatus: accepted\nsource_title: Mutation\nsupports: [{id}]\n---\n\nAdded.\n"

            fs @ [ Pipeline.sourceFile repo path text ],
            {
                Step = n
                Kind = "add-incoming-support"
                Detail = t
            }
        | 3 ->
            let path = $"research/requirements/RQ-MUT-2026-{n:D4}--added.md"

            let text =
                $"---\nid: RQ-MUT-2026-{n:D4}\ntitle: Added requirement {n}\nstatus: accepted\ndepends_on: [{id}]\n---\n\n## Acceptance\n\n- Returns 1.\n"

            fs @ [ Pipeline.sourceFile repo path text ],
            {
                Step = n
                Kind = "add-incoming-dependency"
                Detail = t
            }
        | 4 ->
            match neighbours t |> List.choose (fun k -> fileOf k fs) with
            | [] -> fs, { Step = n; Kind = "noop"; Detail = t }
            | xs ->
                let f = pick xs

                fs
                |> List.filter (fun x -> not (x.Repository = f.Repository && x.Path = f.Path)),
                {
                    Step = n
                    Kind = "delete-neighbour"
                    Detail = f.Path
                }
        | _ ->
            let path = $"research/hypotheses/{id}--duplicate-{n}.md"

            let text =
                $"---\nid: {id}\ntitle: Colliding copy {n}\nstatus: proposed\n---\n\nCopy.\n"

            fs @ [ Pipeline.sourceFile repo path text ],
            {
                Step = n
                Kind = "id-collision"
                Detail = t
            }

    [ 1..steps ]
    |> List.scan (fun (fs, _) n -> step fs n) (files, { Step = 0; Kind = "base"; Detail = "" })

let private evaluate
    (strategy: Strategy<'s>)
    (cfg: CompileConfig)
    (sequence: (SourceFile list * Mutation) list)
    targets
    (truth: Outputs list)
    =
    let _, timings, stale =
        List.zip sequence truth
        |> List.fold
            (fun (state, timings, stale) ((files, m), expected) ->
                let sw = Stopwatch.StartNew()
                let outputs, state' = strategy.Step state cfg files targets
                sw.Stop()

                // The header's corpus-wide inputs hash is compared separately: a memo
                // must refresh it, which is cheap; content staleness is what matters.
                let norm (o: string option) =
                    o |> Option.map (fun t -> inputsLine.Replace(t, "inputs <hash>"))

                let wrong =
                    targets
                    |> List.filter (fun t -> norm (outputs.TryFind t) <> norm (expected.TryFind t))

                state', sw.Elapsed.TotalMilliseconds :: timings, (m, wrong) :: stale)
            (strategy.Init, [], [])

    let stale = List.rev stale
    let staleSteps = stale |> List.filter (fun (_, w) -> not w.IsEmpty)

    {
        Strategy = strategy.Name
        Steps = sequence.Length
        StalePackages = stale |> List.sumBy (fun (_, w) -> w.Length)
        StaleSteps = staleSteps.Length
        MedianMs = median timings
        TotalMs = List.sum timings
        FirstStale =
            staleSteps
            |> List.tryHead
            |> Option.map (fun (m, w) -> $"step {m.Step} {m.Kind} ({m.Detail}): {w.Head}")
            |> Option.defaultValue ""
    }

/// Runs every strategy over the same mutation sequence; ground truth is a full compile.
let run (cfg: CompileConfig) (files: SourceFile list) (targetCount: int) (steps: int) (seed: int) =
    let c0 = Pipeline.compile cfg Map.empty files |> fst

    let targets =
        c0.Graph.Records
        |> Map.filter (fun k r ->
            (r.Kind = Requirement || r.Kind = Hypothesis || r.Kind = Decision)
            && (Graph.neighbourhood c0.Graph 1 k).Count > 2)
        |> Map.keys
        |> List.ofSeq
        |> List.sortBy (fun k -> Pipeline.sha256 (string seed + k))
        |> List.truncate targetCount

    let sequence = mutations seed steps targets c0 files

    let truth =
        sequence
        |> List.map (fun (fs, _) -> packages (Pipeline.compile cfg Map.empty fs |> fst) targets)

    let configChange =
        // A configuration change with unchanged files must not reuse units parsed
        // under the old configuration.
        let fs = sequence |> List.last |> fst
        let _, cache = Pipeline.compile cfg Map.empty fs

        let cfg2 =
            { cfg with
                SupportedSchema = cfg.SupportedSchema + 1
            }

        let viaCache = Pipeline.compile cfg2 cache fs |> fst
        let fresh = Pipeline.compile cfg2 Map.empty fs |> fst
        viaCache.Diagnostics = fresh.Diagnostics

    let results =
        [
            evaluate full cfg sequence targets truth
            evaluate incremental cfg sequence targets truth
            evaluate naiveMemo cfg sequence targets truth
            evaluate onDemand cfg sequence targets truth
            evaluate hybrid cfg sequence targets truth
        ]

    targets, sequence |> List.map snd, results, configChange
