using System.Windows;

namespace ForgeCare.App;

public partial class DemoSessionWindow : Window
{
    public DemoSessionWindow()
    {
        InitializeComponent();
    }

    private void CloseDemoButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
