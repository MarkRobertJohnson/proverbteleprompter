using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProverbTeleprompter.Controls
{
    public class ScrollViewerExt : ScrollViewer
    {
        public ScrollViewerExt()
        {
            ScrollChanged += ScrollViewerExt_ScrollChanged;
        }

        void ScrollViewerExt_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //It is only a vertical scroll aif the vertical change is not 0
            if(e.VerticalChange != 0)
            {
                VerticalOffsetExt = e.VerticalOffset;
            }

            if(e.ExtentHeightChange != 0)
            {
                ExtentHeightExt = e.ExtentHeight - e.ViewportHeight;
            }

            if(e.ViewportHeightChange != 0)
            {
                ViewportHeightExt = e.ViewportHeight;
            }
            
        }


        #region public double VerticalOffsetExt

        /// <summary>
        /// Identifies the VerticalOffsetExt dependency property.
        /// </summary>
        public static new DependencyProperty VerticalOffsetExtProperty =
            DependencyProperty.Register("VerticalOffsetExt", typeof(double), typeof(ScrollViewerExt),
            new PropertyMetadata { PropertyChangedCallback = VerticalOffsetExtChanged
                ,CoerceValueCallback = CoerceVerticalOffset
            });


        private static void VerticalOffsetExtChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var scrollViewer = obj as ScrollViewerExt;
            var newValue = (double) args.NewValue;
            scrollViewer.VerticalOffsetExt = newValue; 
       
        }


        private static object CoerceVerticalOffset(DependencyObject obj, object value)
        {
            var scrollViewer = obj as ScrollViewerExt;
            var newValue = GetConstrainedVerticalOffset(scrollViewer, (double) value);

            return newValue;
        }

        static double GetConstrainedVerticalOffset(ScrollViewerExt scrollViewer, double newValue)
        {
            if (newValue > scrollViewer.ExtentHeight)
            {
                newValue = scrollViewer.ExtentHeight;
            }

            if (newValue < 0)
            {
                newValue = 0;
            }

            return newValue;
        }
        /// <summary>
        /// 
        /// </summary>
        public  double VerticalOffsetExt
        {
            get { 
                return (double)GetValue(VerticalOffsetProperty);
                //return VerticalOffset;
            }

            set
            {
                
                SetValue(VerticalOffsetExtProperty, value);
                ScrollChanged -= ScrollViewerExt_ScrollChanged;
                ScrollToVerticalOffset(value);
                    
                ScrollChanged += ScrollViewerExt_ScrollChanged;

            }
        }

        #endregion public double VerticalOffsetExt

        #region public double ExtentHeightExt

        /// <summary>
        /// Identifies the ExtentHeightExt dependency property.
        /// </summary>
        public static DependencyProperty ExtentHeightExtProperty =
            DependencyProperty.Register("ExtentHeightExt", typeof (double), typeof (ScrollViewerExt), null);

        /// <summary>
        /// 
        /// </summary>
        public double ExtentHeightExt
        {
            get { return (double) GetValue(ExtentHeightExtProperty); }

            set
            {
                SetValue(ExtentHeightExtProperty, value);
            }
        }

        #endregion public double ExtentHeightExt

        #region public double ViewportHeightExt

        /// <summary>
        /// Identifies the ViewportHeightExt dependency property.
        /// </summary>
        public static DependencyProperty ViewportHeightExtProperty =
            DependencyProperty.Register("ViewportHeightExt", typeof (double), typeof (ScrollViewerExt), null);

        /// <summary>
        /// 
        /// </summary>
        public double ViewportHeightExt
        {
            get { return (double) GetValue(ViewportHeightExtProperty); }

            set { SetValue(ViewportHeightExtProperty, value); }
        }

        #endregion public double ViewportHeightExt
    }
}
