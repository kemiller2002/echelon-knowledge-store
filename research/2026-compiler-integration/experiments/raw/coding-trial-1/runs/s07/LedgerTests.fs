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

let t0 = DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles lastSeen =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = lastSeen }

[<Fact>]
let ``total rounds half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true; entry "e4" "t1" -3m (d 1 1) true ]
    Assert.Equal(7m, total "t1" es)

[<Fact>]
let ``approve allows session idle exactly 15 minutes`` () =
    let now = t0.AddMinutes 15.0
    let r = approve now (sess [ Approver ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    match r with
    | Ok [ e ] -> Assert.True(e.Approved); Assert.Equal(Some "u1", e.ApprovedBy)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``approve rejects session idle 15 minutes 1 second`` () =
    let now = t0.AddMinutes(15.0).AddSeconds 1.0
    Assert.Equal(Error SessionExpired, approve now (sess [ Approver ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve by Admin without Approver is NotAuthorized`` () =
    Assert.Equal(Error NotAuthorized, approve t0 (sess [ Admin ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve other tenant entry is NotFound`` () =
    Assert.Equal(Error NotFound, approve t0 (sess [ Approver ] t0) "e1" [ entry "e1" "t2" 1m (d 1 1) false ])
    Assert.Equal(Error NotFound, approve t0 (sess [ Approver ] t0) "zz" [])

[<Fact>]
let ``approve own entry is SelfApproval and approved is AlreadyApproved`` () =
    let own = { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve t0 (sess [ Approver ] t0) "e1" [ own ])
    Assert.Equal(Error AlreadyApproved, approve t0 (sess [ Approver ] t0) "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``approve changes only the target entry`` () =
    let a, b = entry "e1" "t1" 1m (d 1 1) false, entry "e2" "t1" 2m (d 1 1) false
    match approve t0 (sess [ Approver ] t0) "e2" [ a; b ] with
    | Ok [ x; y ] -> Assert.Equal(a, x); Assert.Equal({ b with Approved = true; ApprovedBy = Some "u1" }, y)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``view returns only own tenant even for Admin`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 2m (d 1 1) true; entry "e3" "t1" 3m (d 1 1) false ]
    match view t0 (sess [ Admin ] t0) es with
    | Ok r -> Assert.Equal<string list>([ "e1"; "e3" ], r |> List.map _.Id)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``view of expired session is SessionExpired`` () =
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) (sess [ Admin ] t0) [])
