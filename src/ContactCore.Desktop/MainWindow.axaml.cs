using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace ContactCore.Desktop;

public sealed partial class MainWindow : Window
{
    private const int MaxImportCharacters = 5_000_000;
    private MainWindowViewModel? _wiredViewModel;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnwireViewModel();
        _wiredViewModel = DataContext as MainWindowViewModel;
        if (_wiredViewModel is null) return;

        _wiredViewModel.FocusSearchRequested = () => SearchBox.Focus();
        _wiredViewModel.PickImportTextRequested = PickImportTextAsync;
        _wiredViewModel.SaveTextRequested = SaveTextAsync;
        _wiredViewModel.PickBackupFileRequested = PickBackupFileAsync;
        _wiredViewModel.ConfirmActionRequested = ConfirmActionAsync;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        UnwireViewModel();
        _wiredViewModel = null;
    }

    private void UnwireViewModel()
    {
        if (_wiredViewModel is null) return;
        _wiredViewModel.FocusSearchRequested = null;
        _wiredViewModel.PickImportTextRequested = null;
        _wiredViewModel.SaveTextRequested = null;
        _wiredViewModel.PickBackupFileRequested = null;
        _wiredViewModel.ConfirmActionRequested = null;
    }

    private async Task<PickedTextFile?> PickImportTextAsync()
    {
        if (!StorageProvider.CanOpen) return null;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import contacts",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Contact files") { Patterns = ["*.csv", "*.vcf", "*.vcard"] },
                new FilePickerFileType("CSV") { Patterns = ["*.csv"] },
                new FilePickerFileType("vCard") { Patterns = ["*.vcf", "*.vcard"] }
            ]
        });

        using var file = files.FirstOrDefault();
        if (file is null) return null;
        await using var stream = await file.OpenReadAsync();
        var content = await ReadLimitedTextAsync(stream, MaxImportCharacters);
        return new PickedTextFile(file.Name, content);
    }

    private async Task<bool> SaveTextAsync(string suggestedName, string content)
    {
        if (!StorageProvider.CanSave) return false;
        var extension = Path.GetExtension(suggestedName);
        var fileType = extension.Equals(".vcf", StringComparison.OrdinalIgnoreCase)
            ? new FilePickerFileType("vCard") { Patterns = ["*.vcf"] }
            : new FilePickerFileType("CSV") { Patterns = ["*.csv"] };

        using var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export contacts",
            SuggestedFileName = suggestedName,
            FileTypeChoices = [fileType]
        });
        if (file is null) return false;

        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek) stream.SetLength(0);
        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 1024,
            leaveOpen: false);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        return true;
    }

    private async Task<PickedBackupFile?> PickBackupFileAsync()
    {
        if (!StorageProvider.CanOpen) return null;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Restore ContactCore backup",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("ContactCore database backup") { Patterns = ["*.db"] }]
        });

        using var file = files.FirstOrDefault();
        if (file is null) return null;

        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
            return new PickedBackupFile(localPath);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "ContactCore", "restore-picker");
        Directory.CreateDirectory(tempDirectory);
        var tempPath = Path.Combine(tempDirectory, $"restore-{Guid.NewGuid():N}.db");
        await using (var source = await file.OpenReadAsync())
        await using (var target = File.Create(tempPath))
            await source.CopyToAsync(target);

        return new PickedBackupFile(tempPath, DeleteAfterUse: true);
    }

    private async Task<bool> ConfirmActionAsync(string message)
    {
        var dialog = new ConfirmDialog(message);
        var result = await dialog.ShowDialog<bool?>(this);
        return result == true;
    }

    private static async Task<string> ReadLimitedTextAsync(Stream stream, int maxCharacters)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var builder = new StringBuilder(Math.Min(maxCharacters, 32_768));
        var buffer = new char[8_192];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory());
            if (read == 0) break;
            if (builder.Length + read > maxCharacters)
                throw new InvalidDataException("The selected import file is too large. The maximum supported text size is 5,000,000 characters.");
            builder.Append(buffer, 0, read);
        }
        return builder.ToString();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.NewContactCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (vm.IsEditorVisible && vm.SaveCommand.CanExecute(null)) vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.FocusSearchRequested?.Invoke();
            e.Handled = true;
        }
        if (e.Key == Key.Escape)
        {
            vm.CancelEditCommand.Execute(null);
            e.Handled = true;
        }
    }
}
