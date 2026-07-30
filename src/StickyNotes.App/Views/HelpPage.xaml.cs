using System.Windows;
using System.Windows.Controls;

namespace StickyNotes.App.Views;

public partial class HelpPage : UserControl
{
    public HelpPage() => InitializeComponent();

    public event RoutedEventHandler? BackRequested;

    private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, e);
}
