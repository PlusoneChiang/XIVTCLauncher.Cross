using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace FFXIVSimpleLauncher.Views;

public partial class OtpDialog : Window
{
    public string OtpCode => OtpTextBox.Text;

    public OtpDialog()
    {
        InitializeComponent();
        OtpTextBox.Focus();
    }

    private void OtpTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow digits
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void OtpTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && OtpTextBox.Text.Length == 6)
        {
            DialogResult = true;
            Close();
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (OtpTextBox.Text.Length != 6)
        {
            MessageBox.Show("Please enter a 6-digit OTP code.", "Invalid OTP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
