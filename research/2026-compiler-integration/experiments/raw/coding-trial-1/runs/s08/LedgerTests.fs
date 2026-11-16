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
let sess user tenant roles (lastSeen: DateTimeOffset) =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = lastSeen }

[<Fact>]
let ``total rounds half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true; entry "e4" "t1" -0.5m (d 1 1) true ]
    Assert.Equal(0.5m, total "t1" es)

[<Fact>]
let ``approve works at exactly 15 minutes idle and expires after`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) false ]
    let s = sess "u1" "t1" [ Approver ] t0
    match approve (t0.AddMinutes 15.0) s "e1" es with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "unexpected %A" r
    Assert.Equal(Error SessionExpired, approve (t0.AddMinutes(15.0).AddSeconds(1.0)) s "e1" es)

[<Fact>]
let ``approve rejects admin without approver`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error NotAuthorized, approve t0 (sess "u1" "t1" [ Admin ] t0) "e1" es)

[<Fact>]
let ``approve error order and tenant hiding`` () =
    let es = [ entry "e1" "t2" 1m (d 1 1) false; entry "e2" "t1" 1m (d 1 1) false; { entry "e3" "t1" 1m (d 1 1) true with CreatedBy = "u2" } ]
    let s = sess "u1" "t1" [ Approver ] t0
    Assert.Equal(Error NotFound, approve t0 s "e1" es)
    Assert.Equal(Error NotFound, approve t0 s "zz" es)
    Assert.Equal(Error AlreadyApproved, approve t0 s "e3" es)
    Assert.Equal(Error SelfApproval, approve t0 (sess "u9" "t1" [ Approver ] t0) "e2" es)

[<Fact>]
let ``view returns only own tenant for admin and expired gets error`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true; entry "e3" "t1" 2m (d 1 1) false ]
    let s = sess "u1" "t1" [ Admin ] t0
    Assert.Equal(Ok [ es[0]; es[2] ], view t0 s es)
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) s es)
