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
    let sum =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sumBy _.Amount

    Math.Round(sum, 2, MidpointRounding.AwayFromZero)

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let description = entry.Description.Trim()

    if entry.Amount <= 0m || entry.Amount > 1000000m then Error InvalidAmount
    elif description.Length = 0 || description.Length > 200 || description |> Seq.exists Char.IsControl then Error InvalidDescription
    elif entry.Date > today then Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then Error DuplicateId
    else Ok(entries @ [ { entry with Description = description; Approved = false; ApprovedBy = None } ])

/// Work item WI-0003. Returns the ledger with the entry approved.
let approve
    (now: DateTimeOffset)
    (session: Session)
    (entryId: string)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    let target = entries |> List.tryFind (fun e -> e.Id = entryId && e.TenantId = session.TenantId)

    if not (isActive now session) then Error SessionExpired
    elif not (hasRole Approver session) then Error NotAuthorized
    else
        match target with
        | None -> Error NotFound
        | Some e when e.Amount > 10000m && not (hasRole Admin session) -> Error NotAuthorized
        | Some e when e.CreatedBy = session.UserId -> Error SelfApproval
        | Some e when e.Approved -> Error AlreadyApproved
        | Some _ ->
            entries
            |> List.map (fun e ->
                if e.Id = entryId then { e with Approved = true; ApprovedBy = Some session.UserId } else e)
            |> Ok

/// Balance of a tenant's approved entries dated up to `until`.
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date <= until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see.
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    if isActive now session then
        Ok(entries |> List.filter (fun e -> e.TenantId = session.TenantId))
    else
        Error SessionExpired

/// Work item WI-0006.
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let inv = Globalization.CultureInfo.InvariantCulture

    let field (s: string) =
        if s.IndexOfAny [| ','; '"'; '\r'; '\n' |] >= 0 then "\"" + s.Replace("\"", "\"\"") + "\"" else s

    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
    |> List.sortWith (fun a b ->
        match compare a.Date b.Date with
        | 0 -> String.CompareOrdinal(a.Id, b.Id)
        | c -> c)
    |> List.map (fun e ->
        String.Join(",", [ field e.Id; e.Date.ToString("yyyy-MM-dd", inv); e.Amount.ToString("F2", inv); field (if e.Description.Length > 0 && "=+-@".Contains e.Description[0] then "'" + e.Description else e.Description) ]))
    |> List.fold (fun acc line -> acc + line + "\n") "id,date,amount,description\n"

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    Error(Unsupported "RQ-FXL-0008 is proposed; the rates source (DF-FXL-0009) is undecided")

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
    let original =
        entries
        |> List.tryFind (fun e -> e.Id = originalId && e.TenantId = session.TenantId && e.Approved && e.RefundOf.IsNone)

    let refunded =
        entries |> List.filter (fun e -> e.RefundOf = Some originalId) |> List.sumBy (fun e -> -e.Amount)

    if not (isActive now session) then Error SessionExpired
    elif not (hasRole Clerk session || hasRole Admin session) then Error NotAuthorized
    else
        match original with
        | None -> Error NotFound
        | Some o when today.DayNumber - o.Date.DayNumber > 90 -> Error(Unsupported "refund window closed")
        | Some _ when entries |> List.exists (fun e -> e.Id = refundId) -> Error DuplicateId
        | Some _ when amount <= 0m -> Error InvalidAmount
        | Some o when amount + refunded > o.Amount -> Error RefundExceedsOriginal
        | Some _ ->
            Ok(
                entries
                @ [ { Id = refundId; TenantId = session.TenantId; Amount = -amount
                      Description = $"Refund of {originalId}"; Date = today; CreatedBy = session.UserId
                      Approved = false; ApprovedBy = None; RefundOf = Some originalId } ]
            )
