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

let sess user tenant roles (lastSeen: DateTimeOffset) =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0.AddMinutes -30.0; LastSeen = lastSeen }

// WI-0001 / RQ-FXL-0003
[<Fact>]
let ``total rounds approved 0.005 to 0.01 and -0.005 to -0.01`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants entries`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true
               entry "e4" "t1" -3m (d 1 1) true ]
    Assert.Equal(7m, total "t1" es)

// WI-0003 / RQ-FXL-0004 + RQ-FXL-0104
let ap = [ Approver ]
let one amt = [ { entry "e1" "t1" amt (d 1 1) false with CreatedBy = "author" } ]

[<Fact>]
let ``approve session idle exactly 15 minutes can approve`` () =
    let s = sess "u1" "t1" ap (t0.AddMinutes -15.0)
    match approve t0 s "e1" (one 5m) with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``approve session idle 15 minutes 1 second is SessionExpired`` () =
    let s = sess "u1" "t1" ap (t0.AddSeconds -901.0)
    Assert.Equal(Error SessionExpired, approve t0 s "e1" (one 5m))

[<Fact>]
let ``approve Admin without Approver is NotAuthorized`` () =
    let s = sess "u1" "t1" [ Admin ] t0
    Assert.Equal(Error NotAuthorized, approve t0 s "e1" (one 5m))

[<Fact>]
let ``approve entry of other tenant is NotFound`` () =
    let s = sess "u1" "t2" ap t0
    Assert.Equal(Error NotFound, approve t0 s "e1" (one 5m))

[<Fact>]
let ``approve self approval and already approved`` () =
    let s = sess "author" "t1" ap t0
    Assert.Equal(Error SelfApproval, approve t0 s "e1" (one 5m))
    let s2 = sess "u1" "t1" ap t0
    let approved = [ { (one 5m).Head with Approved = true } ]
    Assert.Equal(Error AlreadyApproved, approve t0 s2 "e1" approved)

[<Fact>]
let ``approve high value 10000.01 needs Admin too`` () =
    let s = sess "u1" "t1" ap t0
    Assert.Equal(Error NotAuthorized, approve t0 s "e1" (one 10000.01m))
    let s2 = sess "u1" "t1" [ Approver; Admin ] t0
    Assert.True(Result.isOk (approve t0 s2 "e1" (one 10000.01m)))

[<Fact>]
let ``approve entry of exactly 10000 needs only Approver`` () =
    let s = sess "u1" "t1" ap t0
    Assert.True(Result.isOk (approve t0 s "e1" (one 10000m)))

// WI-0005 / RQ-FXL-0006
[<Fact>]
let ``view Admin of t1 sees only t1 entries in order`` () =
    let s = sess "u1" "t1" [ Admin ] t0
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) false ]
    match view t0 s es with
    | Ok r -> Assert.Equal<string list>([ "a"; "c" ], r |> List.map _.Id)
    | e -> failwithf "unexpected %A" e

[<Fact>]
let ``view expired session gets SessionExpired`` () =
    let s = sess "u1" "t1" [ Admin ] (t0.AddMinutes -16.0)
    Assert.Equal(Error SessionExpired, view t0 s [])
