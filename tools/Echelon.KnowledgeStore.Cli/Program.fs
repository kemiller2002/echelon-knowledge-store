module Echelon.KnowledgeStore.Cli.Program

open System
open System.IO
open Echelon.KnowledgeStore.Compiler

let usage =
    """eks — Echelon Knowledge Store research CLI (data-only; never executes knowledge)

usage: eks <command> [args] --root DIR [--repos a,b] [--private a,b] [--audience public|private]
           [--artifacts DIR] [--as-of YYYY-MM-DD] [--allow-incomplete]

  validate                 diagnostics (exit 4 on errors)
  compile --out DIR        write index.md, records.jsonl, diagnostics.json, precomputed.json, manifest.json
  verify                   compare --artifacts manifest with sources (exit 2 if stale)
  query ID                 record card
  explain ID               card, text, relations, derived facts
  context ID               task package for implementing/changing ID (exit 3 if incomplete)
  evidence ID              evidence bundle for a claim
  impact ID                change-impact report
  obligations ID           requirement contract (JSON)
  manifest ID              machine-readable manifest of the task package for ID
  privacy-check [paths]    scan public artefacts against .privacy-denylist (exit 5 on findings)
  diff BASE_ROOT HEAD_ROOT added/removed/changed records and the impact of changes"""

type Options =
    {
        Root: string
        Repos: string list
        Private: string list
        Audience: Visibility
        Artifacts: string option
        AsOf: DateOnly
        AllowIncomplete: bool
        Out: string option
        Positional: string list
    }

let rec private parseArgs (o: Options) =
    function
    | "--root" :: v :: rest -> parseArgs { o with Root = v } rest
    | "--repos" :: v :: rest ->
        parseArgs
            { o with
                Repos = v.Split(',') |> List.ofArray
            }
            rest
    | "--private" :: v :: rest ->
        parseArgs
            { o with
                Private = v.Split(',') |> List.ofArray
            }
            rest
    | "--audience" :: "public" :: rest -> parseArgs { o with Audience = Public } rest
    | "--audience" :: _ :: rest -> parseArgs { o with Audience = Private } rest
    | "--artifacts" :: v :: rest -> parseArgs { o with Artifacts = Some v } rest
    | "--as-of" :: v :: rest -> parseArgs { o with AsOf = DateOnly.Parse v } rest
    | "--allow-incomplete" :: rest -> parseArgs { o with AllowIncomplete = true } rest
    | "--out" :: v :: rest -> parseArgs { o with Out = Some v } rest
    | x :: rest ->
        parseArgs
            { o with
                Positional = o.Positional @ [ x ]
            }
            rest
    | [] -> o

let private config (o: Options) =
    {
        AsOf = o.AsOf
        StaleAfterDays = 180
        Visibility = o.Private |> List.map (fun r -> r, Private) |> Map.ofList
        Audience = o.Audience
        SupportedSchema = 1
        ExistingPaths = Some(Sources.existingPaths o.Root o.Repos)
        DisabledRules = Set.empty
        Revisions = Sources.revisions o.Root o.Repos
    }

let private compileAt (o: Options) root =
    Sources.load root o.Repos |> Pipeline.compile (config o) Map.empty |> fst

/// Refuses to answer from stale artefacts (fail safe).
let private fresh (o: Options) (files: SourceFile list) =
    match o.Artifacts with
    | None -> Ok()
    | Some dir ->
        let path = Path.Combine(dir, "manifest.json")

        if not (File.Exists path) then
            Error($"error: no manifest in {dir}", 2)
        else
            match Artifacts.freshness (File.ReadAllText path) files with
            | Fresh -> Ok()
            | Stale changed ->
                let shown = changed |> List.truncate 10 |> String.concat ", "

                Error(
                    $"error: compiled artefacts are stale ({changed.Length} source(s) changed: {shown}). Run `eks compile`.",
                    2
                )

let run (argv: string list) : string * int =
    let defaults =
        {
            Root = "."
            Repos = []
            Private = []
            Audience = Private
            Artifacts = None
            AsOf = DateOnly.FromDateTime DateTime.UtcNow
            AllowIncomplete = false
            Out = None
            Positional = []
        }

    let o = parseArgs defaults argv
    let files () = Sources.load o.Root o.Repos

    let withCompilation (f: Compilation -> string * int) =
        let fs = files ()

        match fresh o fs with
        | Error e -> e
        | Ok() -> f (Pipeline.compile (config o) Map.empty fs |> fst)

    match o.Positional with
    | [ "validate" ] -> withCompilation Query.validate
    | [ "compile" ] ->
        match o.Out with
        | None -> "error: compile needs --out DIR", 1
        | Some dir ->
            let c = compileAt o o.Root
            Artifacts.write dir c
            $"compiled {c.Records.Length} records to {dir}\n", (if Pipeline.hasErrors c then 4 else 0)
    | [ "verify" ] ->
        match fresh o (files ()) with
        | Ok() -> "fresh\n", 0
        | Error e -> e
    | [ "query"; id ] -> withCompilation (fun c -> Query.query c id)
    | [ "explain"; id ] -> withCompilation (fun c -> Query.explain c id)
    | [ "context"; id ] -> withCompilation (fun c -> Query.context c o.AllowIncomplete id)
    | [ "evidence"; id ] -> withCompilation (fun c -> Query.evidence c o.AllowIncomplete id)
    | [ "impact"; id ] -> withCompilation (fun c -> Query.impact c o.AllowIncomplete id)
    | [ "obligations"; id ] -> withCompilation (fun c -> Query.obligations c id)
    | "privacy-check" :: dirs ->
        let denyFile = Path.Combine(o.Root, ".privacy-denylist")

        let patterns =
            if File.Exists denyFile then
                File.ReadAllLines denyFile
                |> Array.map _.Trim()
                |> Array.filter (fun l -> l <> "" && not (l.StartsWith "#"))
                |> List.ofArray
            else
                []

        let dirs =
            if dirs.IsEmpty then
                [ "research"; "docs"; "benchmarks"; "README.md"; "AGENTS.md" ]
            else
                dirs

        match Sources.privacyFindings o.Root dirs patterns with
        | [] -> $"privacy-check: no findings ({patterns.Length} patterns)\n", 0
        | fs ->
            (fs
             |> List.map (fun (f, l, p) -> $"{f}({l}): matches deny pattern {p}")
             |> String.concat "\n")
            + $"\n{fs.Length} finding(s)\n",
            5
    | [ "manifest"; id ] ->
        withCompilation (fun c ->
            match
                c.Graph.Records
                |> Map.filter (fun k r -> k = id || r.Id = id)
                |> Map.keys
                |> List.ofSeq
            with
            | [ k ] -> Packages.manifest c (Packages.task c k) + "\n", 0
            | [] -> $"error: no record '{id}'", 1
            | ks -> $"error: ambiguous '{id}': " + String.concat ", " ks, 1)
    | [ "diff"; baseRoot; headRoot ] -> Query.diff (compileAt o baseRoot) (compileAt o headRoot)
    | _ -> usage, 1

[<EntryPoint>]
let main argv =
    let text, code = run (List.ofArray argv)
    (if code = 0 then stdout else stderr).Write text
    code
