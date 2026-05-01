namespace AvaloniaApplication1.Models;

/// <summary>
/// Stores application startup settings that can be saved to a JSON config file.
/// </summary>
public sealed class StartupConfiguration
{
    /// <summary>
    /// The folder that the tree should open first when the app starts.
    /// </summary>
    public string? InitialRoot { get; set; }
}

