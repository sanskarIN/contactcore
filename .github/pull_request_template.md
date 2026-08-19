## Summary

Describe the user-visible or engineering change and why it is needed.

## Verification

- [ ] `dotnet format ContactCore.slnx --verify-no-changes`
- [ ] `dotnet build ContactCore.slnx -c Release`
- [ ] `dotnet test ContactCore.slnx -c Release`
- [ ] New/changed behavior has automated coverage where practical
- [ ] No real contact data, credentials, secrets, or private endpoints are included
- [ ] Accessibility and keyboard behavior were considered for UI changes
- [ ] Documentation and `what_changed.md` are updated when needed

## Screenshots

For UI changes, add screenshots using fictional data only.

## Security / privacy impact

State whether the change affects stored data, file access, network access, permissions, import/export, or backup behavior.
