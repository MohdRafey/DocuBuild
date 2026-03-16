using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using DocBuilder.Models;

namespace DocBuilder.WPF.Services
{
  public class HtmlGenerator
  {
    public void GenerateProject(IEnumerable<DocPage> pages, ProjectSettings settings)
    {
      // Ensure directory exists
      if (!Directory.Exists(settings.OutputPath))
        Directory.CreateDirectory(settings.OutputPath);

      foreach (var p in pages)
      {
        p.FileName = SanitizeFilename(p.Title);
      }

      // Create shared navigation HTML
      string sidebarHtml = BuildSidebarHtml(pages);

      foreach (var page in pages)
      {
        string contentHtml = BuildBodyHtml(page.Sections);

        string finalHtml = GetMasterTemplate()
            .Replace("{{PrimaryColor}}", settings.PrimaryColor)
            .Replace("{{SecondaryColor}}", settings.SecondaryColor)
            .Replace("{{BrandName}}", settings.BrandName)
            .Replace("{{Sidebar}}", sidebarHtml)
            .Replace("{{PageTitle}}", page.Title)
            .Replace("{{Content}}", contentHtml)
            .Replace("{{Footer}}", settings.FooterText);

        File.WriteAllText(Path.Combine(settings.OutputPath, page.FileName), finalHtml);
      }
    }

    private string SanitizeFilename(string title)
    {
      if (string.IsNullOrWhiteSpace(title)) return "page.html";

      // Remove invalid chars, replace spaces with hyphens, lowercase it
      var invalidChars = Path.GetInvalidFileNameChars();
      string clean = new string(title.Where(c => !invalidChars.Contains(c)).ToArray());

      return clean.Replace(" ", "-").ToLower().Trim() + ".html";
    }

    private string BuildSidebarHtml(IEnumerable<DocPage> pages)
    {
      var sb = new StringBuilder();
      sb.Append("<nav>");
      foreach (var p in pages)
      {
        sb.Append($"<a href='{p.FileName}'>{p.Title}</a>");
      }
      sb.Append("</nav>");
      return sb.ToString();
    }

    private string BuildBodyHtml(IEnumerable<DocSection> sections)
    {
      var sb = new StringBuilder();
      foreach (var s in sections)
      {
        switch (s.Type)
        {
          case SectionType.H1: sb.Append($"<h1>{s.Content}</h1>"); break;
          case SectionType.H2: sb.Append($"<h2>{s.Content}</h2>"); break;
          case SectionType.H3: sb.Append($"<h3>{s.Content}</h3>"); break;
          case SectionType.Paragraph: sb.Append($"<p>{s.Content}</p>"); break;
          case SectionType.Code:
            sb.Append($"<pre><code>{System.Net.WebUtility.HtmlEncode(s.Content)}</code></pre>");
            break;
          case SectionType.Image:
            sb.Append($"<img src='{s.Content}' style='max-width:100%; height:auto;' />");
            break;
        }
      }
      return sb.ToString();
    }

    private string GetMasterTemplate()
    {
      return @"<!DOCTYPE html>
<html>
<head>
    <title>{{PageTitle}}</title>
    <style>
        :root { --primary: {{PrimaryColor}}; --secondary: {{SecondaryColor}}; }
        body { font-family: 'Segoe UI', Tahoma, sans-serif; display: flex; margin: 0; color: #333; }
        .sidebar { width: 260px; background: #f8f9fa; height: 100vh; border-right: 1px solid #dee2e6; padding: 20px; position: fixed; }
        .sidebar h2 { color: var(--primary); font-size: 1.2rem; margin-bottom: 20px; }
        .sidebar nav a { display: block; padding: 10px; color: #4a5568; text-decoration: none; border-radius: 5px; margin-bottom: 5px; }
        .sidebar nav a:hover { background: #edf2f7; color: var(--primary); }
        .main-content { margin-left: 300px; padding: 50px; flex: 1; max-width: 900px; }
        h1 { font-size: 2.5rem; color: #1a202c; border-bottom: 2px solid var(--primary); padding-bottom: 10px; }
        h2 { font-size: 1.8rem; margin-top: 30px; color: #2d3748; }
        h3 { font-size: 1.4rem; margin-top: 20px; color: #4a5568; }
        p { line-height: 1.6; font-size: 1.1rem; }
        pre { background: #2d3748; color: #fff; padding: 20px; border-radius: 8px; overflow-x: auto; }
    </style>
</head>
<body>
    <div class='sidebar'>
        <h2>{{BrandName}}</h2>
        {{Sidebar}}
    </div>
    <div class='main-content'>
        {{Content}}
    </div>
</body>
</html>";
    }
  }
}