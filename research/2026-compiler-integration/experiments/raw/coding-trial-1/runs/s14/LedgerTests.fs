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

let today = d 6 1
let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)

[<Fact>]
let ``balance includes approved entry dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``addEntry stores trimmed unapproved entry`` () =
    let e = { entry "n1" "t1" 5m today true with Description = "  hi  "; ApprovedBy = Some "x" }
    match addEntry today e [ entry "e1" "t1" 1m today true ] with
    | Ok [ _; n ] ->
        Assert.Equal("hi", n.Description)
        Assert.False n.Approved
        Assert.Equal(None, n.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``addEntry validation errors follow fixed order`` () =
    let e = entry "e1" "t1" 0m (d 12 1) false
    Assert.Equal(Error InvalidAmount, addEntry today e [ e ])
    Assert.Equal(Error InvalidAmount, addEntry today { e with Amount = 1000000.01m } [])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Amount = 1m; Description = "   " } [ e ])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Amount = 1m; Description = String('a', 201) } [])
    Assert.Equal(Error FutureDate, addEntry today { e with Amount = 1m } [ e ])
    Assert.Equal(Error DuplicateId, addEntry today { e with Amount = 1000000m; Date = today } [ e ])

[<Fact>]
let ``exportCsv formats approved rows`` () =
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "a,\"b\"" }
          entry "a" "t1" 1.5m (d 1 2) true
          entry "c" "t1" -5m (d 1 1) true
          entry "u" "t1" 9m (d 1 1) false
          entry "o" "t2" 9m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nc,2026-01-01,-5.00,x\na,2026-01-02,1.50,x\nb,2026-01-02,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" es)

let sess = { UserId = "u1"; TenantId = "t1"; Roles = [ Clerk ]; IssuedAt = now; LastSeen = now }

[<Fact>]
let ``refund caps at remainder`` () =
    let es = [ entry "o" "t1" 100m (d 1 1) true ]
    let step id amt es = refund now sess today id "o" amt es
    let r = step "r1" 60m es |> Result.bind (step "r2" 40m)
    match r with
    | Ok l ->
        Assert.Equal(3, l.Length)
        Assert.Equal(Error RefundExceedsOriginal, step "r3" 0.01m l)
    | e -> failwithf "%A" e
    Assert.Equal(Error RefundExceedsOriginal, step "r1" 100.01m es)

[<Fact>]
let ``refund checks in order`` () =
    let es = [ entry "o" "t1" 100m (d 1 1) true ]
    Assert.Equal(Error SessionExpired, refund (now.AddHours 1.0) sess today "r" "o" 1m es)
    Assert.Equal(Error NotAuthorized, refund now { sess with Roles = [ Approver ] } today "r" "o" 1m es)
    Assert.Equal(Error NotFound, refund now sess today "r" "zz" 1m es)
    Assert.Equal(Error NotFound, refund now { sess with TenantId = "t2" } today "r" "o" 1m es)
    Assert.Equal(Error DuplicateId, refund now sess today "o" "o" 0m es)
    Assert.Equal(Error InvalidAmount, refund now sess today "r" "o" 0m es)
