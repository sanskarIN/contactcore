# Development

## Repository layout

```text
ContactCore.slnx
src/
  ContactCore.Domain/
  ContactCore.Application/
  ContactCore.Infrastructure/
  ContactCore.Desktop/
tests/
  ContactCore.Domain.Tests/
  ContactCore.Application.Tests/
  ContactCore.Infrastructure.Tests/
docs/
.github/
```

Central compiler settings live in `Directory.Build.props`; central NuGet versions live in `Directory.Packages.props`.

## Daily workflow

```bash
git pull --ff-only
dotnet restore ContactCore.slnx
dotnet build ContactCore.slnx
```

Create a focused branch, implement one coherent change, add or update tests, then run the smallest relevant test project during development. Before requesting review, run the complete quality suite:

```bash
dotnet format ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

## Coding conventions

- Nullable reference types stay enabled.
- Warnings are errors; fix warnings rather than suppressing them globally.
- Prefer immutable records for simple value data and explicit classes for aggregates/services.
- Validate at application/domain boundaries rather than sprinkling rules through UI code.
- Use `CancellationToken` for I/O-facing asynchronous APIs.
- Use `ConfigureAwait(false)` in non-UI library code where context capture is unnecessary.
- Keep SQL parameterized. Dynamic SQL identifiers are permitted only from internal constants.
- Use invariant formats for persisted dates/timestamps and external machine-readable formats.
- Never log or commit real contact data.

## Adding a contact field

1. Model it in `ContactCore.Domain`.
2. Add validation/normalization if needed.
3. Add an SQLite migration rather than rewriting an existing released migration.
4. Persist/materialize it in Infrastructure.
5. Expose it through Application workflows.
6. Add UI support.
7. Add domain and persistence tests.
8. Update import/export formats only after deciding backward compatibility.
9. Update docs and `what_changed.md`.

## Database changes

`SqliteDatabase` records applied versions in `schema_migrations`. Once a schema version ships, do not edit its SQL in place. Add the next version and an upgrade test that starts from the previous schema.

Multi-step writes must remain transactional. Run `PRAGMA integrity_check` in backup/restore verification and any migration-repair tooling.

## Import/export changes

Parsers process untrusted input. Avoid unbounded recursion, catastrophic regexes, uncontrolled allocation, path traversal, or interpreting imported content as commands. Add tests for quoting, delimiters, Unicode, line endings, empty fields, malformed records, and large-but-allowed values.

## Desktop UI changes

Keep presentation state in view models and domain/storage rules in lower layers. Review:

- keyboard reachability;
- focus visibility;
- text scaling;
- light/dark theme contrast;
- narrow-window behavior;
- meaningful labels and status text;
- safe handling of destructive actions.

See `docs/accessibility.md`.

## Commit style

Use small Conventional Commits. Good examples:

```text
feat(storage): add schema migration for birthdays
fix(import): reject unterminated CSV quote
test(domain): cover phone normalization
docs: explain restore safety
```

The repository's preferred commit email is `sanskarin@outlook.in`.
