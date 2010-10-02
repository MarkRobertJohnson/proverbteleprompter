using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Data;

namespace ProverbTeleprompter.Converters
{

    public class PixelConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ToUnits(System.Convert.ToDouble(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Implement this if you want TwoWay or OneWayToSource binding
            throw new NotImplementedException();
        }

        #endregion

        #region Static Methods

        public static double ToUnits(double desiredPixels)
        {
            double finalUnits = desiredPixels * 96 / Dpi;
            return finalUnits;
        }

        public static double ToPixels(double desiredUnits)
        {
            double finalPixels = desiredUnits/96 * Dpi;
            return finalPixels;
        }

        #endregion

        #region Static Properties

        public static int DpiX
        {
            get
            {
                IntPtr dc = GetDC(IntPtr.Zero);
                return GetDeviceCaps(dc, LOGPIXELSX);
            }
        }

        public static int DpiY
        {
            get
            {
                IntPtr dc = GetDC(IntPtr.Zero);
                return GetDeviceCaps(dc, LOGPIXELSY);
            }
        }

        #endregion

        #region Win32 Interop

        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("Gdi32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int HORZRES = 8;
        private const int VERTRES = 10;
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        #endregion

        #region Consts

        private static readonly int Dpi = DpiX;

        #endregion
    }

}

