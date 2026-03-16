using DocBuilder.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace DocBuilder.WPF.ViewModels
{
  public class MainViewModel : ViewModelBase
  {
    public ProjectSettings Settings { get; set; }
    public ObservableCollection<DocPage> Pages { get; set; }

    private DocPage _currentPage;
    public DocPage CurrentPage
    {
      get => _currentPage;
      set { _currentPage = value; OnPropertyChanged(); }
    }

    public RelayCommand AddSectionCommand { get; }
    public RelayCommand PublishCommand { get; }

    public MainViewModel()
    {
      Settings = new ProjectSettings();
      Pages = new ObservableCollection<DocPage>();

      // Dummy data for testing
      var firstPage = new DocPage { Title = "Introduction", FileName = "index.html" };
      firstPage.Sections.Add(new DocSection { Type = SectionType.H1, Content = "Welcome" });
      Pages.Add(firstPage);
      CurrentPage = firstPage;

      AddSectionCommand = new RelayCommand(param =>
      {
        if (CurrentPage == null) return;

        SectionType type = (SectionType)Enum.Parse(typeof(SectionType), param.ToString());
        CurrentPage.Sections.Add(new DocSection { Type = type, Content = "New " + type });
      });

      AddSectionCommand = new RelayCommand(param =>
      {
        if (CurrentPage == null) return;

        // Convert the string parameter (e.g., "H1", "Paragraph") to the Enum
        if (Enum.TryParse(param.ToString(), out SectionType type))
        {
          CurrentPage.Sections.Add(new DocSection
          {
            Type = type,
            Content = $"Enter {type} content here..."
          });
        }
      });

      PublishCommand = new RelayCommand(o => PublishAll());
    }

    private void PublishAll()
    {
      if (Pages == null || Pages.Count == 0)
      {
        System.Windows.MessageBox.Show("No pages found to publish.");
        return;
      }

      try
      {
        // 1. Initialize Services
        var generator = new DocBuilder.WPF.Services.HtmlGenerator();
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

        // 2. Ensure Output Directory exists
        if (!Directory.Exists(Settings.OutputPath))
          Directory.CreateDirectory(Settings.OutputPath);

        // 3. Loop through each page to save its Data (JSON) and View (HTML)
        foreach (var page in Pages)
        {
          // Sanitize filename based on Title if not already set
          // (Assumes you have the SanitizeFilename logic we discussed)
          string baseName = Path.GetFileNameWithoutExtension(page.FileName);
          if (string.IsNullOrEmpty(baseName)) baseName = "index";

          // A. Save the modular JSON file (The "Source of Truth")
          string jsonPath = Path.Combine(Settings.OutputPath, baseName + ".json");
          string jsonString = System.Text.Json.JsonSerializer.Serialize(page, jsonOptions);
          File.WriteAllText(jsonPath, jsonString);
        }

        // 4. Save the navigation.json Manifest in the root folder
        // This tells the app which files belong to the project
        string rootDir = Directory.GetParent(Settings.OutputPath).FullName;
        string manifestPath = Path.Combine(rootDir, "navigation.json");

        var manifest = new
        {
          ProjectName = Settings.BrandName,
          LastBuild = DateTime.Now,
          PageFiles = Pages.Select(p => p.FileName).ToList()
        };

        File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest, jsonOptions));

        // 5. Run the HTML Generation (The "Visual Export")
        generator.GenerateProject(Pages, Settings);

        // 6. Success Notification
        var result = System.Windows.MessageBox.Show(
            $"Project published successfully!\n\nSource: {manifestPath}\nOutput: {Settings.OutputPath}\n\nOpen folder?",
            "Publish Complete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Information);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
          System.Diagnostics.Process.Start("explorer.exe", Settings.OutputPath);
        }
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show($"Publishing failed: {ex.Message}", "Error",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
      }
    }
  }
}