namespace Echelon.KnowledgeStore.Compiler

open System
open Echelon.KnowledgeStore

/// The query surface agents use instead of reading files. Each command returns
/// (output text, exit code). Exit codes: 0 ok, 1 not found / usage, 2 stale
/// artefacts, 3 incomplete output, 4 compilation errors (validate).
[<RequireQualifiedAccess>]
module Query =

    let private resolve (c: Compilation) (id: string) =
        if c.Graph.Records.ContainsKey id then
            [ id ]
        else
            c.Graph.Records
            |> Map.filter (fun _ r -> r.Id = id)
            |> Map.keys
            |> List.ofSeq
            |> List.sort

    let private withRecord (c: Compilation) (id: string) (f: string -> string * int) =
        match resolve c id with
        | [ k ] -> f k
        | [] -> $"error: no record '{id}'", 1
        | ks -> $"""error: '{id}' is ambiguous: {String.concat ", " ks} (qualify as repo:ID)""", 1

    let private emit (allowIncomplete: bool) (p: Package) =
        match p.Status with
        | Complete -> p.Text, 0
        | Incomplete _ -> p.Text, (if allowIncomplete then 0 else 3)

    let validate (c: Compilation) =
        let text = c.Diagnostics |> List.map Diagnostic.format |> String.concat "\n"

        let summary =
            c.Diagnostics
            |> List.countBy _.Level
            |> List.map (fun (l, n) -> $"{n} {l}")
            |> String.concat ", "

        text + $"\n\n{c.Records.Length} records; {summary}\n", (if Pipeline.hasErrors c then 4 else 0)

    let context c allowIncomplete id =
        withRecord c id (Packages.task c >> emit allowIncomplete)

    let evidence c allowIncomplete id =
        withRecord c id (Packages.evidence c >> emit allowIncomplete)

    let impact c allowIncomplete id =
        withRecord c id (Packages.impact c >> emit allowIncomplete)

    let explain c id =
        withRecord c id (Packages.explain c >> emit true)

    let query (c: Compilation) (id: string) =
        withRecord c id (fun k -> Context.card c.Graph k + "\n", 0)

    let obligations (c: Compilation) (id: string) =
        withRecord c id (fun k ->
            match Packages.contractJson c k with
            | Ok(json, hash, obs) -> json + $"\ncontentHash: sha256:{hash}\n", (if obs.IsEmpty then 3 else 0)
            | Error e -> e, 1)

    /// Differences between two compilations, with the impact of changed records.
    let diff (baseC: Compilation) (head: Compilation) =
        let keys (c: Compilation) =
            c.Graph.Records |> Map.keys |> Set.ofSeq

        let added = Set.difference (keys head) (keys baseC)
        let removed = Set.difference (keys baseC) (keys head)

        let changed =
            Set.intersect (keys head) (keys baseC)
            |> Set.filter (fun k -> head.Graph.Records[k] <> baseC.Graph.Records[k])

        let impacted =
            changed
            |> Seq.collect (fun k ->
                head.Precomputed.Impact
                |> Map.tryFind k
                |> Option.map (Map.keys >> List.ofSeq)
                |> Option.defaultValue [])
            |> Set.ofSeq
            |> fun s -> Set.difference s changed

        let line title (s: Set<string>) =
            $"## {title} ({s.Count})\n"
            + (if s.IsEmpty then
                   "none"
               else
                   s |> Set.toList |> String.concat "\n")

        String.concat
            "\n\n"
            [
                "# EKS diff"
                line "Added" added
                line "Removed" removed
                line "Changed" changed
                line "Requires review (impact of changes)" impacted
            ]
        + "\n",
        0
