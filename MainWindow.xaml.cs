using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Tools.API.Messages.lParam;
using DataFormats = System.Windows.DataFormats;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _scrollTimer;

        private TalentWindow _talentWindow;

        private bool _configInitialized;

        private double _speed = 0;

        private double _defaultSpeed = 1;

        private double _speedBoostAmount = 0;

        private double _totalBoostAmount = 0;

        private string _documentPath;



        private double CurrentSpeed
        {
            get
            {
                return _speed;
            }

            set
            {
                 _speed = value;
            }
        }

        /// <summary>
        /// The speed to return to after pausing
        /// </summary>
        private double _desiredSpeed;
        private double DesiredSpeed
        {
            get
            {
                return _desiredSpeed;   
            }

            set
            {
                _desiredSpeed = value;
            }
        }

        private double TotalBoostAmount
        {
            get
            {
                return _totalBoostAmount;
            }

            set {
                _totalBoostAmount = value;
               if(  value > SpeedSlider.Maximum)
               {
                   _totalBoostAmount = SpeedSlider.Maximum;
               } 
               if(value < SpeedSlider.Minimum)
               {
                   _totalBoostAmount = SpeedSlider.Minimum;
               }
            }
        }

        public static FrameworkElement PromptView { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            InitializeConfig();

            KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            KeyUp += new KeyEventHandler(MainWindow_KeyUp);

            Loaded += new RoutedEventHandler(MainWindow_Loaded);

            PreviewKeyDown += new KeyEventHandler(MainWindow_PreviewKeyDown);

            PreviewKeyUp += new KeyEventHandler(MainWindow_PreviewKeyUp);

            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            //Main scroll loop timer
            _scrollTimer = new DispatcherTimer();
            _scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            _scrollTimer.IsEnabled = true;
            _scrollTimer.Tick += new EventHandler(_scrollTimer_Tick);
            _scrollTimer.Start();

            PromptView = MainTextBox;

 
    
           // RemoteHandler.RemoteButtonPressed += new EventHandler<RemoteButtonPressedEventArgs>(RemoteHandler_RemoteButtonPressed);
        }

        void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            
        }

        void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_talentWindow != null)
            {
                _talentWindow.Close();
            }
        }

        void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                SpeedSlider.Value = _defaultSpeed;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Up)
            {
                SpeedSlider.Value = _defaultSpeed;
               // SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Next)
            {
                SpeedSlider.Value = _defaultSpeed;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Prior)
            {
                //SpeedSlider.Value -= TotalBoostAmount;
                SpeedSlider.Value = _defaultSpeed;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
        }

        void RemoteHandler_RemoteButtonPressed(object sender, RemoteButtonPressedEventArgs e)
        {
           if(e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_FASTFORWARD)
           {
               SpeedSlider.Value++;
           }
           else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_REWIND)
           {
               SpeedSlider.Value--;
           }
           else if(e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PLAY_PAUSE || 
               e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PAUSE)
           {
               PauseScrolling();
           }
           else if(e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PLAY)
           {
               ResumeScrolling();
           }
           else if(e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PREVIOUSTRACK)
           {
               ScrollToTop();
           }
           else if(e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_CHANNEL_UP)
           {
              PageUp();
           }
           else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_CHANNEL_DOWN)
           {
               PageDown();
           }
        }


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Setup event handler for remote control buttons (multi media buttons)
            //HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            //source.AddHook(new HwndSourceHook(RemoteHandler.WndProc));
        }

        private void PageDown()
        {
            MainTextBox.ScrollToVerticalOffset(MainTextBox.VerticalOffset + (MainTextBox.ActualHeight - MainTextBox.ActualHeight * 0.5));
        }

        private void PageUp()
        {
            MainTextBox.ScrollToVerticalOffset(MainTextBox.VerticalOffset - (MainTextBox.ActualHeight - MainTextBox.ActualHeight * 0.5));
        }


        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Down)
            {
                SpeedForward();
            }
            else if(e.Key == Key.Up)
            {
                SpeedReverse();
            }
            else if(e.Key == Key.Right)
            {
                HideTools();
            }
            else if (e.Key == Key.Left)
            {
                ShowTools();
            }
            //Slide forward / page down button To work with Logitech PowerPoint remote
            else if(e.Key == Key.Next)
            {
                
                //PageDown();
                SpeedForward();
            }
            //Slid back button / page up To work with Logitech PowerPoint remote
            else if(e.Key == Key.Prior)
            {
                
                //PageUp();
                SpeedReverse();
            }
            //F5 To work with Logitech PowerPoint remote
            else if(e.Key == Key.F5 || 
                e.Key == Key.MediaStop ||
                e.Key == Key.MediaPlayPause)
            {
                PauseScrolling();
            }
            //Period To work with Logitech PowerPoint remote
            else if(e.Key == Key.OemPeriod)
            {
                ScrollToTop();
            }
            else if(e.Key == Key.MediaPreviousTrack)
            {
                PageUp();
            }
            else if(e.Key == Key.MediaNextTrack)
            {
                PageDown();
            }
     
            
        }

        private void PauseScrolling()
        {
            PausedCheckbox.IsChecked = !PausedCheckbox.IsChecked.GetValueOrDefault();
        }

        private void ResumeScrolling()
        {
            PausedCheckbox.IsChecked = false;
        }

        private void ScrollToTop()
        {
            MainTextBox.ScrollToVerticalOffset(0);
        }

        private void ShowTools()
        {
            Storyboard sb = (Storyboard)this.FindResource("ToolFlyin");
            sb.Begin();  
        }

        private void HideTools()
        {
            Storyboard sb = (Storyboard)this.FindResource("ToolFlyout");
            sb.Begin();

            EditableCheckbox.IsChecked = false;
        }

        private void SpeedForward()
        {
            _speedBoostAmount = 2;
            if (SpeedSlider.Value < 0)
            {
                _speedBoostAmount = 2 + Math.Abs(SpeedSlider.Value);
            }
            TotalBoostAmount += _speedBoostAmount;
            SpeedSlider.Value += _speedBoostAmount;
        }

        private void SpeedReverse()
        {
            _speedBoostAmount = -2;

            if (SpeedSlider.Value > 0)
            {
                _speedBoostAmount = -2 - SpeedSlider.Value;
            }

            TotalBoostAmount += _speedBoostAmount;


            SpeedSlider.Value += _speedBoostAmount;
        }

        void _scrollTimer_Tick(object sender, EventArgs e)
        {

            if (!PausedCheckbox.IsChecked.GetValueOrDefault())
            {
                MainTextBox.ScrollToVerticalOffset(MainTextBox.VerticalOffset + SpeedSlider.Value);
            }
            else
            {
                MainTextBox.ScrollToVerticalOffset(MainTextBox.VerticalOffset + _totalBoostAmount);
            }

            PercentComplete.Text = string.Format("{0:F}%", (MainTextBox.VerticalOffset / MainTextBox.ExtentHeight) * 100);
            LayoutRoot.UseLayoutRounding = true;
            LayoutRoot.SnapsToDevicePixels = true;
            RenderOptions.SetEdgeMode(LayoutRoot, EdgeMode.Aliased);
            

        }


        private void InitializeConfig()
        {
            _configInitialized = true;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (!File.Exists(config.FilePath))
            {
                AppConfigHelper.SetAppSetting("ColorScheme",
                    WhiteOnBlackButton.IsChecked == true ? "WhiteOnBlack" : "BlackOnWhite", config);

            }
            var colorScheme = ConfigurationManager.AppSettings["ColorScheme"];
            if (colorScheme != null && colorScheme.ToLowerInvariant() == "whiteonblack")
            {
                WhiteOnBlackButton.IsChecked = true;
            }
            else
            {
                BlackOnWhiteButton.IsChecked = true;
            }

            var fontSize = ConfigurationManager.AppSettings["FontSize"];
            if(fontSize != null)
            {
                SetFontSize(Double.Parse(fontSize));
            }

            var speed = ConfigurationManager.AppSettings["Speed"];
            if (speed != null)
            {
                SpeedSlider.Value = Double.Parse(speed);
                _defaultSpeed = SpeedSlider.Value;
            }


            _documentPath = ConfigurationManager.AppSettings["DocumentPath"];
            if(!string.IsNullOrWhiteSpace(_documentPath) && File.Exists(_documentPath))
            {
                LoadDocument(_documentPath);
            }
            else
            {
                //Load default text
                using(MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(Properties.Resources.Proverbs_1)))
                {
                   
                    LoadDocument(ms, DataFormats.Rtf);
                }
            }

            var value = ConfigurationManager.AppSettings["FlipTalentWindowVert"];
            if(!string.IsNullOrWhiteSpace(value))
            {
                FlipTalentWindowVertCheckBox.IsChecked = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["FlipTalentWindowHoriz"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipTalentWindowHorizCheckBox.IsChecked = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["FlipMainWindowVert"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipMainWindowVertCheckBox.IsChecked = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["FlipMainWindowHoriz"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipMainWindowHorizCheckBox.IsChecked = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowLeft"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                _talentWindowLeft = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowTop"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                _talentWindowTop = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowWidth"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                _talentWindowWidth = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowHeight"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                _talentWindowHeight = double.Parse(value);
            }
        }

        private void BlackOnWhiteButton_Checked(object sender, RoutedEventArgs e)
        {
            SetBlackOnWhite();
        }

        private void WhiteOnBlackButton_Checked(object sender, RoutedEventArgs e)
        {
            SetWhiteOnBlack();
        }

        private void SetColorScheme()
        {
            if(BlackOnWhiteButton.IsChecked.GetValueOrDefault())
            {
                SetBlackOnWhite();
            }
            else if(WhiteOnBlackButton.IsChecked.GetValueOrDefault())
            {
                SetWhiteOnBlack();
            }
        }

        private void SetWhiteOnBlack()
        {

            MainFlowDocument.Background = Brushes.Black;

            TextRange range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);
            //DocumentHelpers.ChangeTextColor(MainFlowDocument, Brushes.White, Brushes.Black);
            DocumentHelpers.ChangePropertyValue(MainFlowDocument, TextElement.ForegroundProperty, Brushes.White, Brushes.Black);
            DocumentHelpers.ChangePropertyValue(MainFlowDocument, TextElement.BackgroundProperty, Brushes.Black, Brushes.White);
            //range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Black);

            if (_configInitialized)
                AppConfigHelper.SetAppSetting("ColorScheme", "WhiteOnBlack");
        }

        private  void SetBlackOnWhite()
        {
            MainFlowDocument.Background = Brushes.White;
            TextRange range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);
            //range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.White);

            //DocumentHelpers.ChangeTextColor(MainFlowDocument, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(MainFlowDocument, TextElement.ForegroundProperty, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(MainFlowDocument, TextElement.BackgroundProperty, Brushes.White, Brushes.Black);
            if (_configInitialized)
                AppConfigHelper.SetAppSetting("ColorScheme", "BlackOnWhite");
        }


        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_configInitialized)
                AppConfigHelper.SetAppSetting("FontSize", e.NewValue.ToString());
            if(e.NewValue != e.OldValue)
                SetFontSize(e.NewValue);

        }

        private void SetFontSize(double newSize)
        {
            TextRange range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);
            range.ApplyPropertyValue(TextElement.FontSizeProperty, newSize);

            FontSizeSlider.Value = newSize;
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_configInitialized)
                AppConfigHelper.SetAppSetting("Speed", e.NewValue.ToString());

            CurrentSpeed = e.NewValue;
        }

        private void EditableCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            MainTextBox.Focusable = true;
        }

        private void EditableCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainTextBox.Focusable = false;
        }



        private void SaveDocumentAs(string documentPath)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            
            if(!string.IsNullOrWhiteSpace(documentPath))
            {
                dlg.FileName = Path.GetFileName(documentPath);
                dlg.InitialDirectory = Path.GetDirectoryName(documentPath);
 
            }
            else
            {
                dlg.FileName = "untitled"; // Default file name
            }
            

            dlg.DefaultExt = ".rtf"; // Default file extension 

            dlg.Filter = "Rich Text Documents|*.rtf"; // Filter files by extension 

            // Show save file dialog box 

            Nullable<bool> result = dlg.ShowDialog();
            // Process save file dialog box results 

            if (result == true)
            {

                // Save document 

                _documentPath = dlg.FileName;

                if (!string.IsNullOrWhiteSpace(_documentPath))
                {
                    SaveDocument(_documentPath);
                }

                AppConfigHelper.SetAppSetting("DocumentPath", _documentPath);

            }


        }

        private void SaveDocument(string fullFilePath)
        {
            TextRange range;

            FileStream fStream;

            range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);

            using(fStream = new FileStream(fullFilePath, FileMode.Create))
            {
                range.Save(fStream, System.Windows.DataFormats.Rtf);
            }
            ;
            string xamlPath =System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullFilePath),
                                   System.IO.Path.GetFileNameWithoutExtension(fullFilePath) + ".xaml");

            using (fStream = new FileStream(xamlPath, FileMode.Create))
            {
                range.Save(fStream, System.Windows.DataFormats.Xaml);
            }

       
        }



        private void LoadDocumentDialog(string documentPath)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            if (!string.IsNullOrWhiteSpace(documentPath))
            {
                dlg.FileName = Path.GetFileName(documentPath);
                dlg.InitialDirectory = Path.GetDirectoryName(documentPath);
                dlg.Multiselect = false;
                dlg.Title = "Load document for Proverb Teleprompter...";
            }



            dlg.DefaultExt = ".rtf"; // Default file extension 

            dlg.Filter = "Rich Text Documents|*.rtf|XAML Documents|*.xaml|Text Documents|*.txt"; // Filter files by extension 

            // Show save file dialog box 

            Nullable<bool> result = dlg.ShowDialog();
            // Process open file dialog box results 

            if (result == true)
            {

                // Load document 

                _documentPath = dlg.FileName;

                if (!string.IsNullOrWhiteSpace(_documentPath))
                {
                    LoadDocument(_documentPath);
                }

                AppConfigHelper.SetAppSetting("DocumentPath", _documentPath);
                InitializeConfig();
                SetColorScheme();
            }


        }


        private void LoadDocument(Stream fileStream, string dataFormat)
        {
            try
            {
                TextRange range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);

                range.Load(fileStream, dataFormat);

            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Unsupported file type.");
            }
        }


        private void LoadDocument(string fullFilePath)
        {
            try
            {
                string ext = Path.GetExtension(fullFilePath).ToLowerInvariant();
                string dataFormat = DataFormats.Rtf;
                if (ext.EndsWith("xaml"))
                {
                    dataFormat = DataFormats.Xaml;
                }
                else if(ext.EndsWith("txt"))
                {
                    dataFormat = DataFormats.Text;
                }

                using (FileStream fStream = new FileStream(fullFilePath, FileMode.Open))
                {
                    LoadDocument(fStream, dataFormat);
                }


                //SetColorScheme();
                


            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            
            }
           
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_documentPath))
            {
                SaveDocumentAs(_documentPath);
            }
            else
            {
                SaveDocument(_documentPath);
            }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDocumentAs(_documentPath);
        }

        private void PausedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DesiredSpeed = CurrentSpeed;
            SpeedSlider.Value = 0;
            
            CurrentSpeed = 0;


        }

        private void PausedCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            SpeedSlider.Value = DesiredSpeed;
        }

        private void SetDefaultSpeedButton_Click(object sender, RoutedEventArgs e)
        {
            AppConfigHelper.SetAppSetting("Speed", SpeedSlider.Value.ToString());
            _defaultSpeed = SpeedSlider.Value;
        }

        private void ToggleTalentWindowButton_Click(object sender, RoutedEventArgs e)
        {

            ToggleTalentWindow();
        }

        private void ToggleTalentWindow()
        {
            if (_talentWindow == null)
            {
                _talentWindow = new TalentWindow();
                _talentWindow.Owner = this;
                _talentWindow.Closed += new EventHandler(_talentWindow_Closed);
                _talentWindow.KeyDown += MainWindow_KeyDown;
                _talentWindow.KeyUp += MainWindow_KeyUp;
                _talentWindow.SizeChanged += new SizeChangedEventHandler(_talentWindow_SizeChanged);
                _talentWindow.LocationChanged += new EventHandler(_talentWindow_LocationChanged);

                //If a secondary monitor is attached, display the talent windows maximized on it
                //if(SystemInformation.MonitorCount > 1)
                //{
                //    System.Drawing.Rectangle workingArea = Screen.AllScreens[1].WorkingArea;

                //    _talentWindow.Left = workingArea.Left;
                //    _talentWindow.Top = workingArea.Top;
                //    _talentWindow.Width = workingArea.Width;
                //    _talentWindow.Height = workingArea.Height;
       
                //    _talentWindow.WindowStyle = WindowStyle.None;


                //}

                _talentWindow.Left = _talentWindowLeft;
                _talentWindow.Top = _talentWindowTop;
                _talentWindow.Width = _talentWindowWidth;
                _talentWindow.Height = _talentWindowHeight;


                _talentWindow.MouseDoubleClick += _talentWindow_MouseDoubleClick;
                _talentWindow.MouseLeftButtonDown += _talentWindow_MouseLeftButtonDown;
                _talentWindow.Loaded += _talentWindow_Loaded;

                _talentWindow.Show();
                

                FlipTalentWindowVert(FlipTalentWindowVertCheckBox.IsChecked.GetValueOrDefault());
                FlipTalentWindowHoriz(FlipTalentWindowHorizCheckBox.IsChecked.GetValueOrDefault());

                ToggleTalentWindowButton.Content = "Hide Talent Window";

            }
            else
            {
                HideTalentWindow();
            }
        }

        private double _talentWindowLeft = 100;
        private double _talentWindowTop = 100;
        private double _talentWindowWidth = 300;
        private double _talentWindowHeight = 200;

        void _talentWindow_LocationChanged(object sender, EventArgs e)
        {
            _talentWindowLeft = _talentWindow.Left;
            _talentWindowTop = _talentWindow.Top;
            AppConfigHelper.SetAppSetting("TalentWindowLeft", _talentWindow.Left.ToString());
            AppConfigHelper.SetAppSetting("TalentWindowTop", _talentWindow.Top.ToString());
        }

        void _talentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _talentWindowWidth = e.NewSize.Width;
            _talentWindowHeight = e.NewSize.Height;
            AppConfigHelper.SetAppSetting("TalentWindowWidth", e.NewSize.Width.ToString());
            AppConfigHelper.SetAppSetting("TalentWindowHeight", e.NewSize.Height.ToString());

        }

        void _talentWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _talentWindow.DragMove();
        }

        void _talentWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(_talentWindow.WindowState != WindowState.Maximized)
            {
                _talentWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                _talentWindow.WindowState = WindowState.Normal;
            }
        }

        void _talentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (SystemInformation.MonitorCount > 1)
            {
                _talentWindow.WindowState = WindowState.Maximized;
            }
        }


        void _talentWindow_Closed(object sender, EventArgs e)
        {
            _talentWindow = null;
            HideTalentWindow();
        }

        private void HideTalentWindow()
        {
            if(_talentWindow != null)
            {
                _talentWindow.Close();
            }
            ToggleTalentWindowButton.Content = "Show Talent Window";

        }
        private void FlipTalentWindowVert(bool isFlippedVert)
        {
            if (_talentWindow != null)
            {
                if (isFlippedVert)
                {
                    _talentWindow.TalentScale.ScaleY = -1;
                }
                else
                {
                    _talentWindow.TalentScale.ScaleY = 1;
                }

            }

            AppConfigHelper.SetAppSetting("FlipTalentWindowVert", isFlippedVert.ToString());
        }

        private void FlipTalentWindowHoriz(bool isFlippedHoriz)
        {
            if (_talentWindow != null)
            {
                if (isFlippedHoriz)
                {
                    _talentWindow.TalentScale.ScaleX = -1;
                }
                else
                {
                    _talentWindow.TalentScale.ScaleX = 1;
                }

            }

            AppConfigHelper.SetAppSetting("FlipTalentWindowHoriz", isFlippedHoriz.ToString());
        }


        private void FlipMainWindowVert(bool isFlippedVert)
        {

            if (isFlippedVert)
            {
                MainScale.ScaleY = -1;
            }
            else
            {
                MainScale.ScaleY = 1;
            }

            AppConfigHelper.SetAppSetting("FlipMainWindowVert", isFlippedVert.ToString());
        }


        private void FlipMainWindowHoriz(bool isFlippedHoriz)
        {

            if (isFlippedHoriz)
            {
                MainScale.ScaleX = -1;
            }
            else
            {
                MainScale.ScaleX = 1;
            }

            AppConfigHelper.SetAppSetting("FlipMainWindowHoriz", isFlippedHoriz.ToString());
        }



        private void FlipTalentWindowVertCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FlipTalentWindowVert(FlipTalentWindowVertCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipTalentWindowVertCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FlipTalentWindowVert(FlipTalentWindowVertCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipTalentWindowHorizCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FlipTalentWindowHoriz(FlipTalentWindowHorizCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipTalentWindowHorizCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FlipTalentWindowHoriz(FlipTalentWindowHorizCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipMainWindowVertCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FlipMainWindowVert(FlipMainWindowVertCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipMainWindowVertCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FlipMainWindowVert(FlipMainWindowVertCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipMainWindowHorizCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FlipMainWindowHoriz(FlipMainWindowHorizCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipMainWindowHorizCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FlipMainWindowHoriz(FlipMainWindowHorizCheckBox.IsChecked.GetValueOrDefault());
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDocumentDialog(_documentPath);
        }


    }
}
