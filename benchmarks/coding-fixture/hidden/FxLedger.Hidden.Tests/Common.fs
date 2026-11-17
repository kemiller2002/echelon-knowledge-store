module Hidden.Common

open System
open FxIdentity.Identity
open FxLedger.Ledger

let t0 = DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero)
let today = DateOnly(2026, 10, 10)
let d (m: int) (day: int) = DateOnly(2026, m, day)

let entry id tenant amount date approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "author"; Approved = approved; ApprovedBy = None; RefundOf = None }

let session user tenant roles =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = t0 }

let isError expected (r: Result<'a, LedgerError>) =
    match r with
    | Error e -> e = expected
    | Ok _ -> false

let ok (r: Result<'a, LedgerError>) =
    match r with
    | Ok v -> v
    | Error e -> failwithf "expected Ok, got %A" e
