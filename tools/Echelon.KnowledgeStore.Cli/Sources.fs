module Echelon.KnowledgeStore.Cli.Sources

open System.IO
open Echelon.KnowledgeStore.Compiler

let private excluded (rel: string) =
    let q = "/" + rel

    [
        "/.git/"
        "/node_modules/"
        "/bin/"
        "/obj/"
        "/vendor/"
        "/archive/"
        "/tests/"
        "/fixtures/"
        "/templates/"
        "/starter/"
    ]
    |> List.exists q.Contains

/// Markdown sources of the given repositories under `root` (all subdirectories
/// when `repos` is empty). Read-only.
let load (root: string) (repos: string list) : SourceFile list =
    let repos =
        match repos with
        | [] ->
            Directory.GetDirectories root
            |> Array.map (Path.GetFileName >> string)
            |> Array.sort
            |> List.ofArray
        | rs -> rs

    repos
    |> List.collect (fun repo ->
        let dir = Path.Combine(root, repo)

        if Directory.Exists dir then
            Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories)
            |> Seq.map (fun f -> Path.GetRelativePath(dir, f).Replace('\\', '/'))
            |> Seq.filter (excluded >> not)
            |> Seq.sort
            |> Seq.map (fun p -> Pipeline.sourceFile repo p (File.ReadAllText(Path.Combine(dir, p))))
            |> List.ofSeq
        else
            [])

/// Every file path (any extension) under the repositories, as `repository/path`.
let existingPaths (root: string) (repos: string list) : Set<string> =
    let repos =
        match repos with
        | [] ->
            Directory.GetDirectories root
            |> Array.map (Path.GetFileName >> string)
            |> List.ofArray
        | rs -> rs

    repos
    |> List.collect (fun repo ->
        let dir = Path.Combine(root, repo)

        if Directory.Exists dir then
            Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            |> Seq.map (fun f -> repo + "/" + Path.GetRelativePath(dir, f).Replace('\\', '/'))
            |> Seq.filter (fun p -> not (p.Contains "/.git/"))
            |> List.ofSeq
        else
            [])
    |> Set.ofList

/// The checked-out commit of each repository, read from `.git` as data
/// (HEAD, the ref it names, or packed-refs). Missing information is omitted.
let revisions (root: string) (repos: string list) : Map<string, string> =
    let repos =
        match repos with
        | [] ->
            Directory.GetDirectories root
            |> Array.map (Path.GetFileName >> string)
            |> List.ofArray
        | rs -> rs

    let read (path: string) =
        if File.Exists path then
            Some((File.ReadAllText path).Trim())
        else
            None

    repos
    |> List.choose (fun repo ->
        let git = Path.Combine(root, repo, ".git")

        read (Path.Combine(git, "HEAD"))
        |> Option.bind (fun head ->
            if head.StartsWith "ref: " then
                let reference = head.Substring 5

                read (Path.Combine(git, reference))
                |> Option.orElse (
                    read (Path.Combine(git, "packed-refs"))
                    |> Option.bind (fun packed ->
                        packed.Split('\n')
                        |> Array.tryFind (fun l -> l.EndsWith(" " + reference))
                        |> Option.map (fun l -> l.Split(' ')[0]))
                )
            else
                Some head)
        |> Option.map (fun sha -> repo, sha))
    |> Map.ofList

/// Text files under `dirs` (relative to `root`) that match any deny pattern,
/// as (path, line number, pattern). Binary-looking files are skipped.
let privacyFindings (root: string) (dirs: string list) (patterns: string list) =
    let regexes =
        patterns |> List.map (fun p -> p, System.Text.RegularExpressions.Regex p)

    let textExt =
        set
            [
                ".md"
                ".json"
                ".csv"
                ".txt"
                ".fs"
                ".fsproj"
                ".yml"
                ".yaml"
                ".jsonl"
                ".eks"
            ]

    dirs
    |> List.collect (fun d ->
        let dir = Path.Combine(root, d)

        if Directory.Exists dir then
            Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            |> Seq.map string
            |> List.ofSeq
        elif File.Exists dir then
            [ dir ]
        else
            [])
    |> List.filter (fun (f: string) ->
        textExt.Contains(string (Path.GetExtension f))
        && not (f.Contains "/bin/" || f.Contains "/obj/"))
    |> List.collect (fun f ->
        File.ReadAllLines f
        |> Array.indexed
        |> Array.collect (fun (i, line) ->
            regexes
            |> List.filter (fun (_, rx) -> rx.IsMatch line)
            |> List.map (fun (p, _) -> Path.GetRelativePath(root, f), i + 1, p)
            |> Array.ofList)
        |> List.ofArray)
