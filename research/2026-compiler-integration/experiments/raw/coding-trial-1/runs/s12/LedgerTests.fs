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

let addOk e es = match addEntry (d 6 1) e es with Ok r -> r | Error x -> failwithf "%A" x

[<Fact>]
let ``balance includes entry dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``addEntry amount bounds`` () =
    Assert.True(Result.isOk (addEntry (d 6 1) (entry "a" "t1" 1000000m (d 6 1) false) []))
    Assert.Equal(Error InvalidAmount, addEntry (d 6 1) (entry "a" "t1" 1000000.01m (d 6 1) false) [])
    Assert.Equal(Error InvalidAmount, addEntry (d 6 1) (entry "a" "t1" 0m (d 6 1) false) [])

[<Fact>]
let ``addEntry description, date, duplicate and order`` () =
    let e = entry "a" "t1" 1m (d 6 1) false
    Assert.True(Result.isOk (addEntry (d 6 1) { e with Description = String('x', 200) } []))
    Assert.Equal(Error InvalidDescription, addEntry (d 6 1) { e with Description = String('x', 201) } [])
    Assert.Equal(Error InvalidDescription, addEntry (d 6 1) { e with Description = "  " } [])
    Assert.Equal(Error FutureDate, addEntry (d 6 1) { e with Date = d 6 2 } [])
    Assert.Equal(Error DuplicateId, addEntry (d 6 1) e [ { e with TenantId = "t2" } ])
    Assert.Equal(Error InvalidAmount, addEntry (d 6 1) { e with Amount = 0m; Description = ""; Date = d 7 1 } [ e ])

[<Fact>]
let ``addEntry stores trimmed unapproved appended last`` () =
    let first = entry "z" "t1" 1m (d 1 1) true
    let e = { entry "a" "t1" 1m (d 6 1) true with Description = "  rent "; ApprovedBy = Some "u1" }
    let r = addOk e [ first ]
    Assert.Equal<Entry list>([ first; { e with Description = "rent"; Approved = false; ApprovedBy = None } ], r)

[<Fact>]
let ``exportCsv formats approved entries`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "a" "t1" 1m (d 1 1) false ])
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "a,\"b\"" }
          entry "c" "t1" -5m (d 1 1) true
          entry "B" "t1" 1.5m (d 1 2) true
          entry "o" "t2" 1m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nc,2026-01-01,-5.00,x\nB,2026-01-02,1.50,x\nb,2026-01-02,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" es)

open FxIdentity.Identity
let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now; LastSeen = now }
let orig = entry "o" "t1" 100m (d 5 1) true

[<Fact>]
let ``refund happy path and cap`` () =
    let s = sess [ Clerk ]
    let r1 = match refund now s (d 6 1) "r1" "o" 60m [ orig ] with Ok r -> r | Error x -> failwithf "%A" x
    Assert.Equal(
        { Id = "r1"; TenantId = "t1"; Amount = -60m; Description = "Refund of o"; Date = d 6 1; CreatedBy = "u1"
          Approved = false; ApprovedBy = None; RefundOf = Some "o" },
        List.last r1)
    let r2 = match refund now s (d 6 1) "r2" "o" 40m r1 with Ok r -> r | Error x -> failwithf "%A" x
    Assert.Equal(Error RefundExceedsOriginal, refund now s (d 6 1) "r3" "o" 0.01m r2)
    Assert.Equal(Error RefundExceedsOriginal, refund now s (d 6 1) "r3" "o" 100.01m [ orig ])

[<Fact>]
let ``refund rejections`` () =
    let s = sess [ Admin ]
    Assert.Equal(Error SessionExpired, refund now { s with LastSeen = now.AddSeconds -901.0 } (d 6 1) "r" "o" 1m [ orig ])
    Assert.Equal(Error NotAuthorized, refund now (sess [ Approver ]) (d 6 1) "r" "o" 1m [ orig ])
    Assert.Equal(Error NotFound, refund now s (d 6 1) "r" "o" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, refund now s (d 6 1) "r" "o" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, refund now s (d 6 1) "r" "o" 1m [ { orig with RefundOf = Some "x" } ])
    Assert.Equal(Error DuplicateId, refund now s (d 6 1) "o" "o" 1m [ orig ])
    Assert.Equal(Error InvalidAmount, refund now s (d 6 1) "r" "o" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, refund now s (d 6 1) "r" "o" -1m [ orig ])
