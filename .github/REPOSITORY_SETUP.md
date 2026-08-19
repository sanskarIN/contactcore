# GitHub repository setup

The files in `.github/` configure the portions of repository quality that can live in source control. A few GitHub settings must still be enabled in the repository UI by an administrator.

## Discussions

Enable GitHub Discussions when community Q&A becomes useful. Suggested categories:

- **Announcements** — maintainer-only release/project news.
- **Q&A** — installation, usage, and development questions.
- **Ideas** — early product ideas before they become actionable feature issues.
- **Show and tell** — screenshots, packaging, integrations, and accessibility feedback using fictional data only.

Never ask users to upload a real contacts database to a public Discussion.

## Suggested labels

Create a small, stable label vocabulary instead of dozens of overlapping labels:

- `bug`
- `enhancement`
- `documentation`
- `accessibility`
- `security`
- `privacy`
- `performance`
- `database`
- `import-export`
- `desktop-ui`
- `good first issue`
- `help wanted`
- `blocked`
- `needs reproduction`

Security vulnerabilities that are not yet public must use private vulnerability reporting rather than a public `security` issue.

## Suggested milestones

- `0.1 Foundation`
- `0.2 UX completion`
- `0.3 Release hardening`
- `1.0 Stable`

Milestones should track release outcomes, not become a duplicate roadmap. `ROADMAP.md` remains the durable product plan.

## Branch protection

Apply the policy in `.github/BRANCH_PROTECTION.md` after CI has successfully reported its real check names.

## Repository security

Enable Dependabot alerts/security updates, secret scanning, push protection, and private vulnerability reporting when the GitHub plan/repository settings expose them. Keep the committed CodeQL workflow enabled unless replacing it intentionally with GitHub default setup.

## Releases

Tags matching `v*.*.*` invoke `.github/workflows/release.yml`. Before creating a production tag, verify the release checklist in `docs/release.md`. Unsigned artifacts must be described as unsigned; do not imply notarization or code signing that has not happened.

## Funding

`.github/FUNDING.yml` points to the project's Buy Me a Coffee page. Funding links must remain optional and non-disruptive to application functionality.
