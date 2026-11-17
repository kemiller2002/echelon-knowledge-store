module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``balance includes entries dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

let today = d 6 1
let withDesc desc (e: Entry) = { e with Description = desc }

[<Fact>]
let ``addEntry accepts boundary amount and rejects out of range`` () =
    Assert.True(Result.isOk (addEntry today (entry "n" "t1" 1000000m today false) []))
    Assert.Equal(Error InvalidAmount, addEntry today (entry "n" "t1" 1000000.01m today false) [])
    Assert.Equal(Error InvalidAmount, addEntry today (entry "n" "t1" 0m today false) [])

[<Fact>]
let ``addEntry description limits after trimming`` () =
    let ok = entry "n" "t1" 1m today false |> withDesc (" " + String('a', 200) + " ")
    Assert.True(Result.isOk (addEntry today ok []))
    let bad = entry "n" "t1" 1m today false |> withDesc (String('a', 201))
    Assert.Equal(Error InvalidDescription, addEntry today bad [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "   " (entry "n" "t1" 1m today false)) [])

[<Fact>]
let ``addEntry date and duplicate id`` () =
    Assert.Equal(Error FutureDate, addEntry today (entry "n" "t1" 1m (d 6 2) false) [])
    Assert.Equal(Error DuplicateId, addEntry today (entry "e1" "t1" 1m today false) [ entry "e1" "t2" 1m today true ])

[<Fact>]
let ``addEntry reports first failing rule in fixed order`` () =
    let e = entry "e1" "t1" 0m (d 6 2) false |> withDesc ""
    Assert.Equal(Error InvalidAmount, addEntry today e [ entry "e1" "t1" 1m today true ])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Amount = 1m } [ entry "e1" "t1" 1m today true ])
    Assert.Equal(Error FutureDate, addEntry today { e with Amount = 1m; Description = "x" } [ entry "e1" "t1" 1m today true ])

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "e0" "t1" 1m today true ]
    let e = { entry "n" "t1" 5m today true with Description = "  rent "; ApprovedBy = Some "boss" }
    match addEntry today e existing with
    | Ok [ first; last ] ->
        Assert.Equal(existing.Head, first)
        Assert.Equal("rent", last.Description)
        Assert.False last.Approved
        Assert.Equal(None, last.ApprovedBy)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``exportCsv header only when no approved entries`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m today false ])

[<Fact>]
let ``exportCsv quotes, formats and orders rows`` () =
    let entries =
        [ { entry "b" "t1" 2m (d 1 2) true with Description = "a,\"b\"" }
          { entry "a" "t1" -5m (d 1 2) true with Description = "plain" }
          entry "c" "t1" 1.5m (d 1 1) true
          entry "z" "t2" 1m (d 1 1) true ]
    let expected =
        "id,date,amount,description\nc,2026-01-01,1.50,x\na,2026-01-02,-5.00,plain\nb,2026-01-02,2.00,\"a,\"\"b\"\"\"\n"
    Assert.Equal(expected, exportCsv "t1" entries)

open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let session roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours -1.0; LastSeen = now.AddMinutes -1.0 }
let orig = [ entry "o" "t1" 100m (d 5 1) true ]

[<Fact>]
let ``refund cap allows full amount in parts and rejects more`` () =
    let s = session [ Clerk ]
    let r1 = refund now s today "r1" "o" 60m orig |> Result.bind (refund now s today "r2" "o" 40m)
    Assert.True(Result.isOk r1)
    Assert.Equal(Error RefundExceedsOriginal, r1 |> Result.bind (refund now s today "r3" "o" 0.01m))
    Assert.Equal(Error RefundExceedsOriginal, refund now s today "r1" "o" 100.01m orig)

[<Fact>]
let ``refund stored form`` () =
    match refund now (session [ Admin ]) today "r1" "o" 10m orig with
    | Ok [ _; r ] ->
        Assert.Equal({ Id = "r1"; TenantId = "t1"; Amount = -10m; Description = "Refund of o"; Date = today
                       CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = Some "o" }, r)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``refund check order and errors`` () =
    let expired = { session [ Viewer ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, refund now expired today "o" "nope" 0m orig)
    Assert.Equal(Error NotAuthorized, refund now (session [ Viewer; Approver ]) today "o" "nope" 0m orig)
    let s = session [ Clerk ]
    Assert.Equal(Error NotFound, refund now s today "r" "nope" 0m orig)
    Assert.Equal(Error NotFound, refund now { s with TenantId = "t2" } today "r" "o" 1m orig)
    Assert.Equal(Error NotFound, refund now s today "r" "o" 1m [ entry "o" "t1" 100m (d 5 1) false ])
    Assert.Equal(Error NotFound, refund now s today "r" "r0" 1m [ { entry "r0" "t1" -5m (d 5 1) true with RefundOf = Some "x" } ])
    Assert.Equal(Error DuplicateId, refund now s today "o" "o" 0m orig)
    Assert.Equal(Error InvalidAmount, refund now s today "r" "o" 0m orig)
