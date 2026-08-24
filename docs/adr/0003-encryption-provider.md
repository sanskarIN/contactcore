# ADR 0003: Keep Database Encryption Optional and Fail Closed

- **Status:** Accepted
- **Scope:** SQLite-at-rest encryption integration boundary
- **Related:** ADR 0002, `../security.md`

## Context

Contact data is personal data and some deployments may require database-at-rest encryption. SQLite itself does not provide SQLCipher encryption merely because application code executes `PRAGMA key`; encryption requires a compatible native SQLite implementation such as SQLCipher.

Bundling a native encryption provider directly into an open-source cross-platform desktop application creates additional concerns:

- native binaries for Windows/macOS/Linux and multiple architectures;
- licensing/distribution terms;
- security updates independent of managed NuGet packages;
- packaging and runtime-loading behavior;
- CI/release testing for each RID;
- temptation to falsely claim encryption when the process actually loaded ordinary SQLite.

The project therefore needs an integration point without silently degrading into plaintext when users request a key.

## Decision

The default open-source repository continues to use `Microsoft.Data.Sqlite` with ordinary SQLite unless a compatible native provider is deliberately integrated by the deployment.

ContactCore accepts a runtime key through `CONTACTCORE_DATABASE_KEY` / the `IAppPreferences.DatabaseKey` runtime property, but **a non-empty key is treated as a request that must be verified**.

`SqliteConnectionFactory`:

1. retrieves the key from its provider;
2. converts UTF-8 key bytes to hexadecimal;
3. executes a hex-literal `PRAGMA key` so the original secret is not directly interpolated into SQL text;
4. executes `PRAGMA cipher_version`;
5. closes the connection and throws if no cipher version is returned.

Therefore an ordinary SQLite build with a configured key fails closed instead of letting the application proceed while implying encryption.

## Secret persistence decision

The database key is runtime-only in normal ContactCore preferences.

`JsonAppPreferences` may initialize `DatabaseKey` from the process environment, but its serialized model includes only:

- Theme;
- ReducedMotion;
- ConfirmPermanentDelete.

The key/property is not written to `settings.json`, and tests assert this.

The repository does not currently implement an OS credential-vault adapter. Such an adapter is a potential future improvement.

## Consequences

### Positive

- The default application has no proprietary/native encryption-bundle requirement.
- Users/deployers cannot accidentally mistake ignored `PRAGMA key` behavior for verified encryption.
- The original key text is not placed directly into the PRAGMA SQL string.
- Normal settings do not persist the key.
- Provider choice can evolve independently from Domain/Application contracts.

### Negative

- The default ordinary SQLite build is not encrypted at rest.
- Setting `CONTACTCORE_DATABASE_KEY` without a compatible provider intentionally prevents database opening.
- Deployers wanting encryption must integrate/package/test a native provider themselves until the project adopts one.
- Environment variables are not a perfect secret store and can be exposed through process/environment inspection depending on OS/tooling.
- Encrypted-provider backup/restore behavior must be validated on every supported platform/provider combination; default tests cannot prove a provider that is not bundled.

## Security clarification

This ADR does **not** claim that ContactCore performs independent cryptography. When a compatible provider is present, cryptographic properties belong to that provider/configuration.

ContactCore's responsibility is to:

- provide the key before normal database access;
- verify that a cipher-capable provider responded;
- avoid obvious secret persistence/logging mistakes;
- refuse misleading plaintext fallback.

## Backup implications

`BackupService` opens source/destination connections through `SqliteConnectionFactory`, so the same key-provider behavior is used for active and backup paths. A production encrypted deployment must test:

- creation of a backup;
- backup reopening;
- integrity checks;
- restore source verification;
- staging migration;
- final reopen after replacement;
- recovery snapshot behavior.

Do not assume behavior of a particular SQLCipher/native build without integration tests.

## Alternatives considered

### Always bundle SQLCipher

Deferred/rejected as the default decision until licensing, native distribution, maintenance ownership, and all release RID tests are explicitly solved.

### Accept a key without checking `cipher_version`

Rejected because ordinary SQLite can ignore/accept unknown PRAGMA behavior in ways that would create a dangerous false sense of encryption.

### Store the key in `settings.json`

Rejected because a plaintext key beside the encrypted database would substantially weaken the intended at-rest protection and create accidental repository/support leakage risk.

### Build custom application-layer field encryption

Rejected for now. It would require key derivation, nonce/version/ciphertext formats, selective-query/search redesign, rotation/migration, backup behavior, and extensive cryptographic review. A mature SQLite encryption provider is a better boundary if full-database encryption is needed.

### Mandatory OS secret store

Not selected as the current baseline because cross-platform implementations differ, but it is a strong future option for improving key handling after provider support is defined.

## Guardrails

Any encryption-related change must:

- preserve fail-closed verification;
- avoid storing the key in normal preference files;
- avoid logging the key or full sensitive database content;
- document which native provider/version is actually supported;
- document licensing/distribution requirements;
- add platform/RID-specific integration tests;
- ensure backups/restores preserve expected encryption state;
- ensure release artifacts contain only intentionally distributable native binaries;
- update `security.md`, `setup.md`, and release docs.

## When to revisit

Revisit when ContactCore chooses an officially supported encryption provider, adds an OS secret-store integration, or adopts a different storage architecture.

A future “encryption enabled” user-facing setting must not be released until the application can prove the effective provider/state rather than merely storing configuration intent.
