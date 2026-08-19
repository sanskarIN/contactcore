using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ContactCore.Desktop;

public sealed partial class ConfirmDialog : Window
{
    public ConfirmDialog(string message)
    {
        InitializeComponent();
        DataContext = new ConfirmDialogModel(message);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    private sealed record ConfirmDialogModel(string Message);
}
