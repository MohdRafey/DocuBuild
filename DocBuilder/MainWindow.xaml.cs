using DocBuilder.Models;
using DocBuilder.Views;
using DocBuilder.WPF.ViewModels;
using System.Collections.ObjectModel;
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
using Clipboard = System.Windows.Clipboard;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using RichTextBox = System.Windows.Controls.RichTextBox;

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

    private void PageTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _startPoint = e.GetPosition(null);
    }

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

    private void PageTree_Drop(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent("DocPage"))
      {
        DocPage? draggedPage = e.Data.GetData("DocPage") as DocPage;
        TreeViewItem targetItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);
        DocPage? targetPage = targetItem?.Header as DocPage;
        var vm = (MainViewModel)this.DataContext;

        if (draggedPage == null || draggedPage == targetPage) return;

        // --- PRE-PROCESSING: Prevent nesting recursion ---
        // If the dragged page is about to become a child, it cannot keep its own children.
        if (targetPage != null && draggedPage.Children.Count > 0)
        {
          var result = MessageBox.Show(
              "Moving this category into another page will move its sub-pages back to the top level. Continue?",
              "Flatten Hierarchy",
              MessageBoxButton.YesNo);

          if (result == MessageBoxResult.No) return;

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

    private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
      DependencyObject parentObject = VisualTreeHelper.GetParent(child);

      if (parentObject == null) return null;

      if (parentObject is T parent)
        return parent;
      else
        return FindVisualParent<T>(parentObject);
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

    private void ToggleFormat_Click(object sender, RoutedEventArgs e)
    {
      // Find the toolbar in the current template and toggle Visibility
      var btn = sender as System.Windows.Controls.Button;
      var grid = FindVisualParent<Grid>(btn);
      var toolbar = (Border)grid.FindName("FormatToolbar");
      toolbar.Visibility = toolbar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ToggleHighlight_Click(object sender, RoutedEventArgs e)
    {
      var rtb = FindVisualParent<Grid>((System.Windows.Controls.Button)sender).Children.OfType<RichTextBox>().First();
      var selection = rtb.Selection;

      if (!selection.IsEmpty)
      {
        // Toggle yellow background
        var currentBg = selection.GetPropertyValue(TextElement.BackgroundProperty);
        selection.ApplyPropertyValue(TextElement.BackgroundProperty,
            currentBg == System.Windows.Media.Brushes.Yellow ?
            System.Windows.Media.Brushes.Transparent :
            System.Windows.Media.Brushes.Yellow);
      }
    }

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
      var rtb = FindVisualParent<Grid>((System.Windows.Controls.Button)sender).Children.OfType<RichTextBox>().First();
      var selection = rtb.Selection;

      if (selection.IsEmpty)
      {
        MessageBox.Show("Please select some text first to turn it into a link.");
        return;
      }

      // A simple way to get input without a whole new window
      string url = Microsoft.VisualBasic.Interaction.InputBox("Enter the URL:", "Insert Hyperlink", "https://");

      if (!string.IsNullOrEmpty(url))
      {
        // Simple check to add https if the user forgot it
        if (!url.StartsWith("http")) url = "https://" + url;

        var link = new Hyperlink(selection.Start, selection.End);
        try
        {
          link.NavigateUri = new Uri(url);
        }
        catch
        {
          MessageBox.Show("Invalid URL. Please try again.");
        }
      }
    }

    private void ToggleBold_Click(object sender, RoutedEventArgs e)
    {
      var rtb = FindVisualParent<Grid>((System.Windows.Controls.Button)sender).Children.OfType<RichTextBox>().First();
      EditingCommands.ToggleBold.Execute(null, rtb);
    }

    private void ToggleItalic_Click(object sender, RoutedEventArgs e)
    {
      var rtb = FindVisualParent<Grid>((System.Windows.Controls.Button)sender).Children.OfType<RichTextBox>().First();
      EditingCommands.ToggleItalic.Execute(null, rtb);
    }

    private void ToggleUnderline_Click(object sender, RoutedEventArgs e)
    {
      var rtb = FindVisualParent<Grid>((System.Windows.Controls.Button)sender).Children.OfType<RichTextBox>().First();
      EditingCommands.ToggleUnderline.Execute(null, rtb);
    }

    private void PasteAsPlainText_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      if (Clipboard.ContainsText())
      {
        var rtb = sender as RichTextBox;
        rtb.BeginChange();
        rtb.Selection.Text = Clipboard.GetText(); // Inserts as raw text, no HTML/RTF mess
        rtb.EndChange();
      }
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
      var vm = (MainViewModel)this.DataContext;

      // 1. THE DIRTY CHECK (Safety First)
      if (vm.IsDirty)
      {
        var result = MessageBox.Show(
            $"You have unsaved changes in '{vm.Settings?.BrandName ?? "Current Project"}'.\n\nWould you like to Publish (Save) before switching?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
          vm.PublishCommand.Execute(null);
          // If publishing failed or was cancelled, you might want to return here.
        }
        else if (result == MessageBoxResult.Cancel)
        {
          return; // User changed their mind, stay in current project
        }
      }

      // 2. LAUNCH THE SETUP WIZARD
      var setup = new SetupWindow();
      setup.Owner = this;

      if (setup.ShowDialog() == true)
      {
        // 3. APPLY NEW SETTINGS
        vm.Settings = setup.ResultSettings;

        if (setup.ResultSettings.IsExistingProject)
        {
          // Path to the navigation.json chosen in the OpenFileDialog
          string manifestPath = setup.ResultSettings.ManifestPath;
          vm.LoadProject(manifestPath);
        }
        else
        {
          // START A FRESH PROJECT
          InitializeNewProject(vm, setup.ResultSettings);
        }
      }
    }

    private void InitializeNewProject(MainViewModel vm, ProjectSettings settings)
    {
      // Clear old state
      vm.Pages.Clear();
      vm.DeletedPages.Clear();

      // Create a default "Getting Started" page
      var homePage = new DocPage
      {
        Title = "Getting Started",
        FileName = "index.html",
        IsRoot = true,
        Sections = new ObservableCollection<DocSection>
        {
            new DocSection { Type = SectionType.H1, Content = settings.BrandName },
            new DocSection { Type = SectionType.Paragraph, Content = "Welcome to your new documentation project." }
        }
      };

      // Start watching and add to collection
      vm.WatchPageSections(homePage);
      vm.Pages.Add(homePage);
      vm.CurrentPage = homePage;

      // A brand new project is "Dirty" until it's published for the first time
      vm.IsDirty = true;
    }

    private void ProjectSettings_Click(object sender, RoutedEventArgs e)
    {
      var vm = (MainViewModel)this.DataContext;
      var settingsWindow = new ProjectSettingsWindow(vm.Settings);
      settingsWindow.Owner = this;

      if (settingsWindow.ShowDialog() == true)
      {
        // Call a method IN the ViewModel instead of firing PropertyChanged from here
        vm.UpdateSettings(settingsWindow.UpdatedSettings);

        StatusTextBlock.Text = "Settings updated successfully.";
      }
    }

    //Update to a dedicated page with details.
    private void About_Click(object sender, RoutedEventArgs e)
    {
      string version = "1.0.0";
      string developer = "Your Name/Company";

      MessageBox.Show(
          $"DocBuilder v{version}\n\n" +
          $"A professional documentation generator for .NET developers.\n" +
          $"Developed by {developer}",
          "About DocBuilder",
          MessageBoxButton.OK,
          MessageBoxImage.Information);
    }
  }
}