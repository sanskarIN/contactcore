# Testing

The test suite contains domain validation/normalization tests, duplicate/merge tests, CSV/vCard round trips, randomized parser input, and SQLite integration tests using isolated temporary databases.

Run all tests with `dotnet test ContactCore.slnx -c Release`. CI runs restore, formatting verification, release build, and tests on Windows, Linux, and macOS. Coverage is collected in CI for inspection; no misleading fixed coverage percentage is claimed in the README.
