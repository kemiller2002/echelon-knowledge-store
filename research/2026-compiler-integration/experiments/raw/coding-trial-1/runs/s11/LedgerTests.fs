module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxLedger.Ledger
open FxIdentity.Identity

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
let add e es = addEntry today e es

[<Fact>]
let ``addEntry accepts boundary amount and rejects above or zero`` () =
    Assert.True(Result.isOk (add (entry "a" "t1" 1000000m today false) []))
    Assert.Equal(Error InvalidAmount, add (entry "a" "t1" 1000000.01m today false) [])
    Assert.Equal(Error InvalidAmount, add (entry "a" "t1" 0m today false) [])

[<Fact>]
let ``addEntry description length and date rules`` () =
    let e = entry "a" "t1" 1m today false
    Assert.True(Result.isOk (add { e with Description = " " + String('x', 200) + " " } []))
    Assert.Equal(Error InvalidDescription, add { e with Description = String('x', 201) } [])
    Assert.Equal(Error InvalidDescription, add { e with Description = "  " } [])
    Assert.Equal(Error FutureDate, add { e with Date = today.AddDays 1 } [])

[<Fact>]
let ``addEntry duplicate id across tenants`` () =
    Assert.Equal(Error DuplicateId, add (entry "a" "t1" 1m today false) [ entry "a" "t2" 1m today false ])

[<Fact>]
let ``addEntry reports first failing rule in fixed order`` () =
    let bad = { entry "a" "t1" 0m (today.AddDays 1) false with Description = "" }
    Assert.Equal(Error InvalidAmount, add bad [ entry "a" "t1" 1m today false ])
    Assert.Equal(Error InvalidDescription, add { bad with Amount = 1m } [ entry "a" "t1" 1m today false ])
    Assert.Equal(Error FutureDate, add { bad with Amount = 1m; Description = "x" } [ entry "a" "t1" 1m today false ])

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "z" "t1" 1m today true ]
    let e = { entry "a" "t1" 5m today true with Description = "  rent "; ApprovedBy = Some "u1" }
    match add e existing with
    | Ok [ first; last ] ->
        Assert.Equal(existing.Head, first)
        Assert.Equal("rent", last.Description)
        Assert.False last.Approved
        Assert.Equal(None, last.ApprovedBy)
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``exportCsv format`` () =
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "a,\"b\"" }
          entry "a" "t1" -5m (d 1 2) true
          entry "c" "t1" 1.5m (d 1 1) true
          entry "u" "t1" 9m (d 1 1) false
          entry "o" "t2" 9m (d 1 1) true ]
    let expected =
        "id,date,amount,description\nc,2026-01-01,1.50,x\na,2026-01-02,-5.00,x\nb,2026-01-02,5.00,\"a,\"\"b\"\"\"\n"
    Assert.Equal(expected, exportCsv "t1" es)

[<Fact>]
let ``exportCsv with no approved entries is header only`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "u" "t1" 9m (d 1 1) false ])

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let session roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddMinutes -30.0; LastSeen = now }
let orig = entry "o1" "t1" 100m (d 5 1) true
let doRefund s id amt es = refund now s today id "o1" amt es

[<Fact>]
let ``refund cumulative cap`` () =
    let s = session [ Clerk ]
    let r1 = doRefund s "r1" 60m [ orig ] |> Result.defaultWith (fun _ -> failwith "r1")
    let r2 = doRefund s "r2" 40m r1 |> Result.defaultWith (fun _ -> failwith "r2")
    Assert.Equal(Error RefundExceedsOriginal, doRefund s "r3" 0.01m r2)
    Assert.Equal(Error RefundExceedsOriginal, doRefund s "r3" 100.01m [ orig ])
    let last = List.last r2
    Assert.Equal(-40m, last.Amount)
    Assert.Equal("Refund of o1", last.Description)
    Assert.Equal(Some "o1", last.RefundOf)
    Assert.False last.Approved

[<Fact>]
let ``refund checks in order`` () =
    let stale = { session [ Clerk ] with LastSeen = now.AddMinutes -15.0 - TimeSpan.FromSeconds 1.0 }
    Assert.Equal(Error SessionExpired, doRefund stale "r" 1m [ orig ])
    Assert.True(Result.isOk (doRefund { stale with LastSeen = now.AddMinutes -15.0 } "r" 1m [ orig ]))
    Assert.Equal(Error NotAuthorized, doRefund (session [ Approver; Viewer ]) "r" 1m [ orig ])
    Assert.True(Result.isOk (doRefund (session [ Admin ]) "r" 1m [ orig ]))
    Assert.Equal(Error NotFound, doRefund (session [ Clerk ]) "r" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, doRefund (session [ Clerk ]) "r" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, doRefund (session [ Clerk ]) "r" 1m [ { orig with RefundOf = Some "x" } ])
    Assert.Equal(Error DuplicateId, doRefund (session [ Clerk ]) "o1" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, doRefund (session [ Clerk ]) "r" 0m [ orig ])
