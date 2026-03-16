using DocBuilder.Models;
using DocBuilder.WPF.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DocBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

    private void RemoveSection_Click(object sender, RoutedEventArgs e)
    {
      // 1. Identify the button that was clicked
      var button = sender as System.Windows.Controls.Button;

      // 2. The DataContext of the button is the DocSection instance
      var sectionToRemove = button?.DataContext as DocSection;

      // 3. Get access to the MainViewModel to reach the collection
      var viewModel = this.DataContext as MainViewModel;

      if (sectionToRemove != null && viewModel?.CurrentPage != null)
      {
        // 4. Remove from the ObservableCollection; the UI will update instantly
        viewModel.CurrentPage.Sections.Remove(sectionToRemove);
      }
    }
    private void MoveSectionUp_Click(object sender, RoutedEventArgs e)
    {
      var section = (sender as System.Windows.Controls.Button)?.DataContext as DocSection;
      var sections = (DataContext as MainViewModel)?.CurrentPage?.Sections;

      if (section != null && sections != null)
      {
        int index = sections.IndexOf(section);
        if (index > 0)
        {
          sections.Move(index, index - 1);
        }
      }
    }

    private void MoveSectionDown_Click(object sender, RoutedEventArgs e)
    {
      var section = (sender as System.Windows.Controls.Button)?.DataContext as DocSection;
      var sections = (DataContext as MainViewModel)?.CurrentPage?.Sections;

      if (section != null && sections != null)
      {
        int index = sections.IndexOf(section);
        if (index < sections.Count - 1)
        {
          sections.Move(index, index + 1);
        }
      }
    }
  }
}