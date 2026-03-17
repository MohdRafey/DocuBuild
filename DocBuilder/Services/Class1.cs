using System.Collections.Generic;
using System.Collections.ObjectModel;
using DocBuilder.Models;

namespace DocBuilder.Services
{
  public static class ThemeRegistry
  {
    private const string PlaceholderPath = "pack://application:,,,/Resources/placeholder.png";

    /// <summary>
    /// Returns the master list of available themes.
    /// </summary>
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
  }
}