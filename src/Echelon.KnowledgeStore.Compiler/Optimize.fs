namespace Echelon.KnowledgeStore.Compiler

open Echelon.KnowledgeStore

/// One knowledge item selected for an output.
type Item =
    {
        Key: string
        Section: string
        Full: bool
    }

/// A transformation an optimisation pass applied. Every item that leaves an
/// output is recorded here: nothing is dropped silently.
type LedgerEntry =
    {
        Pass: string
        Key: string
        Action: string
        Reason: string
    }

/// Compiler-style optimisation passes over selected items. Passes never delete
/// source knowledge; they change what one output shows and record why.
[<RequireQualifiedAccess>]
module Optimize =

    type Pass = Compilation -> Item list -> Item list * LedgerEntry list

    /// Replaces superseded records in active sections by their current
    /// replacement; the superseded record moves to the History section.
    let supersessionElimination: Pass =
        fun c items ->
            let moved =
                items
                |> List.filter (fun i -> i.Section <> "History" && i.Section <> "Target")
                |> List.choose (fun i ->
                    match c.Precomputed.Current |> Map.tryFind i.Key with
                    | Some cur when cur <> i.Key -> Some(i, cur)
                    | _ -> None)

            let movedKeys = moved |> List.map (fst >> _.Key) |> Set.ofList

            let output =
                (items
                 |> List.map (fun i ->
                     if movedKeys.Contains i.Key && i.Section <> "History" then
                         { i with
                             Section = "History"
                             Full = false
                         }
                     else
                         i))
                @ (moved |> List.map (fun (i, cur) -> { i with Key = cur }))

            output,
            moved
            |> List.map (fun (i, cur) ->
                {
                    Pass = "supersession-elimination"
                    Key = i.Key
                    Action = $"moved to History; replaced in {i.Section} by {cur}"
                    Reason = "superseded records are history, not active knowledge"
                })

    /// Removes repeated items (same record listed in several sections keeps its
    /// first, highest-priority section).
    let deduplicate: Pass =
        fun _ items ->
            let kept = items |> List.distinctBy _.Key
            let removed = List.length items - List.length kept

            kept,
            (if removed = 0 then
                 []
             else
                 items
                 |> List.groupBy _.Key
                 |> List.filter (fun (_, xs) -> xs.Length > 1)
                 |> List.map (fun (k, xs) ->
                     {
                         Pass = "deduplicate"
                         Key = k
                         Action =
                             $"""listed once (also in {xs |> List.tail |> List.map _.Section |> String.concat ", "})"""
                         Reason = "same record selected by several rules"
                     }))

    /// Collapses byte-identical vendored copies (same id, same body) to the copy
    /// in the target's repository, or the first by key.
    let vendoredCopies: Pass =
        fun c items ->
            let bodyOf k =
                c.Graph.Records
                |> Map.tryFind k
                |> Option.map (fun r -> r.Id, Pipeline.sha256 r.Body)

            let groups =
                items
                |> List.distinctBy _.Key
                |> List.groupBy (fun i -> bodyOf i.Key)
                |> List.filter (fun (b, xs) -> b.IsSome && xs.Length > 1)

            // The target is never collapsed: within a group the target's item is kept.
            let keepFirst (xs: Item list) =
                xs |> List.sortBy (fun i -> (if i.Section = "Target" then 0 else 1), i.Key)

            let drop =
                groups
                |> List.collect (fun (_, xs) -> keepFirst xs |> List.tail)
                |> List.map _.Key
                |> Set.ofList

            items |> List.filter (fun i -> not (drop.Contains i.Key)),
            drop
            |> Set.toList
            |> List.map (fun k ->
                {
                    Pass = "vendored-copies"
                    Key = k
                    Action = "collapsed into an identical copy"
                    Reason = "byte-identical vendored record"
                })

    /// Delivers bodies only for the target; other items become cards.
    let cards: Pass =
        fun _ items ->
            items
            |> List.map (fun i -> if i.Section = "Target" then i else { i with Full = false }),
            items
            |> List.filter (fun i -> i.Full && i.Section <> "Target")
            |> List.map (fun i ->
                {
                    Pass = "cards"
                    Key = i.Key
                    Action = "body replaced by card"
                    Reason = "card carries status, links and source path; the body stays at its source"
                })

    let standard: Pass list = [ supersessionElimination; deduplicate; vendoredCopies ]

    /// Runs passes in order, collecting the ledger.
    let run (passes: Pass list) (c: Compilation) (items: Item list) =
        passes
        |> List.fold
            (fun (xs, ledger) pass ->
                let ys, l = pass c xs
                ys, ledger @ l)
            (items, [])

    /// Semantic-preservation check: every input key is in the output or in the
    /// ledger. Returns the keys that vanished without a ledger entry.
    let unaccounted (input: Item list) (output: Item list) (ledger: LedgerEntry list) =
        let out = output |> List.map _.Key |> Set.ofList
        let logged = ledger |> List.map _.Key |> Set.ofList

        input
        |> List.map _.Key
        |> List.filter (fun k -> not (out.Contains k || logged.Contains k))
        |> List.distinct
