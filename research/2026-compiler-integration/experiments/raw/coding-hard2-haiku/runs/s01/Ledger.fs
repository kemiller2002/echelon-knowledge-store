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
/// RQ-FXL-0001 (validation, DF-FXL-0001 order), RQ-FXL-0101 (control characters),
/// RQ-FXL-0002 (stored form).
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let description = entry.Description.Trim()

    if entry.Amount <= 0m || entry.Amount > 1000000m then
        Error InvalidAmount
    elif
        description.Length = 0
        || description.Length > 200
        || Seq.exists (fun c -> Char.IsControl c) description
    then
        Error InvalidDescription
    elif entry.Date > today then
        Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then
        Error DuplicateId
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

/// Balance of a tenant's approved entries dated on or before `until` (RQ-FXL-0005).
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date <= until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see.
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// CSV field per RQ-FXL-0007 and RQ-FXL-0107: a leading `=`, `+`, `-` or `@` gets an
/// apostrophe first; then fields containing `,`, `"`, CR or LF are quoted.
let private csvDescription (value: string) =
    let neutralised =
        if value.Length > 0 && "=+-@".Contains(value.[0]) then "'" + value else value

    if neutralised |> Seq.exists (fun c -> c = ',' || c = '"' || c = '\r' || c = '\n') then
        "\"" + neutralised.Replace("\"", "\"\"") + "\""
    else
        neutralised

/// Work item WI-0006 (RQ-FXL-0007, RQ-FXL-0107, DF-FXL-0004).
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let rows =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            match compare a.Date b.Date with
            | 0 -> String.CompareOrdinal(a.Id, b.Id)
            | c -> c)

    let line (e: Entry) =
        String.Join(
            ",",
            e.Id,
            e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            e.Amount.ToString("F2", CultureInfo.InvariantCulture),
            csvDescription e.Description
        )
        + "\n"

    "id,date,amount,description\n" + (rows |> List.map line |> String.concat "")

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    failwith "not implemented"

/// Work item WI-0008. Records a refund of `amount` against entry `originalId`
/// (RQ-FXL-0009, DF-FXL-0006, amended by RQ-FXL-0109).
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
        let found =
            entries
            |> List.tryFind (fun e ->
                e.Id = originalId
                && e.TenantId = session.TenantId
                && e.Approved
                && e.RefundOf.IsNone)

        match found with
        | None -> Error NotFound
        | Some original when today.DayNumber - original.Date.DayNumber > 90 ->
            Error(Unsupported "refund window closed")
        | Some original ->
            if entries |> List.exists (fun e -> e.Id = refundId) then
                Error DuplicateId
            elif amount <= 0m then
                Error InvalidAmount
            else
                let alreadyRefunded =
                    entries
                    |> List.filter (fun e -> e.RefundOf = Some originalId)
                    |> List.sumBy (fun e -> -e.Amount)

                if amount + alreadyRefunded > original.Amount then
                    Error RefundExceedsOriginal
                else
                    let refundEntry =
                        { Id = refundId
                          TenantId = session.TenantId
                          Amount = -amount
                          Description = "Refund of " + originalId
                          Date = today
                          CreatedBy = session.UserId
                          Approved = false
                          ApprovedBy = None
                          RefundOf = Some originalId }

                    Ok(entries @ [ refundEntry ])
