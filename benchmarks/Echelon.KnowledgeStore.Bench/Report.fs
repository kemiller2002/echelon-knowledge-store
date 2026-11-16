module Echelon.KnowledgeStore.Bench.Report

open System
open System.IO
open System.Text.Json

let private options = JsonSerializerOptions(WriteIndented = true)

/// Writes a value as indented JSON.
let json (dir: string) (name: string) (value: obj) =
    Directory.CreateDirectory dir |> ignore
    File.WriteAllText(Path.Combine(dir, name), JsonSerializer.Serialize(value, options) + "\n")

/// Writes text.
let text (dir: string) (name: string) (content: string) =
    Directory.CreateDirectory dir |> ignore
    File.WriteAllText(Path.Combine(dir, name), content)

/// A GitHub Markdown table.
let table (headers: string list) (rows: string list list) =
    let line (cells: string list) = "| " + String.Join(" | ", cells) + " |"

    [ line headers; line (headers |> List.map (fun _ -> "---")) ]
    @ (rows |> List.map line)
    |> String.concat "\n"

let pct (n: int) (d: int) =
    if d = 0 then
        "–"
    else
        (100.0 * float n / float d).ToString("0.0") + "%"

let f1 (x: float) = x.ToString("0.0")
let f2 (x: float) = x.ToString("0.00")
let f3 (x: float) = x.ToString("0.000")
