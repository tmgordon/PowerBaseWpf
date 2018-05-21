using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace PowerBaseWpf.Helpers
{
    //public class BooleanToBrushConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value == null)
    //            return Brushes.Transparent;

    //        Brush[] brushes = parameter as Brush[];
    //        if (brushes == null)
    //            return Brushes.Transparent;

    //        bool isTrue;
    //        bool.TryParse(value.ToString(), out isTrue);

    //        if (isTrue)
    //        {
    //            var brush = (SolidColorBrush) brushes[0];
    //            return brush ?? Brushes.Transparent;
    //        }
    //        else
    //        {
    //            var brush = (SolidColorBrush) brushes[1];
    //            return brush ?? Brushes.Transparent;
    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    [ValueConversion(typeof(Enum), typeof(IEnumerable<ValueDescription>))]
    public class EnumToCollectionConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return EnumHelper.GetAllValuesAndDescriptions(value?.GetType());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }


    public class CheckIfUsedConverter : MarkupExtension, IValueConverter
    {
        public object Row { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stop = "";
            var type = Row.GetType();

            stop = "";
            var column = (DataGridTextColumn) Row;

            stop = "";

            var tester = column.Binding;

            stop = "";


            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class BooleanToBrushConverter : MarkupExtension, IValueConverter
    {
        public Brush TrueBrush { get; set; }
        public Brush FalseBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToBoolean(value) ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class VisibilityConverter : MarkupExtension, IValueConverter
    {
        public Visibility TrueVisibility { get; set; }
        public Visibility FalseVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stop = "";


            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public sealed class TrueToHiddenConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
            {
                flag = (bool) value;
            }
            var visibility = (object) (Visibility) (flag ? 1 : 0);
            return visibility;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                var visibility = (object) ((Visibility) value == Visibility.Visible);
                return visibility;
            }
            return (object) false;
        }
    }
    public sealed class FalseToHiddenConverter : IValueConverter
    {
        /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
        /// <returns>
        /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
        /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            var path = "";
            if (value is bool)
            {
                flag = (bool) value;
            }
            var visibility = (object) (Visibility) (flag ? 0 : 1);
            return visibility;
        }

        /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
        /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
        /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                var visibility = (object) ((Visibility) value == Visibility.Visible);
                return visibility;
            }

            return (object) false;
        }
    }
    public sealed class TrueToVisibleConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
            {
                flag = (bool) value;
            }
            var visibility = (object) (Visibility) (flag ? 0 : 2);
            return visibility;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                var visibility = (object) ((Visibility) value == Visibility.Visible);
                return visibility;
            }
            return (object) false;
        }
    }
    public sealed class FalseToVisibleConverter : IValueConverter
    {
        /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
        /// <returns>
        /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
        /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            var path = "";
            if (value is bool)
            {
                flag = (bool) value;
            }
            var visibility = (object) (Visibility) (flag ? 2 : 0);
            return visibility;
        }

        /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
        /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
        /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                var visibility = (object) ((Visibility) value == Visibility.Visible);
                return visibility;
            }

            return (object) false;
        }
    }

    //public sealed class MoreVisibleConverter : IValueConverter
    //{
    //    /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
    //    /// <returns>
    //    /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
    //    /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        bool flag = false;
    //        var status = value as Status;

    //        if (status != null && status.More)
    //        {
    //            flag = true;
    //        }

    //        var visibility = (object) (Visibility) (flag ? 0 : 2);
    //        return visibility;
    //    }

    //    /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
    //    /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
    //    /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is Visibility)
    //        {
    //            var visibility = (object) ((Visibility) value == Visibility.Visible);
    //            return visibility;
    //        }

    //        return (object) false;
    //    }
    //}


    //public sealed class ErrorToVisibleConverter : IValueConverter
    //{
    //    /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
    //    /// <returns>
    //    /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
    //    /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        bool flag = false;
    //        var level = value as StatusLevel?;
    //        if (level == StatusLevel.Error)
    //        {
    //            flag = true;
    //        }
    //        var visibility = (object) (Visibility) (flag ? 0 : 2);
    //        return visibility;
    //    }

    //    /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
    //    /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
    //    /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is Visibility)
    //        {
    //            var visibility = (object) ((Visibility) value == Visibility.Visible);
    //            return visibility;
    //        }

    //        return (object) false;
    //    }
    //}

    //public sealed class WarnToVisibleConverter : IValueConverter
    //{
    //    /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
    //    /// <returns>
    //    /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
    //    /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        bool flag = false;
    //        var level = value as StatusLevel?;
    //        if (level == StatusLevel.Warning)
    //        {
    //            flag = true;
    //        }
    //        var visibility = (object) (Visibility) (flag ? 0 : 2);
    //        return visibility;
    //    }

    //    /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
    //    /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
    //    /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is Visibility)
    //        {
    //            var visibility = (object) ((Visibility) value == Visibility.Visible);
    //            return visibility;
    //        }

    //        return (object) false;
    //    }
    //}

    //public sealed class InfoToVisibleConverter : IValueConverter
    //{
    //    /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
    //    /// <returns>
    //    /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
    //    /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        bool flag = false;
    //        var level = value as StatusLevel?;
    //        if (level == StatusLevel.Information)
    //        {
    //            flag = true;
    //        }
    //        var visibility = (object) (Visibility) (flag ? 0 : 2);
    //        return visibility;
    //    }

    //    /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
    //    /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
    //    /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
    //    /// <param name="targetType">This parameter is not used.</param>
    //    /// <param name="parameter">This parameter is not used.</param>
    //    /// <param name="culture">This parameter is not used.</param>
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is Visibility)
    //        {
    //            var visibility = (object) ((Visibility) value == Visibility.Visible);
    //            return visibility;
    //        }

    //        return (object) false;
    //    }
    //}



    public sealed class NotPathToVisibilityConverter : IValueConverter
    {
        /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
        /// <returns>
        /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
        /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            var path = "";
            var s = value as string;
            if (s != null)
            {
                path = s;
            }
            if (path.IndexOf("File:", StringComparison.CurrentCulture) != 0)
            {
                flag = true;
            }
            return (object) (Visibility) (flag ? 0 : 2);
        }

        /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
        /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
        /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
                return (object) ((Visibility) value == Visibility.Visible);
            return (object) false;
        }
    }
    public sealed class IsPathToVisibilityConverter : IValueConverter
    {
        /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
        /// <returns>
        /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is true; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
        /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            var path = "";
            var s = value as string;
            if (s != null)
            {
                path = s;
            }
            if (path.IndexOf("File:", StringComparison.CurrentCulture) == 0)
            {
                flag = true;
            }
            return (object) (Visibility) (flag ? 0 : 2);
        }

        /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
        /// <returns>true if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, false.</returns>
        /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
                return (object) ((Visibility) value == Visibility.Visible);
            return (object) false;
        }
    }
}
