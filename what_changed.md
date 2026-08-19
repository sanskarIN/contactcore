# ContactCore — Work Handoff

## Current milestone

Phase 1 — complete the missing executable solution skeleton and establish a reliable end-to-end local contact workflow.

## Repository assessment

- Repository: `sanskarIN/contactcore` (public, default branch `main`).
- Existing direction: C# / .NET 10 / Avalonia desktop application with Domain, Application, Infrastructure, and Desktop projects referenced by `ContactCore.slnx`.
- Existing root configuration already pins Avalonia, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, MSTest, and coverlet.
- Existing README describes ContactCore as a private, offline-first contact manager.
- At this checkpoint the solution references `src/` and `tests/` projects that are not yet present in the repository, so the documented build cannot currently succeed.

## Uploaded-prompt reconciliation

The uploaded source prompt is titled **LibraCore** and specifies a Java/Spring/React library-management product, while the explicitly supplied destination repository is **ContactCore**, whose existing committed architecture and README define a .NET/Avalonia contact-management product. The prompt itself also says to inspect an existing repository and preserve useful working history rather than replacing it. Therefore this continuation preserves ContactCore and applies the prompt's transferable engineering requirements (quality, architecture, tests, accessibility, security, documentation, CI, release discipline, and granular commits) without replacing the repository with an unrelated LibraCore product.

## Implementation plan

1. Create the four missing solution projects and three missing test projects.
2. Implement immutable/validated contact-domain value objects and entities.
3. Implement search, duplicate scoring, merge, import/export, backup, and repository abstractions.
4. Implement transactional SQLite persistence with schema migrations and indexed search.
5. Implement the Avalonia composition root, shell, contact list/editor workflow, theming, shortcuts, and About view.
6. Add deterministic domain/application/infrastructure tests.
7. Add CI, CodeQL, Dependabot, release workflows, issue/PR templates, and funding metadata.
8. Complete security/privacy/support/contribution docs, ADRs, setup/testing/release/troubleshooting/accessibility/performance docs.
9. Perform repository-level static verification available from this environment and document any toolchain limitation.

## Verification limitation

The execution environment available to this coding session does not contain the `dotnet` SDK, so local restore/build/test/format commands cannot be executed here. GitHub Actions will be configured to perform those checks on pushes and pull requests. This limitation must not be interpreted as a verified passing build.

## Commit-author limitation

The available GitHub repository API creates commits using the authenticated GitHub identity and does not expose per-commit author/committer email fields. Commit messages can be controlled, but `sanskarin@outlook.in` cannot be forced through this connector. Configure that email on the authenticated GitHub account/Git client for future local commits if exact author metadata is required.

## Next exact task

Create `src/ContactCore.Domain/ContactCore.Domain.csproj`, followed by the domain model and validation primitives.
