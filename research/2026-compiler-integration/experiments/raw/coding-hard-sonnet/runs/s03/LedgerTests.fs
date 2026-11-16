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

let t0 = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)

let sess user tenant roles lastSeen =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0.AddHours -1.0; LastSeen = lastSeen }

[<Fact>]
let ``total rounds midpoints away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "a" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "a" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    Assert.Equal(3m, total "t1" [ entry "a" "t1" 3m (d 1 1) true; entry "b" "t1" 4m (d 1 1) false; entry "c" "t2" 5m (d 1 1) true ])

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle and expires after`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false ]
    let ok = sess "u1" "t1" [ Approver ] (t0.AddMinutes -15.0)
    match approve t0 ok "e1" es with
    | Ok [ e ] -> Assert.True(e.Approved); Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "%A" r
    let old = sess "u1" "t1" [ Approver ] (t0.AddMinutes(-15.0).AddSeconds -1.0)
    Assert.Equal(Error SessionExpired, approve t0 old "e1" es)

[<Fact>]
let ``approve rules`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false; entry "big" "t1" 10000.01m (d 1 1) false; entry "x" "t2" 1m (d 1 1) false ]
    let s roles = sess "u1" "t1" roles t0
    Assert.Equal(Error NotAuthorized, approve t0 (s [ Admin ]) "e1" es)
    Assert.Equal(Error NotFound, approve t0 (s [ Approver ]) "x" es)
    Assert.Equal(Error NotFound, approve t0 (s [ Approver ]) "zz" es)
    Assert.Equal(Error NotAuthorized, approve t0 (s [ Approver ]) "big" es)
    Assert.True(Result.isOk (approve t0 (s [ Approver; Admin ]) "big" es))
    Assert.Equal(Error SelfApproval, approve t0 (sess "u9" "t1" [ Approver ] t0) "e1" es)
    let done' = [ { entry "e1" "t1" 10m (d 1 1) true with Approved = true } ]
    Assert.Equal(Error AlreadyApproved, approve t0 (s [ Approver ]) "e1" done')

[<Fact>]
let ``view is tenant isolated even for admin`` () =
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) true ]
    match view t0 (sess "u1" "t1" [ Admin ] t0) es with
    | Ok r -> Assert.Equal<string list>([ "a"; "c" ], r |> List.map _.Id)
    | r -> failwithf "%A" r
    Assert.Equal(Error SessionExpired, view t0 (sess "u1" "t1" [ Admin ] (t0.AddHours -1.0)) es)
