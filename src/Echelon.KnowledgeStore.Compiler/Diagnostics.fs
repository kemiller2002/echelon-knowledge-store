namespace Echelon.KnowledgeStore.Compiler

open System
open System.Text.RegularExpressions
open Echelon.KnowledgeStore

/// Diagnostic level. `RequiresReview` marks suspected problems (e.g. possible
/// contradictions) that a human or model must judge; they are never proven errors.
[<RequireQualifiedAccess>]
type Level =
    | Error
    | Warning
    | Advisory
    | RequiresReview

/// Where a finding is, as precisely as the source allows.
type SourceLocation =
    {
        Repository: string
        Path: string
        Line: int
        Field: string option
    }

type Diagnostic =
    {
        Code: string
        Level: Level
        Record: string option
        Location: SourceLocation option
        Message: string
        Remediation: string
    }

/// The diagnostic catalogue (experimental; codes reviewed against the corpus,
/// not yet adopted).
[<RequireQualifiedAccess>]
module Codes =
    let catalogue =
        [
            "EKS0001",
            Level.Error,
            "Record file without an id",
            "Add `id:` to the front matter or move the file out of the record directory."
            "EKS0002",
            Level.Error,
            "Unresolved reference",
            "Correct the id, create the record, or qualify it as `repo:ID` if it lives elsewhere."
            "EKS0003",
            Level.Warning,
            "Invalid relationship for record kinds",
            "Use a relation whose meaning fits both record kinds."
            "EKS0004",
            Level.Error,
            "Unsupported schema version",
            "Upgrade the record with the migration for its type or pin a compatible compiler."
            "EKS0005",
            Level.Error,
            "Missing required provenance",
            "Add `source_uri`/`source_title` or link the evidence with `derived_from`."
            "EKS0006",
            Level.Error,
            "Circular dependency",
            "Break the cycle; one record must not depend on itself transitively."
            "EKS0007",
            Level.Warning,
            "Reference to a superseded record",
            "Point to the current replacement or record why the old version is intended."
            "EKS0008",
            Level.RequiresReview,
            "Conflicting records share an id",
            "Make one repository the owner; mark copies with `vendored_from`."
            "EKS0009",
            Level.Warning,
            "Incomplete requirement contract",
            "Add an `## Acceptance` list of checkable criteria."
            "EKS0010",
            Level.RequiresReview,
            "Potentially stale knowledge",
            "Re-check the evidence or update the claim's status."
            "EKS0011",
            Level.Error,
            "Front matter cannot be parsed",
            "Fix the YAML; aliases, anchors and multiple documents are rejected."
            "EKS0012",
            Level.Advisory,
            "Link declared on one side only",
            "Declare the link on the evidence (`supports`/`contradicts`); the compiler derives the inverse."
            "EKS0013", Level.Error, "Superseded without replacement", "Add `superseded_by` naming the replacement."
            "EKS0014",
            Level.Error,
            "Claim asserts support without evidence",
            "Link supporting evidence or lower the status."
            "EKS0015",
            Level.RequiresReview,
            "Record text addresses agents with instructions",
            "Knowledge is data; move instructions to governance files or rephrase."
            "EKS0016", Level.Advisory, "Missing status", "Add `status:`."
            "EKS0017",
            Level.RequiresReview,
            "Contested claim (suspected contradiction)",
            "Weigh the supporting and contradicting evidence; this is not a proven contradiction."
            "EKS0018",
            Level.Error,
            "Duplicate id in one repository",
            "Give each record a unique id; ids are never reused."
            "EKS0019",
            Level.Warning,
            "Confidence not backed by evidence",
            "Lower the confidence or link supporting evidence."
            "EKS0020",
            Level.Error,
            "Private knowledge would flow to a wider audience",
            "Compile for the private audience or exclude private repositories."
            "EKS0021",
            Level.RequiresReview,
            "Supersession claimed by one side only",
            "Confirm with `superseded_by` on the older record, or remove the claim."
            "EKS0022",
            Level.Error,
            "Unknown field resembles a known field",
            "Rename the field; misspelled relation fields silently drop links."
            "EKS0023",
            Level.Error,
            "Status not allowed for this record kind",
            "Use a status from the record kind's schema."
            "EKS0024",
            Level.Warning,
            "Confidence outside the standard scale",
            "Use very-low, low, medium, high or very-high."
            "EKS0025",
            Level.Warning,
            "Unrecognised status value",
            "Use an established lifecycle status (draft, review, accepted, canonical, superseded, ...)."
            "EKS0026",
            Level.Warning,
            "Experiment records no hypothesis",
            "Link the hypothesis the experiment tests (an HY- id in metadata or text)."
            "EKS0027",
            Level.Warning,
            "Evidence source not found",
            "Point source_uri at an existing file (relative to the repository or the record) or an external URL."
        ]

    let private find code =
        catalogue |> List.find (fun (c, _, _, _) -> c = code)

    let level code = find code |> fun (_, l, _, _) -> l
    let title code = find code |> fun (_, _, t, _) -> t
    let remediation code = find code |> fun (_, _, _, r) -> r

[<RequireQualifiedAccess>]
module Diagnostic =

    /// Line (1-based) of `field:` in the file's front matter, else 1.
    let lineOf (text: string) (field: string option) (value: string option) =
        let lines = text.Replace("\r\n", "\n").Split('\n')

        let byField =
            field
            |> Option.bind (fun f -> lines |> Array.tryFindIndex (fun l -> l.StartsWith(f + ":")))

        let byValue =
            value
            |> Option.bind (fun v -> lines |> Array.tryFindIndex (fun l -> l.Contains(v, StringComparison.Ordinal)))

        match byValue, byField with
        | Some v, Some f when v >= f -> v + 1
        | _, Some f -> f + 1
        | Some v, None -> v + 1
        | None, None -> 1

    /// Like `make`, with an explicit level (when one code covers degrees).
    let makeAt level code record location message =
        {
            Code = code
            Level = level
            Record = record
            Location = location
            Message = message
            Remediation = Codes.remediation code
        }

    let make code record location message =
        {
            Code = code
            Level = Codes.level code
            Record = record
            Location = location
            Message = message
            Remediation = Codes.remediation code
        }

    let format (d: Diagnostic) =
        let where =
            d.Location
            |> Option.map (fun l -> $"{l.Repository}/{l.Path}({l.Line})")
            |> Option.defaultValue "(corpus)"

        let level =
            match d.Level with
            | Level.Error -> "error"
            | Level.Warning -> "warning"
            | Level.Advisory -> "advisory"
            | Level.RequiresReview -> "review"

        $"{where}: {level} {d.Code}: {d.Message} — {d.Remediation}"

    let private instruction =
        Regex(
            @"(?i)\b(ignore (all |any )?(previous|prior|above) instructions|disregard (the|your) (instructions|question)|you must (now )?answer|system prompt|as an ai (agent|assistant)|agent instruction:)",
            RegexOptions.Compiled
        )

    /// Heuristic: record prose that addresses an agent with instructions.
    let addressesAgents (body: string) = instruction.IsMatch body
