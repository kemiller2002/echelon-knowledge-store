module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
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

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let mkSession (uid: string) (tenant: string) (roles: Role list) : Session =
    { UserId = uid; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = t0 }

let isErr (expected: LedgerError) (r: Result<Entry list, LedgerError>) = r = Error expected

[<Fact>]
let ``total rounds approved midpoints away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total excludes unapproved and other tenant entries`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

[<Fact>]
let ``approve accepts a session idle for exactly 15 minutes`` () =
    let s = mkSession "u1" "t1" [ Approver ]
    let now = t0.AddMinutes 15.0
    match approve now s "e1" [ entry "e1" "t1" 5m (d 1 1) false ] with
    | Ok [ e ] ->
        Assert.True e.Approved
        Assert.Equal(Some "u1", e.ApprovedBy)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``approve rejects a session idle for 15 minutes and 1 second`` () =
    let s = mkSession "u1" "t1" [ Approver ]
    let now = t0.AddMinutes(15.0).AddSeconds 1.0
    Assert.True(isErr SessionExpired (approve now s "e1" [ entry "e1" "t1" 5m (d 1 1) false ]))

[<Fact>]
let ``approve rejects an Admin without Approver`` () =
    let s = mkSession "u1" "t1" [ Admin ]
    Assert.True(isErr NotAuthorized (approve t0 s "e1" [ entry "e1" "t1" 5m (d 1 1) false ]))

[<Fact>]
let ``approve over 10000 without Admin is NotAuthorized`` () =
    let s = mkSession "u1" "t1" [ Approver ]
    Assert.True(isErr NotAuthorized (approve t0 s "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ]))

[<Fact>]
let ``approve of exactly 10000 needs only Approver`` () =
    let s = mkSession "u1" "t1" [ Approver ]
    Assert.True(Result.isOk (approve t0 s "e1" [ entry "e1" "t1" 10000m (d 1 1) false ]))

[<Fact>]
let ``view returns only the session tenant entries in original order`` () =
    let s = mkSession "u1" "t1" [ Admin ]
    let entries = [ entry "e1" "t2" 1m (d 1 1) true; entry "e2" "t1" 2m (d 1 2) true; entry "e3" "t1" 3m (d 1 3) false ]
    match view t0 s entries with
    | Ok visible -> Assert.Equal<string list>([ "e2"; "e3" ], visible |> List.map _.Id)
    | Error err -> failwithf "unexpected %A" err

[<Fact>]
let ``view returns SessionExpired for an expired session`` () =
    let s = mkSession "u1" "t1" [ Admin ]
    Assert.True(isErr SessionExpired (view (t0.AddMinutes 16.0) s [ entry "e1" "t1" 1m (d 1 1) true ]))
