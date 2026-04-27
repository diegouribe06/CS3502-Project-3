using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace AvaloniaApplication1.Views;

public partial class CloseViewerConfirmation : Window
{
    private TaskCompletionSource<bool>? _resultSource;

    public CloseViewerConfirmation()
    {
        InitializeComponent();
        Closed += (_, _) => _resultSource?.TrySetResult(false);
    }

    public Task<bool> ShowAsync(Window owner)
    {
        _resultSource = new TaskCompletionSource<bool>();
        _ = ShowDialog(owner);
        return _resultSource.Task;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        _resultSource?.TrySetResult(false);
        Close();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _resultSource?.TrySetResult(true);
        Close();
    }
}