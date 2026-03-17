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
    private string _currentPageFileName;

    public void GenerateProject(IEnumerable<DocPage> pages, ProjectSettings settings)
    {
      if (!Directory.Exists(settings.OutputPath))
        Directory.CreateDirectory(settings.OutputPath);

      _settings = settings;

      // Flat list for easy navigation lookup
      var allPagesList = pages.ToList();

      foreach (var page in allPagesList)
      {
        _currentPageFileName = page.FileName;

        // 1. Build dynamic components for THIS specific page
        string sidebarHtml = BuildSidebarHtml(allPagesList);
        string logoHtml = BuildLogoHtml(settings.LogoPath);
        string contentHtml = BuildBodyHtml(page.Sections);

        // 2. Hydrate the Master Template
        string finalHtml = GetMasterTemplate()
            .Replace("{{BrandName}}", settings.BrandName)
            .Replace("{{LogoHtml}}", logoHtml)
            .Replace("{{Sidebar}}", sidebarHtml)
            .Replace("{{PageTitle}}", page.Title)
            .Replace("{{Content}}", contentHtml);

        // 3. Save to output
        File.WriteAllText(Path.Combine(settings.OutputPath, page.FileName), finalHtml);
      }
    }

    private string BuildLogoHtml(string relativePath)
    {
      if (string.IsNullOrWhiteSpace(relativePath)) return "";
      // Assuming images are moved to an /img folder in output
      return $@"<div class='header-brand'>
                        <img src='..img/{Path.GetFileName(relativePath)}' alt='Logo' />
                      </div>";
    }

    private string BuildSidebarHtml(IEnumerable<DocPage> allPages)
    {
      var sb = new StringBuilder();
      sb.Append("<ul>");

      // Only start from Root pages
      var rootPages = allPages.Where(p => p.IsRoot).OrderBy(p => p.Position);
      foreach (var page in rootPages)
      {
        sb.Append(GenerateNavLine(page));
      }

      sb.Append("</ul>");
      return sb.ToString();
    }

    private string GenerateNavLine(DocPage page)
    {
      var sb = new StringBuilder();
      // Check if this is the active page
      string activeClass = (page.FileName == _currentPageFileName) ? "class='active'" : "";

      sb.Append("<li>");
      sb.Append($"<a href='{page.FileName}' {activeClass}>{page.Title}</a>");

      if (page.Children != null && page.Children.Any())
      {
        sb.Append("<ul class='sub-menu'>");
        foreach (var child in page.Children.OrderBy(c => c.Position))
        {
          sb.Append(GenerateNavLine(child));
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
          case SectionType.H1: sb.Append($"<h2>{s.Content}</h2>"); break;
          case SectionType.H2: sb.Append($"<h3>{s.Content}</h3>"); break;
          case SectionType.Paragraph: sb.Append($"<p>{s.Content}</p>"); break;
          case SectionType.Separator: sb.Append("<hr class='section-sep'/>"); break;

          case SectionType.Warning:
            sb.Append($@"<div class='alert alert-warning'>
                                        <span class='alert-icon'>⚠️</span>
                                        <div class='alert-body'>{s.Content}</div>
                                      </div>");
            break;

          case SectionType.Note:
            sb.Append($@"<div class='alert alert-note'>
                                        <span class='alert-icon'>💡</span>
                                        <div class='alert-body'>{s.Content}</div>
                                      </div>");
            break;

          case SectionType.Bullets:
            var lines = s.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            sb.Append("<ul>");
            foreach (var line in lines)
            {
              string cleanLine = line.Trim().TrimStart('•', '-', '*').Trim();
              sb.Append($"<li>{cleanLine}</li>");
            }
            sb.Append("</ul>");
            break;

          case SectionType.Code:
            sb.Append($@"<div class='code-container'>
                                        <div class='code-header'><span>Source Code</span></div>
                                        <pre><code>{System.Net.WebUtility.HtmlEncode(s.Content)}</code></pre>
                                      </div>");
            break;

          case SectionType.Image:
            sb.Append($@"<figure>
                                        <img src='../img/{Path.GetFileName(s.Content)}' class='content-img' />
                                     </figure>");
            break;
        }
      }
      return sb.ToString();
    }

    private string GetMasterTemplate()
    {
      // Note: Year is generated at runtime
      return @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{{PageTitle}} - {{BrandName}}</title>
    <link rel='stylesheet' href='Themes/active-theme.css'>
</head>
<body>
    <header class='doc-header'>
        <div class='header-content'>
            {{LogoHtml}}
            <h1>{{BrandName}}</h1>
        </div>
        <nav class='top-right-nav'>
            <a href='index.html' class='home-btn'>🏠 Home</a>
        </nav>
    </header>

    <div class='main-wrapper'>
        <aside class='sidebar'>
            <div class='sidebar-content'>
                <h3>Contents</h3>
                {{Sidebar}}
            </div>
        </aside>

        <main class='doc-container'>
            <nav class='breadcrumb'>
                <a href='index.html'>Home</a> <span>/</span> {{PageTitle}}
            </nav>
            
            <article class='doc-card'>
                <h1 class='doc-title'>{{PageTitle}}</h1>
                <div class='doc-content'>
                    {{Content}}
                </div>
            </article>

            <footer>
                &copy; " + DateTime.Now.Year + @" {{BrandName}}. Generated with DocBuilder.
            </footer>
        </main>
    </div>
</body>
</html>";
    }
  }
}