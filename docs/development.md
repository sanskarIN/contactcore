# Development

This guide explains how to make code changes without weakening ContactCore's local-first, data-integrity, security, and testability guarantees.

## Before editing

Read:

- `architecture.md` for layer ownership;
- `data-model.md` for aggregate/schema semantics;
- `storage-backup-recovery.md` before changing SQLite, migrations, backup, or restore;
- `desktop-ui.md` before changing view models or Avalonia views;
- `security.md` for input, secrets, and database-key constraints;
- `../CONTRIBUTING.md` for repository contribution rules.

## Solution structure

Production projects:

- `ContactCore.Domain`
- `ContactCore.Application`
- `ContactCore.Infrastructure`
- `ContactCore.Desktop`

Test projects mirror those layers. `ContactCore.slnx` contains all eight projects.

## Shared compiler/build policy

`Directory.Build.props` applies to all projects:

- `net10.0` target framework;
- latest C# language version;
- nullable enabled;
- implicit usings enabled;
- warnings treated as errors;
- latest-recommended analyzer level;
- deterministic build output;
- CI-build metadata when `CI=true`.

Do not disable warnings-as-errors globally to make a branch build. Fix the warning or apply the narrowest justified suppression with review context.

## Central package management

`Directory.Packages.props` owns package versions. Add package references to project files without duplicating versions unless a specific exception is required by NuGet/MSBuild semantics.

When adding a package, justify why an existing BCL/project dependency is insufficient and consider privacy/native-distribution implications.

## Layer placement

### Put code in Domain when

It is a pure contact concept, validation rule, normalization rule, or model behavior that does not need I/O/framework APIs.

### Put code in Application when

It represents a use case, cross-layer contract, import/export transformation, duplicate/merge policy, or orchestration around domain behavior.

### Put code in Infrastructure when

It performs SQLite/file-system/serialization/native-provider work or implements an Application abstraction.

### Put code in Desktop when

It renders Avalonia UI, responds to keyboard/window events, invokes native file pickers/dialogs, or adapts application services for presentation.

Avoid moving lower-layer concepts into Desktop simply because the first consumer is UI code.

## Contact changes

A `Contact` is persisted as a complete aggregate. Saving through the current repository replaces contact-owned child/link rows with the supplied collection state.

If adding/editing fields:

1. update Domain model;
2. update validation/normalization;
3. add a schema migration if persistence changes;
4. update repository read/write code;
5. update `DeepCopy` and merger behavior;
6. add tests;
7. update codecs where deliberately supported;
8. update desktop UI without dropping hidden fields;
9. update documentation.

The current desktop draft only exposes first phone/email and no addresses/organizations/groups/tags. Treat this as a known preservation hazard when extending the UI.

## Async and cancellation

Use cancellation-aware async APIs for I/O. Pass tokens through Application abstractions into Infrastructure when practical.

Desktop search already cancels superseded debounced requests. Do not introduce fire-and-forget I/O that can update stale UI state after a newer action unless its lifecycle is explicitly controlled.

## SQL rules

- Parameterize user/data values.
- Escape SQL `LIKE` pattern metacharacters when user text is intended literally.
- Do not build table/column identifiers from untrusted input.
- Keep foreign keys enabled.
- Keep aggregate/batch writes transactional.
- Add indexes only after identifying a query pattern/measurement that benefits from them.

## Migration rules

Append a new numbered migration in `DatabaseMigrator`; do not rewrite a released migration's meaning.

Each migration should have an upgrade test. Reject future-schema databases rather than attempting downgrade behavior.

Any change to the `schema_family` identity contract requires careful backup/restore design review and an ADR.

## Preferences

Persist non-secret preferences only. `DatabaseKey` is intentionally runtime-only.

New settings should have safe defaults and tolerant loading when older `settings.json` files omit the field. Preference writes should remain temporary-file/replace based.

## Import parsers

Treat imports as hostile/malformed input even in an offline application.

Requirements:

- malformed input should fail predictably or produce explicit warnings;
- resource use should be bounded at an appropriate layer;
- parsing should not itself persist;
- domain validation happens after parsing;
- batch persistence remains atomic;
- tests should include escaping, Unicode, malformed boundaries, and representative limits.

The current desktop input limit is 5,000,000 characters.

## Backup/restore development

Do not test destructive restore changes against personal data. Use a disposable `CONTACTCORE_DATA_PATH` and generated fictional contacts.

Preserve source verification, pre-restore snapshot, staging migration/verification, final verification, and rollback semantics unless a reviewed design proves an equally safe replacement.

## Error messages and privacy

Avoid putting full contact data, keys, database contents, or raw untrusted input in exception messages. The desktop redactor is defense-in-depth, not a substitute for safe lower-layer messages.

Validation messages should identify what is wrong without echoing sensitive values.

## UI development

Preserve:

- keyboard reachability;
- visible focus;
- field labels;
- theme-aware dynamic resources;
- logical tab order;
- text wrapping/trimming where necessary;
- reduced-motion preference for new custom motion;
- destructive-action confirmation guarantees.

If adding a new platform service, prefer a narrow view-model callback/interface instead of embedding platform picker/dialog APIs throughout business logic.

## Formatting

Verify formatting without modifying files:

```bash
dotnet format ContactCore.slnx --verify-no-changes
```

Apply formatting when needed:

```bash
dotnet format ContactCore.slnx
```

Review the resulting diff before committing.

## Quality commands

From the repository root:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

CI performs equivalent restore/format/build/test checks on Ubuntu, Windows, and macOS.

## Test placement

Prefer the lowest meaningful layer:

- validation/normalization → Domain tests;
- service/codec/duplicate policy → Application tests;
- migration/repository/preferences/backup → Infrastructure tests;
- draft/desktop orchestration → Desktop tests.

A behavior crossing multiple layers may deserve both focused unit coverage and one integration test.

## Test data

Use fictional names, addresses, phone numbers, and domains. Never copy real user data into fixtures, snapshots, screenshots, or failing test output.

Use isolated temporary directories/databases for infrastructure tests and delete them after the test where possible.

## Git discipline

Keep a commit focused on one meaningful change. Avoid combining feature code, broad formatting, dependency updates, and unrelated documentation cleanup in one commit.

Before commit:

```bash
git status
git diff --check
git diff
```

Do not commit generated output, secrets, contact databases, backups, exports, or local `.env` files.

## Pull requests

A good PR explains:

- the user/developer problem;
- architecture/layer choice;
- data migration implications;
- privacy/security implications;
- tests added/changed;
- manual verification performed;
- known limitations.

Wait for the final PR head's CI/CodeQL results before merging. A green run from a superseded commit is not sufficient.

## Documentation definition of done

For behavior changes, update the relevant guides in the same PR. `repository-reference.md` should remain exhaustive whenever files are added/renamed/deleted.

`what_changed.md` is the continuation/handoff record and should describe exact branch/PR state, meaningful changes, verification state, and remaining work without falsely claiming pending checks passed.
