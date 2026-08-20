# Release

1. Update `CHANGELOG.md`, `ROADMAP.md`, and `what_changed.md`.
2. Verify the final pull-request head passes restore, format, build, and test on Windows, Linux, and macOS.
3. Verify CodeQL and dependency checks on the same final head.
4. Tag with Semantic Versioning, for example `v0.1.0`.
5. The release workflow publishes self-contained single-file builds for Windows x64, Linux x64, macOS x64, and macOS arm64.
6. Windows output is packaged as `.zip`; Linux and macOS output is packaged as `.tar.gz`.
7. The workflow treats missing expected package files as an error before creating the GitHub Release.
8. Release packages omit debug symbols. Signing and notarization remain intentionally outside the repository until real protected signing credentials are configured.

Do not describe a target as release-verified until its GitHub Actions job has actually produced the expected archive. A successful build on one runner does not prove the other runtime identifiers package correctly.
