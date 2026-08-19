# ADR 0003: CSV and vCard for portable interchange

- Status: Accepted
- Date: 2026-08-19

## Context

Users need a way to move data without proprietary cloud services. A contact manager also benefits from interoperability with spreadsheet workflows and established address-book formats.

## Decision

Support two text formats in Application:

- CSV for simple, inspectable bulk transfer and spreadsheet workflows;
- vCard 4.0 for contact-oriented interoperability.

Codecs are dependency-free and deterministic. Imported records still pass through domain/application validation before persistence. UI bulk-import exposure will add explicit file-size/record-count limits and conflict handling.

## Consequences

- Users retain portable copies of their data.
- CSV cannot represent every rich contact concept without conventions; the initial schema intentionally exports core fields.
- vCard has broad syntax and extension possibilities; unsupported properties are ignored rather than executed.
- Parser fuzz/property testing becomes important before high-volume import is considered release-complete.
- Spreadsheet formula injection must be addressed with an explicit spreadsheet-safe export mode rather than silently modifying the lossless CSV representation.
