using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DatabaseEditorV3
{
    /// <summary>
    /// Interaction logic for UndoChangesWindow.xaml
    /// </summary>
    public partial class UndoChangesWindow : Window
    {
        public UndoChangesWindow(MainWindowModel context)
        {
            DataContext = context;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            Command command = el.DataContext as Command;
            command.Run();
        }
    }
    
    public class TableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Command command)
            {
                return command.Item.Table.DisplayName;
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is UpdateCommand)
                return "Update";
            else if (value is InsertCommand)
                return "Insert";
            else if (value is DeleteCommand)
                return "Delete";
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is UpdateCommand update)
                return update.oldValue.Column;
            else if (value is Command command)
                return string.Join(", ", command.Item.GetDynamicMemberNames().ToArray());
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NewValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is UpdateCommand update)
                return update.oldValue.Value;
            else if (value is InsertCommand insert)
                return string.Join(", ", insert.Item.GetDynamicMemberNames().Select(x => insert.Item[x]).ToArray());
            else if (value is DeleteCommand delete)
                return "";
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OldValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is UpdateCommand update)
                return update.newValue.Value;
            else if (value is InsertCommand insert)
                return "";
            else if (value is DeleteCommand delete)
                return string.Join(", ", delete.Item.GetDynamicMemberNames().Select(x => delete.Item[x]).ToArray());
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
