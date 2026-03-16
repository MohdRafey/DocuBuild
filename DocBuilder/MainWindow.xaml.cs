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

    private System.Windows.Point _startPoint;

    private void PageTree_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        System.Windows.Point mousePos = e.GetPosition(null);
        Vector diff = _startPoint - mousePos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
          System.Windows.Controls.TreeView? treeView = sender as System.Windows.Controls.TreeView;
          TreeViewItem treeViewItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);

          if (treeViewItem != null)
          {
            DocPage draggedItem = (DocPage)treeViewItem.Header;
            System.Windows.DataObject dragData = new System.Windows.DataObject("DocPage", draggedItem);
            DragDrop.DoDragDrop(treeViewItem, dragData, System.Windows.DragDropEffects.Move);
          }
        }
      }
    }

    private void PageTree_Drop(object sender, System.Windows.DragEventArgs e)
    {
      if (e.Data.GetDataPresent("DocPage"))
      {
        DocPage draggedPage = e.Data.GetData("DocPage") as DocPage;
        TreeViewItem targetItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);
        DocPage targetPage = targetItem?.Header as DocPage;
        var vm = (MainViewModel)this.DataContext;

        if (draggedPage == null || draggedPage == targetPage) return;

        // --- PRE-PROCESSING: Prevent nesting recursion ---
        // If the dragged page is about to become a child, it cannot keep its own children.
        if (targetPage != null && draggedPage.Children.Count > 0)
        {
          var result = System.Windows.MessageBox.Show(
              "Moving this category into another page will move its sub-pages back to the top level. Continue?",
              "Flatten Hierarchy",
              System.Windows.MessageBoxButton.YesNo);

          if (result == System.Windows.MessageBoxResult.No) return;

          // Evict children to Root before moving the parent
          var childrenToEvict = draggedPage.Children.ToList();
          foreach (var child in childrenToEvict)
          {
            draggedPage.Children.Remove(child);
            child.IsRoot = true;
            vm.Pages.Add(child);
          }
        }

        // --- EXECUTION PHASE ---
        vm.RemovePageFromHierarchy(draggedPage);

        // Scenario A: Move to Root
        if (targetPage == null)
        {
          draggedPage.IsRoot = true;
          vm.Pages.Add(draggedPage);
        }
        else
        {
          // Scenario B: Nest into Target
          draggedPage.IsRoot = false;
          targetPage.Children.Add(draggedPage);
          targetItem.IsExpanded = true;
        }

        e.Handled = true;
      }
    }

    private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
    {
      do
      {
        if (current is T) return (T)current;
        current = System.Windows.Media.VisualTreeHelper.GetParent(current);
      }
      while (current != null);
      return null;
    }

    private void PageTree_QueryContinueDrag(object sender, System.Windows.QueryContinueDragEventArgs e)
    {
      // If the Escape key is pressed, cancel the drag operation
      if (e.EscapePressed)
      {
        e.Action = System.Windows.DragAction.Cancel;
      }
    }

    private void PageTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      var vm = (MainViewModel)this.DataContext;
      if (e.NewValue is DocPage selectedPage)
      {
        vm.CurrentPage = selectedPage;
      }
    }
  }
}