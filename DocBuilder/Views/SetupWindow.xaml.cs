using DocBuilder.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace DocBuilder.Views
{
  public partial class SetupWindow : Window
  {
    public ProjectSettings ResultSettings { get; private set; }

    public SetupWindow()
    {
      InitializeComponent();
    }

    private void ShowNewProjectConfig_Click(object sender, RoutedEventArgs e)
    {
      WelcomeView.Visibility = Visibility.Collapsed;
      ConfigView.Visibility = Visibility.Visible;
    }

    private void BackToWelcome_Click(object sender, RoutedEventArgs e)
    {
      ConfigView.Visibility = Visibility.Collapsed;
      WelcomeView.Visibility = Visibility.Visible;
    }

    private void BrowseDestination_Click(object sender, RoutedEventArgs e)
    {
      using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
      {
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
          TxtDestination.Text = dialog.SelectedPath;
      }
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.svg" };
      if (dialog.ShowDialog() == true) TxtLogoPath.Text = dialog.FileName;
    }

    private void FinalizeNewProject_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(TxtDestination.Text))
      {
        System.Windows.MessageBox.Show("Please select a destination folder.");
        return;
      }

      string rootPath = TxtDestination.Text;
      string docsPath = Path.Combine(rootPath, "Docs");
      string imgPath = Path.Combine(rootPath, "img");

      if (!Directory.Exists(docsPath)) Directory.CreateDirectory(docsPath);
      if (!Directory.Exists(imgPath)) Directory.CreateDirectory(imgPath);

      string relativeLogoPath = "";

      // 1. Copy the logo
      if (!string.IsNullOrWhiteSpace(TxtLogoPath.Text) && File.Exists(TxtLogoPath.Text))
      {
        string extension = Path.GetExtension(TxtLogoPath.Text);
        string destinationFileName = "logo" + extension;
        string fullDestinationPath = Path.Combine(imgPath, destinationFileName);

        try
        {
          File.Copy(TxtLogoPath.Text, fullDestinationPath, true);
          relativeLogoPath = "img/" + destinationFileName;
        }
        catch (Exception ex)
        {
          System.Windows.MessageBox.Show("Could not copy logo: " + ex.Message);
        }
      }

      // 2. Handle the Template (Skeleton Creation)
      // IMPORTANT: This was missing in your snippet!
      if (ChkUseTemplate.IsChecked == true)
      {
        // This calls the external class we created to generate index.json
        DocBuilder.Services.StarterTemplate.CreateGettingStarted(docsPath);
      }

      // 3. Save Master Manifest (navigation.json)
      var manifest = new
      {
        ProjectName = TxtProjectName.Text,
        LogoPath = relativeLogoPath,
        Created = DateTime.Now,
        PageFiles = ChkUseTemplate.IsChecked == true ? new[] { "index.html" } : new string[] { }
      };

      File.WriteAllText(Path.Combine(rootPath, "navigation.json"),
          JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

      // 4. Set Result Settings for the MainViewModel
      ResultSettings = new ProjectSettings
      {
        IsExistingProject = false,
        BrandName = TxtProjectName.Text,
        LogoPath = relativeLogoPath,
        OutputPath = docsPath,
        HomeFileName = "index.html"
      };

      this.DialogResult = true;
    }

    private void OpenExisting_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new Microsoft.Win32.OpenFileDialog
      {
        Filter = "Project Config (navigation.json)|navigation.json",
        Title = "Select the Project Navigation File"
      };

      if (dialog.ShowDialog() == true)
      {
        string selectedFile = dialog.FileName;
        string rootDir = Path.GetDirectoryName(selectedFile);
        string docsPath = Path.Combine(rootDir, "Docs");

        // Safety check: ensure the Docs folder exists
        if (!Directory.Exists(docsPath))
        {
          System.Windows.MessageBox.Show("Could not find the 'Docs' folder in this directory.",
              "Missing Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        ResultSettings = new ProjectSettings
        {
          IsExistingProject = true,
          // The output path is the Docs folder where HTML/JSON sidecars live
          OutputPath = docsPath,
          // We assume the first file in the manifest is the home, 
          // but we'll set a default for now.
          HomeFileName = "index.html"
        };

        this.DialogResult = true;
      }
    }
  }
}