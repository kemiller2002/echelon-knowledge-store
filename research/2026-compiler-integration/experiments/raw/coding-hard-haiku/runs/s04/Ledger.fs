module FxLedger.Ledger

open System
open System.Globalization
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
let total (tenantId: string) (entries: Entry list) : decimal = failwith "not implemented"

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    // RQ-FXL-0001 / DF-FXL-0001 order: amount, description, date, duplicate id.
    let description = entry.Description.Trim()

    if entry.Amount <= 0m || entry.Amount > 1_000_000m then
        Error InvalidAmount
    elif
        description.Length = 0
        || description.Length > 200
        || String.exists (fun c -> Char.IsControl c) description // RQ-FXL-0101
    then
        Error InvalidDescription
    elif entry.Date > today then
        Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then
        Error DuplicateId
    else
        // RQ-FXL-0002: stored trimmed, unapproved, appended last.
        let stored =
            { entry with
                Description = description
                Approved = false
                ApprovedBy = None }

        Ok(entries @ [ stored ])

/// Work item WI-0003. Returns the ledger with the entry approved.
let approve
    (now: DateTimeOffset)
    (session: Session)
    (entryId: string)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Balance of a tenant's approved entries dated up to `until`.
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date <= until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see.
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Work item WI-0006.
/// RQ-FXL-0007 quoting: wrap in double quotes when the field holds , " CR or LF.
let private csvField (value: string) =
    if value.IndexOfAny([| ','; '"'; '\r'; '\n' |]) >= 0 then
        "\"" + value.Replace("\"", "\"\"") + "\""
    else
        value

/// RQ-FXL-0107: a leading = + - or @ gets an apostrophe before quoting.
let private neutraliseFormula (value: string) =
    if value.Length > 0 && "=+-@".IndexOf(value.[0]) >= 0 then "'" + value else value

let exportCsv (tenantId: string) (entries: Entry list) : string =
    let rows =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            match compare a.Date b.Date with
            | 0 -> String.CompareOrdinal(a.Id, b.Id)
            | c -> c)
        |> List.map (fun e ->
            let date = e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            let amount = e.Amount.ToString("F2", CultureInfo.InvariantCulture)
            let description = e.Description |> neutraliseFormula |> csvField
            String.concat "," [ e.Id; date; amount; description ] + "\n")

    "id,date,amount,description\n" + String.concat "" rows

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
    // RQ-FXL-0009 checks, in order, with the RQ-FXL-0109 window before the duplicate-id check.
    if not (isActive now session) then
        Error SessionExpired
    elif not (hasRole Clerk session || hasRole Admin session) then
        Error NotAuthorized
    else
        let original =
            entries
            |> List.tryFind (fun e ->
                e.Id = originalId
                && e.TenantId = session.TenantId
                && e.Approved
                && e.RefundOf.IsNone)

        match original with
        | None -> Error NotFound
        | Some original when today.DayNumber - original.Date.DayNumber > 90 ->
            Error(Unsupported "refund window closed")
        | Some original ->
            if entries |> List.exists (fun e -> e.Id = refundId) then
                Error DuplicateId
            elif amount <= 0m then
                Error InvalidAmount
            else
                // DF-FXL-0006: refunds are stored as negative amounts, so negate to sum them.
                let refunded =
                    entries
                    |> List.filter (fun e -> e.RefundOf = Some originalId)
                    |> List.sumBy (fun e -> -e.Amount)

                if amount + refunded > original.Amount then
                    Error RefundExceedsOriginal
                else
                    let refundEntry =
                        { Id = refundId
                          TenantId = session.TenantId
                          Amount = -amount
                          Description = $"Refund of {originalId}"
                          Date = today
                          CreatedBy = session.UserId
                          Approved = false
                          ApprovedBy = None
                          RefundOf = Some originalId }

                    Ok(entries @ [ refundEntry ])
