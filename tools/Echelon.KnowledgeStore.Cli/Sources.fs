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
