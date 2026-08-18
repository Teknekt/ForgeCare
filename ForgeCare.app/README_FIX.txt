ForgeCare Sprint 11A startup crash quickfix

Cause:
MainWindow.xaml referenced StaticResource PrimaryActionButtonStyle,
but that resource key did not exist. WPF therefore threw a
Markup.XamlParseException during InitializeComponent() at startup.

Fix:
MainWindow.xaml now uses explicit gold primary-button properties for
SAVE PREFERENCES instead of the missing StaticResource.

Install:
1. Replace MainWindow.xaml in the ForgeCare.app project root.
2. Build > Clean Solution
3. Build > Rebuild Solution
4. F5

No C# changes are required for this quickfix.
