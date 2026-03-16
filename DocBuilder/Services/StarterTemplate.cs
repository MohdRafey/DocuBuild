using System;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using DocBuilder.Models;

namespace DocBuilder.Services
{
  public static class StarterTemplate
  {
    /// <summary>
    /// Generates the initial index.json skeleton for a new project.
    /// </summary>
    public static void CreateGettingStarted(string docsPath)
    {
      // Getting Started is always index.json physically
      string jsonPath = Path.Combine(docsPath, "index.json");

      var welcomePage = new DocPage
      {
        Title = "Getting Started",
        FileName = "index.html",
        IsRoot = true,      // Set explicitly
        Position = 0,       // First item
        Category = "General",
        Sections = new ObservableCollection<DocSection>
                {
                    new DocSection
                    {
                        Type = SectionType.H1,
                        Content = "Getting Started",
                        Metadata = null
                    },
                    new DocSection
                    {
                        Type = SectionType.Paragraph,
                        Content = "Welcome to your new documentation project. This page is designed to help your users get up and running quickly.",
                        Metadata = null
                    },
                    new DocSection
                    {
                        Type = SectionType.H2,
                        Content = "Next Steps",
                        Metadata = null
                    },
                    new DocSection
                    {
                        Type = SectionType.Paragraph,
                        Content = "Use the editor to add new sections, code blocks, and images to this guide.",
                        Metadata = null
                    }
                }
      };

      var options = new JsonSerializerOptions { WriteIndented = true };
      string jsonString = JsonSerializer.Serialize(welcomePage, options);
      File.WriteAllText(jsonPath, jsonString);
    }
  }
}