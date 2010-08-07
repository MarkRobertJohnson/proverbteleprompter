using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _scrollTimer;

        private bool _configInitialized;

        private double _speed = 0;

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

        public MainWindow()
        {
            InitializeComponent();

            InitializeConfig();

            KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            KeyUp += new KeyEventHandler(MainWindow_KeyUp);

            Loaded += new RoutedEventHandler(MainWindow_Loaded);

            //Main scroll loop timer
            _scrollTimer = new DispatcherTimer();
            _scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            _scrollTimer.IsEnabled = true;
            _scrollTimer.Tick += new EventHandler(_scrollTimer_Tick);
            _scrollTimer.Start();

    
            RemoteHandler.RemoteButtonPressed += new EventHandler<RemoteButtonPressedEventArgs>(RemoteHandler_RemoteButtonPressed);
        }

        void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                SpeedSlider.Value = 1;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Up)
            {
                SpeedSlider.Value = 1;
               // SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Next)
            {
                SpeedSlider.Value = 1;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Prior)
            {
                //SpeedSlider.Value -= TotalBoostAmount;
                SpeedSlider.Value = 1;
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
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(RemoteHandler.WndProc));
        }

        private void PageDown()
        {
            MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset + (MainScroller.ActualHeight - MainScroller.ActualHeight * 0.5));
        }

        private void PageUp()
        {
            MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset - (MainScroller.ActualHeight - MainScroller.ActualHeight * 0.5));
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
            else if(e.Key == Key.Next)
            {
                
                //PageDown();
                SpeedForward();
            }

            else if(e.Key == Key.Prior)
            {
                
                //PageUp();
                SpeedReverse();
            }
            else if(e.Key == Key.F5)
            {
                PauseScrolling();
            }
            else if(e.Key == Key.OemPeriod)
            {
                ScrollToTop();
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
            MainScroller.ScrollToVerticalOffset(0);
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
                MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset + SpeedSlider.Value);
            }
            else
            {
                MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset + _totalBoostAmount);
            }

            PercentComplete.Text = string.Format("{0:F}%", (MainScroller.VerticalOffset / MainScroller.ScrollableHeight) * 100);
            

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
                FontSizeSlider.Value = Double.Parse(fontSize);
            }

            var speed = ConfigurationManager.AppSettings["Speed"];
            if (speed != null)
            {
                SpeedSlider.Value = Double.Parse(speed);
            }


            _documentPath = ConfigurationManager.AppSettings["DocumentPath"];
            if(!string.IsNullOrWhiteSpace(_documentPath) && File.Exists(_documentPath))
            {
                LoadDocument(_documentPath);
            }
        }

        private void BlackOnWhiteButton_Checked(object sender, RoutedEventArgs e)
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

        private void WhiteOnBlackButton_Checked(object sender, RoutedEventArgs e)
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


        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_configInitialized)
                AppConfigHelper.SetAppSetting("FontSize", e.NewValue.ToString());

            TextRange range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);
            range.ApplyPropertyValue(TextElement.FontSizeProperty, e.NewValue);

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
                dlg.FileName = documentPath; 
 
            }
            else
            {
                dlg.FileName = "untitled"; // Default file name
            }
            

            dlg.DefaultExt = ".rtf"; // Default file extension 

            dlg.Filter = "Rich Text Documents (.rtf)|*.rtf"; // Filter files by extension 

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


        private void LoadDocument(string fullFilePath)
        {
            try
            {
                TextRange range;

                FileStream fStream;

                range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);

                fStream = new FileStream(fullFilePath, FileMode.Open);

                range.Load(fStream, System.Windows.DataFormats.Rtf);

                fStream.Close();
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

    }
}
