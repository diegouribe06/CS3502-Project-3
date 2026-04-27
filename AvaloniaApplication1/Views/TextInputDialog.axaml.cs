using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaApplication1.Views;

public partial class TextInputDialog : Window
{
    private TaskCompletionSource<string?>? _resultSource;
    private readonly TextBlock _messageTextBlock;
    private readonly TextBox _inputTextBox;
    private readonly TextBlock _validationTextBlock;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;
    private Func<string, string?>? _validator;

    public TextInputDialog()
    {
        InitializeComponent();

        _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock")
            ?? throw new InvalidOperationException("MessageTextBlock control was not found.");
        _inputTextBox = this.FindControl<TextBox>("InputTextBox")
            ?? throw new InvalidOperationException("InputTextBox control was not found.");
        _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock")
            ?? throw new InvalidOperationException("ValidationTextBlock control was not found.");
        _confirmButton = this.FindControl<Button>("ConfirmButton")
            ?? throw new InvalidOperationException("ConfirmButton control was not found.");
        _cancelButton = this.FindControl<Button>("CancelButton")
            ?? throw new InvalidOperationException("CancelButton control was not found.");

        _inputTextBox.TextChanged += OnInputTextChanged;
        Closed += (_, _) => _resultSource?.TrySetResult(null);
    }

    public TextInputDialog(
        string title,
        string message,
        string initialValue,
        string confirmText,
        string cancelText,
        Func<string, string?>? validator = null)
        : this()
    {
        Title = title;
        _messageTextBlock.Text = message;
        _inputTextBox.Text = initialValue;
        _confirmButton.Content = confirmText;
        _cancelButton.Content = cancelText;
        _validator = validator;

        ValidateInput();
    }

    public Task<string?> ShowAsync(Window owner)
    {
        _resultSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = ShowDialog(owner);
        return _resultSource.Task;
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        ValidateInput();
    }

    private void ValidateInput()
    {
        string currentText = _inputTextBox.Text ?? string.Empty;
        string? validationError = _validator?.Invoke(currentText);
        bool hasError = !string.IsNullOrWhiteSpace(validationError);

        _validationTextBlock.Text = validationError ?? string.Empty;
        _validationTextBlock.IsVisible = hasError;
        _confirmButton.IsEnabled = !hasError;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        _resultSource?.TrySetResult(null);
        Close();
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (!_confirmButton.IsEnabled)
        {
            return;
        }

        _resultSource?.TrySetResult(_inputTextBox.Text ?? string.Empty);
        Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

