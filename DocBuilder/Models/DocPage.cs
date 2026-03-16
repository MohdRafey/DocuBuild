using DocBuilder.WPF.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DocBuilder.Models
{
  public class DocPage: ViewModelBase
  {
    public string Title { get; set; } = "New Page";
    public string FileName { get; set; } = "page.html";
    public string Category { get; set; } = "General";
    public int Position { get; set; }
    public ObservableCollection<DocSection> Sections { get; set; } = new ObservableCollection<DocSection>();

    [JsonIgnore] // This prevents children from being nested inside the parent's JSON file
    public ObservableCollection<DocPage> Children { get; set; } = new ObservableCollection<DocPage>();
    private bool _isRoot = true; // Default to true for new pages
    public bool IsRoot
    {
      get => _isRoot;
      set { _isRoot = value; OnPropertyChanged(); }
    }
  }
}