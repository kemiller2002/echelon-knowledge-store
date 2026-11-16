namespace Echelon.KnowledgeStore

open System
open System.IO
open YamlDotNet.Core
open YamlDotNet.Core.Events

/// Inert YAML value. Produced by a data-only reader: no type activation,
/// no tags, no anchors or aliases (rejected to rule out alias-expansion attacks).
type Yaml =
    | YNull
    | YScalar of string
    | YSeq of Yaml list
    | YMap of (string * Yaml) list

/// Data-only YAML reading with explicit resource limits.
[<RequireQualifiedAccess>]
module Yaml =

    /// Maximum events accepted in one document (bounds parser work).
    [<Literal>]
    let MaxEvents = 20_000

    let private readEvents (text: string) : Result<ParsingEvent list, string> =
        try
            let parser = Parser(new StringReader(text))

            let rec loop count acc =
                if count > MaxEvents then
                    Error "YAML exceeds event limit"
                elif parser.MoveNext() then
                    match parser.Current with
                    | null -> loop count acc
                    | :? AnchorAlias -> Error "YAML aliases are not allowed"
                    | :? NodeEvent as n when not n.Anchor.IsEmpty -> Error "YAML anchors are not allowed"
                    | e -> loop (count + 1) (e :: acc)
                else
                    Ok(List.rev acc)

            loop 0 []
        with :? YamlException as ex ->
            Error $"YAML syntax error: {ex.Message}"

    let private scalarValue (s: Scalar) =
        match s.Style, s.Value with
        | ScalarStyle.Plain, ("" | "~" | "null" | "Null" | "NULL") -> YNull
        | _, v -> YScalar v

    /// Parses a node; returns the value and the remaining events.
    let rec private node (events: ParsingEvent list) : Result<Yaml * ParsingEvent list, string> =
        match events with
        | (:? Scalar as s) :: rest -> Ok(scalarValue s, rest)
        | (:? SequenceStart) :: rest -> items [] rest
        | (:? MappingStart) :: rest -> pairs [] rest
        | e :: _ -> Error $"unexpected YAML event {e.GetType().Name}"
        | [] -> Error "unexpected end of YAML"

    and private items acc events =
        match events with
        | (:? SequenceEnd) :: rest -> Ok(YSeq(List.rev acc), rest)
        | _ -> node events |> Result.bind (fun (v, rest) -> items (v :: acc) rest)

    and private pairs acc events =
        match events with
        | (:? MappingEnd) :: rest -> Ok(YMap(List.rev acc), rest)
        | (:? Scalar as k) :: rest -> node rest |> Result.bind (fun (v, rest') -> pairs ((k.Value, v) :: acc) rest')
        | _ -> Error "only scalar mapping keys are supported"

    /// Parses a single YAML document into an inert value.
    let parse (text: string) : Result<Yaml, string> =
        readEvents text
        |> Result.bind (fun events ->
            let body =
                events
                |> List.filter (function
                    | :? StreamStart
                    | :? StreamEnd
                    | :? DocumentStart
                    | :? DocumentEnd
                    | :? Comment -> false
                    | _ -> true)

            match body with
            | [] -> Ok YNull
            | _ ->
                node body
                |> Result.bind (fun (v, rest) ->
                    match rest with
                    | [] -> Ok v
                    | _ -> Error "multiple YAML documents are not supported"))

    /// Field lookup on a mapping.
    let field (key: string) (y: Yaml) =
        match y with
        | YMap kvs -> kvs |> List.tryFind (fst >> (=) key) |> Option.map snd
        | _ -> None

    /// A scalar as text.
    let text (y: Yaml) =
        match y with
        | YScalar s -> Some s
        | _ -> None

    /// A scalar or sequence of scalars as a list of strings ("[]", null → empty).
    let strings (y: Yaml) =
        match y with
        | YScalar s when String.IsNullOrWhiteSpace s -> []
        | YScalar s -> [ s ]
        | YSeq xs -> xs |> List.choose text
        | YNull
        | YMap _ -> []
