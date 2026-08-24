# Import and Export

ContactCore includes CSV and focused vCard 4.0 interchange codecs in `src/ContactCore.Application/ImportExport.cs`. The desktop application exposes file-based import/export actions and routes parsed contacts through `ContactService` before persistence.

These formats are designed for interoperability. They are **not** substitutes for a verified SQLite backup when complete ContactCore fidelity is required.

## Import pipeline

The safe import path is:

1. Read one selected text file through the desktop picker.
2. Enforce the desktop text-size limit before accepting the complete string.
3. Decode with the format-specific codec.
4. Return parsed `Contact` objects plus non-fatal warnings.
5. Deep-copy and normalize every parsed contact in `ContactService.ImportAsync`.
6. Validate the complete normalized batch.
7. If any validation issue exists, reject the batch before repository persistence.
8. Apply one consistent import-time `UpdatedAt` value.
9. Persist all contacts through `IContactRepository.UpsertManyAsync`.
10. `SqliteContactRepository` writes the entire batch in one transaction.

This combination prevents normal validation/database failures from silently leaving only part of an import committed.

## Desktop input limit and encoding

`MainWindow.axaml.cs` reads `.csv`, `.vcf`, and `.vcard` files with UTF-8 `StreamReader` and BOM detection. The selected text is bounded at **5,000,000 characters**. Oversized input raises a controlled `InvalidDataException` rather than being read without limit.

The codecs operate on .NET strings after this boundary. Arbitrary legacy encodings are not claimed as supported unless a conversion path is explicitly implemented and tested.

## CSV

### Supported header and export columns

The ContactCore CSV header is:

```text
GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes
```

Export writes:

- given name;
- family name;
- nickname;
- **first** email address only;
- **first** phone number only;
- birthday as `yyyy-MM-dd`;
- notes.

Every exported field is quoted. Embedded quotes are doubled. Quoted commas and newlines are supported by the parser/round-trip tests.

CSV does **not** serialize all repeated phones/emails, addresses, organizations, groups, tags, favorite/archive flags, ContactCore IDs, or timestamps.

### Header handling

Import treats the first parsed row as the header.

- Header names are trimmed and matched case-insensitively.
- Supported names are `GivenName`, `FamilyName`, `Nickname`, `Email`, `Phone`, `Birthday`, and `Notes`.
- Unknown columns are ignored when at least one supported column is present.
- If a header name occurs more than once, the **first** column wins and a warning is returned.
- If the file contains **no recognized ContactCore columns**, the importer returns zero contacts plus a warning instead of turning unrelated rows into `Unnamed contact` records.
- Blank data rows are skipped.

### Row handling

For each accepted data row:

- missing supported columns resolve to empty values;
- one nonblank email becomes one imported `ContactEmail`;
- one nonblank phone becomes one imported `ContactPhone`;
- birthday accepts exact `yyyy-MM-dd`;
- invalid nonblank birthday adds `Row N: birthday was not yyyy-MM-dd.` and leaves birthday unset;
- parser warnings remain separate from later domain-validation errors.

The parser tracks quoted fields, doubled quotes, commas, CR/LF, and a final non-newline-terminated row. Randomized malformed/unicode input tests exercise the no-crash boundary.

### Spreadsheet-formula warning

Some spreadsheet software may interpret text beginning with `=`, `+`, `-`, or `@` as a formula depending on application/settings.

ContactCore currently **preserves the original text** rather than modifying contact data for spreadsheet-specific neutralization. When CSV import sees a supported text value whose first non-whitespace character is one of those formula prefixes, it returns a warning reminding the user that the value is stored as text by ContactCore but may require care if exported/opened in spreadsheet software.

Therefore:

- treat CSV as data, not trusted spreadsheet instructions;
- do not claim formula-injection mitigation;
- review untrusted contact text before opening an export in spreadsheet software;
- prefer another workflow if spreadsheet interpretation would be risky.

## vCard

### Export format

Each contact produces CRLF-delimited vCard 4.0 text:

```text
BEGIN:VCARD
VERSION:4.0
...
END:VCARD
```

Supported exported properties are:

- `N` for family/given name;
- `FN` for display name;
- repeated `TEL` with lower-case field-kind `TYPE`;
- repeated `EMAIL` with lower-case field-kind `TYPE`;
- optional `BDAY` as `yyyyMMdd`;
- optional `NOTE`.

Text escaping covers backslash, newline, comma, and semicolon.

### Import behavior

The importer:

- recognizes `BEGIN:VCARD` / `END:VCARD` case-insensitively;
- unfolds continuation lines beginning with a space or tab;
- splits each property at the first colon;
- dispatches by the base property name before parameters;
- reads `FN`, structured `N`, `TEL`, `EMAIL`, `BDAY`, and `NOTE`;
- splits structured `N` on **unescaped** semicolons so escaped name delimiters survive;
- unescapes backslash/newline/comma/semicolon character-by-character;
- maps common `TYPE` tokens (`home`, `work`, `cell`/`mobile`, `other`) to `ContactFieldKind`;
- uses the parsed field kind as the imported phone/email label;
- accepts birthday in `YYYYMMDD` or hyphenated `YYYY-MM-DD` form;
- returns a generic invalid-birthday warning without echoing the imported value;
- warns when a nested `BEGIN:VCARD` abandons an incomplete previous card;
- warns and ignores a final card that never reaches `END:VCARD`.

### Scope limitations

This remains a focused subset, not a complete implementation of the vCard ecosystem. It does not claim full support for:

- every structured/parameterized property;
- every RFC parameter/escaping corner case;
- advanced encodings;
- media/photo properties;
- postal-address or organization round-trip;
- ContactCore groups/tags;
- ContactCore IDs/timestamps/archive/favorite metadata;
- custom/extension properties from every client.

Interoperability changes should use fictional fixtures from representative clients and add focused regression tests.

## Validation after parsing

Codec parsing and domain validation are separate concerns. A file may parse successfully but produce a contact with an invalid email/phone/name constraint. `ContactService.ImportAsync` performs domain validation for the complete batch before persistence and prefixes issue fields with the one-based imported position, for example `Contact[2].Email`.

Validation/error messages are designed not to echo invalid contact values where avoidable.

## Duplicate handling

Import does not silently merge contacts. Decoder-generated IDs normally create new aggregate identities. Duplicate detection/review is a separate user-controlled workflow with side-by-side evidence, survivor choice, confirmation, and atomic repository merge.

This separation avoids converting an import heuristic into an automatic destructive action.

## Export behavior

Desktop export queries with `IncludeArchived: true`, so archived contacts are included. The user chooses the destination; text is written UTF-8 without a BOM.

CSV and vCard should be used for interchange. Use ContactCore's verified SQLite backup path when complete repeated fields, relationship tables, IDs, archive/favorite state, and schema-compatible recovery matter.

## Privacy and security

Import files and exports may contain personal data.

- Never commit real exports.
- Never attach real exports to public issues.
- Use fictional samples in tests and documentation.
- Treat external files as untrusted input.
- Keep the desktop input bound in place when adding new formats.
- Keep validation and batch persistence atomic.
- Do not echo secrets/contact values unnecessarily in parse/validation errors.
- Do not use CSV/vCard as a substitute for a verified database backup.

## Adding another interchange format

A new codec should include:

- a documented supported-field matrix;
- deterministic escaping/encoding rules;
- malformed-input tests;
- supported-field round-trip tests;
- explicit size/resource limits where appropriate;
- domain validation through the same application service;
- one-transaction persistence for batch import;
- privacy/security notes;
- UI picker/export integration where intended;
- updates to the user guide, testing guide, repository reference, roadmap/changelog, and `what_changed.md` when relevant.
