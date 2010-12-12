using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace ProverbTeleprompter.Converters
{

    public class StringToFloatConverter : MarkupExtension, IValueConverter
    {
        public float Min { get; set; }
        public float Max { get; set; }

        public StringToFloatConverter()
        {
            Min = float.MinValue;
            Max = float.MaxValue;

        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GetConstrainedValue(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GetConstrainedValue(value).ToString();
        }

        #endregion

        private object GetConstrainedValue(object value)
        {
            float val;
            var canParse = float.TryParse(value.ToString(), out val);
            if (!canParse) return value;
            if (val > Max)
            {
                val = Max;
            }

            if (val < Min)
            {
                val = Min;
            }

            return val;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

}

