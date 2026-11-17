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

/// Work item WI-0001.
let total (tenantId: string) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
    |> List.sumBy _.Amount
    |> fun x -> Math.Round(x, 2, MidpointRounding.AwayFromZero)

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Work item WI-0003. Returns the ledger with the entry approved.
let approve
    (now: DateTimeOffset)
    (session: Session)
    (entryId: string)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    if not (isActive now session) then Error SessionExpired
    elif not (hasRole Approver session) then Error NotAuthorized
    else
        match entries |> List.tryFind (fun e -> e.Id = entryId && e.TenantId = session.TenantId) with
        | None -> Error NotFound
        | Some e when e.CreatedBy = session.UserId -> Error SelfApproval
        | Some e when e.Approved -> Error AlreadyApproved
        | Some target ->
            entries
            |> List.map (fun e ->
                if obj.ReferenceEquals(e, target) then
                    { e with Approved = true; ApprovedBy = Some session.UserId }
                else e)
            |> Ok

/// Balance of a tenant's approved entries dated up to `until`.
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date < until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see.
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    if isActive now session then
        entries |> List.filter (fun e -> e.TenantId = session.TenantId) |> Ok
    else Error SessionExpired

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
