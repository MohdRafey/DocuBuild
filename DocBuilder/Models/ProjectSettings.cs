using System;

namespace DocBuilder.Models
{
  public class ProjectSettings
  {
    // Branding & Visuals
    public string BrandName { get; set; } = "My Docs";
    public string PrimaryColor { get; set; } = "#667eea";
    public string SecondaryColor { get; set; } = "#764ba2";
    public string FooterText { get; set; } = "© 2026 Internal Docs";

    // Paths & Logic
    public string OutputPath { get; set; } = @"D:\DocBuilder\Docs";

    /// <summary>
    /// Defines the entry point of the documentation (e.g., index.html or home.html)
    /// </summary>
    public string HomeFileName { get; set; } = "index.html";

    /// <summary>
    /// Flags if the user is working on a newly created folder or an imported one
    /// </summary>
    public bool IsExistingProject { get; set; }

    /// <summary>
    /// Metadata to show in the UI status bar
    /// </summary>
    public DateTime? LastPublished { get; set; }

    // Optional: Add a project name if you want to support multiple saved projects
    public string ProjectName { get; set; } = "New Documentation Project";

    public string LogoPath { get; set; }
  }
}