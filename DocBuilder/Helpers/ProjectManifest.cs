using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocBuilder.Helpers
{
  public class ProjectManifest
  {
    public string ProjectName { get; set; }
    public List<PageReference> Structure { get; set; }
  }

  public class PageReference
  {
    public string Title { get; set; }
    public string FileName { get; set; }
  }
}
