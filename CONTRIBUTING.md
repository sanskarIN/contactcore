# Contributing to ContactCore

Thank you for contributing. Keep changes focused, tested, accessible, privacy-preserving, and easy to review.

1. Install the .NET 10 SDK.
2. Fork/clone and create a feature branch.
3. Run `dotnet restore ContactCore.slnx`.
4. Implement one coherent change with tests.
5. Run `dotnet format ContactCore.slnx`, `dotnet build ContactCore.slnx -c Release`, and `dotnet test ContactCore.slnx -c Release`.
6. Open a pull request using the template.

Use Conventional Commits where practical. Never commit real contact data, secrets, local databases, private keys, or generated signing material.
