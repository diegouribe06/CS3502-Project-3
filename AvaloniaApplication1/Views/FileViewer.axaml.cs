using System;
using System.IO;
using Avalonia.Controls;
using System.Windows.Input;
using System.Threading.Tasks;

namespace AvaloniaApplication1.Views;

public partial class FileViewer : Window
{
    public string FilePath { get; }
    public string FileContent { get; set; } = string.Empty;
    public bool CanEditFile { get; }
    public bool IsReadOnlyMode => !CanEditFile;
    public bool ShowSaveButton => CanEditFile;
    private string _originalFileContent = string.Empty;
    public ICommand CloseCommand { get; }
    public ICommand SaveCommand { get; }

    public FileViewer() : this(string.Empty, canEditFile: true)
    {
    }

    public FileViewer(string filePath, bool canEditFile = true)
    {
        FilePath = filePath;
        CanEditFile = canEditFile;
        CloseCommand = new RelayCommand(async _ => await PromptCloseAsync());
        SaveCommand = new RelayCommand(_ => SaveFile(), _ => !string.IsNullOrWhiteSpace(FilePath) && CanEditFile);
        Title = string.IsNullOrEmpty(filePath)
            ? "File Viewer"
            : $"File Viewer - {Path.GetFileName(filePath)}";

        try
        {
            FileContent = string.IsNullOrEmpty(filePath)
                ? "No file loaded."
                : File.ReadAllText(filePath);
            _originalFileContent = FileContent;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileContent = $"Unable to read file '{filePath}'.{Environment.NewLine}{ex.Message}";
            _originalFileContent = FileContent;
        }

        InitializeComponent();
        DataContext = this;
    }

    private void SaveFile()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            return;
        }

        try
        {
            File.WriteAllText(FilePath, FileContent);
            _originalFileContent = FileContent;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileContent = $"Unable to save file '{FilePath}'.{Environment.NewLine}{ex.Message}";
        }
    }

    private async Task PromptCloseAsync()
    {
        if (!HasUnsavedChanges())
        {
            Close();
            return;
        }

        var confirmation = new CloseViewerConfirmation();
        bool shouldClose = await confirmation.ShowAsync(this);

        if (shouldClose)
        {
            Close();
        }
    }

    private bool HasUnsavedChanges()
    {
        return !string.Equals(FileContent, _originalFileContent, StringComparison.Ordinal);
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}