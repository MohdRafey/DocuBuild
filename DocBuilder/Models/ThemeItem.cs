using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DocBuilder.Models
{
  public class ThemeItem : INotifyPropertyChanged
  {
    public string Name { get; set; }
    public string CssFileName { get; set; }
    public string ScreenshotPath { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected != value)
        {
          _isSelected = value;
          OnPropertyChanged();
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    // The CallerMemberName attribute automatically picks up "IsSelected"
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}