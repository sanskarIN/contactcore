# Import and Export

ContactCore currently includes two interchange codecs in `src/ContactCore.Application/ImportExport.cs`: CSV and vCard 4.0. The desktop application exposes file-based import/export actions and routes imported contacts through the application service before persistence.

## Import pipeline

The safe import path is:

1. Read the selected text file.
2. Decode it with the format-specific codec.
3. Return parsed `Contact` objects plus non-fatal warnings.
4. Deep-copy and normalize each contact through `ContactService.ImportAsync`.
5. Validate every normalized contact.
6. If any validation issue exists, reject the batch before repository persistence.
7. Set a consistent import-time `UpdatedAt` value.
8. Persist all contacts through `IContactRepository.UpsertManyAsync`.
9. The SQLite repository writes the entire collection in one transaction.

The combination of whole-batch validation and one database transaction prevents a normal validation or database error from silently leaving only part of the selected batch imported.

## CSV

### Exported columns

The current header is:

```text
GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes
```

Each field is always quoted. Embedded quotes are doubled. Commas and newlines inside quoted values survive round-trip tests.

### Current field coverage

CSV export writes:

- given name;
- family name;
- nickname;
- **first** email address only;
- **first** phone number only;
- birthday as `yyyy-MM-dd`;
- notes.

The current CSV format does **not** serialize every repeated email/phone, postal addresses, organizations, groups, tags, favorite/archive flags, IDs, or timestamps. It should therefore be viewed as a simple interoperability/export format rather than a full-fidelity backup format.

Use database backup for full-fidelity recovery.

### Import behavior

- The first parsed row is treated as the header.
- Header names are trimmed and matched case-insensitively.
- Missing expected columns resolve to empty values.
- Unknown columns are ignored.
- Blank rows are skipped.
- Email and phone values, when present, become one imported repeated field each.
- Birthday accepts exact `yyyy-MM-dd`.
- An invalid non-empty birthday adds a warning such as `Row N: birthday was not yyyy-MM-dd.` and leaves birthday unset.

The CSV parser tracks quoted fields, doubled quotes, commas, CR/LF, and final unterminated rows. A deterministic randomized-Unicode test repeatedly feeds arbitrary input to the parser to check that it does not throw for ordinary malformed text.

### Spreadsheet formula safety

CSV is often opened in spreadsheet programs, which may interpret cells beginning with characters such as `=`, `+`, `-`, or `@` as formulas depending on the application and settings. ContactCore currently performs standard CSV quoting but does not add spreadsheet-specific formula neutralization.

Therefore:

- treat exported CSV as data, not trusted spreadsheet instructions;
- use caution when opening exports containing untrusted contact text in spreadsheet software;
- do not claim formula-injection mitigation until a deliberate compatibility/safety policy is implemented and tested.

## vCard

### Export format

Each contact produces:

```text
BEGIN:VCARD
VERSION:4.0
...
END:VCARD
```

Supported exported properties are:

- `N` for family/given name;
- `FN` for display name;
- repeated `TEL` values with a lower-case field-kind `TYPE`;
- repeated `EMAIL` values with a lower-case field-kind `TYPE`;
- optional `BDAY` as `yyyyMMdd`;
- optional `NOTE`.

Text escaping covers backslash, newline, comma, and semicolon for exported values.

### Import behavior

The importer:

- recognizes `BEGIN:VCARD` and `END:VCARD` case-insensitively;
- unfolds continuation lines beginning with a space or tab;
- splits each property at the first colon;
- ignores property parameters after the base property name for dispatch;
- reads `FN`, `N`, `TEL`, `EMAIL`, `BDAY`, and `NOTE`;
- strips hyphens from birthday text before exact `yyyyMMdd` parsing;
- records a warning for an invalid birthday;
- records a warning and ignores a final vCard missing `END:VCARD`.

Imported phone/email labels are currently `Imported`; the importer does not map all vCard `TYPE` parameter variants back to `ContactFieldKind`.

### Scope limitations

This is intentionally a focused vCard subset, not a complete RFC implementation. Current limitations include no complete support for:

- all structured/parameterized properties;
- advanced encodings;
- media/photo properties;
- organization/address round-trip;
- groups/tags;
- stable ContactCore IDs;
- every escaping/parameter corner case in the vCard ecosystem.

Interoperability changes should be backed by examples from multiple real clients, but any fixtures committed to this public repository must use fictional data.

## Validation after parsing

Codec parsing and domain validation are separate concerns. A file may parse successfully yet contain an invalid email or phone. `ContactService.ImportAsync` performs the domain validation pass before persistence and prefixes validation fields with the one-based imported contact position, for example `Contact[2].Email`.

Validation messages intentionally avoid repeating the invalid contact value.

## Duplicate handling

Import does not automatically merge duplicates. IDs generated while decoding normally produce new aggregate identities. Duplicate review/merge is a separate application workflow so the user can inspect candidates instead of silently losing information.

## Character encoding

The codecs operate on .NET strings. Desktop file handling should use a clearly defined text encoding (normally UTF-8) and must avoid claiming compatibility with arbitrary legacy encodings unless conversion is implemented and tested.

## Privacy and security

Import files and exports may contain personal data. Follow these rules:

- never commit real exports;
- never attach real exports to public issues;
- use fictional samples in tests/docs;
- validate file size and resource behavior before expanding import to very large files;
- treat unexpected external files as untrusted input;
- do not use CSV/vCard as a substitute for a verified full database backup.

## Adding a new import/export format

A new codec should include:

- a documented supported-field matrix;
- deterministic escaping/encoding rules;
- malformed-input tests;
- round-trip tests for supported fields;
- size/resource limits when appropriate;
- domain validation through the same service layer;
- atomic persistence for a batch import;
- explicit privacy/security notes;
- updates to the user guide, testing guide, repository reference, and changelog.
