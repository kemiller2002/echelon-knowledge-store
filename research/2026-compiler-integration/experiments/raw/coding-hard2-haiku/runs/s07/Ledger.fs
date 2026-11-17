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
/// RQ-FXL-0001 (validation, DF-FXL-0001 order), RQ-FXL-0002 (stored form),
/// RQ-FXL-0101 (no control characters in descriptions).
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let description = entry.Description.Trim()

    if entry.Amount <= 0m || entry.Amount > 1_000_000m then
        Error InvalidAmount
    elif description.Length = 0
         || description.Length > 200
         || Seq.exists Char.IsControl description then
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

/// CSV field encoding (RQ-FXL-0107 formula neutralisation, then RFC 4180 quoting).
let private csvField (text: string) : string =
    let neutral =
        if text.Length > 0 && (text.[0] = '=' || text.[0] = '+' || text.[0] = '-' || text.[0] = '@') then
            "'" + text
        else
            text

    if neutral |> Seq.exists (fun c -> c = ',' || c = '"' || c = '\r' || c = '\n') then
        "\"" + neutral.Replace("\"", "\"\"") + "\""
    else
        neutral

/// Work item WI-0006. Approved entries of the tenant as CSV (RQ-FXL-0007, RQ-FXL-0107).
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let rows =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            match compare a.Date b.Date with
            | 0 -> String.CompareOrdinal(a.Id, b.Id)
            | c -> c)
        |> List.map (fun e ->
            // Rounding per DF-FXL-0007 (half away from zero); amount always has 2 decimals.
            let amount =
                Math.Round(e.Amount, 2, MidpointRounding.AwayFromZero)
                    .ToString("F2", CultureInfo.InvariantCulture)

            String.Join(
                ",",
                [ e.Id
                  e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                  amount
                  csvField e.Description ]
            ))

    ("id,date,amount,description" :: rows)
    |> List.map (fun line -> line + "\n")
    |> String.concat ""

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    failwith "not implemented"

/// Work item WI-0008. Records a refund of `amount` against entry `originalId` (RQ-FXL-0009,
/// DF-FXL-0006, amendment RQ-FXL-0109 90-day window).
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
        let original =
            entries
            |> List.tryFind (fun e ->
                e.Id = originalId
                && e.TenantId = session.TenantId
                && e.Approved
                && e.RefundOf.IsNone)

        match original with
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
                          Description = "Refund of " + originalId
                          Date = today
                          CreatedBy = session.UserId
                          Approved = false
                          ApprovedBy = None
                          RefundOf = Some originalId }

                    Ok(entries @ [ refundEntry ])
