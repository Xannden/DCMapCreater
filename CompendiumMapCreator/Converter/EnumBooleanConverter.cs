﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace CompendiumMapCreator.Converter
{
	public class EnumBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.Equals(parameter);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value?.Equals(true) == true ? parameter : Binding.DoNothing;
	}
}