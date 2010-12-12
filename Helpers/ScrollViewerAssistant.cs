using System.Windows;
using System.Windows.Controls;

namespace ProverbTeleprompter.Helpers
{
    public class ScrollViewerAssistant : DependencyObject
    {

        public static double GetVerticalOffset(DependencyObject obj) { return (double)obj.GetValue(VerticalOffsetProperty); }
        public static void SetVerticalOffset(DependencyObject obj, double value) { obj.SetValue(VerticalOffsetProperty, value); }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof (double),
                                                typeof (ScrollViewerAssistant),
                                                new FrameworkPropertyMetadata
                                                    {
                                                        BindsTwoWayByDefault = true,
                                                        PropertyChangedCallback = VerticalOffsetChanged,
                                                    });


        private static void VerticalOffsetChanged(DependencyObject obj,
                    DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = obj as ScrollViewer;
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty) ; }
            set { SetValue(VerticalOffsetProperty, value); }
        }

    }
}
