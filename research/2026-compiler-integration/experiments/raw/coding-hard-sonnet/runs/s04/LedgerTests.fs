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
let ent id amt desc = { entry id "t1" amt (d 5 1) false with Description = desc }

[<Fact>]
let ``addEntry validates amount description date duplicate in order`` () =
    Assert.Equal(Ok [ { ent "a" 1000000m "rent" with Approved = false } ], addEntry today { ent "a" 1000000m "  rent " with Approved = true } [])
    Assert.Equal(Error InvalidAmount, addEntry today (ent "a" 1000000.01m "x") [])
    Assert.Equal(Error InvalidAmount, addEntry today (ent "a" 0m "") [])
    Assert.Equal(Ok [ ent "a" 1m (String('x', 200)) ], addEntry today (ent "a" 1m (" " + String('x', 200))) [])
    Assert.Equal(Error InvalidDescription, addEntry today (ent "a" 1m (String('x', 201))) [])
    Assert.Equal(Error InvalidDescription, addEntry today (ent "a" 1m "a\tb") [])
    Assert.Equal(Error InvalidDescription, addEntry today (ent "a" 1m "   ") [])
    Assert.True((addEntry today { ent "a" 1m "x" with Date = today } []).IsOk)
    Assert.Equal(Error FutureDate, addEntry today { ent "a" 1m "x" with Date = today.AddDays 1 } [])
    Assert.Equal(Error DuplicateId, addEntry today (ent "a" 1m "x") [ { ent "a" 1m "y" with TenantId = "t2" } ])
    Assert.Equal(Error InvalidAmount, addEntry today { ent "a" 0m "" with Date = today.AddDays 1 } [])

[<Fact>]
let ``addEntry appends last keeping order`` () =
    let r = addEntry today (ent "b" 2m "x") [ ent "a" 1m "x" ]
    Assert.Equal<string list>([ "a"; "b" ], r |> Result.map (List.map _.Id) |> Result.defaultValue [])

[<Fact>]
let ``exportCsv formats rows`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m (d 1 1) false ])
    let es =
        [ { entry "b" "t1" -5m (d 1 1) true with Description = "a,\"b\"" }
          { entry "a" "t1" 1.5m (d 1 1) true with Description = "=SUM(A1)" }
          { entry "c" "t1" 2m (d 1 1) true with Description = "-x,y" }
          entry "z" "t2" 2m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\na,2026-01-01,1.50,'=SUM(A1)\nb,2026-01-01,-5.00,\"a,\"\"b\"\"\"\nc,2026-01-01,2.00,\"'-x,y\"\n", exportCsv "t1" es)

open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now; LastSeen = now }
let orig = { entry "o" "t1" 100m (d 5 1) true with Description = "o" }

[<Fact>]
let ``refund checks and cap`` () =
    let s = sess [ Clerk ]
    let r1 = refund now s today "r1" "o" 60m [ orig ]
    Assert.True r1.IsOk
    let l1 = Result.defaultValue [] r1
    Assert.Equal(-60m, (List.last l1).Amount)
    Assert.Equal(Some "o", (List.last l1).RefundOf)
    let l2 = Result.defaultValue [] (refund now s today "r2" "o" 40m l1)
    Assert.Equal(Error RefundExceedsOriginal, refund now s today "r3" "o" 0.01m l2)
    Assert.Equal(Error InvalidAmount, refund now s today "r3" "o" 0m [ orig ])
    Assert.Equal(Error DuplicateId, refund now s today "o" "o" 1m [ orig ])
    Assert.Equal(Error NotAuthorized, refund now (sess [ Viewer ]) today "r" "o" 1m [ orig ])
    Assert.Equal(Error SessionExpired, refund now { s with LastSeen = now.AddMinutes -16.0 } today "r" "o" 1m [ orig ])
    Assert.Equal(Error NotFound, refund now s today "r" "o" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, refund now { s with TenantId = "t9" } today "r" "o" 1m [ orig ])
    Assert.Equal(Error NotFound, refund now s today "r" "r1" 1m l1)

[<Fact>]
let ``refund window is 90 days`` () =
    let s = sess [ Admin ]
    let o = { orig with Date = today.AddDays -90 }
    Assert.True((refund now s today "r" "o" 1m [ o ]).IsOk)
    Assert.Equal(Error(Unsupported "refund window closed"), refund now s today "r" "o" 1m [ { o with Date = today.AddDays -91 } ])
