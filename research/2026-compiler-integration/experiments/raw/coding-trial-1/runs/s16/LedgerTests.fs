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
let ``balance includes approved entry dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

let today = d 6 1
let ok r = match r with Ok v -> v | Error e -> failwithf "%A" e

[<Fact>]
let ``addEntry amount bounds`` () =
    Assert.True(Result.isOk (addEntry today (entry "n" "t1" 1000000m today false) []))
    Assert.Equal(Error InvalidAmount, addEntry today (entry "n" "t1" 1000000.01m today false) [])
    Assert.Equal(Error InvalidAmount, addEntry today (entry "n" "t1" 0m today false) [])

[<Fact>]
let ``addEntry description date and duplicate`` () =
    let e = entry "n" "t1" 1m today false
    Assert.True(Result.isOk (addEntry today { e with Description = " " + String('a', 200) + " " } []))
    Assert.Equal(Error InvalidDescription, addEntry today { e with Description = String('a', 201) } [])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Description = "  " } [])
    Assert.Equal(Error FutureDate, addEntry today { e with Date = today.AddDays 1 } [])
    Assert.Equal(Error DuplicateId, addEntry today e [ entry "n" "t2" 1m today true ])

[<Fact>]
let ``addEntry validation order`` () =
    let e = { entry "n" "t1" 0m (today.AddDays 1) false with Description = "" }
    Assert.Equal(Error InvalidAmount, addEntry today e [ e ])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Amount = 1m } [ e ])
    Assert.Equal(Error FutureDate, addEntry today { e with Amount = 1m; Description = "x" } [ e ])

[<Fact>]
let ``addEntry stores trimmed unapproved appended last`` () =
    let e = { entry "n" "t1" 5m today true with Description = "  rent "; ApprovedBy = Some "u" }
    let existing = [ entry "a" "t1" 1m today true ]
    let r = ok (addEntry today e existing)
    Assert.Equal<Entry list>(existing @ [ { e with Description = "rent"; Approved = false; ApprovedBy = None } ], r)

[<Fact>]
let ``exportCsv format`` () =
    let e id date amt desc ap t = { entry id t amt date ap with Description = desc }
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ e "a" today 1m "x" false "t1" ])
    let csv =
        exportCsv "t1" [ e "b" (d 1 2) 5m "a,\"b\"" true "t1"; e "B" (d 1 2) -5m "r" true "t1"
                         e "a" (d 1 1) 1.5m "ok" true "t1"; e "z" (d 1 1) 1m "o" true "t2" ]
    Assert.Equal("id,date,amount,description\na,2026-01-01,1.50,ok\nB,2026-01-02,-5.00,r\nb,2026-01-02,5.00,\"a,\"\"b\"\"\"\n", csv)

open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddMinutes -5.0; LastSeen = now.AddMinutes -1.0 }
let orig = entry "o" "t1" 100m (d 5 1) true
let rf s id amt es = refund now s today id "o" amt es

[<Fact>]
let ``refund checks in order`` () =
    let s = sess [ Clerk ]
    Assert.Equal(Error SessionExpired, rf { s with LastSeen = now.AddMinutes -16.0 } "r" 1m [ orig ])
    Assert.Equal(Error NotAuthorized, rf (sess [ Approver ]) "r" 1m [ orig ])
    Assert.Equal(Error NotFound, rf s "r" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, rf s "r" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, rf s "r" 1m [ { orig with RefundOf = Some "x" } ])
    Assert.Equal(Error DuplicateId, rf s "o" 1m [ orig ])
    Assert.Equal(Error InvalidAmount, rf s "r" 0m [ orig ])

[<Fact>]
let ``refund ceiling and stored form`` () =
    let s = sess [ Admin ]
    let es1 = ok (rf s "r1" 60m [ orig ])
    let es2 = ok (rf s "r2" 40m es1)
    Assert.Equal(Error RefundExceedsOriginal, rf s "r3" 0.01m es2)
    Assert.Equal(Error RefundExceedsOriginal, rf s "r9" 100.01m [ orig ])
    let last = List.last es1
    Assert.Equal({ Id = "r1"; TenantId = "t1"; Amount = -60m; Description = "Refund of o"; Date = today
                   CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = Some "o" }, last)
