using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DanteConfigEditor.Mac;

internal sealed partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    private T? FindControl<T>(string name) where T : Control => ControlExtensions.FindControl<T>(this, name);

    public static Task<bool> ShowAsync(
        Window owner,
        string title,
        string message,
        string primaryText,
        string secondaryText)
    {
        MessageDialog dialog = new();
        dialog.FindControl<TextBlock>("DialogTitleText")!.Text = title;
        dialog.FindControl<TextBlock>("DialogMessageText")!.Text = message;
        dialog.FindControl<Button>("PrimaryButton")!.Content = primaryText;
        dialog.FindControl<Button>("SecondaryButton")!.Content = secondaryText;
        return dialog.ShowDialog<bool>(owner);
    }

    public static Task<bool> ShowInfoAsync(Window owner, string title, string message, string closeText)
    {
        MessageDialog dialog = new();
        dialog.FindControl<TextBlock>("DialogTitleText")!.Text = title;
        dialog.FindControl<TextBlock>("DialogMessageText")!.Text = message;
        dialog.FindControl<Button>("PrimaryButton")!.Content = closeText;
        dialog.FindControl<Button>("SecondaryButton")!.IsVisible = false;
        return dialog.ShowDialog<bool>(owner);
    }

    private void PrimaryButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    private void SecondaryButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
}
