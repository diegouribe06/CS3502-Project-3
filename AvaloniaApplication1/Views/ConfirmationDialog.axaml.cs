using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaApplication1.Views;

public partial class ConfirmationDialog : Window
{
    private TaskCompletionSource<bool>? _resultSource;
    private readonly TextBlock _messageTextBlock;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;

    public ConfirmationDialog()
    {
        InitializeComponent();

        _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock")
            ?? throw new InvalidOperationException("MessageTextBlock control was not found.");
        _confirmButton = this.FindControl<Button>("ConfirmButton")
            ?? throw new InvalidOperationException("ConfirmButton control was not found.");
        _cancelButton = this.FindControl<Button>("CancelButton")
            ?? throw new InvalidOperationException("CancelButton control was not found.");

        Closed += (_, _) => _resultSource?.TrySetResult(false);
    }

    public ConfirmationDialog(string title, string message, string confirmText, string cancelText)
        : this()
    {
        Title = title;
        _messageTextBlock.Text = message;
        _confirmButton.Content = confirmText;
        _cancelButton.Content = cancelText;
    }

    public Task<bool> ShowAsync(Window owner)
    {
        _resultSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = ShowDialog(owner);
        return _resultSource.Task;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        _resultSource?.TrySetResult(false);
        Close();
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        _resultSource?.TrySetResult(true);
        Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
