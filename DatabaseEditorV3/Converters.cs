using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows;
using System.Windows.Markup;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;

namespace SharedLibrary
{
    /// <summary>
    /// Convert a bool to any value. Set the TrueValue, FalseValue, and NullValue inline in the XAML. 
    /// The ConvertBack references a list of commonly used representations of true and false.
    /// </summary>
    public class BoolConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }
        public object NullValue { get; set; }

        private static string[] allTrues = new string[] { "YES", "Y", "TRUE", "1" };
        private static string[] allFalses = new string[] { "NO", "N", "FALSE", "0" };
        private static string[] allNulls = new string[] { "UNKNOWN", "U", "", "NULL" };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool?)
            {
                if ((bool?)value == true)
                {
                    return TrueValue;
                }
                else if ((bool?)value == false)
                {
                    return FalseValue;
                }
                else
                {
                    return NullValue;
                }
            }
            else
            {
                return NullValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == TrueValue)
            {
                return true;
            }
            else if (value == FalseValue)
            {
                return false;
            }
            else if (value == NullValue)
            {
                return null;
            }
            else if (allTrues.Contains(value.ToString().ToUpper()))
            {
                return true;
            }
            else if (allFalses.Contains(value.ToString().ToUpper()))
            {
                return false;
            }
            else if (allNulls.Contains(value.ToString().ToUpper()))
            {
                return null;
            }
            else
            {
                return value;
            }
        }

    }

    /// <summary>
    /// BoolConverter TrueValue = 'False' FalseValue = 'True' does not work in XAML. So, use this.
    /// </summary>
    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                return !(bool)value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                return !(bool)value;
            }
        }
    }

    /// <summary>
    /// Converts any value to a bool base on TrueValue, FalseValue, and NullValue you set inline in the XAML. Easier to read than using DataTriggers for Button.Content.
    /// </summary>
    public class ToBoolConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }
        public object NullValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (TrueValue.Equals(value))
            {
                return true;
            }
            else if (FalseValue.Equals(value))
            {
                return false;
            }
            else if (NullValue.Equals(value))
            {
                return null;
            }
            else
            {
                return new Exception("The value cannot be converted by ToBoolConverter. Please check the set attributes.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool?)
            {
                bool? b = (bool?)value;
                if (b == true)
                {
                    return TrueValue;
                }
                else if (b == false)
                {
                    return FalseValue;
                }
                else
                {
                    return NullValue;
                }
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A lot of my users like inputing time as "HHMM", but need to see the time value as "HH:MM".
    /// </summary>
    public class LazyTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == DBNull.Value || value == null || value.Equals(""))
            {
                return null;
            }
            else
            {
                string str = value.ToString();
                if (System.Text.RegularExpressions.Regex.IsMatch(str, "^([0-1][0-9]|2[0-3]):?[0-5][0-9]$"))
                {
                    string s1 = str.Substring(0, 2);
                    string s2 = str.Substring(str.Length - 2, 2);
                    return s1 + ":" + s2;
                }
                else
                {
                    return value;
                }
            }
        }
    }

    /// <summary>
    /// If the Converter parameter equals the Converter value, returns true, else returns false.
    /// </summary>
    public class BoolToParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value.Equals(parameter))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return parameter;
        }
    }

    /// <summary>
    /// Converts a path to display just the file name.
    /// </summary>
    public class PathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.IO.Path.GetFileName(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    /// <summary>
    /// Masks a substring at the beginning of the string, and adds that prefix if it does not begin with it.
    /// </summary>
    public class MaskPrefixConverter : DependencyObject, IValueConverter
    {
        public string Prefix { get; set; }
        public int Padding { get; set; } = 0;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value == DBNull.Value || string.IsNullOrEmpty(value.ToString()))
            {
                return value;
            }
            string result = (string)value;
            if (!string.IsNullOrEmpty(Prefix) && result.Length > Prefix.Length)
            {
                if (result.Substring(0, Prefix.Length).ToUpper() == Prefix.ToUpper())
                {
                    result = result.Substring(Prefix.Length);
                }
            }
            return result.Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value == DBNull.Value || string.IsNullOrEmpty(value.ToString()))
            {
                return value;
            }
            string result = (string)value;
            int number;
            if (!string.IsNullOrEmpty(Prefix))
            {
                if (int.TryParse(result, out number))
                {
                    result = number.ToString().PadLeft(Padding, '0');
                }
                result = Prefix + result;
            }
            return result;
        }

    }

    /// <summary>
    /// Convert not a DisplayValuePair to null.
    /// </summary>
    public class NullableColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DatabaseEditorV3.DisplayValuePair)
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    //Makes binding dependency objects in separate Visual Trees.

    public class DataResource : Freezable
    {
        /// <summary>
        /// Identifies the <see cref="BindingTarget"/> dependency property.
        /// </summary>
        /// <value>
        /// The identifier for the <see cref="BindingTarget"/> dependency property.
        /// </value>
        public static readonly DependencyProperty BindingTargetProperty = DependencyProperty.Register("BindingTarget", typeof(object), typeof(DataResource), new UIPropertyMetadata(null));

        /// <summary>
        /// Initializes a new instance of the <see cref="DataResource"/> class.
        /// </summary>
        public DataResource()
        {
        }

        /// <summary>
        /// Gets or sets the binding target.
        /// </summary>
        /// <value>The binding target.</value>
        public object BindingTarget
        {
            get { return (object)GetValue(BindingTargetProperty); }
            set { SetValue(BindingTargetProperty, value); }
        }

        /// <summary>
        /// Creates an instance of the specified type using that type's default constructor. 
        /// </summary>
        /// <returns>
        /// A reference to the newly created object.
        /// </returns>
        protected override Freezable CreateInstanceCore()
        {
            return (Freezable)Activator.CreateInstance(GetType());
        }

        /// <summary>
        /// Makes the instance a clone (deep copy) of the specified <see cref="Freezable"/>
        /// using base (non-animated) property values. 
        /// </summary>
        /// <param name="sourceFreezable">
        /// The object to clone.
        /// </param>
        protected sealed override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
        }
    }

    public class DataResourceBindingExtension : MarkupExtension
    {
        private object mTargetObject;
        private object mTargetProperty;
        private DataResource mDataResouce;

        /// <summary>
        /// Gets or sets the data resource.
        /// </summary>
        /// <value>The data resource.</value>
        public DataResource DataResource
        {
            get
            {
                return mDataResouce;
            }
            set
            {
                if (mDataResouce != value)
                {
                    if (mDataResouce != null)
                    {
                        mDataResouce.Changed -= DataResource_Changed;
                    }
                    mDataResouce = value;

                    if (mDataResouce != null)
                    {
                        mDataResouce.Changed += DataResource_Changed;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataResourceBindingExtension"/> class.
        /// </summary>
        public DataResourceBindingExtension()
        {
        }

        /// <summary>
        /// When implemented in a derived class, returns an object that is set as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IProvideValueTarget target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

            mTargetObject = target.TargetObject;
            mTargetProperty = target.TargetProperty;

            // mTargetProperty can be null when this is called in the Designer.
            Debug.Assert(mTargetProperty != null || DesignerProperties.GetIsInDesignMode(new DependencyObject()));

            if (DataResource.BindingTarget == null && mTargetProperty != null)
            {
                PropertyInfo propInfo = mTargetProperty as PropertyInfo;
                if (propInfo != null)
                {
                    try
                    {
                        return Activator.CreateInstance(propInfo.PropertyType);
                    }
                    catch (MissingMethodException)
                    {
                        // there isn't a default constructor
                    }
                }

                DependencyProperty depProp = mTargetProperty as DependencyProperty;
                if (depProp != null)
                {
                    DependencyObject depObj = (DependencyObject)mTargetObject;
                    return depObj.GetValue(depProp);
                }
            }

            return DataResource.BindingTarget;
        }

        private void DataResource_Changed(object sender, EventArgs e)
        {
            // Ensure that the bound object is updated when DataResource changes.
            DataResource dataResource = (DataResource)sender;
            DependencyProperty depProp = mTargetProperty as DependencyProperty;

            if (depProp != null)
            {
                DependencyObject depObj = (DependencyObject)mTargetObject;
                object value = Convert(dataResource.BindingTarget, depProp.PropertyType);
                depObj.SetValue(depProp, value);
            }
            else
            {
                PropertyInfo propInfo = mTargetProperty as PropertyInfo;
                if (propInfo != null)
                {
                    object value = Convert(dataResource.BindingTarget, propInfo.PropertyType);
                    propInfo.SetValue(mTargetObject, value, new object[0]);
                }
            }
        }

        private object Convert(object obj, Type toType)
        {
            try
            {
                return System.Convert.ChangeType(obj, toType);
            }
            catch (InvalidCastException)
            {
                return obj;
            }
        }
    }

    
}
