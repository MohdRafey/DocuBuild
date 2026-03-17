using DocBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DocBuilder.Views
{
  /// <summary>
  /// Interaction logic for ProjectSettingsWindow.xaml
  /// </summary>
  public partial class ProjectSettingsWindow : Window
  {
    public ProjectSettings UpdatedSettings { get; private set; }

    public ProjectSettingsWindow(ProjectSettings currentSettings)
    {
      InitializeComponent();

      // Load current values into the UI
      TxtBrandName.Text = currentSettings.BrandName;
      TxtLogoPath.Text = currentSettings.LogoPath;
      TxtOutputPath.Text = currentSettings.OutputPath;
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.png;*.jpg;*.ico" };
      if (dialog.ShowDialog() == true) TxtLogoPath.Text = dialog.FileName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
      // Create the result object
      UpdatedSettings = new ProjectSettings
      {
        BrandName = TxtBrandName.Text,
        LogoPath = TxtLogoPath.Text,
        OutputPath = TxtOutputPath.Text
      };

      this.DialogResult = true;
      this.Close();
    }
  }
}
