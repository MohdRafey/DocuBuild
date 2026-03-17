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
    public ObservableCollection<ThemeItem> Themes { get; set; }
    private ThemeItem _tempSelectedTheme;

    public SetupWindow()
    {
      InitializeComponent();
      LoadThemes();
      DataContext = this;
    }
    //private void LoadThemes()
    //{
    //  // Initialize with your 9 themes. 
    //  // Ensure these images exist in your /Resources/Themes/ folder
    //  Themes = new ObservableCollection<ThemeItem>
    //        {
    //            new ThemeItem { Name = "Modern Blue", CssFileName = "modern-blue.css", ScreenshotPath = "/Resources/Themes/modern-blue.png", IsSelected = true },
    //            new ThemeItem { Name = "Dark Forest", CssFileName = "dark-forest.css", ScreenshotPath = "/Resources/Themes/dark-forest.png" },
    //            new ThemeItem { Name = "Midnight", CssFileName = "midnight.css", ScreenshotPath = "/Resources/Themes/midnight.png" },
    //            new ThemeItem { Name = "Cyberpunk", CssFileName = "cyberpunk.css", ScreenshotPath = "/Resources/Themes/cyberpunk.png" },
    //            new ThemeItem { Name = "Clean White", CssFileName = "clean-white.css", ScreenshotPath = "/Resources/Themes/clean-white.png" },
    //            new ThemeItem { Name = "Slate Grey", CssFileName = "slate-grey.css", ScreenshotPath = "/Resources/Themes/slate-grey.png" },
    //            new ThemeItem { Name = "Crimson", CssFileName = "crimson.css", ScreenshotPath = "/Resources/Themes/crimson.png" },
    //            new ThemeItem { Name = "Solarized", CssFileName = "solarized.css", ScreenshotPath = "/Resources/Themes/solarized.png" },
    //            new ThemeItem { Name = "High Contrast", CssFileName = "high-contrast.css", ScreenshotPath = "/Resources/Themes/high-contrast.png" }
    //        };

    //  ThemeItemsControl.ItemsSource = Themes;
    //  UpdateThemeLabel();
    //}
    private void LoadThemes()
    {
      // Get the master list
      var themeList = DocBuilder.Services.ThemeRegistry.GetAvailableThemes();

      // Set the default selection for a NEW project
      if (themeList.Count > 0)
        themeList[0].IsSelected = true;

      Themes = new ObservableCollection<ThemeItem>(themeList);
      ThemeItemsControl.ItemsSource = Themes;
      UpdateThemeLabel();
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

    private void ShowThemeGallery_Click(object sender, RoutedEventArgs e)
    {
      // Save current selection in case they cancel
      _tempSelectedTheme = Themes.FirstOrDefault(t => t.IsSelected);
      ThemeGalleryView.Visibility = Visibility.Visible;
    }

    private void ConfirmThemeSelection_Click(object sender, RoutedEventArgs e)
    {
      UpdateThemeLabel();
      ThemeGalleryView.Visibility = Visibility.Collapsed;
    }

    private void UpdateThemeLabel()
    {
      var selected = Themes.FirstOrDefault(t => t.IsSelected);
      if (selected != null)
      {
        TxtSelectedThemeName.Text = $"(Current: {selected.Name})";
      }
    }

    private void CancelThemeSelection_Click(object sender, RoutedEventArgs e)
    {
      // Revert to the theme that was selected before opening the gallery
      foreach (var t in Themes) t.IsSelected = (t == _tempSelectedTheme);
      ThemeGalleryView.Visibility = Visibility.Collapsed;
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

      // 1. Prepare Paths
      string rootPath = TxtDestination.Text;
      string docsPath = Path.Combine(rootPath, "Docs");
      string imgPath = Path.Combine(rootPath, "img");

      // Create directories
      if (!Directory.Exists(docsPath)) Directory.CreateDirectory(docsPath);
      if (!Directory.Exists(imgPath)) Directory.CreateDirectory(imgPath);

      // 2. Gather Data for Manifest and ResultSettings
      var selectedTheme = Themes.FirstOrDefault(t => t.IsSelected);
      string relativeLogoPath = "";

      // Copy the logo if it exists
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

      // 3. Handle Template Skeleton
      if (ChkUseTemplate.IsChecked == true)
      {
        DocBuilder.Services.StarterTemplate.CreateGettingStarted(docsPath);
      }

      // 4. Save Master Manifest (navigation.json)
      // We add ThemeName here too so it's remembered when the project is re-opened!
      var manifest = new
      {
        ProjectName = TxtProjectName.Text,
        LogoPath = relativeLogoPath,
        ThemeName = selectedTheme?.Name ?? "ModernBlue",
        Created = DateTime.Now,
        PageFiles = ChkUseTemplate.IsChecked == true ? new[] { "index.html" } : new string[] { }
      };

      File.WriteAllText(Path.Combine(rootPath, "navigation.json"),
          JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

      // 5. FINAL ResultSettings Initialization (The Single Source of Truth)
      ResultSettings = new ProjectSettings
      {
        IsExistingProject = false,
        BrandName = TxtProjectName.Text,
        LogoPath = relativeLogoPath,
        OutputPath = docsPath,
        HomeFileName = "index.html",
        ThemeName = selectedTheme?.Name ?? "ModernBlue" // Defaults to ModernBlue if null
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