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

let t0 = DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = t0 }

[<Fact>]
let ``total rounds half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])
    Assert.Equal(1m, total "t1" [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 5m (d 1 1) true ])

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle`` () =
    match approve (t0.AddMinutes 15.0) (sess [ Approver ]) "e1" [ entry "e1" "t1" 1m (d 1 1) false ] with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``approve expired, admin-only, other tenant, self and already approved`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error SessionExpired, approve (t0.AddMinutes 15.0 + TimeSpan.FromSeconds 1.0) (sess [ Approver ]) "e1" es)
    Assert.Equal(Error NotAuthorized, approve t0 (sess [ Admin ]) "e1" es)
    Assert.Equal(Error NotFound, approve t0 (sess [ Approver ]) "e1" [ entry "e1" "t2" 1m (d 1 1) false ])
    Assert.Equal(Error SelfApproval, approve t0 (sess [ Approver ]) "e1" [ { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" } ])
    Assert.Equal(Error AlreadyApproved, approve t0 (sess [ Approver ]) "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``view is tenant isolated even for admin`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true ]
    Assert.Equal(Ok [ es.[0] ], view t0 (sess [ Admin ]) es)
    Assert.Equal(Error SessionExpired, view (t0.AddHours 1.0) (sess [ Admin ]) es)
