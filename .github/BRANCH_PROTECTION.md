# Recommended branch protection for `main`

Configure this in GitHub repository settings after the first successful CI run. The exact names below correspond to workflows committed in this repository; verify the displayed check names in GitHub before making them required.

## Required pull-request policy

- Require a pull request before merging.
- Require at least one approving review when the repository has more than one active maintainer.
- Dismiss stale approvals when new commits are pushed.
- Require conversation resolution before merge.
- Do not allow force pushes to `main`.
- Do not allow deletion of `main`.

## Required status checks

Require the cross-platform CI matrix after it has completed successfully at least once:

- `build-test (ubuntu-latest)`
- `build-test (windows-latest)`
- `build-test (macos-latest)`

Require the CodeQL analysis check once its exact check name is visible in the repository UI.

Enable **Require branches to be up to date before merging** if the repository's available Actions capacity makes the extra verification run acceptable. Otherwise use GitHub merge queue when available so required checks run against the final merge candidate.

## Administration and bypass

Prefer applying protection rules to administrators as well. Keep bypass permissions limited to repository recovery, and document any emergency bypass in the pull request or an issue.

## Signed commits

GitHub-created merge commits are normally platform-signed. Requiring signed commits for every contributor is optional; if enabled, document local signing setup in `CONTRIBUTING.md` before enforcing it.

## Security settings

For this public repository also enable, where available:

- Dependabot alerts.
- Dependabot security updates.
- Secret scanning and push protection.
- Private vulnerability reporting.
- CodeQL/default code scanning if the committed advanced workflow is not used.

Do not mark a check as required until it has successfully reported on the default branch; otherwise repository administration can accidentally block all merges.
