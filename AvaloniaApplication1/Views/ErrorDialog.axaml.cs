using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaApplication1.Views;

/// <summary>
/// A detailed error dialog that shows error messages with optional context and suggestions.
/// This replaces simple message boxes with a richer error experience.
/// </summary>
public partial class ErrorDialog : Window
{
    private TaskCompletionSource<bool>? _taskCompletionSource;

    public ErrorDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the error dialog with a title and main message.
    /// Shows the error dialog with title, message, optional context, optional suggestion, and optional raw details.
    /// </summary>
    public Task<bool> ShowAsync(Window owner, string title, string errorMessage, string? context = null, string? suggestion = null, string? details = null)
    {
        _taskCompletionSource = new TaskCompletionSource<bool>();

        // Set the title and main error message.
        Title = title;
        var titleBlock = this.FindControl<TextBlock>("TitleTextBlock");
        if (titleBlock is not null)
        {
            titleBlock.Text = title;
        }

        var messageBlock = this.FindControl<TextBlock>("ErrorMessageTextBlock");
        if (messageBlock is not null)
        {
            messageBlock.Text = errorMessage;
        }

        // Show context if provided.
        if (!string.IsNullOrWhiteSpace(context))
        {
            var contextBorder = this.FindControl<Border>("ContextBorder");
            var contextBlock = this.FindControl<TextBlock>("ContextTextBlock");
            if (contextBorder is not null && contextBlock is not null)
            {
                contextBorder.IsVisible = true;
                contextBlock.Text = context;
            }
        }

        // Show suggestion if provided.
        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            var suggestionBorder = this.FindControl<Border>("SuggestionBorder");
            var suggestionBlock = this.FindControl<TextBlock>("SuggestionTextBlock");
            if (suggestionBorder is not null && suggestionBlock is not null)
            {
                suggestionBorder.IsVisible = true;
                suggestionBlock.Text = suggestion;
            }
        }

        // Populate raw details (advanced) if provided; keep hidden by default.
        var detailsBorder = this.FindControl<Border>("DetailsBorder");
        var detailsBox = this.FindControl<TextBox>("DetailsTextBox");
        var detailsButton = this.FindControl<Button>("DetailsButton");
        if (!string.IsNullOrWhiteSpace(details) && detailsBorder is not null && detailsBox is not null && detailsButton is not null)
        {
            detailsBox.Text = details;
            detailsBorder.IsVisible = false;
            detailsButton.IsVisible = true;
            detailsButton.Content = "Show Details";
            // Wire the click handler from code to avoid XAML event resolution issues.
            detailsButton.Click += OnDetailsClicked;
        }
        else if (detailsButton is not null)
        {
            // Hide the details button when there are no details to show.
            detailsButton.IsVisible = false;
        }

        ShowDialog(owner);
        return _taskCompletionSource.Task;
    }

    public void OnOkClicked(object sender, RoutedEventArgs e)
    {
        _taskCompletionSource?.TrySetResult(true);
        Close();
    }

    public void OnDetailsClicked(object sender, RoutedEventArgs e)
    {
        var detailsBorder = this.FindControl<Border>("DetailsBorder");
        var detailsBox = this.FindControl<TextBox>("DetailsTextBox");
        var detailsButton = this.FindControl<Button>("DetailsButton");
        if (detailsBorder is null || detailsBox is null || detailsButton is null)
            return;

        if (detailsBorder.IsVisible)
        {
            detailsBorder.IsVisible = false;
            detailsButton.Content = "Show Details";
        }
        else
        {
            detailsBorder.IsVisible = true;
            detailsButton.Content = "Hide Details";
        }
    }
}

