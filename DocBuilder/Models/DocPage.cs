using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DocBuilder.Models
{
  public class DocPage
  {
    public string Title { get; set; } = "New Page";
    public string FileName { get; set; } = "page.html";
    public string Category { get; set; } = "General";
    public ObservableCollection<DocSection> Sections { get; set; } = new ObservableCollection<DocSection>();
  }
}