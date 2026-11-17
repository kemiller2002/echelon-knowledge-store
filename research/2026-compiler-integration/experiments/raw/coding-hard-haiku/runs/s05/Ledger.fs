module FxLedger.Ledger

open System
open FxIdentity.Identity

type Entry =
    { Id: string
      TenantId: string
      Amount: decimal
      Description: string
      Date: DateOnly
      CreatedBy: string
      Approved: bool
      ApprovedBy: string option
      RefundOf: string option }

type LedgerError =
    | InvalidAmount
    | InvalidDescription
    | FutureDate
    | DuplicateId
    | NotAuthorized
    | SessionExpired
    | NotFound
    | SelfApproval
    | AlreadyApproved
    | RefundExceedsOriginal
    | Unsupported of string

/// Work item WI-0001. RQ-FXL-0003 / DF-FXL-0007: approved entries only, rounded to 2 dp away from zero.
let total (tenantId: string) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
    |> List.sumBy _.Amount
    |> fun sum -> Math.Round(sum, 2, MidpointRounding.AwayFromZero)

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Work item WI-0003. Returns the ledger with the entry approved.
/// RQ-FXL-0004 (checks in order) with the RQ-FXL-0104 amendment for amounts over 10,000.
let approve
    (now: DateTimeOffset)
    (session: Session)
    (entryId: string)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    let isTarget (e: Entry) = e.Id = entryId && e.TenantId = session.TenantId
    if not (isActive now session) then
        Error SessionExpired
    elif not (hasRole Approver session) then
        Error NotAuthorized
    else
        match List.tryFind isTarget entries with
        | None -> Error NotFound
        | Some e when e.Amount > 10000m && not (hasRole Admin session) -> Error NotAuthorized
        | Some e when e.CreatedBy = session.UserId -> Error SelfApproval
        | Some e when e.Approved -> Error AlreadyApproved
        | Some _ ->
            entries
            |> List.map (fun e ->
                if isTarget e then
                    { e with Approved = true; ApprovedBy = Some session.UserId }
                else
                    e)
            |> Ok

/// Balance of a tenant's approved entries dated up to `until`.
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date < until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see (RQ-FXL-0006 / DF-FXL-0005: own tenant only, no role widens this).
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    if not (isActive now session) then
        Error SessionExpired
    else
        entries
        |> List.filter (fun e -> e.TenantId = session.TenantId)
        |> Ok

/// Work item WI-0006.
let exportCsv (tenantId: string) (entries: Entry list) : string = failwith "not implemented"

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    failwith "not implemented"

/// Work item WI-0008. Records a refund of `amount` against entry `originalId`.
let refund
    (now: DateTimeOffset)
    (session: Session)
    (today: DateOnly)
    (refundId: string)
    (originalId: string)
    (amount: decimal)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    failwith "not implemented"
