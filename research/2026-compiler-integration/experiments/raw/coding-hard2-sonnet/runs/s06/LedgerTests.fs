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

let sess roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = t0 }

[<Fact>]
let ``total rounds midpoints away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true; entry "e4" "t1" -3m (d 1 1) true ]
    Assert.Equal(7m, total "t1" es)

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false ]
    match approve (t0.AddMinutes 15.0) (sess [ Approver ]) "e1" es with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``approve expires after 15 minutes 1 second`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false ]
    Assert.Equal(Error SessionExpired, approve (t0.AddMinutes(15.0).AddSeconds 1.0) (sess [ Approver ]) "e1" es)

[<Fact>]
let ``approve rejects Admin without Approver`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false ]
    Assert.Equal(Error NotAuthorized, approve t0 (sess [ Admin ]) "e1" es)

[<Fact>]
let ``approve other tenant is NotFound, self approval and repeat rejected`` () =
    let s = sess [ Approver ]
    Assert.Equal(Error NotFound, approve t0 s "e1" [ entry "e1" "t2" 1m (d 1 1) false ])
    Assert.Equal(Error NotFound, approve t0 s "zz" [])
    Assert.Equal(Error SelfApproval, approve t0 s "e1" [ { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" } ])
    Assert.Equal(Error AlreadyApproved, approve t0 s "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``view returns only own tenant for Admin, in order`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true; entry "e3" "t1" 1m (d 1 1) false ]
    match view t0 (sess [ Admin ]) es with
    | Ok r -> Assert.Equal<string list>([ "e1"; "e3" ], r |> List.map _.Id)
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``view expired session`` () =
    Assert.Equal(Error SessionExpired, view (t0.AddHours 1.0) (sess [ Admin ]) [])
