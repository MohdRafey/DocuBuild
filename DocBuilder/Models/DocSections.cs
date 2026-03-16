using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocBuilder.Models
{
  public enum SectionType
  {
    H1,
    H2,
    H3,
    Paragraph,
    Code,
    Image,
    Warning,
    Note,
    Separator,
    Tags,
    Bullets
  }

  public class DocSection : INotifyPropertyChanged
  {
    private SectionType _type;
    private string _content;
    private string _metadata;

    public SectionType Type
    {
      get => _type;
      set { _type = value; OnPropertyChanged(); }
    }

    public string Content
    {
      get => _content;
      set { _content = value; OnPropertyChanged(); }
    }

    public string Metadata
    {
      get => _metadata;
      set { _metadata = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}