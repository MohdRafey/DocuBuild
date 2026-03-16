using System.IO;
using System.Windows;
using DocBuilder.Models;
using System.Text.Json; // For creating the navigation JSON

namespace DocBuilder.Views
{
  public partial class SetupWindow : Window
  {
    public ProjectSettings ResultSettings { get; private set; }

    public SetupWindow()
    {
      InitializeComponent();
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
      // 1. Ask for Folder Location
      var dialog = new System.Windows.Forms.FolderBrowserDialog
      {
        Description = "Select a folder to initialize your Documentation Project",
        UseDescriptionForTitle = true
      };

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        string rootPath = dialog.SelectedPath;

        // 2. Create the Structure: /Docs/ folder
        string docsPath = Path.Combine(rootPath, "Docs");
        if (!Directory.Exists(docsPath)) Directory.CreateDirectory(docsPath);

        // 3. Create the Structure: navigation.json
        string jsonPath = Path.Combine(rootPath, "navigation.json");
        if (!File.Exists(jsonPath))
        {
          var initialNav = new { ProjectName = "New Project", Pages = new string[] { "index.html" } };
          File.WriteAllText(jsonPath, JsonSerializer.Serialize(initialNav, new JsonSerializerOptions { WriteIndented = true }));
        }

        // 4. Set Settings and Close
        ResultSettings = new ProjectSettings
        {
          IsExistingProject = false,
          OutputPath = docsPath, // We publish inside the Docs folder
          HomeFileName = IsIndexHome.IsChecked == true ? "index.html" : "home.html"
        };

        this.DialogResult = true;
      }
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