using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleBrowser.Converters
{
	public sealed class BoolFalseToVisibilityCollapsedConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof (Visibility))
				return null;

			var v = (bool) value;
			return v ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}