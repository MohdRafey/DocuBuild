using DocBuilder.Models;
using DocBuilder.WPF.ViewModels;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace DocBuilder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      // PREVENT SHUTDOWN when the Setup window closes
      System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

      var setupWindow = new DocBuilder.Views.SetupWindow();

      if (setupWindow.ShowDialog() == true)
      {
        var mainVM = new MainViewModel();
        mainVM.Settings = setupWindow.ResultSettings;

        // Load project data (this works for both New and Existing)
        // If it's a new project without a template, LoadProjectData will find 0 PageFiles and stay empty.
        LoadProjectData(mainVM);

        var mainWindow = new MainWindow { DataContext = mainVM };
        mainWindow.Show();
      }
      else
      {
        Shutdown();
      }
    }

    private void LoadProjectData(DocBuilder.WPF.ViewModels.MainViewModel vm)
    {
      try
      {
        string rootDir = Directory.GetParent(vm.Settings.OutputPath).FullName;
        string manifestPath = Path.Combine(rootDir, "navigation.json");

        if (File.Exists(manifestPath))
        {
          string json = File.ReadAllText(manifestPath);
          // Dynamic object to read the list of files
          var manifest = System.Text.Json.JsonDocument.Parse(json);
          var pageFiles = manifest.RootElement.GetProperty("PageFiles");

          vm.Pages.Clear();

          foreach (var fileElement in pageFiles.EnumerateArray())
          {
            string htmlFileName = fileElement.GetString();
            string jsonFileName = Path.GetFileNameWithoutExtension(htmlFileName) + ".json";
            string fullJsonPath = Path.Combine(vm.Settings.OutputPath, jsonFileName);

            if (File.Exists(fullJsonPath))
            {
              string pageData = File.ReadAllText(fullJsonPath);
              var loadedPage = System.Text.Json.JsonSerializer.Deserialize<DocBuilder.Models.DocPage>(pageData);
              vm.Pages.Add(loadedPage);
            }
          }
          vm.CurrentPage = vm.Pages.FirstOrDefault();
        }
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show("Error loading project content: " + ex.Message);
      }
    }
  }

}
