using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for ToolsWindow.xaml
    /// </summary>
    public partial class ToolsWindow : Window
    {
        DispatcherTimer _timer = new DispatcherTimer();
        public ToolsWindow()
        {
            InitializeComponent();

            _timer.Interval = new TimeSpan(0,0,1);
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Start();

        }

        void _timer_Tick(object sender, EventArgs e)
        {
        //    Debug.WriteLine("Focued: {0}", FocusManager.GetFocusedElement(this));
        }

        private void SpeedTextBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dc = DataContext as MainWindowViewModel;
            if(dc != null)
            {
                dc.CaptureKeyboard = false;
            }

            var box = sender as TextBox;

            box.Focusable = true;
            box.Focus();
            box.SelectAll();

            box.PreviewKeyDown += new KeyEventHandler(box_PreviewKeyDown);
            box.LostFocus += new RoutedEventHandler(box_LostFocus);

        }

        void box_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var box = sender as TextBox;
            if (e.Key == Key.Enter)
            {

                e.Handled = true;  

                var eInsertBack = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Tab);  
       
                eInsertBack.RoutedEvent = UIElement.KeyDownEvent;  
       
                InputManager.Current.ProcessInput(eInsertBack);  
                
            }
        }

        void box_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            DisableElement(box);
        

        }

      

        private void DisableElement(UIElement element)
        {
            element.Focusable = false;
            this.Focus();

            var dc = DataContext as MainWindowViewModel;
            if (dc != null)
            {
                dc.CaptureKeyboard = true;
            }
        }

   
    }
}
