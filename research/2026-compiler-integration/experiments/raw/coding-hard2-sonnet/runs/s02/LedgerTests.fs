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

open FxIdentity.Identity

let today = d 6 30

[<Fact>]
let ``addEntry stores trimmed unapproved entry`` () =
    let e = { entry "n1" "t1" 5m (d 6 1) true with Description = "  hi  "; ApprovedBy = Some "x" }
    match addEntry today e [ entry "e1" "t1" 1m (d 1 1) true ] with
    | Ok [ _; n ] ->
        Assert.Equal("hi", n.Description)
        Assert.False n.Approved
        Assert.Equal(None, n.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``addEntry validation order and rules`` () =
    let e = entry "n1" "t1" 5m (d 6 1) false
    Assert.Equal(Error InvalidAmount, addEntry today { e with Amount = 0m; Description = "" } [])
    Assert.Equal(Error InvalidAmount, addEntry today { e with Amount = 1000000.01m } [])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Description = "  "; Date = d 12 1 } [])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Description = "a\tb" } [])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Description = String('a', 201) } [])
    Assert.Equal(Error FutureDate, addEntry today { e with Date = d 7 1 } [ e ])
    Assert.Equal(Error DuplicateId, addEntry today e [ { e with TenantId = "t2" } ])

[<Fact>]
let ``balance includes entry dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 2 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``exportCsv format`` () =
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "=SUM(A1)" }
          { entry "a" "t1" -5m (d 1 2) true with Description = "-5,x" }
          { entry "c" "t1" 1.5m (d 1 1) true with Description = "say \"hi\"" }
          entry "z" "t1" 1m (d 1 1) false
          entry "y" "t2" 1m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nc,2026-01-01,1.50,\"say \"\"hi\"\"\"\na,2026-01-02,-5.00,\"'-5,x\"\nb,2026-01-02,5.00,'=SUM(A1)\n", exportCsv "t1" es)
    Assert.Equal("id,date,amount,description\n", exportCsv "none" es)

let now = DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero)
let sess roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now; LastSeen = now }

[<Fact>]
let ``refund rules`` () =
    let orig = entry "o" "t1" 100m (d 6 30) true
    let r id amt es = refund now (sess [ Clerk ]) today id "o" amt es
    let ok = function Ok es -> es | e -> failwithf "%A" e
    let es = [ orig ] |> r "r1" 60m |> ok |> r "r2" 40m |> ok
    Assert.Equal(Error RefundExceedsOriginal, r "r3" 0.01m es)
    Assert.Equal(Error RefundExceedsOriginal, r "r1" 101m [ orig ] |> Result.map ignore |> fun _ -> r "rx" 101m [ orig ])
    let last = List.last es
    Assert.Equal(-40m, last.Amount)
    Assert.Equal("Refund of o", last.Description)
    Assert.Equal(Some "o", last.RefundOf)
    Assert.Equal(Error DuplicateId, r "o" 1m [ orig ])
    Assert.Equal(Error InvalidAmount, r "rz" 0m [ orig ])
    Assert.Equal(Error NotAuthorized, refund now (sess [ Viewer ]) today "r" "o" 1m [ orig ])
    Assert.Equal(Error SessionExpired, refund (now.AddHours 9.0) (sess [ Clerk ]) today "r" "o" 1m [ orig ])
    Assert.Equal(Error NotFound, r "r" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, r "r" 1m [ { orig with TenantId = "t2" } ])

[<Fact>]
let ``refund window 90 days`` () =
    let at days = [ entry "o" "t1" 100m (today.AddDays(-days)) true ]
    Assert.True(Result.isOk (refund now (sess [ Admin ]) today "r" "o" 1m (at 90)))
    Assert.Equal(Error(Unsupported "refund window closed"), refund now (sess [ Admin ]) today "r" "o" 1m (at 91))
