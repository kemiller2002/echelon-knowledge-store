module FxLedger.Ledger

open System
open System.Globalization
open System.Text
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
/// Rules in fixed order (DF-FXL-0001): amount, description, date, duplicate id.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let description = entry.Description.Trim()
    let amountOk = entry.Amount > 0m && entry.Amount <= 1000000m
    let descriptionOk =
        description.Length > 0
        && description.Length <= 200
        && not (description |> Seq.exists Char.IsControl)
    if not amountOk then Error InvalidAmount
    elif not descriptionOk then Error InvalidDescription
    elif entry.Date > today then Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then Error DuplicateId
    else
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

/// RFC 4180 quoting for a field (RQ-FXL-0007).
let private csvField (value: string) =
    if value.IndexOfAny([| ','; '"'; '\r'; '\n' |]) >= 0 then
        "\"" + value.Replace("\"", "\"\"") + "\""
    else
        value

/// Neutralises spreadsheet formulas by prefixing an apostrophe (RQ-FXL-0107).
let private neutraliseFormula (value: string) =
    if value.Length > 0 && "=+-@".IndexOf(value.[0]) >= 0 then "'" + value else value

/// Work item WI-0006. Approved entries of the tenant, ordered by date then id.
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let rows =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            match compare a.Date b.Date with
            | 0 -> String.CompareOrdinal(a.Id, b.Id)
            | c -> c)
    let sb = StringBuilder()
    sb.Append("id,date,amount,description\n") |> ignore
    for e in rows do
        let fields =
            [ csvField e.Id
              e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
              e.Amount.ToString("F2", CultureInfo.InvariantCulture)
              csvField (neutraliseFormula e.Description) ]
        sb.Append(String.Join(",", fields)).Append('\n') |> ignore
    sb.ToString()

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    failwith "not implemented"

/// Work item WI-0008. Records a refund of `amount` against entry `originalId`.
/// RQ-FXL-0009 with the 90-day window of RQ-FXL-0109 and the ceiling of DF-FXL-0006.
let refund
    (now: DateTimeOffset)
    (session: Session)
    (today: DateOnly)
    (refundId: string)
    (originalId: string)
    (amount: decimal)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    if not (isActive now session) then
        Error SessionExpired
    elif not (hasRole Clerk session || hasRole Admin session) then
        Error NotAuthorized
    else
        let originalOpt =
            entries
            |> List.tryFind (fun e ->
                e.Id = originalId
                && e.TenantId = session.TenantId
                && e.Approved
                && e.RefundOf.IsNone)
        match originalOpt with
        | None -> Error NotFound
        | Some original ->
            if today.DayNumber - original.Date.DayNumber > 90 then
                Error(Unsupported "refund window closed")
            elif entries |> List.exists (fun e -> e.Id = refundId) then
                Error DuplicateId
            elif amount <= 0m then
                Error InvalidAmount
            else
                let alreadyRefunded =
                    entries
                    |> List.filter (fun e -> e.RefundOf = Some originalId)
                    |> List.sumBy (fun e -> -e.Amount)
                if alreadyRefunded + amount > original.Amount then
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
