
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        

        private bool _isDraggingEyeline;


        public static FrameworkElement PromptView { get; set; }

        public MainWindowViewModel MainWindowViewModel { get; set; }

        public MainWindow()
        {
            
            InitializeComponent();
            MainWindowViewModel = new MainWindowViewModel(MainTextBox)
                                      {
                                          BookmarkImage = Resources["ClearBookmarkImage"] as ImageSource
                                      };


            DataContext = MainWindowViewModel;
           // MainWindowViewModel.ToolsVisible = true;

            MainTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(MainTextBox_TextChanged);

            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewKeyUp += MainWindow_PreviewKeyUp;
            LocationChanged += MainWindow_LocationChanged;
            SizeChanged += MainWindow_SizeChanged;
            Loaded += MainWindow_Loaded;

            MouseDoubleClick += new MouseButtonEventHandler(MainWindow_MouseDoubleClick);
            
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;


            Closing += MainWindow_Closing;

            PromptView = MainTextGrid;

        }

        void MainTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            MainWindowViewModel.IsDocumentDirty = true;
        }



        void MainWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetToolSizeAndPos();
        }

        void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            SetToolSizeAndPos();
        }

        private void SetToolSizeAndPos()
        {
            MainWindowViewModel.ToolWindowHeight = 250;
            MainWindowViewModel.ToolWindowLeft = Left;
            MainWindowViewModel.ToolWindowWidth = ActualWidth;
            MainWindowViewModel.ToolWindowTop = Top + ActualHeight - MainWindowViewModel.ToolWindowHeight;
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
            Properties.Settings.Default.Save();

            if(MainWindowViewModel.CanShutDownApp())
            {
                MainWindowViewModel.Dispose();
            }
            else
            {
                e.Cancel = true;
            }
            
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

            MainWindowViewModel.ToggleToolsWindow();
            MainWindowViewModel.InitializeConfig();
            
        }

        private void EyelineLeftTriangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingEyeline = true;
            e.Handled = true;
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
                AppConfigHelper.SetUserSetting("EyeLinePosition", MainWindowViewModel.EyelinePosition);
                _isDraggingEyeline = false;
            }
            else
            {
              //  _isDraggingEyeline = false;
            }
        }


    }
}
