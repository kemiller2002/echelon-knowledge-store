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

/// Work item WI-0001. Sum of the tenant's approved entries, rounded to 2 places
/// with midpoints away from zero (RQ-FXL-0003, DF-FXL-0007).
let total (tenantId: string) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
    |> List.sumBy _.Amount
    |> fun sum -> Math.Round(sum, 2, MidpointRounding.AwayFromZero)

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Work item WI-0003. Returns the ledger with the entry approved.
/// Checks in order (RQ-FXL-0004, amended by RQ-FXL-0104): session active,
/// Approver role, entry found in the session's tenant, high-value entries
/// (> 10,000) also need Admin, not self-approved, not already approved.
let approve
    (now: DateTimeOffset)
    (session: Session)
    (entryId: string)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    if not (isActive now session) then
        Error SessionExpired
    elif not (hasRole Approver session) then
        Error NotAuthorized
    else
        match entries |> List.tryFind (fun e -> e.Id = entryId && e.TenantId = session.TenantId) with
        | None -> Error NotFound
        | Some entry ->
            if entry.Amount > 10000m && not (hasRole Admin session) then
                Error NotAuthorized
            elif entry.CreatedBy = session.UserId then
                Error SelfApproval
            elif entry.Approved then
                Error AlreadyApproved
            else
                let approved =
                    { entry with Approved = true; ApprovedBy = Some session.UserId }
                Ok(entries |> List.map (fun e -> if e.Id = entryId && e.TenantId = session.TenantId then approved else e))

/// Balance of a tenant's approved entries dated up to `until`.
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date < until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see (RQ-FXL-0006, DF-FXL-0005):
/// the session's own tenant only, for every role, in original order.
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    if not (isActive now session) then
        Error SessionExpired
    else
        Ok(entries |> List.filter (fun e -> e.TenantId = session.TenantId))

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
