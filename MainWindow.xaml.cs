
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Expression.Shapes;
using ProverbTeleprompter.HtmlConverter;
using Tools.API.Messages.lParam;
using Binding = System.Windows.Data.Binding;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using TextBox = System.Windows.Controls.TextBox;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        private bool _isDraggingEyeline;


        public static FrameworkElement PromptView { get; set; }

        public MainWindowViewModel MainWindowViewModel { get; set; }

        public MainWindow()
        {
            
            InitializeComponent();
            MainWindowViewModel = new MainWindowViewModel(MainTextBox);


            MainWindowViewModel.BookmarkImage = Resources["ClearBookmarkImage"] as ImageSource;


            DataContext = MainWindowViewModel;
            MainWindowViewModel.InitializeConfig();

            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewKeyUp += MainWindow_PreviewKeyUp;    

            KeyDown += MainWindow_KeyDown;

            KeyUp += MainWindow_KeyUp;

            Loaded += MainWindow_Loaded;


            Closing += MainWindow_Closing;

            PromptView = MainScroller;

            //LayoutRoot.UseLayoutRounding = true;
            //LayoutRoot.SnapsToDevicePixels = true;
            //RenderOptions.SetEdgeMode(LayoutRoot, EdgeMode.Aliased);
            
        }

        void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            MainWindowViewModel.KeyUp(sender, e);
        }

        void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            MainWindowViewModel.KeyDown(sender, e);
        }


        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindowViewModel.Dispose();
        }

        void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            //MainWindowViewModel.KeyUp(sender, e);
            
        }


        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {

            //MainWindowViewModel.KeyDown(sender, e);
        }

        void RemoteHandler_RemoteButtonPressed(object sender, RemoteButtonPressedEventArgs e)
        {
            MainWindowViewModel.RemoteButtonPressed(sender, e);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Setup event handler for remote control buttons (multi media buttons)
            //HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            //source.AddHook(new HwndSourceHook(RemoteHandler.WndProc));

            //NOTE: Programmatically set the fly in distance
            //var sb = FindResource("ToolFlyin") as Storyboard;
            //var anim = sb.Children[0];
            //if(anim is DoubleAnimation)
            //{
            //    (anim as DoubleAnimation).To = ToolsGrid.Height;
            //}
            
        }

        private void EyelineLeftTriangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingEyeline = true;
        }

        private void Grid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if(_isDraggingEyeline && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);

                var newPos = mousePos.Y - (EyelineLeftTriangle.Height/2);

                //SetEyeLinePosition(newPos);

                MainWindowViewModel.EyelinePosition = newPos;
                
            }
            else if(_isDraggingEyeline && e.LeftButton == MouseButtonState.Released)
            {
                AppConfigHelper.SetAppSetting("EyeLinePosition", MainWindowViewModel.EyelinePosition.ToString());
                _isDraggingEyeline = false;
            }
            else
            {
              //  _isDraggingEyeline = false;
            }
        }


    }
}
