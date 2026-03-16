using DocBuilder.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace DocBuilder.WPF.ViewModels
{
  public class MainViewModel : ViewModelBase
  {
    public ProjectSettings Settings { get; set; }
    public ObservableCollection<DocPage> Pages { get; set; }
    public ObservableCollection<DocPage> DeletedPages { get; set; } = new ObservableCollection<DocPage>();

    private DocPage _currentPage;
    public DocPage CurrentPage
    {
      get => _currentPage;
      set { _currentPage = value; OnPropertyChanged(); }
    }

    public RelayCommand AddSectionCommand { get; }
    public RelayCommand PublishCommand { get; }
    public RelayCommand AddPageCommand { get; }
    public ICommand DeletePageCommand { get; }
    public ICommand RestorePageCommand { get; }
    public MainViewModel()
    {
      Settings = new ProjectSettings();
      Pages = new ObservableCollection<DocPage>();

      // REMOVED: Dummy data logic that was adding "firstPage" automatically.
      // The collection should now be empty until populated by App.xaml.cs 
      // using the data passed from SetupWindow.

      AddSectionCommand = new RelayCommand(param =>
      {
        if (CurrentPage == null) return;

        if (Enum.TryParse(param.ToString(), out SectionType type))
        {
          string defaultContent = $"Enter {type} content here...";

          // Custom logic for specific types
          switch (type)
          {
            case SectionType.Separator:
              defaultContent = "---"; // Content doesn't matter much for separator
              break;

            case SectionType.Tags:
              defaultContent = "General, Tutorial, Update";
              break;

            case SectionType.Bullets:
              // First line is description, next lines are bullets
              defaultContent = "Key Features of this module:" + Environment.NewLine +
                               "• First bullet point" + Environment.NewLine +
                               "• Second bullet point";
              break;

            case SectionType.Warning:
              defaultContent = "IMPORTANT: Please review the security protocols before proceeding.";
              break;

            case SectionType.Note:
              defaultContent = "TIP: You can use Ctrl+S to save your progress at any time.";
              break;
          }

          CurrentPage.Sections.Add(new DocSection
          {
            Type = type,
            Content = defaultContent
          });
        }
      });

      PublishCommand = new RelayCommand(o => PublishAll());
      AddPageCommand = new RelayCommand(o => ExecuteAddPage());
      DeletePageCommand = new RelayCommand<DocPage>(ExecuteDeletePage);
      RestorePageCommand = new RelayCommand<DocPage>(ExecuteRestorePage);
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
        var generator = new DocBuilder.WPF.Services.HtmlGenerator();
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

        // We'll create a flat list to make sure the generator sees EVERY page
        var allPagesToGenerate = new List<DocPage>();

        if (!Directory.Exists(Settings.OutputPath))
          Directory.CreateDirectory(Settings.OutputPath);

        // 1. Recursive Helper
        void ProcessPageRecursive(DocPage page, int position)
        {
          page.Position = position;

          // Determine Filename
          if (page.Title.Equals("Getting Started", StringComparison.OrdinalIgnoreCase))
            page.FileName = "index.html";
          else
          {
            string slug = page.Title.Replace(" ", "-").ToLower();
            slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            page.FileName = $"{slug}.html";
          }

          // Save individual JSON
          string baseName = Path.GetFileNameWithoutExtension(page.FileName);
          string jsonPath = Path.Combine(Settings.OutputPath, $"{baseName}.json");
          File.WriteAllText(jsonPath, System.Text.Json.JsonSerializer.Serialize(page, jsonOptions));

          // Add to our flat list for the HTML generator
          allPagesToGenerate.Add(page);

          // Process children
          for (int i = 0; i < page.Children.Count; i++)
          {
            ProcessPageRecursive(page.Children[i], i);
          }
        }

        // Run recursion
        for (int i = 0; i < Pages.Count; i++)
        {
          ProcessPageRecursive(Pages[i], i);
        }

        // 2. Save Manifest
        string rootDir = Directory.GetParent(Settings.OutputPath).FullName;
        string manifestPath = Path.Combine(rootDir, "navigation.json");

        var manifest = new
        {
          ProjectName = Settings.BrandName,
          LogoPath = Settings.LogoPath,
          LastBuild = DateTime.Now,
          Structure = Pages.OrderBy(p => p.Position).Select(p => new {
            p.Title,
            p.FileName,
            p.IsRoot,
            p.Position,
            Children = p.Children.OrderBy(c => c.Position).Select(c => new {
              c.Title,
              c.FileName,
              c.Position
            }).ToList()
          }).ToList()
        };

        File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest, jsonOptions));

        // 3. Generate HTML (Using the flattened list)
        // We convert back to ObservableCollection if your generator strictly requires it
        generator.GenerateProject(new ObservableCollection<DocPage>(allPagesToGenerate), Settings);

        // 4. Success UI
        var result = System.Windows.MessageBox.Show(
            $"Successfully published to {Settings.OutputPath}!",
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
        System.Windows.MessageBox.Show($"Publishing failed: {ex.Message}", "Error");
      }
    }
    private void ExecuteAddPage()
    {
      // Initialize our new custom dialog
      var dialog = new DocBuilder.Views.NewPageDialog();

      // Set the owner to the current main window so it centers correctly
      dialog.Owner = System.Windows.Application.Current.MainWindow;

      if (dialog.ShowDialog() == true)
      {
        string title = dialog.PageTitle;

        // Check if page already exists (Case Insensitive)
        if (Pages.Any(p => p.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
        {
          System.Windows.MessageBox.Show("A page with this title already exists.");
          return;
        }

        // Create the new page object
        var newPage = new DocPage
        {
          Title = title,
          Category = "General",
          Sections = new ObservableCollection<DocSection>
            {
                new DocSection { Type = SectionType.H1, Content = title },
                new DocSection { Type = SectionType.Paragraph, Content = "Start writing your content here..." }
            }
        };

        Pages.Add(newPage);
        CurrentPage = newPage;
      }
    }

    private void ExecuteDeletePage(DocPage page)
    {
      if (page == null) return;

      // 1. Move to "Deleted" collection for UI
      RemovePageFromHierarchy(page);
      DeletedPages.Add(page);

      // 2. Physical File Move
      string deletedDir = Path.Combine(Settings.OutputPath, "deleted");
      if (!Directory.Exists(deletedDir)) Directory.CreateDirectory(deletedDir);

      string jsonName = Path.GetFileNameWithoutExtension(page.FileName) + ".json";
      string sourcePath = Path.Combine(Settings.OutputPath, jsonName);
      string destPath = Path.Combine(deletedDir, jsonName);

      if (File.Exists(sourcePath)) File.Move(sourcePath, destPath, true);

      // 3. Permanent HTML deletion (optional, keeps published site clean)
      string htmlPath = Path.Combine(Settings.OutputPath, page.FileName);
      if (File.Exists(htmlPath)) File.Delete(htmlPath);
    }

    private void ExecuteRestorePage(DocPage page)
    {
      if (page == null) return;

      // Remove from trash
      DeletedPages.Remove(page);

      // RESTORATION STRATEGY: 
      // Always restore as a Root page. This is the safest way to prevent 
      // "Orphan" errors if the original parent was also deleted or moved.
      page.IsRoot = true;
      Pages.Add(page);
    }

    public void RemovePageFromHierarchy(DocPage pageToRemove)
    {
      // Check if it's in the top-level collection
      if (Pages.Contains(pageToRemove))
      {
        Pages.Remove(pageToRemove);
        return;
      }

      // Check if it's a child of any page
      foreach (var parent in Pages)
      {
        if (parent.Children.Contains(pageToRemove))
        {
          parent.Children.Remove(pageToRemove);
          return;
        }
      }
    }

  }
}