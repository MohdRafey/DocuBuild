using System.Collections.Generic;
using System.Collections.ObjectModel;
using DocBuilder.Models;

namespace DocBuilder.Services
{
  public static class ThemeRegistry
  {
    private const string PlaceholderPath = "pack://application:,,,/Resources/placeholder.png";
    public static List<ThemeItem> GetAvailableThemes()
    {
      return new List<ThemeItem>
            {
                new ThemeItem { Name = "ModernBlue", ScreenshotPath = PlaceholderPath },
                new ThemeItem { Name = "ModernDark", ScreenshotPath = PlaceholderPath },
                new ThemeItem { Name = "CardBright", ScreenshotPath = PlaceholderPath },
                new ThemeItem { Name = "Midnight", ScreenshotPath = PlaceholderPath },
                new ThemeItem { Name = "Cyberpunk", ScreenshotPath = PlaceholderPath },
                new ThemeItem { Name = "CleanWhite", ScreenshotPath = PlaceholderPath },
                new ThemeItem { Name = "SlateGrey", ScreenshotPath = PlaceholderPath }
            };
    }

    public static ThemeItem GetThemeByName(string themeName)
    {
      var themes = GetAvailableThemes();

      if (string.IsNullOrWhiteSpace(themeName))
        return themes.FirstOrDefault() ?? new ThemeItem();

      // Normalizes "ModernBlue", "Modern Blue", "modern-blue" so dictionary/key mismatch doesn't throw
      string cleanName = themeName.Replace(" ", "").Replace("-", "").ToLowerInvariant();

      var match = themes.FirstOrDefault(t =>
          t.Name.Replace(" ", "").Replace("-", "").ToLowerInvariant() == cleanName);

      return match ?? themes.FirstOrDefault() ?? new ThemeItem();
    }
  }
}