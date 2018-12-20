using System.Windows;
using System.Windows.Data;

namespace DatabaseEditorV3
{
    /// <summary>
    /// User controls to adjust the "filter" properties of the main data model.
    /// </summary>
    public partial class FilterWindow : Window
    {
        MainWindowModel context;

        public FilterWindow(MainWindowModel context)
        {
            this.context = context;
            DataContext = this.context;
            InitializeComponent();
        }

        private void ButtonAddCondition_Click(object sender, RoutedEventArgs e)
        {
            context.FilterConditions.Add(new FilterCondition());
            context.HasUnsavedFilter = true;
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            context.FilterConditions.Remove((sender as FrameworkElement).DataContext as FilterCondition);
            context.HasUnsavedFilter = true;
        }

        private void ButtonHide_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            context.FilterConditions.Clear();
            context.AdvancedFilterString = string.Empty;
            context.IsFiltered = false;
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            context.IsFiltered = true;
        }
        
        private void Window_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            context.HasUnsavedFilter = true;
        }
    }
}
