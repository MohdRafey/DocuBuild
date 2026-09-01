using DocBuilder.Models;
using DocBuilder.WPF.ViewModels;
using System;
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

        // Load project data for both new and existing projects
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
        if (string.IsNullOrEmpty(vm.Settings?.OutputPath)) return;

        string rootDir = Directory.GetParent(vm.Settings.OutputPath)?.FullName;
        if (string.IsNullOrEmpty(rootDir)) return;

        string manifestPath = Path.Combine(rootDir, "navigation.json");

        if (File.Exists(manifestPath))
        {
          // Delegate loading to MainViewModel which handles hierarchy and sidecars
          vm.LoadProject(manifestPath);
        }
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show("Error loading project content: " + ex.Message);
      }
    }
  }
}