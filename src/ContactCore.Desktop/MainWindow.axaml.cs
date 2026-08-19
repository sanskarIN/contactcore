using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

public sealed partial class MainWindow : Window
{
    private static readonly FilePickerFileType CsvType = new("CSV contacts") { Patterns = ["*.csv"] };
    private static readonly FilePickerFileType VCardType = new("vCard contacts") { Patterns = ["*.vcf", "*.vcard"] };
    private static readonly FilePickerFileType BackupType = new("ContactCore SQLite backup") { Patterns = ["*.db", "*.sqlite", "*.sqlite3"] };

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        DataContextChanged += (_, _) => WireViewModel();
    }

    private void WireViewModel()
    {
        if (DataContext is not MainWindowViewModel vm) return;
        vm.FocusSearchRequested = () => SearchBox.Focus();
        vm.ThemeRequested = ApplyTheme;
        ApplyTheme(vm.SelectedTheme);
    }

    private static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
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
            if (vm.SaveCommand.CanExecute(null)) vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.FocusSearchRequested?.Invoke();
            e.Handled = true;
        }
        if (e.Key == Key.Escape)
        {
            if (vm.IsDeleteConfirmVisible) vm.CancelDeleteCommand.Execute(null);
            else if (vm.IsSettingsVisible || vm.IsDataToolsVisible || vm.IsDuplicatesVisible) vm.CloseOverlayCommand.Execute(null);
            else vm.CancelEditCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void ImportContacts_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !StorageProvider.CanOpen) return;
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import contacts",
                AllowMultiple = false,
                FileTypeFilter = [CsvType, VCardType]
            });
            var file = files.FirstOrDefault();
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            await vm.ImportTextAsync(await reader.ReadToEndAsync(), file.Name);
        }
        catch (Exception ex)
        {
            vm.DataToolsMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    private async void ExportCsv_Click(object? sender, RoutedEventArgs e) =>
        await ExportAsync("csv", CsvType, "contacts.csv");

    private async void ExportVCard_Click(object? sender, RoutedEventArgs e) =>
        await ExportAsync("vcard", VCardType, "contacts.vcf");

    private async Task ExportAsync(string format, FilePickerFileType type, string suggestedName)
    {
        if (DataContext is not MainWindowViewModel vm || !StorageProvider.CanSave) return;
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = format == "csv" ? "Export contacts as CSV" : "Export contacts as vCard",
                SuggestedFileName = suggestedName,
                DefaultExtension = Path.GetExtension(suggestedName).TrimStart('.'),
                FileTypeChoices = [type]
            });
            if (file is null) return;
            var text = await vm.ExportTextAsync(format);
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(text);
            vm.DataToolsMessage = $"Exported {file.Name}.";
        }
        catch (Exception ex)
        {
            vm.DataToolsMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    private async void CreateBackup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !StorageProvider.CanPickFolder) return;
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose backup folder",
                AllowMultiple = false
            });
            var path = folders.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path)) await vm.CreateBackupAsync(path);
        }
        catch (Exception ex)
        {
            vm.DataToolsMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    private async void RestoreBackup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !StorageProvider.CanOpen) return;
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Restore ContactCore backup",
                AllowMultiple = false,
                FileTypeFilter = [BackupType]
            });
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path)) await vm.RestoreBackupAsync(path);
        }
        catch (Exception ex)
        {
            vm.DataToolsMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    private void OpenExternal_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string url }) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel vm) vm.StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }
}
