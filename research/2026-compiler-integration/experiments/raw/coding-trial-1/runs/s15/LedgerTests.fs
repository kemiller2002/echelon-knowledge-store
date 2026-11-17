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

// ---- WI-0001 total
[<Fact>]
let ``total rounds approved 0.005 to 0.01`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds approved -0.005 to -0.01`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true; entry "e4" "t1" -3m (d 1 1) true ]
    Assert.Equal(7m, total "t1" es)

// ---- sessions
open FxIdentity.Identity

let t0 = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let sess user tenant roles =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = t0 }

// ---- WI-0003 approve
[<Fact>]
let ``approve allows session idle exactly 15 minutes`` () =
    let r = approve (t0.AddMinutes 15.0) (sess "u1" "t1" [ Approver ]) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    match r with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``approve rejects session idle 15 minutes 1 second`` () =
    let r = approve (t0.AddMinutes(15.0).AddSeconds 1.0) (sess "u1" "t1" [ Approver ]) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error SessionExpired, r)

[<Fact>]
let ``approve Admin without Approver is NotAuthorized`` () =
    let r = approve t0 (sess "u1" "t1" [ Admin ]) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error NotAuthorized, r)

[<Fact>]
let ``approve other tenant entry is NotFound`` () =
    let r = approve t0 (sess "u1" "t1" [ Approver ]) "e1" [ entry "e1" "t2" 1m (d 1 1) false ]
    Assert.Equal(Error NotFound, r)

[<Fact>]
let ``approve self and already approved`` () =
    let own = { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve t0 (sess "u1" "t1" [ Approver ]) "e1" [ own ])
    Assert.Equal(Error AlreadyApproved, approve t0 (sess "u1" "t1" [ Approver ]) "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``approve expiry is checked before role`` () =
    Assert.Equal(Error SessionExpired, approve (t0.AddHours 1.0) (sess "u1" "t1" [ Viewer ]) "e1" [])

// ---- WI-0005 view
[<Fact>]
let ``view Admin sees only own tenant in order`` () =
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) false ]
    match view t0 (sess "u1" "t1" [ Admin ]) es with
    | Ok r -> Assert.Equal<string list>([ "a"; "c" ], r |> List.map _.Id)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``view expired session gets SessionExpired`` () =
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) (sess "u1" "t1" [ Admin ]) [])
