using System.Windows;

namespace DocBuilder.Views
{
  public partial class NewPageDialog : Window
  {
    public string PageTitle { get; private set; }

    public NewPageDialog()
    {
      InitializeComponent();
      TxtTitle.Focus();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(TxtTitle.Text))
      {
        System.Windows.MessageBox.Show("Please enter a page title.");
        return;
      }

      PageTitle = TxtTitle.Text.Trim();
      this.DialogResult = true;
    }

    private void TxtTitle_GotFocus(object sender, RoutedEventArgs e)
    {
      TxtTitle.SelectAll();
    }
  }
}