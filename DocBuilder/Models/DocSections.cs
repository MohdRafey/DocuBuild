using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DocBuilder.Models
{
  public enum SectionType { H1, H2, H3, Paragraph, Code, Image, List }

  public class DocSection
  {
    public SectionType Type { get; set; }
    public string Content { get; set; }
    public string Metadata { get; set; } // e.g. "csharp" for code, "alt text" for images
  }
}