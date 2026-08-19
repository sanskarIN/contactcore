# Development

Keep domain rules independent from Avalonia/SQLite. Add database changes as ordered migrations. Prefer cancellation-aware async APIs for I/O. Use parameterized SQL. New import formats must have malformed-input tests. UI changes should preserve keyboard navigation, visible focus, scalable text, and non-color-only status information.

Quality commands:
```bash
dotnet format ContactCore.slnx
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```
