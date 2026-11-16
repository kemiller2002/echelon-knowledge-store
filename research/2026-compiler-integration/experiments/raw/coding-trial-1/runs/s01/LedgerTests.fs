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

// ---- WI-0001 / WI-0003 / WI-0005 ----
open FxIdentity.Identity

let t0 = DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)

let sess user tenant roles lastSeen =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = lastSeen }

[<Fact>]
let ``total rounds 0.005 away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds -0.005 away from zero`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" es)

[<Fact>]
let ``approve allows session idle exactly 15 minutes`` () =
    let s = sess "u1" "t1" [ Approver ] t0
    let r = approve (t0.AddMinutes 15.0) s "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    match r with
    | Ok [ e ] -> Assert.True(e.Approved); Assert.Equal(Some "u1", e.ApprovedBy)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``approve rejects session idle 15 minutes 1 second`` () =
    let s = sess "u1" "t1" [ Approver ] t0
    Assert.Equal(Error SessionExpired, approve (t0.AddSeconds 901.0) s "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve admin without approver is not authorized`` () =
    let s = sess "u1" "t1" [ Admin ] t0
    Assert.Equal(Error NotAuthorized, approve t0 s "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve other tenant entry is not found`` () =
    let s = sess "u1" "t1" [ Approver ] t0
    Assert.Equal(Error NotFound, approve t0 s "e1" [ entry "e1" "t2" 1m (d 1 1) false ])

[<Fact>]
let ``approve own entry and already approved`` () =
    let s = sess "u9" "t1" [ Approver ] t0
    Assert.Equal(Error SelfApproval, approve t0 s "e1" [ entry "e1" "t1" 1m (d 1 1) false ])
    let s2 = sess "u1" "t1" [ Approver ] t0
    Assert.Equal(Error AlreadyApproved, approve t0 s2 "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``view admin sees only own tenant in order`` () =
    let s = sess "u1" "t1" [ Admin ] t0
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true; entry "e3" "t1" 1m (d 1 1) false ]
    match view t0 s es with
    | Ok r -> Assert.Equal<string list>([ "e1"; "e3" ], r |> List.map _.Id)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``view expired session`` () =
    let s = sess "u1" "t1" [ Admin ] t0
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) s [])
