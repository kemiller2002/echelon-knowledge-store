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

let session roles tenant user =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = t0 }

[<Fact>]
let ``total rounds approved 0.005 to 0.01`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds approved -0.005 to -0.01`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" es)

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle`` () =
    let s = session [ Approver ] "t1" "u1"
    let r = approve (t0.AddMinutes 15.0) s "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    match r with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``approve gets SessionExpired at 15 minutes 1 second`` () =
    let s = session [ Approver ] "t1" "u1"
    let r = approve (t0.AddMinutes(15.0).AddSeconds 1.0) s "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error SessionExpired, r)

[<Fact>]
let ``approve by Admin without Approver is NotAuthorized`` () =
    let s = session [ Admin ] "t1" "u1"
    Assert.Equal(Error NotAuthorized, approve t0 s "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve other tenant entry is NotFound, missing is NotFound`` () =
    let s = session [ Approver ] "t1" "u1"
    Assert.Equal(Error NotFound, approve t0 s "e1" [ entry "e1" "t2" 1m (d 1 1) false ])
    Assert.Equal(Error NotFound, approve t0 s "zz" [])

[<Fact>]
let ``approve own entry is SelfApproval and approved is AlreadyApproved`` () =
    let s = session [ Approver ] "t1" "u9"
    Assert.Equal(Error SelfApproval, approve t0 s "e1" [ entry "e1" "t1" 1m (d 1 1) false ])
    let s2 = session [ Approver ] "t1" "u1"
    Assert.Equal(Error AlreadyApproved, approve t0 s2 "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``approve check order expired before role`` () =
    let s = session [ Viewer ] "t1" "u9"
    Assert.Equal(Error SessionExpired, approve (t0.AddHours 1.0) s "e1" [])

[<Fact>]
let ``approve changes nothing else`` () =
    let s = session [ Approver ] "t1" "u1"
    let e1 = entry "e1" "t1" 1m (d 1 1) false
    let e2 = entry "e2" "t1" 2m (d 1 1) false
    match approve t0 s "e1" [ e1; e2 ] with
    | Ok [ a; b ] -> Assert.Equal({ e1 with Approved = true; ApprovedBy = Some "u1" }, a); Assert.Equal(e2, b)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``view Admin sees only own tenant entries in order`` () =
    let s = session [ Admin ] "t1" "u1"
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) false ]
    match view t0 s es with
    | Ok r -> Assert.Equal<string list>([ "a"; "c" ], r |> List.map _.Id)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``view expired session gets SessionExpired`` () =
    let s = session [ Admin ] "t1" "u1"
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) s [ entry "a" "t1" 1m (d 1 1) true ])
