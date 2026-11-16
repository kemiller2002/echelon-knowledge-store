module Echelon.KnowledgeStore.FcsProbe.Generate

open System
open System.Text.Json
open Echelon.KnowledgeStore

/// F# identifier for a record id: `HY-FE-2026-B006` → `HY_FE_2026_B006`.
let ident (id: string) = id.Replace('-', '_').Replace('.', '_')

let private statusCase (s: string) =
    match s.Trim().ToLowerInvariant() with
    | "proposed" -> "Proposed"
    | "active" -> "Active"
    | "supported" -> "Supported"
    | "rejected" -> "Rejected"
    | "superseded" -> "Superseded"
    | "withdrawn" -> "Withdrawn"
    | "accepted" -> "Accepted"
    | "canonical" -> "Canonical"
    | "draft" -> "Draft"
    | "review" -> "Review"
    | "deprecated" -> "Deprecated"
    | "completed" -> "Completed"
    | "implemented" -> "Implemented"
    | other -> $"Other {JsonSerializer.Serialize other}"

let private confidenceCase =
    function
    | VeryLow -> "Some VeryLow"
    | Low -> "Some Low"
    | Medium -> "Some Medium"
    | High -> "Some High"
    | VeryHigh -> "Some VeryHigh"
    | Unstated -> "None"
    | Label l -> $"Some(OtherConfidence {JsonSerializer.Serialize l})"

/// Schema prelude: ids as a union (compile-time referential integrity), status
/// and confidence as unions, one record type.
let prelude (ids: string list) =
    let cases = ids |> List.map (fun i -> "    | " + ident i) |> String.concat "\n"

    $"""module Knowledge

type Id =
{cases}

type Status =
    | Proposed | Active | Supported | Rejected | Superseded | Withdrawn | Accepted | Canonical
    | Draft | Review | Deprecated | Completed | Implemented
    | Other of string

type Confidence = VeryLow | Low | Medium | High | VeryHigh | OtherConfidence of string

type Kind = Requirement | Decision | Hypothesis | Evidence | Experiment | Theory | ResearchPackage | Journal | Governance | Method | Verification | Document

type Record =
    {{ Id: Id
      Kind: Kind
      Title: string
      Status: Status option
      Confidence: Confidence option
      Supports: Id list
      Contradicts: Id list
      DependsOn: Id list
      DerivedFrom: Id list
      Supersedes: Id list
      SupersededBy: Id list }}
"""

let private kindCase (k: KnowledgeKind) =
    match k with
    | Requirement -> "Requirement"
    | Decision -> "Decision"
    | Hypothesis -> "Hypothesis"
    | Evidence -> "Evidence"
    | Experiment -> "Experiment"
    | Theory -> "Theory"
    | ResearchPackage -> "ResearchPackage"
    | Journal -> "Journal"
    | Governance -> "Governance"
    | Method -> "Method"
    | Verification -> "Verification"
    | ResearchQuestion
    | Document
    | Other _ -> "Document"

/// Record values. Relation targets outside `known` are skipped (counted).
let records (known: Set<string>) (rs: KnowledgeRecord list) =
    let skipped = ref 0

    let rel kind (r: KnowledgeRecord) =
        r.Relations
        |> List.filter (fun x -> x.Kind = kind)
        |> List.map _.Target
        |> List.filter (fun t ->
            if known.Contains t then
                true
            else
                skipped.Value <- skipped.Value + 1
                false)
        |> List.map ident
        |> String.concat "; "

    let body =
        rs
        |> List.map (fun r ->
            $"""    {{ Id = {ident r.Id}; Kind = {kindCase r.Kind}; Title = {JsonSerializer.Serialize r.Title}
      Status = {(r.Status
                 |> Option.map (statusCase >> sprintf "Some(%s)")
                 |> Option.defaultValue "None")}; Confidence = {confidenceCase r.Confidence}
      Supports = [ {rel Supports r} ]; Contradicts = [ {rel Contradicts r} ]; DependsOn = [ {rel DependsOn r} ]
      DerivedFrom = [ {rel DerivedFrom r} ]; Supersedes = [ {rel Supersedes r} ]; SupersededBy = [ {rel SupersededBy r} ] }}""")
        |> String.concat "\n"

    "\nlet records : Record list =\n  [\n" + body + "\n  ]\n", skipped.Value
