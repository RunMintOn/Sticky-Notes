using System.Windows;
using System.Windows.Controls;
using StickyNotes.App.Services;

namespace StickyNotes.App.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage() => InitializeComponent();

    public event RoutedEventHandler? BackRequested;

    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, e);

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserSettings settings) settings.Reset();
    }
}
