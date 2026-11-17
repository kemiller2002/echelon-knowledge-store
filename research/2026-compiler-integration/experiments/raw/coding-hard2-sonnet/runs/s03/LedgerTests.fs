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

let t0 = DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles lastSeen =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = lastSeen }

[<Fact>]
let ``total rounds half away from zero and counts approved own-tenant only`` () =
    Assert.Equal(0.01m, total "t1" [ entry "a" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "a" "t1" -0.005m (d 1 1) true ])
    Assert.Equal(0m, total "t1" [ entry "a" "t1" 5m (d 1 1) false; entry "b" "t2" 5m (d 1 1) true ])

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle`` () =
    let now = t0.AddMinutes 15.0
    match approve now (sess [ Approver ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) false ] with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``approve expired after 15 minutes 1 second`` () =
    let now = t0.AddMinutes(15.0).AddSeconds 1.0
    Assert.Equal(Error SessionExpired, approve now (sess [ Approver ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve admin only is not authorized`` () =
    Assert.Equal(Error NotAuthorized, approve t0 (sess [ Admin ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve other tenant is not found, self and double approval rejected`` () =
    let s = sess [ Approver ] t0
    Assert.Equal(Error NotFound, approve t0 s "e1" [ entry "e1" "t2" 1m (d 1 1) false ])
    Assert.Equal(Error SelfApproval, approve t0 s "e1" [ { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" } ])
    Assert.Equal(Error AlreadyApproved, approve t0 s "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``view admin sees only own tenant in order`` () =
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) false ]
    match view t0 (sess [ Admin ] t0) es with
    | Ok r -> Assert.Equal<string list>([ "a"; "c" ], r |> List.map _.Id)
    | r -> failwithf "%A" r

[<Fact>]
let ``view expired session`` () =
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) (sess [ Admin ] t0) [])
