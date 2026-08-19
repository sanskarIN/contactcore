# Release

ContactCore publishes self-contained desktop builds from Git tags matching `v*.*.*`.

## Preconditions

Before tagging a release:

```bash
git status --short
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

Also confirm:

- CI and CodeQL are green on the exact commit;
- `CHANGELOG.md`, `ROADMAP.md`, and `what_changed.md` match reality;
- no real contact data, secrets, generated databases, exports, or local configuration are tracked;
- migrations have clean-database and upgrade coverage where applicable;
- platform smoke checks have been completed for release candidates;
- screenshots/docs use fictional data only.

## Versioning

Use semantic versions:

- patch: compatible bug/security fixes;
- minor: backward-compatible features;
- major: intentionally incompatible behavior/data/API changes.

Pre-1.0 releases may use `0.x.y` while architecture and data formats are still evolving.

## Tag and push

Example:

```bash
git tag -a v0.1.0 -m "ContactCore v0.1.0"
git push origin v0.1.0
```

The `Release` workflow builds:

- `win-x64`
- `linux-x64`
- `osx-x64`
- `osx-arm64`

Each matrix job runs tests, publishes a self-contained desktop build, packages it, and uploads an artifact. A final job creates the GitHub Release and attaches all packages.

## Local publish check

Example for the current operating system/runtime identifier:

```bash
dotnet publish src/ContactCore.Desktop/ContactCore.Desktop.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o artifacts/linux-x64
```

Change the RID as appropriate.

## Release notes

Release notes should include:

- user-visible additions/fixes;
- security/privacy-relevant changes;
- migration or backup implications;
- known limitations;
- exact supported platforms/artifacts;
- upgrade instructions when needed.

## Rollback

Do not silently overwrite a published tag. If a release is defective, mark it clearly, fix forward on a new commit, and publish a new patch version. If a schema migration is implicated, preserve user data first and document recovery steps before releasing another migration.

## Signing

Current automation creates unsigned self-contained artifacts. Platform code signing/notarization is future release-hardening work and must not be claimed until configured and verified.
