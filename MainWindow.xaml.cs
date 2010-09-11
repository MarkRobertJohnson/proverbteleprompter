
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
        private DispatcherTimer _scrollTimer;

        private TalentWindow _talentWindow;

        private bool _configInitialized;

        private double _speed = 0;

        private double _defaultSpeed = 1;

        private double _speedBoostAmount = 0;

        private double _totalBoostAmount = 0;

        private string _documentPath;
        private string _tempDocumentPath;

        Dictionary<string, FileSystemWatcher> _watchedFiles = new Dictionary<string, FileSystemWatcher>();

        private Process _wordpadProcess;

        private bool _toolsVisible = true;

        private double _pixelsPerSecond;
        private DateTime _prevTime = DateTime.Now;
        private double _prevScrollOffset;
        private TimeSpan _eta;

        private double _talentWindowLeft = 100;
        private double _talentWindowTop = 100;
        private double _talentWindowWidth = 300;
        private double _talentWindowHeight = 200;

        private bool _isDraggingEyeline;

        EditWindow _editWindow = null;

        private static SemaphoreSlim _changeSemaphore = new SemaphoreSlim(1);

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


            MainScroller.ScrollChanged += new ScrollChangedEventHandler(MainScroller_ScrollChanged);
            //Main scroll loop timer
            _scrollTimer = new DispatcherTimer();
            _scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            _scrollTimer.IsEnabled = true;
            _scrollTimer.Tick += new EventHandler(_scrollTimer_Tick);
            _scrollTimer.Start();

            PromptView = MainTextGrid;


            //LayoutRoot.UseLayoutRounding = true;
            //LayoutRoot.SnapsToDevicePixels = true;
            //RenderOptions.SetEdgeMode(LayoutRoot, EdgeMode.Aliased);
            

           // RemoteHandler.RemoteButtonPressed += new EventHandler<RemoteButtonPressedEventArgs>(RemoteHandler_RemoteButtonPressed);
        }

        void MainScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
        }

        void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            
        }

        void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (_wordpadProcess != null  &&!_wordpadProcess.HasExited)
            {
                _wordpadProcess.CloseMainWindow();
                _wordpadProcess.Close();

            }
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

            //NOTE: Programmatically set the fly in distance
            var sb = FindResource("ToolFlyin") as Storyboard;
            var anim = sb.Children[0];
            if(anim is DoubleAnimation)
            {
                (anim as DoubleAnimation).To = ToolsGrid.Height;
            }
            
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
            else if(e.Key == Key.Tab)
            {
                ToggleTools();
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
                e.Key == Key.MediaPlayPause || 
                e.Key == Key.Escape)
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
            //Numbers 1-9 should jump to the corresponding bookmark
            else if( (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) )
            {
                KeyConverter converter = new KeyConverter();
                string val = converter.ConvertToString(e.Key);

               JumpToBookmarkByOrdinal(int.Parse(val)); 
            }
            else if(e.Key == Key.F1)
            {
                LoadRandomBibleChapter();
                
            }
         
   

        }

        private void LoadRandomBibleChapter()
        {

            MainTextBox.Document = HtmlToXamlConverter.ConvertHtmlToXaml(BibleHelpers.GetRandomBibleChapterHtml());
            MainTextBox.Document.ContentStart.InsertLineBreak();
            MainTextBox.Document.ContentStart.InsertLineBreak();
            MainTextBox.Document.ContentStart.InsertLineBreak();
            MainScroller.ScrollToTop();
            SetDocumentConfig();
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

        private void ToggleTools()
        {
            if(_toolsVisible)
            {
                HideTools();
            }
            else
            {
                ShowTools();
            }
        }

        private void ShowTools()
        {
            _toolsVisible = true;
            Storyboard sb = (Storyboard)this.FindResource("ToolFlyin");
            sb.Begin();  
        }

        private void HideTools()
        {

            _toolsVisible = false;
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

        private int _ticksElapsed;
        void _scrollTimer_Tick(object sender, EventArgs e)
        {
            _ticksElapsed++;
            if (!PausedCheckbox.IsChecked.GetValueOrDefault())
            {
                MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset + SpeedSlider.Value);
            }
            else
            {
                MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset + _totalBoostAmount);
            }

            

            //Only update calculations every 10 timer ticks (100 ms)
            if(_ticksElapsed % 10 == 0)
            {
                //Calculate pixels per second (velocity)
                if (DateTime.Now - _prevTime > TimeSpan.FromSeconds(1))
                {
                    CalcEta();

                }

                var pos = MainTextBox.GetPositionFromPoint(new Point(0, MainScroller.VerticalOffset + EyelineLeftTriangle.Margin.Top + EyelineRightTriangle.Height / 2), true);
             //   var num = DocumentHelpers.GetLineNumberFromSelection(pos);

              //  CurrentLine.Text = num.ToString();






                //at top of document:
                //The eye line may not line up with the top of the document
                //1)  Padd the beginning of the document with white space
                //2)  
                var eyeLineOffset = MainScroller.ViewportHeight - EyelineLeftTriangle.Margin.Top;

                PercentComplete.Text = string.Format("{0:F}%", ((MainScroller.VerticalOffset + MainScroller.ViewportHeight - eyeLineOffset) / MainScroller.ExtentHeight) * 100);

            }

            //if(_ticksElapsed % 50 == 0)
            //{
            //    var endPos = MainTextBox.GetPositionFromPoint(new Point(0, MainScroller.ExtentHeight), true);

            //    var totalLines = DocumentHelpers.GetLineNumberFromSelection(endPos);
            //    TotalLines.Text = totalLines.ToString();
            //}

        }


        private void CalcEta()
        {
            var diff = DateTime.Now - _prevTime;
            var pixelChange = (MainScroller.VerticalOffset - _prevScrollOffset);
            _pixelsPerSecond = pixelChange  / diff.TotalSeconds;

            var pixelsToGo = MainScroller.ScrollableHeight - MainScroller.VerticalOffset;


            if(pixelsToGo == 0)
            {
                Eta.Text = TimeSpan.FromSeconds(0).ToString();
                return;
            }

            var secondsToDone = pixelsToGo / _pixelsPerSecond;

            _eta = new TimeSpan(0, 0, (int)secondsToDone);

            Eta.Text = _eta >= TimeSpan.FromSeconds(0) ? _eta.ToString() : "N/A";

            

            _prevTime = DateTime.Now;
            _prevScrollOffset = MainScroller.VerticalOffset;
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
                    DocumentHelpers.LoadDocument(ms, MainTextBox.Document, DataFormats.Rtf);
                }
            }


            SetDocumentConfig();

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

            value = ConfigurationManager.AppSettings["EyeLinePosition"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetEyeLinePosition(double.Parse(value));
            }
        }

        private void SetDocumentConfig()
        {
            var colorScheme = ConfigurationManager.AppSettings["ColorScheme"];
            if (colorScheme != null && colorScheme.ToLowerInvariant() == "whiteonblack")
            {
                if (WhiteOnBlackButton.IsChecked == true)
                {
                    SetWhiteOnBlack();
                }
                WhiteOnBlackButton.IsChecked = true;
            }
            else
            {
                if (BlackOnWhiteButton.IsChecked == true)
                {
                    SetBlackOnWhite();
                }
                BlackOnWhiteButton.IsChecked = true;
            }

            var lineHeight = ConfigurationManager.AppSettings["LineHeight"];
            if (lineHeight != null)
            {
                SetLineHeight(Double.Parse(lineHeight), FontSizeSlider.Value);
            }

            var fontSize = ConfigurationManager.AppSettings["FontSize"];
            if (fontSize != null)
            {
                SetFontSize(Double.Parse(fontSize));
            }


            LoadBookmarks(MainTextBox.Document);
        }



        private void WatchDocumentForChanges(string fullFilePath, Action<object, FileSystemEventArgs> onChangedAction)
        {
            if(!_watchedFiles.ContainsKey(fullFilePath))
            {
                var fsw = new FileSystemWatcher();
                _watchedFiles.Add(fullFilePath, fsw);
                
                fsw = new FileSystemWatcher();
                fsw.BeginInit();
                fsw.Path = Path.GetDirectoryName(fullFilePath);
                fsw.Filter = Path.GetFileName(fullFilePath);
                fsw.IncludeSubdirectories = false;
                fsw.NotifyFilter = NotifyFilters.LastWrite;


                fsw.Changed += onChangedAction.Invoke;
                fsw.EnableRaisingEvents = true;
                fsw.EndInit();
            }
            
        }

        private void UnWatchDocumentForChanges(string fullFilePath, Action<object, FileSystemEventArgs> onChangedAction)
        {
            if (_watchedFiles.ContainsKey(fullFilePath))
            {
                _watchedFiles[fullFilePath].Changed -= onChangedAction.Invoke;
                _watchedFiles[fullFilePath].Dispose();
                _watchedFiles.Remove(fullFilePath);
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

            MainTextBox.Document.Background = Brushes.Black;
            try
            {
                DocumentHelpers.ChangePropertyValue(MainTextBox.Document, TextElement.ForegroundProperty, Brushes.White, Brushes.Black);
                DocumentHelpers.ChangePropertyValue(MainTextBox.Document, TextElement.BackgroundProperty, Brushes.Black, Brushes.White);

            }
            catch (Exception ex)
            {
                
                throw;
            }


            if (_configInitialized)
                AppConfigHelper.SetAppSetting("ColorScheme", "WhiteOnBlack");

            MainTextBox.CaretBrush = Brushes.White;
        }

        private  void SetBlackOnWhite()
        {
            MainTextBox.Document.Background = Brushes.White;

            DocumentHelpers.ChangePropertyValue(MainTextBox.Document, TextElement.ForegroundProperty, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(MainTextBox.Document, TextElement.BackgroundProperty, Brushes.White, Brushes.Black);
            if (_configInitialized)
                AppConfigHelper.SetAppSetting("ColorScheme", "BlackOnWhite");

            MainTextBox.CaretBrush = Brushes.Black;
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
          //  TextRange range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);
          //  range.ApplyPropertyValue(TextElement.FontSizeProperty, newSize);
            DocumentHelpers.ChangePropertyValue(MainTextBox.Document, TextElement.FontSizeProperty, newSize);

            FontSizeSlider.Value = newSize;

            if (LineHeightSlider != null)
            {
                SetLineHeight(LineHeightSlider.Value, FontSizeSlider.Value);
            }
            

        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_configInitialized && e.NewValue != 0)
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
            try
            {
                

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                if (!string.IsNullOrWhiteSpace(documentPath))
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
                    //No longer watch the old document
                    if(dlg.FileName != documentPath)
                    {
                        UnWatchDocumentForChanges(_documentPath, Document_Changed);
                    }

                    // Save document 
                    
                    _documentPath = dlg.FileName;

                    if (!string.IsNullOrWhiteSpace(_documentPath))
                    {
                        SaveDocument(_documentPath);
                    }

                    AppConfigHelper.SetAppSetting("DocumentPath", _documentPath);

                }
            }
            finally
            {

                
            }
            


        }

        private void SaveDocument(string fullFilePath)
        {
            TextRange range;

            FileStream fStream;

            try
            {
                UnWatchDocumentForChanges(fullFilePath, Document_Changed);

                range = new TextRange(MainTextBox.Document.ContentStart, MainTextBox.Document.ContentEnd);

                using (fStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    DocumentHelpers.SaveDocument(fStream, MainTextBox.Document, DataFormats.Rtf);
                }
                ;
                string xamlPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullFilePath),
                                       System.IO.Path.GetFileNameWithoutExtension(fullFilePath) + ".xaml");

                using (fStream = new FileStream(xamlPath, FileMode.Create))
                {
                    DocumentHelpers.SaveDocument(fStream, MainTextBox.Document, DataFormats.Xaml);
                }

            }
            finally
            {
                WatchDocumentForChanges(fullFilePath, Document_Changed);
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
                SetDocumentConfig();
                SetColorScheme();
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
                    DocumentHelpers.LoadDocument(fStream,MainTextBox.Document, dataFormat);
                    fStream.Close();

                    
                }

                WatchDocumentForChanges(_documentPath, Document_Changed);

                
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

        #region Talent Window Methods

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
              
                _talentWindow.Left = _talentWindowLeft;
                _talentWindow.Top = _talentWindowTop;
                _talentWindow.Width = _talentWindowWidth;
                _talentWindow.Height = _talentWindowHeight;


                _talentWindow.MouseDoubleClick += _talentWindow_MouseDoubleClick;
                _talentWindow.MouseLeftButtonDown += _talentWindow_MouseLeftButtonDown;
                _talentWindow.MouseLeftButtonUp += new MouseButtonEventHandler(_talentWindow_MouseLeftButtonUp);
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

        void _talentWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AppConfigHelper.SetAppSetting("TalentWindowLeft", _talentWindow.Left.ToString());
            AppConfigHelper.SetAppSetting("TalentWindowTop", _talentWindow.Top.ToString());

        }

        void _talentWindow_LocationChanged(object sender, EventArgs e)
        {
            _talentWindowLeft = _talentWindow.Left;
            _talentWindowTop = _talentWindow.Top;

        }

        void _talentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _talentWindowWidth = e.NewSize.Width;
            _talentWindowHeight = e.NewSize.Height;
            AppConfigHelper.SetAppSetting("TalentWindowWidth", _talentWindow.Width.ToString());
            AppConfigHelper.SetAppSetting("TalentWindowHeight", _talentWindow.Height.ToString());
            
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

        #endregion

        private void FlipMainWindowVertCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FlipMainWindowVert(FlipMainWindowVertCheckBox.IsChecked.GetValueOrDefault());
        }

        private void FlipMainWindowVertCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FlipMainWindowVert(FlipMainWindowVertCheckBox.IsChecked.GetValueOrDefault());
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

                SetEyeLinePosition(newPos);
                
            }
            else if(_isDraggingEyeline && e.LeftButton == MouseButtonState.Released)
            {
                AppConfigHelper.SetAppSetting("EyeLinePosition", EyelineLeftTriangle.Margin.Top.ToString());
                _isDraggingEyeline = false;
            }
            else
            {
              //  _isDraggingEyeline = false;
            }
        }

        private void SetEyeLinePosition(double position)
        {
            if(position < 0)
            {
                position = 0;
            }
            Thickness loc = new Thickness(0, position, 0, 0);
            EyelineLeftTriangle.Margin = loc;
            EyelineRightTriangle.Margin = new Thickness(EyelineRightTriangle.Margin.Left,
                position, EyelineRightTriangle.Margin.Right, EyelineRightTriangle.Margin.Bottom);
        }


        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            //EditWindow editWindow = new EditWindow();
            //editWindow.Owner = this;
            //editWindow.Show();

            MemoryStream ms = new MemoryStream();
            DocumentHelpers.SaveDocument(ms,MainTextBox.Document, DataFormats.Rtf);
            

            if(_editWindow != null)
            {
                
                _editWindow.Dispatcher.Invoke((Action)(() =>
                {
                    UpdateEditWindowDocument(ms);
                    _editWindow.Activate();
                    _editWindow.Visibility = Visibility.Visible;
                    _editWindow.Show();
                }));

            }
            else
            {
                //So the edit window doesn't interfere with the scrolling of the prompter window
                //Spawn the edit window on a separate thread.
                //The down side of doing this is that we cannot set the child window's owner to the MainWindow
                //due to thread ownership
                Thread thread = new Thread(() =>
                {
                    _editWindow = new EditWindow();

                    _editWindow.ShowActivated = true;

                    _editWindow.DocumentUpdated += new EventHandler<DocumentUpdatedEventArgs>(_editWindow_DocumentUpdated);
                    

                    _editWindow.Loaded += (sender2, e2) =>
                    {
                        UpdateEditWindowDocument(ms);
                    };

                    _editWindow.Show();
                    _editWindow.Closed += (sender2, e2) =>
                     _editWindow.Dispatcher.InvokeShutdown();


                    Dispatcher.Run();
                });

                thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
     
            }
           
        }

        private void UpdateEditWindowDocument(MemoryStream ms)
        {
            _editWindow.RichTextEditor.RichTextBox.VerifyAccess();
            _editWindow.RichTextEditor.LoadDocument(ms, DataFormats.Rtf);
            ms.Dispose();

            ConvertDocumentToEditableFormat(_editWindow.RichTextEditor.RichTextBox.Document);
            _editWindow.RichTextEditor.RichTextBox.CaretBrush = Brushes.Black;
        }

        private void ConvertDocumentToEditableFormat(FlowDocument document)
        {
            DocumentHelpers.ChangePropertyValue(document, TextElement.FontSizeProperty, (double)12);

            DocumentHelpers.ChangePropertyValue(document, TextElement.ForegroundProperty, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(document, TextElement.BackgroundProperty, Brushes.White, Brushes.Black);
        }

        void _editWindow_DocumentUpdated(object sender, DocumentUpdatedEventArgs e)
        {
            Dispatcher.Invoke((Action) (() =>
            {
            
                DocumentHelpers.LoadDocument(e.DocumentData,MainTextBox.Document, e.DataFormat);

                SetDocumentConfig();
            }));
        }
        
        
        
        private void EditInWordpadButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(_documentPath))
            {
                SaveDocumentAs(_documentPath);
            }
           
            //Cancelled from saving document
            if(string.IsNullOrWhiteSpace(_documentPath))
            {
                return;
            }

            _tempDocumentPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(_documentPath));

            

            using(var ms = new MemoryStream())
            {
                DocumentHelpers.SaveDocument(ms, MainTextBox.Document, DataFormats.Rtf); 

               // SaveDocument(_tempDocumentPath);
                

                FlowDocument tempDoc = new FlowDocument();
                DocumentHelpers.LoadDocument(ms,tempDoc, DataFormats.Rtf );
                ConvertDocumentToEditableFormat(tempDoc);
                using (var tempFileStream = File.OpenWrite(_tempDocumentPath))
                {
                    DocumentHelpers.SaveDocument(tempFileStream, tempDoc, DataFormats.Rtf);
                }
            }



            WatchDocumentForChanges(_tempDocumentPath, Document_Changed);

            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = string.Format("\"{0}\"", _tempDocumentPath);
            info.FileName = "wordpad.exe";
            _wordpadProcess = Process.Start(info);
        }


        void Document_Changed(object sender, FileSystemEventArgs e)
        {

        
            try
            {
                _changeSemaphore.Wait();
                var storeStream = new MemoryStream();

                using (var filestream = File.OpenRead(e.FullPath))
                {
                    storeStream.SetLength(filestream.Length);
                    filestream.Read(storeStream.GetBuffer(), 0, (int)filestream.Length);
                    storeStream.Flush();
                }

                Dispatcher.Invoke((Action)(() =>
                {

                    DocumentHelpers.LoadDocument(storeStream, MainTextBox.Document, DataFormats.Rtf);
                    storeStream.Dispose();
                    SetDocumentConfig();
                }));
            }
            catch (Exception)
            {

            }
            finally
            {
                _changeSemaphore.Release();
            }



            


        }

        private void LineHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetLineHeight(e.NewValue, FontSizeSlider.Value);
        }

        private void SetLineHeight(double height, double fontSize)
        {
            LineHeightSlider.Value = height;
            MainTextBox.Document.SetValue(Paragraph.LineHeightProperty, height * fontSize);
            if (_configInitialized)
                AppConfigHelper.SetAppSetting("LineHeight", height.ToString());
        }

        private void AddBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            var bookmarkOffset = MainScroller.VerticalOffset + EyelineLeftTriangle.Margin.Top;

            var pos = MainTextBox.GetPositionFromPoint(new Point(0, bookmarkOffset), true);
            

            var num = DocumentHelpers.GetLineNumberFromSelection(pos);

            Hyperlink hyperlink = new Hyperlink(pos, pos);
            

            Image img = new Image();

            img.Source = Resources["ClearBookmarkImage"] as ImageSource;

            

 
            img.Visibility = Visibility.Collapsed;
            Bookmark bm = new Bookmark();
            bm.Name = string.Format("Boomark {0}", BookmarksListbox.Items.Count + 1);
            bm.Line = num;
            bm.TopOffset = bookmarkOffset;

            bm.Image = img;

            hyperlink.NavigateUri = new Uri(String.Format("http://bookmark/{0}",  bm.Name));
            hyperlink.Inlines.Add(img);
            bm.Hyperlink = hyperlink;
            bm.Position = pos;
            
            BookmarksListbox.Items.Add(bm);
            bm.Ordinal = BookmarksListbox.Items.Count;
            


             
        }

        private void BookmarksListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            JumpToBookmark(e.AddedItems[0] as Bookmark);
        }

        private  void JumpToBookmark(Bookmark bookmark)
        {
            bookmark.Hyperlink.BringIntoView();

            BookmarksListbox.SelectedItem = bookmark;
        }

        private  void JumpToBookmarkByOrdinal(int ordinal)
        {
            foreach (var bookmark in BookmarksListbox.Items)
            {
                if(bookmark is Bookmark)
                {
                    if((bookmark as Bookmark).Ordinal == ordinal)
                    {
                        JumpToBookmark(bookmark as Bookmark);
                    }
                }
            }
        }

        void BookmarkItemClicked(object sender, MouseButtonEventArgs e)
        {
            var bookmark = (sender as ContentControl).Content as Bookmark; 
            JumpToBookmark(bookmark);
        }


        private ICommand _renameBookmarkCommand;
        public ICommand RenameBookmarkCommand
        {
            get
            {
                if(_renameBookmarkCommand == null)
                {
                    _renameBookmarkCommand = new RelayCommand(x =>
                    {
                        var listItem = x as DependencyObject;
                        var children = listItem.FindChildren<TextBox>();

                        foreach (var textBox in children)
                        {
                            textBox.Focusable = true;
                            textBox.IsEnabled = true;
                            textBox.SelectionStart = 0;
                            textBox.SelectionLength = textBox.Text.Length;
                            
                            textBox.Focus();


                        }
                    });
                }
                return _renameBookmarkCommand;
            }
        }

        private ICommand _deleteBookmarkCommand;
        public ICommand DeleteBookmarkCommand
        {
            get
            {
                if (_deleteBookmarkCommand == null)
                {
                    _deleteBookmarkCommand = new RelayCommand(x =>
                    {
                        var bookmark = (x as ContentControl).Content as Bookmark;
                        bookmark.Hyperlink.Inlines.Clear();
                       BookmarksListbox.Items.Remove((x as ContentControl).Content);


                    });
                }
                return _deleteBookmarkCommand;
            }
        }


        private void LoadBookmarks(FlowDocument document)
        {
            BookmarksListbox.Items.Clear();
            var hyperlinks = document.GetLogicalChildren<Hyperlink>(true);
            foreach (var hyperlink in hyperlinks)
            {
                AddBookmarkFromHyperlink(hyperlink);
            }
        }

        private void AddBookmarkFromHyperlink(Hyperlink hyperlink)
        {


            if (hyperlink.NavigateUri.IsAbsoluteUri && hyperlink.NavigateUri.Host.StartsWith("bookmark"))
            {
                
                Bookmark bm = new Bookmark();

                bm.Name = Uri.UnescapeDataString(hyperlink.NavigateUri.Segments[1]);
                bm.Hyperlink = hyperlink;
                
               
                
                BookmarksListbox.Items.Add(bm);

                bm.Ordinal = BookmarksListbox.Items.Count;
                bm.Image = (hyperlink.Inlines.FirstInline as InlineUIContainer).Child as Image;
                bm.Image.Height = FontSizeSlider.Value;
                
            }


        }

    }
}
