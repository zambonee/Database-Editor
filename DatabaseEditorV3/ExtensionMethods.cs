using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

namespace SharedLibrary
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Allows for nullable types for the extension DataRow.field<T?>(DataColumn).
        /// </summary>
        public static T? ToNullable<T>(this string s)
            where T : struct
        {
            Nullable<T> result = new Nullable<T>();
            try
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)conv.ConvertFrom(s);
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Get the first child dependency object of a specific type at any level down the visual tree. Returns null if none are found.
        /// </summary>
        public static T GetChildOfType<T>(this DependencyObject obj)
           where T : DependencyObject
        {
            if (obj == null)
            {
                return null;
            }
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                DependencyObject result = child is T ? child : GetChildOfType<T>(child);
                if (result != null)
                {
                    return result as T;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the first parent dependency object of a type at any level up the visual tree. Returns null if none are found.
        /// </summary>
        public static T GetParentOfType<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            while (obj != null && !(obj is T))
            {
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }
            return obj as T;
        }

        /// <summary>
        /// Get the BindingExpression for a TextBlock.Text, ComboBox.Text, ComboBox.SelectedValue, or ToggleButton.IsChecked property at any level down the visual tree.
        /// </summary>
        public static BindingExpression GetBindingWithin(this DependencyObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            BindingExpression b = null;
            if (obj is FrameworkElement)
            {
                b = (obj as FrameworkElement).GetBindingExpression(TextBlock.TextProperty);
                if (b == null)
                {
                    b = (obj as FrameworkElement).GetBindingExpression(ComboBox.TextProperty);
                }
                if (b == null)
                {
                    b = (obj as FrameworkElement).GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty);
                }
                if (b == null)
                {
                    b = (obj as FrameworkElement).GetBindingExpression(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty);
                }
            }
            if (b != null)
            {
                return b;
            }
            else
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                    b = child.GetBindingWithin();
                    if (b != null)
                    {
                        return b;
                    }
                }
            }
            return null;
        }
    }
}

