# Release

1. Update `CHANGELOG.md`, `ROADMAP.md`, and `what_changed.md`.
2. Run format/build/test from a clean checkout on .NET 10.
3. Review dependency and CodeQL results.
4. Tag with Semantic Versioning, for example `v0.1.0`.
5. The release workflow publishes self-contained Windows x64, Linux x64, macOS x64, and macOS arm64 artifacts and attaches them to a GitHub Release.
6. Signing/notarization is intentionally outside the repository until real protected signing credentials are configured.
