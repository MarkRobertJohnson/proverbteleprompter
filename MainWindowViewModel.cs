using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Presentation.Commands;
using ProverbTeleprompter.HtmlConverter;
using Tools.API.Messages.lParam;

namespace ProverbTeleprompter
{
    public class MainWindowViewModel : NotifyPropertyChangedBase, IDisposable
    {
        private DispatcherTimer _scrollTimer;
        private double _pixelsPerSecond;
        private DateTime _prevTime = DateTime.Now;
        private double _prevScrollOffset;
        private TimeSpan _eta;
        private int _ticksElapsed;
        private EditWindow _editWindow = null;
        private TalentWindow _talentWindow;
        private double _speedBoostAmount = 0;
            

        public MainWindowViewModel(RichTextBox mainTextBox)
        {
            _scrollTimer = new DispatcherTimer();
            _scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);
            _scrollTimer.IsEnabled = true;
            _scrollTimer.Tick += new EventHandler(_scrollTimer_Tick);
            _scrollTimer.Start();

            MainTextBox = mainTextBox;
        }

        void _scrollTimer_Tick(object sender, EventArgs e)
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
            if (_ticksElapsed % 10 == 0)
            {
                //Calculate pixels per second (velocity)
                if (DateTime.Now - _prevTime > TimeSpan.FromSeconds(1))
                {
                    CalcEta();

                }

                var eyeLineOffset = MainScrollerViewportHeight - EyelinePosition;

                PercentComplete = ((MainScrollerVerticalOffset + MainScrollerViewportHeight -
                                                        eyeLineOffset) / MainScrollerExtentHeight) * 100;
            }


        }


        private void CalcEta()
        {
            var diff = DateTime.Now - _prevTime;
            var pixelChange = (MainScrollerVerticalOffset - _prevScrollOffset);
            _pixelsPerSecond = pixelChange / diff.TotalSeconds;

            var pixelsToGo = MainScrollerExtentHeight - MainScrollerVerticalOffset;


            if (pixelsToGo == 0)
            {
                TimeRemaining = TimeSpan.FromSeconds(0).ToString();
                return;
            }

            var secondsToDone = pixelsToGo / _pixelsPerSecond;

            _eta = new TimeSpan(0, 0, (int)secondsToDone);

            TimeRemaining = _eta >= TimeSpan.FromSeconds(0) ? _eta.ToString() : "N/A";



            _prevTime = DateTime.Now;
            _prevScrollOffset = MainScrollerVerticalOffset;
        }

        Dictionary<string, FileSystemWatcher> _watchedFiles = new Dictionary<string, FileSystemWatcher>();
        
        public Dictionary<string, FileSystemWatcher> WatchedFiles
        {
            get { return _watchedFiles; }
            set
            {
                _watchedFiles = value;
                Changed(() => WatchedFiles);
            }
        }

        #region Bindable Properties

        private bool? _editable = false;

        public bool? Editable
        {
            get { return _editable; }
            set
            {
                _editable = value;
                Changed(() => Editable);
            }
        }

        private FlowDocument _mainDocument;
        public FlowDocument MainDocument
        {
            get
            {
                if(_mainDocument == null)
                {
                    MainDocument = new FlowDocument
                    {
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                    };
                    
                }
                return _mainDocument;
            }
            set
            {
                _mainDocument = value;
                Changed(() => MainDocument);
                
            }
        }




        public void LoadRandomBibleChapter()
        {
            MainDocument = HtmlToXamlConverter.ConvertHtmlToXaml(BibleHelpers.GetRandomBibleChapterHtml());
            MainDocument.ContentStart.InsertLineBreak();
            MainDocument.ContentStart.InsertLineBreak();
            MainDocument.ContentStart.InsertLineBreak();

            SetDocumentConfig();
        }

        private RichTextBox _mainTextBox;
        public RichTextBox MainTextBox
        {
            get { return _mainTextBox; }
            set
            {
                _mainTextBox = value;
                Changed(()=>MainTextBox);
            }
        }

        private bool _isWhiteOnBlack = false;
        public bool IsWhiteOnBlack
        {
            get { return _isWhiteOnBlack; }
            set
            {
                _isWhiteOnBlack = value;

                if (_isWhiteOnBlack)
                {
                    SetWhiteOnBlack();
                }
                Changed(() => IsWhiteOnBlack);
            }

        }

        private bool _toolsVisible;
        public bool ToolsVisible
        {
            get { return _toolsVisible; }
            set
            {
                _toolsVisible = value;
                Changed(() => ToolsVisible);
            }
        }

        private bool _isBlackOnWhite = true;
        public bool IsBlackOnWhite
        {
            get { return _isBlackOnWhite; }
            set
            {
                _isBlackOnWhite = value;
                if(_isBlackOnWhite)
                {
                    SetBlackOnWhite();
                }
                Changed(() => IsBlackOnWhite);
            }
        }

        private double _defaultSpeed;
        public double DefaultSpeed
        {
            get { return _defaultSpeed; }
            set
            {
                _defaultSpeed = value;
                Changed(() => DefaultSpeed);
            }
        }

        private double _speed;
        [DependsUpon("SpeedMax")]
        [DependsUpon("SpeedMin")]
        public double Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                if(_speed < SpeedMin)
                {
                    _speed = SpeedMin;
                }
                if (_speed > SpeedMax)
                {
                    _speed = SpeedMax;
                }

                if (_configInitialized && value != 0)
                    AppConfigHelper.SetAppSetting("Speed", _speed.ToString());

                Changed(() => Speed);
            }
        }

        private double _speedMax = 8;
        public double SpeedMax
        {
            get { return _speedMax; }
            set
            {
                _speedMax = value;
                Changed(() => SpeedMax);
            }
        }

        private double _speedMin= -8;
        public double SpeedMin
        {
            get { return _speedMin; }
            set
            {
                _speedMin = value;
                Changed(() => SpeedMin);
            }
        }

        private double _desiredSpeed;

        /// <summary>
        /// The speed to return to when un-pausing
        /// </summary>
        public double DesiredSpeed
        {
            get { return _desiredSpeed; }
            set
            {
                _desiredSpeed = value;
                Changed(() => DesiredSpeed);
            }
        }

        private double _totalBoostAmount;
        public double TotalBoostAmount
        {
            get { return _totalBoostAmount; }
            set
            {
                _totalBoostAmount = value;
                if (value > SpeedMax)
                {
                    _totalBoostAmount = SpeedMax;
                }
                if (value < SpeedMin)
                {
                    _totalBoostAmount = SpeedMin;
                }
                Changed(() => TotalBoostAmount);
            }
        }

        private string _documentPath;
        public string DocumentPath
        {
            get { return _documentPath; }
            set
            {
                //No longer watch the old document
                if (value != _documentPath && _documentPath != null)
                {
                    UnWatchDocumentForChanges(_documentPath,
                        Document_Changed);
                }

                _documentPath = value;
                Changed(() => DocumentPath);
                AppConfigHelper.SetAppSetting("DocumentPath", DocumentPath);
            }
        }

        private string _tempDocumentPath;
        public string TempDocumentPath
        {
            get { return _tempDocumentPath; }
            set
            {
                _tempDocumentPath = value;
                Changed(() => TempDocumentPath);
            }
        }

        private bool _flipTalentWindowVert;
        public bool FlipTalentWindowVert
        {
            get { return _flipTalentWindowVert; }
            set
            {
                _flipTalentWindowVert = value;
                TalentWindowScaleY = FlipTalentWindowVert ? -1 : 1;
                AppConfigHelper.SetAppSetting("FlipTalentWindowVert", FlipTalentWindowVert.ToString());
                Changed(() => FlipTalentWindowVert);
            }
        }

        private bool _flipTalentWindowHoriz;
        public bool FlipTalentWindowHoriz
        {
            get { return _flipTalentWindowHoriz; }
            set
            {
                _flipTalentWindowHoriz = value;
                TalentWindowScaleX = FlipTalentWindowHoriz ? -1 : 1;
                AppConfigHelper.SetAppSetting("FlipTalentWindowHoriz", FlipTalentWindowHoriz.ToString());
                Changed(() => FlipTalentWindowHoriz);
            }
        }

        private bool _flipMainWindowVert;
        public bool FlipMainWindowVert
        {
            get { return _flipMainWindowVert; }
            set
            {
                _flipMainWindowVert = value;
                MainWindowScaleY = FlipMainWindowVert ? -1 : 1;
                AppConfigHelper.SetAppSetting("FlipMainWindowVert", FlipMainWindowVert.ToString());
                Changed(() => FlipMainWindowVert);
            }
        }

        private bool _flipMainWindowHoriz;
        public bool FlipMainWindowHoriz
        {
            get { return _flipMainWindowHoriz; }
            set
            {
                _flipMainWindowHoriz = value;
                MainWindowScaleX = FlipMainWindowHoriz ? -1 : 1;
                AppConfigHelper.SetAppSetting("FlipMainWindowHoriz", FlipMainWindowHoriz.ToString());
                Changed(() => FlipMainWindowHoriz);
            }
        }

        private double _talentWindowTop;
        public double TalentWindowTop
        {
            get { return _talentWindowTop; }
            set
            {
                _talentWindowTop = value;
                Changed(() => TalentWindowTop);

                AppConfigHelper.SetAppSetting("TalentWindowTop", _talentWindowTop.ToString());
            }
        }

        private double _talentWindowLeft;
        public double TalentWindowLeft
        {
            get { return _talentWindowLeft; }
            set
            {
                _talentWindowLeft = value;
                Changed(() => TalentWindowLeft);
                AppConfigHelper.SetAppSetting("TalentWindowLeft", _talentWindowLeft.ToString());
                
            }
        }

        private double _talentWindowHeight;
        public double TalentWindowHeight
        {
            get { return _talentWindowHeight; }
            set
            {
                _talentWindowHeight = value;
                Changed(() => TalentWindowHeight);

                AppConfigHelper.SetAppSetting("TalentWindowHeight", _talentWindowHeight.ToString());
            }
        }

        private double _talentWindowWidth;
        public double TalentWindowWidth
        {
            get { return _talentWindowWidth; }
            set
            {
                _talentWindowWidth = value;
                Changed(() => TalentWindowWidth);
                AppConfigHelper.SetAppSetting("TalentWindowWidth", _talentWindowWidth.ToString());
            }
        }


        private double _eyelinePosition;
        public double EyelinePosition
        {
            get { return _eyelinePosition; }
            set
            {
                if (value < 0) value = 0;
                _eyelinePosition = value;
                Changed(() => EyelinePosition);
            }
        }

        private Brush _mainDocumentCaretBrush;
        public Brush MainDocumentCaretBrush
        {
            get { return _mainDocumentCaretBrush; }
            set
            {
                _mainDocumentCaretBrush = value;
                Changed(() => MainDocumentCaretBrush);
            }
        }

        private bool _paused;
        public bool Paused
        {
            get { return _paused; }
            set
            {
                _paused = value;
                if(Paused)
                {
                    DesiredSpeed = Speed;
                    Speed = 0;
                }
                else
                {
                    Speed = DesiredSpeed;
                }
                Changed(()=>Paused);
            }
        }

        private double _percentComplete;
        public double PercentComplete
        {
            get { return _percentComplete; }
            set
            {
                _percentComplete = value;
                Changed(() => PercentComplete);
            }
        }

        private string _timeRemaining;
        public string TimeRemaining
        {
            get { return _timeRemaining; }
            set
            {
                _timeRemaining = value;
                Changed(() => TimeRemaining);
            }
        }

        private double _mainScrollerVerticalOffset;
        public double MainScrollerVerticalOffset
        {
            get { return _mainScrollerVerticalOffset; }
            set
            {
                if (value < 0) value = 0;
                if (value > MainScrollerExtentHeight) value = MainScrollerExtentHeight;

                _mainScrollerVerticalOffset = value;

                Changed(() => MainScrollerVerticalOffset);
            }
        }

        private double _mainScrollerExtentHeight;
        public double MainScrollerExtentHeight
        {
            get { return _mainScrollerExtentHeight; }
            set
            {
                _mainScrollerExtentHeight = value;
                Changed(() => MainScrollerExtentHeight);
            }
        }

        private double _mainScrollerViewportHeight;

        public double MainScrollerViewportHeight
        {
            get { return _mainScrollerViewportHeight; }
            set
            {
                _mainScrollerViewportHeight = value;
                Changed(() =>MainScrollerViewportHeight);
            }
        }

        private double _mainWindowScaleX =1;
        [DependsUpon("FlipMainWindowHoriz")]
        public double MainWindowScaleX
        {
            get { return _mainWindowScaleX; }
            set
            {
                _mainWindowScaleX = value;
                Changed(() => MainWindowScaleX);
            }
        }

        private double _mainWindowScaleY =1;
        [DependsUpon("FlipMainWindowVert")]
        public double MainWindowScaleY
        {
            get { return _mainWindowScaleY; }
            set
            {
                _mainWindowScaleY = value;
                Changed(() => MainWindowScaleY);
            }
        }

        private double _talentWindowScaleX =1;
        [DependsUpon("FlipTalentWindowHoriz")]
        public double TalentWindowScaleX
        {
            get { return _talentWindowScaleX; }
            set
            {
                _talentWindowScaleX = value;
                Changed(() => TalentWindowScaleX);
            }
        }

        private double _talentWindowScaleY =1;
        [DependsUpon("FlipTalentWindowVert")]
        public double TalentWindowScaleY
        {
            get { return _talentWindowScaleY; }
            set
            {
                _talentWindowScaleY = value;
                Changed(() => TalentWindowScaleY);
            }
        }

        private double _fontSize;
        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;

                DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.FontSizeProperty, FontSize);
                if (_configInitialized)
                    AppConfigHelper.SetAppSetting("FontSize", FontSize.ToString());

                Changed(() => FontSize);

            }
        }

        private double _lineHeight;
        [DependsUpon("FontSize")]
        public double LineHeight
        {
            get { return _lineHeight; }
            set
            {
                _lineHeight = value;

                MainDocument.SetValue(Block.LineHeightProperty, LineHeight * FontSize);
                if (_configInitialized)
                    AppConfigHelper.SetAppSetting("LineHeight", LineHeight.ToString());

                Changed(() => LineHeight);
            }
        }

        private Bookmark _selectedBookmark;
        public Bookmark SelectedBookmark
        {
            get { return _selectedBookmark; }
            set
            {
                _selectedBookmark = value;
                Changed(() => SelectedBookmark);
            }
        }

        private double _mainWindowHeight =300;
        public double MainWindowHeight
        {
            get { return _mainWindowHeight; }
            set
            {
                _mainWindowHeight = value;
                Changed(()=>MainWindowHeight);
            }
        }

        private double _mainWindowWidth = 300;
        public double MainWindowWidth
        {
            get { return _mainWindowWidth; }
            set
            {
                _mainWindowWidth = value;
                Changed(()=>MainWindowWidth);
            }
        }

        private double _mainWindowLeft = 0;
        public double MainWindowLeft
        {
            get { return _mainWindowLeft; }
            set
            {
                _mainWindowLeft = value;
                Changed(()=>MainWindowLeft);
            }
        }

        private double _mainWindowTop = 0;
        public double MainWindowTop
        {
            get { return _mainWindowTop; }
            set
            {
                _mainWindowTop = value;
                Changed(() => MainWindowTop);
            }
        }

        private ImageSource _boomarkImage;
        public ImageSource BookmarkImage
        {
            get { return _boomarkImage; }
            set
            {
                _boomarkImage = value;
                Changed(() => BookmarkImage);
            }
        }

        #endregion

        private bool _configInitialized = false;
        public void InitializeConfig()
        {
            _configInitialized = true;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (!File.Exists(config.FilePath))
            {
                AppConfigHelper.SetAppSetting("ColorScheme",
                    IsWhiteOnBlack == true ? "WhiteOnBlack" : "BlackOnWhite", config);

            }

            var speed = ConfigurationManager.AppSettings["Speed"];
            if (speed != null)
            {
                Speed = Double.Parse(speed);
                DefaultSpeed = Speed;
            }


            DocumentPath = ConfigurationManager.AppSettings["DocumentPath"];
            if (!string.IsNullOrWhiteSpace(_documentPath) && File.Exists(DocumentPath))
            {
                LoadDocument(DocumentPath);
            }
            else
            {
                //Load default text
                using (MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(Properties.Resources.Proverbs_1)))
                {
                    DocumentHelpers.LoadDocument(ms, MainDocument, DataFormats.Rtf);
                }
            }


            SetDocumentConfig();

            var value = ConfigurationManager.AppSettings["FlipTalentWindowVert"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipTalentWindowVert = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["FlipTalentWindowHoriz"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipTalentWindowHoriz = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["FlipMainWindowVert"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipMainWindowVert = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["FlipMainWindowHoriz"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                FlipMainWindowHoriz = bool.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowLeft"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                TalentWindowLeft = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowTop"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                TalentWindowTop = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowWidth"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                TalentWindowWidth = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["TalentWindowHeight"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                TalentWindowHeight = double.Parse(value);
            }

            value = ConfigurationManager.AppSettings["EyeLinePosition"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                EyelinePosition = double.Parse(value);
            }
        }

        public void SetDocumentConfig()
        {
            var colorScheme = ConfigurationManager.AppSettings["ColorScheme"];
            if (colorScheme != null && colorScheme.ToLowerInvariant() == "whiteonblack")
            {
                if (IsWhiteOnBlack == true)
                {
                    SetWhiteOnBlack();
                }
                IsWhiteOnBlack = true;
                IsBlackOnWhite = false;
            }
            else
            {
                if (IsBlackOnWhite == true)
                {
                    SetBlackOnWhite();
                }
                IsBlackOnWhite = true;
                IsWhiteOnBlack = false;

            }


            var fontSize = ConfigurationManager.AppSettings["FontSize"];
            if (fontSize != null)
            {
                FontSize = Double.Parse(fontSize);
            }


            var lineHeight = ConfigurationManager.AppSettings["LineHeight"];
            if (lineHeight != null)
            {
                LineHeight = Double.Parse(lineHeight);
            }

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

                using (FileStream fStream = new FileStream(fullFilePath, FileMode.Open))
                {
                    LoadDocument(fStream, dataFormat);
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
                ;
                string xamlPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullFilePath),
                                       System.IO.Path.GetFileNameWithoutExtension(fullFilePath) + ".xaml");

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

        private static SemaphoreSlim _changeSemaphore = new SemaphoreSlim(1);

        private void Document_Changed(object sender, FileSystemEventArgs e)
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

                App.Current.Dispatcher.Invoke((Action)(() =>
                {

                    DocumentHelpers.LoadDocument(storeStream, MainDocument, DataFormats.Rtf);
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
 
            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.ForegroundProperty, Brushes.White, Brushes.Black);
            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.BackgroundProperty, Brushes.Black, Brushes.White);


            if (_configInitialized)
                AppConfigHelper.SetAppSetting("ColorScheme", "WhiteOnBlack");

            MainDocumentCaretBrush = Brushes.White;
        }

        private void SetBlackOnWhite()
        {
            MainDocument.Background = Brushes.White;

            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.ForegroundProperty, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.BackgroundProperty, Brushes.White, Brushes.Black);
            if (_configInitialized)
                AppConfigHelper.SetAppSetting("ColorScheme", "BlackOnWhite");

            MainDocumentCaretBrush = Brushes.Black;
        }

        private Process _wordpadProcess;
        public void EditInWordpad()
        {

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


                FlowDocument tempDoc = new FlowDocument();
                DocumentHelpers.LoadDocument(ms, tempDoc, DataFormats.Rtf);
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

        public static void ConvertDocumentToEditableFormat(FlowDocument document)
        {
            DocumentHelpers.ChangePropertyValue(document, TextElement.FontSizeProperty, (double)12);

            DocumentHelpers.ChangePropertyValue(document, TextElement.ForegroundProperty, Brushes.Black, Brushes.White);
            DocumentHelpers.ChangePropertyValue(document, TextElement.BackgroundProperty, Brushes.White, Brushes.Black);
        }

        private void LoadBookmarks(FlowDocument document)
        {
            Bookmarks.Clear();
            var hyperlinks = document.GetLogicalChildren<Hyperlink>(true);
            foreach (var hyperlink in hyperlinks)
            {
                AddBookmarkFromHyperlink(hyperlink);
            }
        }

        private ObservableCollection<Bookmark> _bookmarks =  new ObservableCollection<Bookmark>();
        public ObservableCollection<Bookmark> Bookmarks
        {
            get { return _bookmarks; }
            set
            {
                _bookmarks = value;
                Changed(() => Bookmarks);
            }
        }

        private void AddBookmarkFromHyperlink(Hyperlink hyperlink)
        {


            if (hyperlink.NavigateUri.IsAbsoluteUri && hyperlink.NavigateUri.Host.StartsWith("bookmark"))
            {

                Bookmark bm = new Bookmark();

                bm.Name = Uri.UnescapeDataString(hyperlink.NavigateUri.Segments[1]);
                bm.Hyperlink = hyperlink;



                Bookmarks.Add(bm);

                bm.Ordinal = Bookmarks.Count;
                bm.Image = (hyperlink.Inlines.FirstInline as InlineUIContainer).Child as Image;
               // bm.Image.Height = FontSizeSlider.Value;

            }


        }

        #region Commands

        private ICommand _bookmarkSelectedCommand;

        public ICommand BookmarkSelectedCommand
        {
            get
            {
                return _bookmarkSelectedCommand ??
                       (_bookmarkSelectedCommand = new DelegateCommand<Bookmark>(JumpToBookmark));
            }
        }


        private ICommand _setBookmarkCommand;

        public ICommand SetBookmarkCommand
        {
            get
            {
                return _setBookmarkCommand ??
                       (_setBookmarkCommand = new RelayCommand(x => InsertBookmarkAtCurrentEyelineMark()));
            }
        }

        private ICommand _toggleTalentWindowCommand;

        public ICommand ToggleTalentWindowCommand
        {
            get {
                return _toggleTalentWindowCommand ??
                       (_toggleTalentWindowCommand = new RelayCommand(x => ToggleTalentWindow()));
            }
        }

        public ICommand SetDefaultSpeedCommand
        {
            get
            {
                if (_setDefaultSpeedCommand == null)
                {
                    _setDefaultSpeedCommand = new RelayCommand(x =>
                    {
                        DefaultSpeed = Speed;
                    });
                }
                return _setDefaultSpeedCommand;
            }
        }

        private ICommand _deleteBookmarkCommand;
        public ICommand DeleteBookmarkCommand
        {
            get
            {
                if (_deleteBookmarkCommand == null)
                {
                    _deleteBookmarkCommand = new DelegateCommand<ContentControl>(x =>
                    {
                        var bookmark = x.Content as Bookmark;
                        bookmark.Hyperlink.Inlines.Clear();
                        Bookmarks.Remove(bookmark);


                    });
                }
                return _deleteBookmarkCommand;
            }
        }

        private ICommand _renameBookmarkCommand;
        public ICommand RenameBookmarkCommand
        {
            get
            {
                if (_renameBookmarkCommand == null)
                {
                    _renameBookmarkCommand = new RelayCommand(x =>
                    {
                        var listItem = x as DependencyObject;
                        var children = listItem.FindChildren<TextBox>();

                        foreach (var bookmarkTextBox in children)
                        {
                            bookmarkTextBox.Focusable = true;
                            bookmarkTextBox.IsEnabled = true;
                            bookmarkTextBox.SelectionStart = 0;
                            bookmarkTextBox.SelectionLength = bookmarkTextBox.Text.Length;
                            bookmarkTextBox.PreviewKeyUp -= bookmarkTextBox_PreviewKeyUp;
                            bookmarkTextBox.PreviewKeyUp += bookmarkTextBox_PreviewKeyUp;
                            bookmarkTextBox.Focus();
                            break;

                        }
                    });
                }
                return _renameBookmarkCommand;
            }
        }

        void bookmarkTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                var box = (sender as TextBox);
                box.Focusable = false;
                box.IsEnabled = false;
            }
        }

        private ICommand _setDefaultSpeedCommand;

        public ICommand SaveDocumentAsCommand
        {
            get {
                return _saveDocumentAsCommand ??
                       (_saveDocumentAsCommand = new RelayCommand(x => SaveDocumentAs(DocumentPath)));
            }
        }

        private ICommand _saveDocumentCommand;
        public ICommand SaveDocumentCommand
        {
            get
            {
                if (_saveDocumentCommand == null)
                {
                    _saveDocumentCommand = new RelayCommand(x =>
                    {
                        if (string.IsNullOrWhiteSpace(DocumentPath))
                        {
                            SaveDocumentAs(DocumentPath);
                        }
                        else
                        {
                            SaveDocument(DocumentPath);
                        }
                    });
                }
                return _saveDocumentCommand;
            }
        }

        private ICommand _loadDocumentCommand;

        public ICommand LoadDocumentCommand
        {
            get {
                return _loadDocumentCommand ??
                       (_loadDocumentCommand = new RelayCommand(x => LoadDocumentDialog(DocumentPath)));
            }
        }

        private ICommand _editInWordpadCommand;

        public ICommand EditInWordpadCommand
        {
            get
            {
                if (_editInWordpadCommand == null)
                {
                    _editInWordpadCommand = new RelayCommand(x =>
                    {
                        if (string.IsNullOrWhiteSpace(DocumentPath))
                        {
                            SaveDocumentAs(DocumentPath);
                        }

                        EditInWordpad();
                    });
                }
                return _editInWordpadCommand;
            }
        }

        private ICommand _editWindowCommand;

        public ICommand EditWindowCommand
        {
            get
            {
                if (_editWindowCommand == null)
                {
                    _editWindowCommand = new RelayCommand(x =>
                    {
                        OpenEditWindow();
                    });
                }
                return _editWindowCommand;
            }
        }

        private ICommand _saveDocumentAsCommand;

        #endregion  Commands

        private void SaveDocumentAs(string documentPath)
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

                DocumentPath = dlg.FileName;

                if (!string.IsNullOrWhiteSpace(DocumentPath))
                {
                    LoadDocument(DocumentPath);
                }


            }


        }

        private void OpenEditWindow()
        {
            //EditWindow editWindow = new EditWindow();
            //editWindow.Owner = this;
            //editWindow.Show();

            MemoryStream ms = new MemoryStream();
            DocumentHelpers.SaveDocument(ms, MainDocument, DataFormats.Rtf);


            if (_editWindow != null)
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

        void _editWindow_DocumentUpdated(object sender, DocumentUpdatedEventArgs e)
        {
            Dispatcher.Invoke((Action)(() => LoadDocument(e.DocumentData, e.DataFormat)));
        }

        private void InsertBookmarkAtCurrentEyelineMark()
        {
            var bookmarkOffset = MainScrollerVerticalOffset + EyelinePosition;

            var pos = MainTextBox.GetPositionFromPoint(new Point(0, bookmarkOffset), true);


            var num = DocumentHelpers.GetLineNumberFromSelection(pos);

            Hyperlink hyperlink = new Hyperlink(pos, pos);

            Bookmark bm = new Bookmark();
            if(BookmarkImage != null)
            {
                var img = new Image();
                img.Source = BookmarkImage;

                img.Visibility = Visibility.Collapsed;
                bm.Image = img;
                hyperlink.Inlines.Add(img);
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
            foreach (var bookmark in Bookmarks)
            {
                ct++;
                if (ct == ordinal)
                {
                    JumpToBookmark(bookmark as Bookmark);
                    return;
                }

                
                
            }
        }

        #region Talent Window Methods

        private void ToggleTalentWindow()
        {
            if (_talentWindow == null)
            {
                _talentWindow = new TalentWindow();
                _talentWindow.Owner = Application.Current.MainWindow;
                _talentWindow.Closed += new EventHandler(_talentWindow_Closed);
                _talentWindow.PreviewKeyDown += KeyDown;
                _talentWindow.PreviewKeyUp += KeyUp;
                
            //    _talentWindow.KeyDown += MainWindow_KeyDown;
              //  _talentWindow.KeyUp += MainWindow_KeyUp;

                _talentWindow.DataContext = this;

                _talentWindow.Show();


          //      ToggleTalentWindowButton.Content = "Hide Talent Window";

            }
            else
            {
                HideTalentWindow();
            }
        }



        void _talentWindow_Closed(object sender, EventArgs e)
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
          // ToggleTalentWindowButton.Content = "Show Talent Window";

        }

        #endregion

        public void Dispose()
        {

            if (_wordpadProcess != null && !_wordpadProcess.HasExited)
            {
                _wordpadProcess.CloseMainWindow();
                _wordpadProcess.Close();

            }
            if (_talentWindow != null)
            {
                _talentWindow.Close();
            }
        }

        private ToolsWindow _toolsWindow;
        private void ToggleToolsWindow()
        {

            if (_toolsWindow == null)
            {
                _toolsWindow = new ToolsWindow();
                _toolsWindow.DataContext = this;
                _toolsWindow.Owner = Application.Current.MainWindow;
                _toolsWindow.ShowActivated = false;
                _toolsWindow.PreviewKeyDown += KeyDown;
                _toolsWindow.PreviewKeyUp += KeyUp;
                _toolsWindow.Closing += new System.ComponentModel.CancelEventHandler(_toolsWindow_Closing);
                _toolsWindow.SizeChanged += (sender, e) => SetToolsWindowSize(new Size(MainWindowWidth, MainWindowHeight));
                _toolsWindow.LocationChanged += (sender, e) => SetToolsWindowLocation(new Point(MainWindowLeft, MainWindowTop));
            }

            if (_toolsWindow.Visibility == Visibility.Visible)
            {
                _toolsWindow.Visibility = Visibility.Collapsed;
                _toolsWindow.Hide();
            }
            else
            {

                _toolsWindow.Show();
                SetToolsWindowSize(new Size(MainWindowWidth, MainWindowHeight));
                SetToolsWindowLocation(new Point(MainWindowLeft, MainWindowTop));
            }

        }

        void _toolsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            (sender as Window).Owner.Close();
            
        }


        private void SetToolsWindowSize(Size size)
        {
            if (_toolsWindow == null) return;
         //   LocationChanged -= MainWindow_LocationChanged;
        //    SizeChanged -= MainWindow_SizeChanged;
            //  _toolsWindow.Top = Top + ActualHeight - _toolsWindow.Height;
            _toolsWindow.Width = MainWindowWidth;
            //  _toolsWindow.Left = Left;
        //    LocationChanged += MainWindow_LocationChanged;
         //   SizeChanged += MainWindow_SizeChanged;

        }

        private void SetToolsWindowLocation(Point location)
        {
            if (_toolsWindow == null) return;

         //   LocationChanged -= MainWindow_LocationChanged;
          //  SizeChanged -= MainWindow_SizeChanged;
            _toolsWindow.Top = MainWindowTop + MainWindowHeight - _toolsWindow.Height;
            _toolsWindow.Width = MainWindowWidth;
            _toolsWindow.Left = MainWindowLeft;
         //   LocationChanged += MainWindow_LocationChanged;
         //   SizeChanged += MainWindow_SizeChanged;
        }

        #region Input Handlers

        internal void KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.Down)
            {
                SpeedForward();
            }
            else if (e.Key == Key.Up)
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
                //Numbers 1-9 should jump to the corresponding bookmark
            else if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                     (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                KeyConverter converter = new KeyConverter();
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
            else if (e.Key == Key.Space)
            {
                ToggleToolsWindow();
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
                             MainScrollerViewportHeight * 0.5;
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

            if(_speedBoostAmount > SpeedMax)
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
    }
}
