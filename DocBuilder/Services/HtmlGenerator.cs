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
    private ProjectSettings _settings;
    public void GenerateProject(IEnumerable<DocPage> pages, ProjectSettings settings)
    {
      if (!Directory.Exists(settings.OutputPath))
        Directory.CreateDirectory(settings.OutputPath);

      _settings = settings;
      // Create shared navigation HTML
      string sidebarHtml = BuildSidebarHtml(pages);

      // Build Logo HTML snippet
      string logoHtml = BuildLogoHtml(settings.LogoPath);

      foreach (var page in pages)
      {
        string contentHtml = BuildBodyHtml(page.Sections);

        string finalHtml = GetMasterTemplate()
            .Replace("{{PrimaryColor}}", settings.PrimaryColor ?? "#667eea")
            .Replace("{{SecondaryColor}}", settings.SecondaryColor ?? "#718096")
            .Replace("{{BrandName}}", settings.BrandName)
            .Replace("{{LogoHtml}}", logoHtml) // New placeholder
            .Replace("{{Sidebar}}", sidebarHtml)
            .Replace("{{PageTitle}}", page.Title)
            .Replace("{{Content}}", contentHtml)
            .Replace("{{Footer}}", settings.FooterText ?? "");

        File.WriteAllText(Path.Combine(settings.OutputPath, page.FileName), finalHtml);
      }
    }

    private string BuildLogoHtml(string relativePath)
    {
      if (string.IsNullOrWhiteSpace(relativePath)) return "";

      // Since HTML is in /Docs and Logo is in /img, we use ../ to go up one level
      string webPath = Path.Combine("..", relativePath).Replace("\\", "/");

      return $@"<div class='brand-logo'>
                        <img src='{webPath}' alt='Logo' />
                      </div>";
    }

    private string BuildSidebarHtml(IEnumerable<DocPage> allPages)
    {
      var sb = new StringBuilder();
      sb.Append("<div class='sidebar'>");
      sb.Append($"<h2>{_settings.BrandName}</h2>");
      sb.Append("<ul class='nav-menu'>");

      // ONLY loop through Root pages at the top level
      var rootPages = allPages.Where(p => p.IsRoot).OrderBy(p => p.Position);

      foreach (var page in rootPages)
      {
        sb.Append(GenerateNavLine(page));
      }

      sb.Append("</ul>");
      sb.Append("</div>");
      return sb.ToString();
    }

    private string GenerateNavLine(DocPage page)
    {
      var sb = new StringBuilder();
      sb.Append("<li>");
      sb.Append($"<a href='{page.FileName}'>{page.Title}</a>");

      // If this page has children, nest them inside this <li>
      if (page.Children != null && page.Children.Any())
      {
        sb.Append("<ul class='sub-menu'>");
        foreach (var child in page.Children.OrderBy(c => c.Position))
        {
          sb.Append(GenerateNavLine(child)); // Recursive call
        }
        sb.Append("</ul>");
      }

      sb.Append("</li>");
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
          case SectionType.Separator: sb.Append("<hr class='section-divider'/>"); break;

          case SectionType.Warning:
            sb.Append($"<div class='alert alert-warning'><span class='icon'>⚠️</span> {s.Content}</div>");
            break;

          case SectionType.Note:
            sb.Append($"<div class='alert alert-note'><span class='icon'>💡</span> {s.Content}</div>");
            break;

          case SectionType.Tags:
            var tags = s.Content.Split(',').Select(t => t.Trim());
            sb.Append("<div class='tag-container'>");
            foreach (var tag in tags) sb.Append($"<span class='tag'>#{tag}</span>");
            sb.Append("</div>");
            break;

          case SectionType.Bullets:
            var lines = s.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length > 0)
            {
              sb.Append($"<p class='list-desc'>{lines[0]}</p>");
              sb.Append("<ul>");
              foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
              {
                // Remove the dot/bullet if the user typed one manually
                string cleanLine = line.TrimStart(' ', '•', '-', '*');
                sb.Append($"<li>{cleanLine}</li>");
              }
              sb.Append("</ul>");
            }
            break;

          case SectionType.Code:
            sb.Append($"<pre><code>{System.Net.WebUtility.HtmlEncode(s.Content)}</code></pre>");
            break;

          case SectionType.Image:
            sb.Append($"<img src='{s.Content}' class='content-img' />");
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
        body { font-family: 'Segoe UI', Tahoma, sans-serif; display: flex; margin: 0; color: #333; background: #fff; }
        .sidebar { width: 280px; background: #f8f9fa; height: 100vh; border-right: 1px solid #dee2e6; padding: 25px; position: fixed; display: flex; flex-direction: column; }
        
        /* Logo Styling */
        .brand-logo { margin-bottom: 20px; text-align: center; }
        .brand-logo img { max-width: 120px; height: auto; border-radius: 8px; }
        
        .sidebar h2 { color: var(--primary); font-size: 1.4rem; margin-bottom: 25px; text-align: center; }
        .sidebar nav { flex: 1; }
        .sidebar nav a { display: block; padding: 12px; color: #4a5568; text-decoration: none; border-radius: 6px; margin-bottom: 8px; font-weight: 500; transition: 0.2s; }
        .sidebar nav a:hover { background: #edf2f7; color: var(--primary); transform: translateX(5px); }
        
        .main-content { margin-left: 330px; padding: 60px; flex: 1; max-width: 900px; }
        h1 { font-size: 2.5rem; color: #1a202c; border-bottom: 3px solid var(--primary); padding-bottom: 15px; margin-bottom: 30px; }
        h2 { font-size: 1.8rem; margin-top: 40px; color: #2d3748; }
        p { line-height: 1.7; font-size: 1.1rem; color: #4a5568; }
        .alert { padding: 15px 20px; border-radius: 8px; margin: 20px 0; border-left: 5px solid; display: flex; align-items: center; }
        .alert-warning { background: #fff5f5; border-color: #feb2b2; color: #c53030; }
        .alert-note { background: #fffaf0; border-color: #f6e05e; color: #b7791f; }
        .alert .icon { margin-right: 15px; font-size: 1.2rem; }
        
        .tag-container { margin: 10px 0 25px 0; }
        .tag { background: #edf2f7; color: var(--primary); padding: 4px 12px; border-radius: 20px; font-size: 0.85rem; margin-right: 8px; font-weight: 600; }
        
        .section-divider { border: 0; height: 1px; background: #e2e8f0; margin: 40px 0; }
        
        ul { line-height: 1.7; color: #4a5568; margin-bottom: 20px; }
        .list-desc { font-weight: 600; margin-bottom: 8px; color: #2d3748; }
        
        .content-img { max-width: 100%; height: auto; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); margin: 20px 0; }
        pre { background: #1a202c; color: #e2e8f0; padding: 20px; border-radius: 10px; overflow-x: auto; font-family: 'Consolas', monospace; font-size: 0.95rem; }
    </style>
</head>
<body>
    <div class='sidebar'>
        {{LogoHtml}}
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