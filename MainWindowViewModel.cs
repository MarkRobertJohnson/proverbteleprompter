using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ProverbTeleprompter.Converters;
using ProverbTeleprompter.Properties;
using Tools.API.Messages.lParam;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using DataFormats = System.Windows.DataFormats;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using RichTextBox = System.Windows.Controls.RichTextBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Windows.Size;

namespace ProverbTeleprompter
{
    public partial class MainWindowViewModel : NotifyPropertyChangedBase, IDisposable
    {
        private static readonly SemaphoreSlim ChangeSemaphore = new SemaphoreSlim(1);
        private readonly DispatcherTimer _scrollTimer;
        private ObservableCollection<Bookmark> _bookmarks = new ObservableCollection<Bookmark>();
        private bool _configInitialized;
        private TimeSpan _eta;
        private double _pixelsPerSecond;
        private double _prevScrollOffset;
        private DateTime _prevTime = DateTime.Now;
        private double _speedBoostAmount;
        private TalentWindow _talentWindow;
        private int _ticksElapsed;
        private ToolsWindow _toolsWindow;
        private Process _wordpadProcess;


        public MainWindowViewModel(RichTextBox mainTextBox)
        {
            _scrollTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 0, 0, 15), IsEnabled = true};
            _scrollTimer.Tick += _scrollTimer_Tick;
            _scrollTimer.Start();

            MainTextBox = mainTextBox;

            RemoteHandler.RemoteButtonPressed += RemoteButtonPressed;
        }

        public ObservableCollection<Bookmark> Bookmarks
        {
            get { return _bookmarks; }
            set
            {
                _bookmarks = value;
                Changed(() => Bookmarks);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            KillWordPadProcess();

            if (_talentWindow != null)
            {
                _talentWindow.Close();
            }
        }

        #endregion

        private void _scrollTimer_Tick(object sender, EventArgs e)
        {
            _ticksElapsed++;
            if (!Paused)
            {
                MainScrollerVerticalOffset = MainScrollerVerticalOffset + Speed;
            }
            else
            {
                MainScrollerVerticalOffset = MainScrollerVerticalOffset +
                                             TotalBoostAmount;
            }


            //Only update calculations every 10 timer ticks (100 ms)
            if (_ticksElapsed%10 == 0)
            {
                //Calculate pixels per second (velocity)
                if (DateTime.Now - _prevTime > TimeSpan.FromSeconds(1))
                {
                    CalcEta();
                }

                PercentComplete = ((MainScrollerVerticalOffset + EyelinePosition)/
                                   (MainScrollerExtentHeight + EyelinePosition))*100;
            }
        }


        private void CalcEta()
        {
            TimeSpan diff = DateTime.Now - _prevTime;
            double pixelChange = (MainScrollerVerticalOffset - _prevScrollOffset);
            _pixelsPerSecond = pixelChange/diff.TotalSeconds;

            double pixelsToGo = MainScrollerExtentHeight - MainScrollerVerticalOffset;


            if (pixelsToGo == 0)
            {
                TimeRemaining = TimeSpan.FromSeconds(0).ToString();
                return;
            }

            double secondsToDone = pixelsToGo/_pixelsPerSecond;

            _eta = new TimeSpan(0, 0, (int) secondsToDone);

            TimeRemaining = _eta >= TimeSpan.FromSeconds(0) ? _eta.ToString() : "N/A";


            _prevTime = DateTime.Now;
            _prevScrollOffset = MainScrollerVerticalOffset;
        }

        public void InitializeConfig()
        {
            _configInitialized = true;

            Speed = DefaultSpeed = Settings.Default.Speed;


            DocumentPath = Settings.Default.DocumentPath;
            if (!string.IsNullOrWhiteSpace(_documentPath) && File.Exists(DocumentPath))
            {
                LoadDocument(DocumentPath);
            }
            else
            {
                //Load default text
                using (var ms = new MemoryStream(Encoding.Default.GetBytes(Resources.Proverbs_1)))
                {
                    DocumentHelpers.LoadDocument(ms, MainDocument, DataFormats.Rtf);
                }
            }


            SetDocumentConfig();

            FlipTalentWindowVert = Settings.Default.FlipTalentWindowVert;

            FlipTalentWindowHoriz = Settings.Default.FlipTalentWindowHoriz;

            FlipMainWindowVert = Settings.Default.FlipMainWindowVert;

            FlipMainWindowHoriz = Settings.Default.FlipMainWindowHoriz;

            TalentWindowLeft = Settings.Default.TalentWindowLeft;

            TalentWindowTop = Settings.Default.TalentWindowTop;

            TalentWindowWidth = Settings.Default.TalentWindowWidth;

            TalentWindowHeight = Settings.Default.TalentWindowHeight;

            if (Settings.Default.TalentWindowVisible)
            {
                ToggleTalentWindow();
            }

            MainWindowState = Settings.Default.MainWindowState;

            TalentWindowState = Settings.Default.TalentWindowState;

            EyelinePosition = Settings.Default.EyeLinePosition;
        }

        public void SetDocumentConfig()
        {
            string colorScheme = Settings.Default.ColorScheme;
            if (colorScheme != null && colorScheme.ToLowerInvariant() == "whiteonblack")
            {
                if (IsWhiteOnBlack)
                {
                    SetWhiteOnBlack();
                }
                IsWhiteOnBlack = true;
                IsBlackOnWhite = false;
            }
            else
            {
                if (IsBlackOnWhite)
                {
                    SetBlackOnWhite();
                }
                IsBlackOnWhite = true;
                IsWhiteOnBlack = false;
            }

            FontSize = Settings.Default.FontSize;

            LineHeight = Settings.Default.LineHeight;


            LoadBookmarks(MainDocument);
        }

        public void LoadDocument(string fullFilePath)
        {
            try
            {
                string ext = Path.GetExtension(fullFilePath).ToLowerInvariant();
                string dataFormat = DataFormats.Rtf;
                if (ext.EndsWith("xaml"))
                {
                    dataFormat = DataFormats.Xaml;
                }
                else if (ext.EndsWith("txt"))
                {
                    dataFormat = DataFormats.Text;
                }

                using (var fStream = new FileStream(fullFilePath, FileMode.Open))
                {
                    LoadDocument(fStream, dataFormat);
                }

                if (fullFilePath == DocumentPath)
                {
                    IsDocumentDirty = false;
                }

                WatchDocumentForChanges(_documentPath, Document_Changed);
            }

            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }

        private void LoadDocument(Stream documentStream, string dataFormat)
        {
            documentStream.Seek(0, SeekOrigin.Begin);
            DocumentHelpers.LoadDocument(documentStream, MainDocument, dataFormat);
            SetDocumentConfig();
            SetColorScheme();
        }


        public void SaveDocument(string fullFilePath)
        {
            TextRange range;

            FileStream fStream;

            try
            {
                UnWatchDocumentForChanges(fullFilePath, Document_Changed);

                range = new TextRange(MainDocument.ContentStart, MainDocument.ContentEnd);

                using (fStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    DocumentHelpers.SaveDocument(fStream, MainDocument, DataFormats.Rtf);
                }
                if (fullFilePath == DocumentPath)
                {
                    IsDocumentDirty = false;
                }
                string xamlPath = Path.Combine(Path.GetDirectoryName(fullFilePath),
                                               Path.GetFileNameWithoutExtension(fullFilePath) + ".xaml");

                using (fStream = new FileStream(xamlPath, FileMode.Create))
                {
                    DocumentHelpers.SaveDocument(fStream, MainDocument, DataFormats.Xaml);
                }
            }
            finally
            {
                WatchDocumentForChanges(fullFilePath, Document_Changed);
            }
        }

        private void WatchDocumentForChanges(string fullFilePath, Action<object, FileSystemEventArgs> onChangedAction)
        {
            if (!WatchedFiles.ContainsKey(fullFilePath))
            {
                var fsw = new FileSystemWatcher();


                fsw.BeginInit();
                fsw.Path = Path.GetDirectoryName(fullFilePath);
                fsw.Filter = Path.GetFileName(fullFilePath);
                fsw.IncludeSubdirectories = false;
                fsw.NotifyFilter = NotifyFilters.LastWrite;


                fsw.Changed += onChangedAction.Invoke;
                fsw.EnableRaisingEvents = true;
                fsw.EndInit();

                WatchedFiles.Add(fullFilePath, fsw);
            }
        }

        private void UnWatchDocumentForChanges(string fullFilePath, Action<object, FileSystemEventArgs> onChangedAction)
        {
            if (WatchedFiles.ContainsKey(fullFilePath))
            {
                WatchedFiles[fullFilePath].Changed -= onChangedAction.Invoke;
                WatchedFiles[fullFilePath].EnableRaisingEvents = false;
                WatchedFiles[fullFilePath].Dispose();
                WatchedFiles.Remove(fullFilePath);
            }
        }

        private void Document_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                ChangeSemaphore.Wait();
                var storeStream = new MemoryStream();

                using (FileStream filestream = File.OpenRead(e.FullPath))
                {
                    storeStream.SetLength(filestream.Length);
                    filestream.Read(storeStream.GetBuffer(), 0, (int) filestream.Length);
                    storeStream.Flush();
                }

                Application.Current.Dispatcher.Invoke((Action) (() =>
                                                                    {
                                                                        DocumentHelpers.LoadDocument(storeStream,
                                                                                                     MainDocument,
                                                                                                     DataFormats.Rtf);
                                                                        storeStream.Dispose();
                                                                        SetDocumentConfig();
                                                                    }));
            }
            catch (Exception)
            {
            }
            finally
            {
                ChangeSemaphore.Release();
            }
        }


        private void SetColorScheme()
        {
            if (IsBlackOnWhite)
            {
                SetBlackOnWhite();
            }
            else if (IsWhiteOnBlack)
            {
                SetWhiteOnBlack();
            }
        }

        private void SetWhiteOnBlack()
        {
            MainDocument.Background = Brushes.Black;

            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.ForegroundProperty, Brushes.White,
                                                Brushes.Black);
            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.BackgroundProperty, Brushes.Black,
                                                Brushes.White);


            if (_configInitialized)
                AppConfigHelper.SetUserSetting("ColorScheme", "WhiteOnBlack");

            MainDocumentCaretBrush = Brushes.White;
        }

        private void SetBlackOnWhite()
        {
            MainDocument.Background = Brushes.White;

            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.ForegroundProperty, Brushes.Black,
                                                Brushes.White);
            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.BackgroundProperty, Brushes.White,
                                                Brushes.Black);
            if (_configInitialized)
                AppConfigHelper.SetUserSetting("ColorScheme", "BlackOnWhite");

            MainDocumentCaretBrush = Brushes.Black;
        }

        public void EditInWordpad()
        {
            KillWordPadProcess();
            //Cancelled from saving document
            if (string.IsNullOrWhiteSpace(DocumentPath))
            {
                return;
            }

            TempDocumentPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(DocumentPath));


            using (var ms = new MemoryStream())
            {
                DocumentHelpers.SaveDocument(ms, MainDocument, DataFormats.Rtf);

                // SaveDocument(_tempDocumentPath);


                var tempDoc = new FlowDocument();
                DocumentHelpers.LoadDocument(ms, tempDoc, DataFormats.Rtf);
                ConvertDocumentToEditableFormat(tempDoc);
                using (FileStream tempFileStream = File.OpenWrite(_tempDocumentPath))
                {
                    DocumentHelpers.SaveDocument(tempFileStream, tempDoc, DataFormats.Rtf);
                }
            }


            WatchDocumentForChanges(_tempDocumentPath, Document_Changed);

            var info = new ProcessStartInfo();
            info.Arguments = string.Format("\"{0}\"", _tempDocumentPath);
            info.FileName = "wordpad.exe";
            _wordpadProcess = Process.Start(info);
        }

        public static void ConvertDocumentToEditableFormat(FlowDocument document)
        {
            DocumentHelpers.ChangePropertyValue(document, TextElement.FontSizeProperty, (double) 12);

            DocumentHelpers.ChangePropertyValue(document, TextElement.ForegroundProperty, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(document, TextElement.BackgroundProperty, Brushes.White, Brushes.Black);
        }

        private void LoadBookmarks(FlowDocument document)
        {
            Bookmarks.Clear();
            IEnumerable<Hyperlink> hyperlinks = document.GetLogicalChildren<Hyperlink>(true);
            foreach (Hyperlink hyperlink in hyperlinks)
            {
                AddBookmarkFromHyperlink(hyperlink);
            }
        }

        private void AddBookmarkFromHyperlink(Hyperlink hyperlink)
        {
            if (hyperlink.NavigateUri.IsAbsoluteUri && hyperlink.NavigateUri.Host.StartsWith("bookmark"))
            {
                var bm = new Bookmark();

                bm.Name = Uri.UnescapeDataString(hyperlink.NavigateUri.Segments[1]);
                bm.Hyperlink = hyperlink;


                Bookmarks.Add(bm);

                bm.Ordinal = Bookmarks.Count;
                //bm.Image = (hyperlink.Inlines.FirstInline as InlineUIContainer).Child as Image;
                // bm.Image.Height = FontSizeSlider.Value;
            }
        }


        private void SaveDocumentAs(string documentPath)
        {
            var dlg = new SaveFileDialog();

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

            bool? result = dlg.ShowDialog();
            // Process save file dialog box results 

            if (result == true)
            {
                // Save document 

                DocumentPath = dlg.FileName;

                if (!string.IsNullOrWhiteSpace(DocumentPath))
                {
                    SaveDocument(DocumentPath);
                }
            }
        }

        private void LoadDocumentDialog(string documentPath)
        {
            var dlg = new OpenFileDialog();

            if (!string.IsNullOrWhiteSpace(documentPath))
            {
                dlg.FileName = Path.GetFileName(documentPath);
                dlg.InitialDirectory = Path.GetDirectoryName(documentPath);
                dlg.Multiselect = false;
                dlg.Title = "Load document for Proverb Teleprompter...";
            }


            dlg.DefaultExt = ".rtf"; // Default file extension 

            dlg.Filter = "Rich Text Documents|*.rtf|XAML Documents|*.xaml|Text Documents|*.txt";
                // Filter files by extension 

            // Show save file dialog box 

            bool? result = dlg.ShowDialog();
            // Process open file dialog box results 

            if (result == true)
            {
                // Load document 

                DocumentPath = dlg.FileName;

                if (!string.IsNullOrWhiteSpace(DocumentPath))
                {
                    LoadDocument(DocumentPath);
                }
            }
        }


        private void _editWindow_DocumentUpdated(object sender, DocumentUpdatedEventArgs e)
        {
            Dispatcher.Invoke((Action) (() => LoadDocument(e.DocumentData, e.DataFormat)));
        }

        private void InsertBookmarkAtCurrentEyelineMark()
        {
            double bookmarkOffset = MainScrollerVerticalOffset + EyelinePosition;

            TextPointer pos = MainTextBox.GetPositionFromPoint(new Point(0, bookmarkOffset), true);


            int num = DocumentHelpers.GetLineNumberFromSelection(pos);

            var hyperlink = new Hyperlink(pos, pos);

            var bm = new Bookmark();
            if (BookmarkImage != null)
            {
                var img = new Image();
                img.Source = BookmarkImage;

                img.Visibility = Visibility.Collapsed;
                bm.Image = img;
                hyperlink.Inlines.Add(" ");
            }

            bm.Name = string.Format("Boomark {0}", Bookmarks.Count + 1);
            bm.Line = num;
            bm.TopOffset = bookmarkOffset;


            hyperlink.NavigateUri = new Uri(String.Format("http://bookmark/{0}", bm.Name));

            bm.Hyperlink = hyperlink;
            bm.Position = pos;

            Bookmarks.Add(bm);
            bm.Ordinal = Bookmarks.Count;
        }

        private void JumpToBookmark(Bookmark bookmark)
        {
            if (bookmark == null) return;
            bookmark.Hyperlink.BringIntoView();

            SelectedBookmark = bookmark;
        }

        private void JumpToBookmarkByOrdinal(int ordinal)
        {
            int ct = 0;
            foreach (Bookmark bookmark in Bookmarks)
            {
                ct++;
                if (ct == ordinal)
                {
                    JumpToBookmark(bookmark);
                    return;
                }
            }
        }

        public bool CanShutDownApp()
        {
            if (IsDocumentDirty)
            {
                string caption = "The document has unsaved changes, would you like to save them?";
                if (!string.IsNullOrWhiteSpace(DocumentPath))
                {
                    caption = string.Format("The document: {0} has unsaved changed, do you want to save them?",
                                            DocumentPath);
                }
                MessageBoxResult result = MessageBox.Show(caption, caption, MessageBoxButton.YesNoCancel,
                                                          MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    SaveDocument(DocumentPath);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }

            return true;
        }

        private void KillWordPadProcess()
        {
            if (_wordpadProcess != null && !_wordpadProcess.HasExited)
            {
                _wordpadProcess.CloseMainWindow();
                _wordpadProcess.Close();
            }
        }

        public void ToggleToolsWindow()
        {
            if (_toolsWindow == null)
            {
                _toolsWindow = new ToolsWindow();
                _toolsWindow.DataContext = this;
                _toolsWindow.Owner = Application.Current.MainWindow;
                // _toolsWindow.ShowActivated = false;
                _toolsWindow.PreviewKeyDown += KeyDown;
                _toolsWindow.PreviewKeyUp += KeyUp;
                _toolsWindow.Closing += _toolsWindow_Closing;
                _toolsWindow.Loaded += _toolsWindow_Loaded;

                //   _toolsWindow.SizeChanged += (sender, e) => SetToolsWindowSize(new Size(MainWindowWidth, MainWindowHeight));
                // _toolsWindow.LocationChanged += (sender, e) => SetToolsWindowLocation(new Point(MainWindowLeft, MainWindowTop));
            }

            if (_toolsWindow.Visibility == Visibility.Visible)
            {
                _toolsWindow.Visibility = Visibility.Collapsed;
                _toolsWindow.Hide();
            }
            else
            {
                _toolsWindow.Show();


                //       SetToolsWindowSize(new Size(MainWindowWidth, MainWindowHeight));
                //     SetToolsWindowLocation(new Point(MainWindowLeft, MainWindowTop));
            }
        }

        private void _toolsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _toolsWindow.WindowState = WindowState.Maximized;
        }


        private static void _toolsWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            ((Window) sender).Owner.Close();
        }


        private void SetToolsWindowSize(Size size)
        {
            if (_toolsWindow == null) return;
            //   LocationChanged -= MainWindow_LocationChanged;
            //    SizeChanged -= MainWindow_SizeChanged;
            // _toolsWindow.Top = Top + ActualHeight - _toolsWindow.Height;
            // _toolsWindow.Width = MainWindowWidth;
            //  _toolsWindow.Left = Left;
            //    LocationChanged += MainWindow_LocationChanged;
            //   SizeChanged += MainWindow_SizeChanged;
        }

        private void SetToolsWindowLocation(Point location)
        {
            if (_toolsWindow == null) return;

            //   LocationChanged -= MainWindow_LocationChanged;
            //  SizeChanged -= MainWindow_SizeChanged;
            // _toolsWindow.Top = MainWindowTop + MainWindowHeight - _toolsWindow.Height;
            // _toolsWindow.Width = MainWindowWidth;
            // _toolsWindow.Left = MainWindowLeft;
            //   LocationChanged += MainWindow_LocationChanged;
            //   SizeChanged += MainWindow_SizeChanged;
        }

        #region Input Handlers

        internal void KeyDown(object sender, KeyEventArgs e)
        {
            if(Editable.GetValueOrDefault()) return;

            e.Handled = true;
            if (e.Key == Key.Down)
            {
                SpeedForward();
            }
            else if (e.Key == Key.Up )
            {
                SpeedReverse();
            }
            else if (e.Key == Key.Tab)
            {
                ToggleTools();
            }

                //Slide forward / page down button To work with Logitech PowerPoint remote
            else if (e.Key == Key.Next)
            {
                //PageDown();
                SpeedForward();
            }
                //Slid back button / page up To work with Logitech PowerPoint remote
            else if (e.Key == Key.Prior)
            {
                //PageUp();
                SpeedReverse();
            }
                //F5 To work with Logitech PowerPoint remote
            else if (e.Key == Key.F5 ||
                     e.Key == Key.MediaStop ||
                     e.Key == Key.MediaPlayPause ||
                     e.Key == Key.Escape)
            {
                PauseScrolling();
            }
                //Period To work with Logitech PowerPoint remote
            else if (e.Key == Key.OemPeriod)
            {
                ScrollToTop();
            }
            else if (e.Key == Key.MediaPreviousTrack)
            {
                PageUp();
            }
            else if (e.Key == Key.MediaNextTrack)
            {
                PageDown();
            }
            else if(e.Key == Key.OemPlus )
            {
                Speed += 0.1;
                DefaultSpeed = Speed;
            }
            else if (e.Key == Key.OemMinus)
            {
                Speed -= 0.1;
                DefaultSpeed = Speed;
            }
                //Numbers 1-9 should jump to the corresponding bookmark
            else if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                     (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                var converter = new KeyConverter();
                string val = converter.ConvertToString(e.Key);

                JumpToBookmarkByOrdinal(int.Parse(val));

                //To allow text boxes to get numbers
                e.Handled = false;
            }
            else if (e.Key == Key.F1)
            {
                LoadRandomBibleChapter();
            }
            else if (e.Key == Key.Insert)
            {
                InsertBookmarkAtCurrentEyelineMark();
            }
            else
            {
                e.Handled = false;
            }
        }


        internal void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                Speed = DefaultSpeed;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Up)
            {
                Speed = DefaultSpeed;
                // SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Next)
            {
                Speed = DefaultSpeed;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (e.Key == Key.Prior)
            {
                //SpeedSlider.Value -= TotalBoostAmount;
                Speed = DefaultSpeed;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
        }

        internal void RemoteButtonPressed(object sender, RemoteButtonPressedEventArgs e)
        {
            if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_FASTFORWARD)
            {
                Speed++;
            }
            else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_REWIND)
            {
                Speed--;
            }
            else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PLAY_PAUSE ||
                     e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PAUSE)
            {
                PauseScrolling();
            }
            else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PLAY)
            {
                ResumeScrolling();
            }
            else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_PREVIOUSTRACK)
            {
                ScrollToTop();
            }
            else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_CHANNEL_UP)
            {
                PageUp();
            }
            else if (e.AppCommand == WM_APPCOMMANDCommands.APPCOMMAND_MEDIA_CHANNEL_DOWN)
            {
                PageDown();
            }
        }

        #endregion

        #region Scroll Control

        private void PageDown()
        {
            MainScrollerVerticalOffset = MainScrollerVerticalOffset + MainScrollerViewportHeight -
                                         MainScrollerViewportHeight*0.5;
        }

        private void PageUp()
        {
            MainScrollerVerticalOffset = MainScrollerVerticalOffset - MainScrollerViewportHeight -
                                         MainScrollerViewportHeight*0.5;
        }


        private void PauseScrolling()
        {
            Paused = !Paused;
        }

        private void ResumeScrolling()
        {
            Paused = false;
        }

        private void ScrollToTop()
        {
            MainScrollerVerticalOffset = 0;
        }

        private void ToggleTools()
        {
            if (_toolsVisible)
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
            ToolsVisible = true;
            //Storyboard sb = (Storyboard)this.FindResource("ToolFlyin");
            //sb.Begin();
        }

        private void HideTools()
        {
            ToolsVisible = false;
            //   Storyboard sb = (Storyboard)this.FindResource("ToolFlyout");
            // sb.Begin();

            Editable = false;
        }

        private void SpeedForward()
        {
            _speedBoostAmount = 2;
            if (Speed < 0)
            {
                _speedBoostAmount = 2 + Math.Abs(Speed);
            }

            if (_speedBoostAmount > SpeedMax)
            {
                _speedBoostAmount = SpeedMax;
            }

            TotalBoostAmount += _speedBoostAmount;
            Speed += _speedBoostAmount;
        }

        private void SpeedReverse()
        {
            _speedBoostAmount = -2;

            if (Speed > 0)
            {
                _speedBoostAmount = -2 - Speed;
            }

            if (_speedBoostAmount < SpeedMin)
            {
                _speedBoostAmount = SpeedMin;
            }

            TotalBoostAmount += _speedBoostAmount;


            Speed += _speedBoostAmount;
        }

        #endregion

        #region Talent Window Methods

        private void ToggleTalentWindow()
        {
            if (SystemInformation.MonitorCount <= 1)
            {
                MessageBox.Show(
                    "A second monitor was not detected.  If a second monitor is connected, try restarting the application",
                    "Second Monitor Not Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (_talentWindow == null &&
                     SystemInformation.MonitorCount > 1)
            {
                _talentWindow = new TalentWindow {Owner = Application.Current.MainWindow};
                _talentWindow.Closed += _talentWindow_Closed;
                _talentWindow.PreviewKeyDown += KeyDown;
                _talentWindow.PreviewKeyUp += KeyUp;

                _talentWindow.Loaded += _talentWindow_Loaded;
                _talentWindow.DataContext = this;

                _talentWindow.Show();


                ToggleTalentWindowCaption = "Hide on 2nd Monitor";
                AppConfigHelper.SetUserSetting("TalentWindowVisible", true);
            }
            else
            {
                HideTalentWindow();
                AppConfigHelper.SetUserSetting("TalentWindowVisible", false);
            }
        }

        private void _talentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (SystemInformation.MonitorCount <= 1)
            {
                _talentWindow.WindowState = WindowState.Normal;
                return;
            }

            Rectangle workingArea = Screen.AllScreens[1].WorkingArea;

            _talentWindow.Left = PixelConverter.ToUnits(workingArea.Left);
            _talentWindow.Top = PixelConverter.ToUnits(workingArea.Top);
        }

        private void _talentWindow_Closed(object sender, EventArgs e)
        {
            _talentWindow = null;
            HideTalentWindow();
        }

        private void HideTalentWindow()
        {
            if (_talentWindow != null)
            {
                _talentWindow.Close();
            }
            ToggleTalentWindowCaption = "Show on 2nd monitor";
        }

        #endregion
    }
}