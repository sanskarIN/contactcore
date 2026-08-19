# Contributing to ContactCore

Thanks for contributing. ContactCore favors small, reviewable changes that preserve local-first privacy, deterministic behavior, and a clean layered architecture.

## Before coding

- Read `README.md`, `docs/architecture.md`, and relevant ADRs.
- Search existing issues and pull requests before duplicating work.
- For large behavioral or architectural changes, open an issue first.
- Never use real contact data in fixtures, screenshots, commits, issues, or test databases.

## Development setup

Install the .NET SDK required by `global.json`, then run:

```bash
dotnet restore ContactCore.slnx
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

Before opening a pull request:

```bash
dotnet format ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

## Architecture rules

- Domain code must not depend on Avalonia or SQLite.
- Application code defines workflows and infrastructure contracts.
- Infrastructure implements persistence/backup concerns.
- Desktop is the composition root and UI layer.
- Keep SQL parameterized. Never concatenate user input into SQL.
- Keep error messages safe for users and avoid leaking contact contents.
- Add migrations for persistent schema changes.

## Tests

Add deterministic tests for behavior changes and regression tests for bug fixes. Persistence changes should include SQLite integration tests. Parser/codec changes should cover malformed input and edge cases. UI changes should be manually checked for keyboard navigation, focus visibility, readable labels, and light/dark themes.

## Commits

Prefer Conventional Commits such as `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `perf:`, `build:`, `ci:`, and `chore:`. Keep commits atomic and meaningful; do not create empty commits or unrelated formatting churn.

Recommended author email for this repository: `sanskarin@outlook.in`.

## Pull requests

Complete the pull-request template, explain security/privacy impact, link relevant issues, and include fictional-data screenshots for visual changes. CI must pass before merge.

By contributing, you agree to follow `CODE_OF_CONDUCT.md` and the MIT license terms for submitted code.
