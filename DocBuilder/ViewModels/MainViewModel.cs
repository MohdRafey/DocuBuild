using DocBuilder.Helpers;
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

    private bool _isDirty;
    public bool IsDirty
    {
      get => _isDirty;
      set
      {
        if (_isDirty != value)
        {
          _isDirty = value;
          OnPropertyChanged(nameof(IsDirty));
          // Optional: Update the Window Title to show an asterisk *
        }
      }
    }

    public MainViewModel()
    {
      Settings = new ProjectSettings();
      Pages = new ObservableCollection<DocPage>();
      Pages.CollectionChanged += (s, e) => IsDirty = true;

      DeletedPages = new ObservableCollection<DocPage>();
      DeletedPages.CollectionChanged += (s, e) => IsDirty = true;

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

    #region Loading Existing project
    public void LoadProject(string navigationJsonPath)
    {
      try
      {
        // 1. Clear current state to avoid mixing projects
        Pages.Clear();
        DeletedPages.Clear();

        if (!File.Exists(navigationJsonPath)) return;

        // 2. Read the Manifest
        string jsonContent = File.ReadAllText(navigationJsonPath);
        var manifest = System.Text.Json.JsonSerializer.Deserialize<ProjectManifest>(jsonContent);

        // 3. Set the directory context (The folder containing the JSON sidecars)
        string docsFolder = Path.Combine(Path.GetDirectoryName(navigationJsonPath), "Docs");

        // 4. Load the structure recursively
        foreach (var pageRef in manifest.Structure)
        {
          var loadedRootPage = LoadPageWithChildren(pageRef.FileName, docsFolder);
          if (loadedRootPage != null)
          {
            Pages.Add(loadedRootPage);
          }
        }

        // 5. Success! The project is now "Clean" because it matches the disk
        IsDirty = false;

        // Auto-select the first page if available
        CurrentPage = Pages.FirstOrDefault();
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show($"Failed to load project: {ex.Message}");
      }
    }

    private DocPage LoadPageWithChildren(string fileName, string docsFolder)
    {
      // Construct path to the individual page JSON (e.g., index.json)
      string jsonName = Path.GetFileNameWithoutExtension(fileName) + ".json";
      string fullPath = Path.Combine(docsFolder, jsonName);

      if (!File.Exists(fullPath)) return null;

      // Load the page data
      string pageJson = File.ReadAllText(fullPath);
      var page = System.Text.Json.JsonSerializer.Deserialize<DocPage>(pageJson);

      if (page != null)
      {
        // IMPORTANT: Attach the dirty-tracking observers
        WatchPageSections(page);

        // Recursively load children if they exist
        if (page.Children != null && page.Children.Count > 0)
        {
          // We need to reload children from THEIR sidecar files to get their SECTIONS
          for (int i = 0; i < page.Children.Count; i++)
          {
            var childFileName = page.Children[i].FileName;
            var fullyLoadedChild = LoadPageWithChildren(childFileName, docsFolder);
            if (fullyLoadedChild != null)
            {
              page.Children[i] = fullyLoadedChild;
            }
          }
        }
      }

      return page;
    }
    #endregion

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

        this.IsDirty = false;

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
        WatchPageSections(newPage);
        Pages.Add(newPage);
        CurrentPage = newPage;

        IsDirty = true;
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

      // 3. Permanent HTML deletion
      string htmlPath = Path.Combine(Settings.OutputPath, page.FileName);
      if (File.Exists(htmlPath)) File.Delete(htmlPath);

      // 4. SET DIRTY FLAG
      IsDirty = true;
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

      IsDirty = true;
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

    public void WatchPageSections(DocPage page)
    {
      if (page == null) return;

      // Listen to the collection itself (Adding/Removing sections)
      page.Sections.CollectionChanged += (s, e) =>
      {
        IsDirty = true;
        // If new sections are added, we need to watch their internal Content too
        if (e.NewItems != null)
        {
          foreach (DocSection section in e.NewItems)
          {
            section.PropertyChanged += Section_PropertyChanged;
          }
        }
      };

      // Listen to existing sections
      foreach (var section in page.Sections)
      {
        section.PropertyChanged += Section_PropertyChanged;
      }
    }

    private void Section_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(DocSection.Content))
      {
        IsDirty = true;
      }
    }

    public void UpdateSettings(ProjectSettings newSettings)
    {
      this.Settings = newSettings;
      this.IsDirty = true;

      // The ViewModel CAN call this because it inherits from ViewModelBase
      OnPropertyChanged(nameof(Settings));
    }

  }
}