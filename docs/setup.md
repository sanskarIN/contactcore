# Setup

## Requirements
- .NET 10 SDK (the repository pins `10.0.100` with feature-band roll-forward)
- Git
- Windows 10/11, a supported macOS release, or a mainstream 64-bit Linux desktop with Avalonia runtime prerequisites

## Commands
```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
dotnet restore ContactCore.slnx
dotnet build ContactCore.slnx -c Release
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

`CONTACTCORE_DATA_PATH` can override the local data directory. `CONTACTCORE_DATABASE_KEY` requests an encrypted database, but the application intentionally refuses to continue unless a SQLCipher-compatible SQLite provider is actually loaded. Do not put real keys into tracked files.
