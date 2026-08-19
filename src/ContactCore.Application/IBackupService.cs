namespace ContactCore.Application;

public sealed record BackupResult(string Path, long SizeBytes, DateTimeOffset CreatedAt);

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string sourcePath, CancellationToken cancellationToken = default);
}
