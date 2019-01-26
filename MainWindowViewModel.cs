using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using ProverbTeleprompter.Converters;
using ProverbTeleprompter.Helpers;
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
            _scrollTimer = new DispatcherTimer (DispatcherPriority.Render) {Interval = new TimeSpan(0, 0, 0, 0, 15), IsEnabled = true};
            _scrollTimer.Tick += _scrollTimer_Tick;
            _scrollTimer.Start();

            MainTextBox = mainTextBox;
            MultipleMonitorsAvailable = SystemInformation.MonitorCount > 1;
            SystemHandler.RemoteButtonPressed += RemoteButtonPressed;


			SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);

            KeyboardHookHelpers.CreateHook();
            KeyboardHookHelpers.KeyDown += KeyboardHookHelpers_KeyDown;
            KeyboardHookHelpers.KeyPress += KeyboardHookHelpers_KeyPress;
            KeyboardHookHelpers.KeyUp += KeyboardHookHelpers_KeyUp;

        	Displays = new ObservableCollection<string>(Screen.AllScreens.Select(x => x.DeviceName));

			MainTextBox.SizeChanged += new SizeChangedEventHandler(MainTextBox_SizeChanged);
			MainTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(MainTextBox_TextChanged);

		
        }

		void MainTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			StartCalcBookmarks();
		}

		DispatcherTimer _calcBookmarksTimer = new DispatcherTimer();
		void MainTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			StartCalcBookmarks(5);
		}

		private void StartCalcBookmarks(int delay = 1)
		{
			_calcBookmarksTimer.Stop();
			_calcBookmarksTimer.Interval = new TimeSpan(0, 0, 0, delay);
			_calcBookmarksTimer.Tick -= calcBookmarksTimer_Tick;
			_calcBookmarksTimer.Tick += calcBookmarksTimer_Tick;
			_calcBookmarksTimer.Start();
		}

		void calcBookmarksTimer_Tick(object sender, EventArgs e)
		{
			_calcBookmarksTimer.Stop();
			UpdateBookmarks();
		}

		private void UpdateBookmarks()
		{
			LoadBookmarks(MainDocument);
			//Bookmarks = new ObservableCollection<Bookmark>(CalculateAllBookMarkInfo());
		}

		private IEnumerable<Bookmark> CalculateAllBookMarkInfo()
		{
			Debug.WriteLine("Calc all bookmark info");
			return Bookmarks.Select(bookmark => CalculateBookMarkInfo(bookmark)).Where(bm => bm != null);
		}

    	/// <summary>
		/// 
		/// </summary>
		/// <param name="bm"></param>
		/// <param name="bookmarkOffset"></param>
		/// <returns>The updated bookmark, null if the hyperlink no longer exists in the document</returns>
		private Bookmark CalculateBookMarkInfo(Bookmark bm, double? bookmarkOffset = null)
		{
			if(!bookmarkOffset.HasValue)
			{
				var rect = bm.Hyperlink.ContentStart.GetCharacterRect(LogicalDirection.Forward);

				if(rect.IsEmpty)
				{
					return null;
				}
				bookmarkOffset = rect.Top;
			}


			TextPointer pos = MainTextBox.GetPositionFromPoint(new Point(0, bookmarkOffset.GetValueOrDefault()), true);
			TextPointer endPos = MainTextBox.GetPositionFromPoint(new Point(MainTextBox.ActualWidth, bookmarkOffset.GetValueOrDefault()), true);


			if (pos == null)
			{
				Trace.Fail("Could not get text start position for bookmark");
				return null;
			}

			if (endPos == null)
			{
				Trace.Fail("Could not get text end position for bookmark");
				return null;
			}

			int num = DocumentHelpers.GetLineNumberFromPosition(_mainTextBox, pos);

			if (bm.Hyperlink == null)
			{
				var hyperlink = new Hyperlink(pos, pos);
				bm.Hyperlink = hyperlink;
				if (BookmarkImage != null)
				{
					var img = new Image();
					img.Source = BookmarkImage;

					img.Visibility = Visibility.Collapsed;
					bm.Image = img;
					hyperlink.Inlines.Add(" ");
				}
			}

    		var textRange = new TextRange(pos, endPos);
			string toolTipText = textRange.Text;

			bm.Line = num;
			bm.TopOffset = bookmarkOffset.GetValueOrDefault();
			if (string.IsNullOrWhiteSpace(toolTipText))
			{
				toolTipText = "<<Blank line>>";
			}

			bm.TooltipText = string.Format("{0} (Line: {1})", toolTipText, num);
			bm.Name = bm.TooltipText;

			bm.Hyperlink.NavigateUri = new Uri(String.Format("http://bookmark/{0}", bm.Name));

			bm.Position = pos;

			return bm;
		}


		void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
		{
			Debug.WriteLine(@"
#############################################

SystemEvents_DisplaySettingsChanged

#############################################
");
			ShowDisplayDiagnostics();

			SetScreenState();


		}

		private void SetScreenState()
		{
			Displays = new ObservableCollection<string>(Screen.AllScreens.Select(x => x.DeviceName));
			MultipleMonitorsAvailable = Screen.AllScreens.Count() > 1;

			if (!MultipleMonitorsAvailable && Displays.Count == 1)
			{
				SelectedTalentWindowDisplay = Displays[0];
			}

			if (Displays.Count > 1)
			{
				SelectedTalentWindowDisplay = Displays[1];
			}

			MoveTalentWindowToDisplay(SelectedTalentWindowDisplay);
		}

		private void ShowDisplayDiagnostics()
		{
			var details = DisplayDetails.GetMonitorDetails();
			#region Diagnostics
			Debug.WriteLine("****************** GetWorkingArea: {0}", Screen.GetWorkingArea(new System.Drawing.Point(0, 0)));
			Debug.WriteLine("****************** GetBounds: {0}", Screen.GetBounds(new System.Drawing.Point(0, 0)));
			Debug.WriteLine("****************** Primary Screen: {0}", Screen.PrimaryScreen.DeviceName, 0);
			Debug.WriteLine("****************** EntireDesktop Res: {0}", ScreenHelpers.GetEntireDesktopArea(), 0);
			foreach (var displayDetails in details)
			{
				Debug.WriteLine("DETAILS:");
				Debug.WriteLine("\t\tAvailability: {0}", displayDetails.Availability);
				Debug.WriteLine("\t\tModel: {0}", displayDetails.Model, 0);
				Debug.WriteLine("\t\tMonitorID: {0}", displayDetails.MonitorID, 0);
				Debug.WriteLine("\t\tPixelHeight: {0}", displayDetails.PixelHeight, 0);
				Debug.WriteLine("\t\tPixelWidth: {0}", displayDetails.PixelWidth);
				Debug.WriteLine("\t\tPnPID: {0}", displayDetails.PnPID, 0);
				Debug.WriteLine("\t\tSerialNumber: {0}", displayDetails.SerialNumber, 0);
			}
			Debug.WriteLine("********* SCREENS *********");
			var screens = Screen.AllScreens;
			foreach (var screen in screens)
			{
				Debug.WriteLine("SCREEN:");
				Debug.WriteLine("\t\tBitsPerPixel: {0}", screen.BitsPerPixel, 0);
				Debug.WriteLine("\t\tBounds: {0}", screen.Bounds, 0);
				Debug.WriteLine("\t\tDeviceName: {0}", screen.DeviceName, 0);
				Debug.WriteLine("\t\tPrimary: {0}", screen.Primary, 0);
				Debug.WriteLine("\t\tWorkingArea: {0}", screen.WorkingArea, 0);

			}
			#endregion
		}


        void KeyboardHookHelpers_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
           GlobalKeyUp(sender, e);
        }

        void KeyboardHookHelpers_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        void KeyboardHookHelpers_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            GlobalKeyDown(sender, e);
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
			SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            if (_talentWindow != null)
            {
                _talentWindow.Close();
            }
        }

        #endregion

        private void _scrollTimer_Tick(object sender, EventArgs e)
        {
            _ticksElapsed++;

			if (!Paused && Speed.CompareTo(0) != 0)
			{
				MainScrollerVerticalOffset = MainScrollerVerticalOffset + Speed;
			}
			else if(TotalBoostAmount.CompareTo(0) != 0)
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


			FullScreenTalentWindow = Settings.Default.TalentWindowState != WindowState.Normal;

			SelectedTalentWindowDisplay = Settings.Default.SelectedTalentWindowDisplay;

			TalentWindowState = Settings.Default.TalentWindowState;

			EyelinePosition = Settings.Default.EyeLinePosition;
            if (Settings.Default.TalentWindowVisible)
            {
                ToggleTalentWindow();
            }

            MainWindowState = Settings.Default.MainWindowState;



            ReceiveGlobalKeystrokes = Settings.Default.ReceiveGlobalKeystrokes;

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

        	TextMarginValue = Settings.Default.TextMarginValue;
        	OuterLeftRightMarginValue = Settings.Default.OuterLeftRightMarginValue;

        	EyelineHeight = Settings.Default.EyelineHeight;
        	EyelineWidth = Settings.Default.EyelineWidth;
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
				CalculateBookMarkInfo(bm);



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
		// If path not available (eg. network, or pedrive missing)
                if (!Directory.Exists (dlg.InitialDirectory ))
                {
                    dlg.InitialDirectory =  Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments) ;
                }

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


        private void InsertBookmarkAtCurrentEyelineMark()
        {
            double bookmarkOffset = MainScrollerVerticalOffset + EyelinePosition + (EyelineHeight / 2);

			var bm = new Bookmark();
			CalculateBookMarkInfo(bm, bookmarkOffset);
			bm.Ordinal = Bookmarks.Count;
            Bookmarks.Add(bm);
            
        }

        private void JumpToBookmark(Bookmark bookmark)
        {
            if (bookmark == null) return;
        	var rect = bookmark.Hyperlink.ContentStart.GetCharacterRect(LogicalDirection.Forward);

        	MainScrollerVerticalOffset = rect.Top - EyelinePosition;
			_mainTextBox.CaretPosition = bookmark.Hyperlink.ContentStart;

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
                    if(!string.IsNullOrWhiteSpace(DocumentPath) && File.Exists(DocumentPath))
                    {
                        SaveDocument(DocumentPath); 
                    }
                    else
                    {
                        SaveDocumentAs(DocumentPath);
                    }
                    
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
            	_toolsWindow.Topmost = true;
                // _toolsWindow.ShowActivated = false;
                _toolsWindow.PreviewKeyDown += KeyDown;
                _toolsWindow.PreviewKeyUp += KeyUp;
                _toolsWindow.Closing += _toolsWindow_Closing;
                _toolsWindow.Loaded += _toolsWindow_Loaded;
            }

            if (_toolsWindow.Visibility == Visibility.Visible)
            {
                _toolsWindow.Visibility = Visibility.Collapsed;
                _toolsWindow.Hide();
            }
            else
            {
                _toolsWindow.Show();
            }
        }

        private void _toolsWindow_Loaded(object sender, RoutedEventArgs e)
        {
           	var area = Screen.PrimaryScreen.WorkingArea;
        	var winHeight = ConvertFromDIPixelsToPixels(_toolsWindow.ActualHeight);
        	var winWidth = ConvertFromDIPixelsToPixels(_toolsWindow.Width);
        	var winTop = area.Height - winHeight;
			_toolsWindow.Top = ConvertPixelsToDIPixels(area.Height - winHeight);
        	var leftPixels = area.Width/2.0 - winWidth/2.0;

			//Check if right edge will be off screen
			if(leftPixels + winWidth > area.Width)
			{
				_toolsWindow.SizeToContent = SizeToContent.Manual;
				//resize and re-center
				winWidth = area.Width;
				leftPixels = 0;
				_toolsWindow.Width = ConvertPixelsToDIPixels(winWidth);
				_toolsWindow.Height = ConvertPixelsToDIPixels(winHeight);

			}
        	_toolsWindow.Left = ConvertPixelsToDIPixels(leftPixels);


        }


		[DllImport("User32.dll")]
		private static extern IntPtr GetDC(HandleRef hWnd);
		[DllImport("User32.dll")]
		private static extern int ReleaseDC(HandleRef hWnd, HandleRef hDC);
		[DllImport("GDI32.dll")]
		private static extern int GetDeviceCaps(HandleRef hDC, int nIndex);
		private static int _dpi = 0;


		public static int Dpi
		{
			get
			{
				if (_dpi == 0)
				{
					var desktopHwnd = new HandleRef(null, IntPtr.Zero);
					var desktopDC = new HandleRef(null, GetDC(desktopHwnd));
					try
					{
						_dpi = GetDeviceCaps(desktopDC, 88/*LOGPIXELSX*/);	
					}
					finally
					{
						ReleaseDC(desktopHwnd, desktopDC);
					}

				}
				return _dpi;
			}
		}

		public static double ConvertPixelsToDIPixels(double pixels)
		{
			return (double)pixels * 96 / Dpi;
		}

		public static double ConvertFromDIPixelsToPixels(double pixels)
		{
			return (double)pixels / 96 * Dpi;
		}


        private static void _toolsWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            ((Window) sender).Owner.Close();
        }

        #region Input Handlers


        internal void GlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            var key = KeyInterop.KeyFromVirtualKey(e.KeyValue);

            if(!e.Alt && !e.Shift && !e.Control)
                HandleKeyDown(key, true);
        }

        internal void GlobalKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            var key = KeyInterop.KeyFromVirtualKey(e.KeyValue);
            HandleKeyUp(key, true);
        }

        internal void KeyDown(object sender, KeyEventArgs e)
        {
          
            if (!ReceiveGlobalKeystrokes)
            {
                e.Handled = HandleKeyDown(e.Key, false);
            }
        }

        internal void KeyUp(object sender, KeyEventArgs e)
        {
            if (!ReceiveGlobalKeystrokes)
            {
                e.Handled = HandleKeyUp(e.Key, false);
            }
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isGlobal"></param>
        /// <returns>True if the key down event is handled</returns>
        public bool HandleKeyDown(Key key, bool isGlobal)
        {
            if (MainTextBox.Focusable || !CaptureKeyboard || (isGlobal && !ReceiveGlobalKeystrokes)) return false;

            bool handled = true;
            if (key == Key.Down)
            {
                SpeedForward();
            }
            else if (key == Key.Up)
            {
                SpeedReverse();
            }
            else if (key == Key.Tab)
            {
                
                ToggleTools();
            
            }

                //Slide forward / page down button To work with Logitech PowerPoint remote
            else if (key == Key.Next)
            {
                //PageDown();
                SpeedForward();
            }
            //Slid back button / page up To work with Logitech PowerPoint remote
            else if (key == Key.Prior)
            {
                //PageUp();
                SpeedReverse();
            }
            //F5 To work with Logitech PowerPoint remote
            else if (key == Key.F5 ||
                     key == Key.MediaStop ||
                     key == Key.MediaPlayPause ||
                     key == Key.Escape ||
                     key == Key.Space)
            {
                PauseScrolling();
            }
            //Period To work with Logitech PowerPoint remote
            else if (key == Key.OemPeriod)
            {
                ScrollToTop();
            }
            else if (key == Key.MediaPreviousTrack)
            {
                PageUp();
            }
            else if (key == Key.MediaNextTrack)
            {
                PageDown();
            }
            else if (key == Key.OemPlus)
            {
                SpeedSliderValue += 0.1;
            }
            else if (key == Key.OemMinus)
            {
                SpeedSliderValue -= 0.1;
            }
            //Numbers 1-9 should jump to the corresponding bookmark
            else if ((key >= Key.D0 && key <= Key.D9) ||
                     (key >= Key.NumPad0 && key <= Key.NumPad9))
            {
                var converter = new KeyConverter();
                string val = converter.ConvertToString(key);

                JumpToBookmarkByOrdinal(int.Parse(val));

                //To allow text boxes to get numbers
                handled = false;
            }
            else if (key == Key.F1)
            {
                LoadRandomBibleChapter();
            }
            else if (key == Key.Insert)
            {
                InsertBookmarkAtCurrentEyelineMark();
            }
            else
            {
                handled = false;
            }
            return handled;
        }

        public bool HandleKeyUp(Key key, bool isGlobal)
        {
            if (MainTextBox.Focusable || !CaptureKeyboard || (isGlobal && !ReceiveGlobalKeystrokes)) return false;

            if (key == Key.Down)
            {
                Speed = DefaultSpeed;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (key == Key.Up)
            {
                Speed = DefaultSpeed;

                // SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (key == Key.Next)
            {
                Speed = DefaultSpeed;
                //SpeedSlider.Value -= TotalBoostAmount;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }
            else if (key == Key.Prior)
            {
                //SpeedSlider.Value -= TotalBoostAmount;
                Speed = DefaultSpeed;
                _speedBoostAmount = 0;
                TotalBoostAmount = 0;
            }

			if (key == Key.Up || key == Key.Down || key == Key.Next || key == Key.Prior)
			{
				if (Paused)
				{
					Speed = 0;
				}
			}

            return false;
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
			if(_toolsWindow != null)
			{
				_toolsWindow.WindowState = WindowState.Normal;
			}
            //Storyboard sb = (Storyboard)this.FindResource("ToolFlyin");
            //sb.Begin();
        }

        private void HideTools()
        {
            ToolsVisible = false;
			if (_toolsWindow != null)
			{
				_toolsWindow.WindowState = WindowState.Minimized;
			}
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
			MoveTalentWindowToDisplay(SelectedTalentWindowDisplay);

            if (_talentWindow == null )
            {
				
                ShowTalentWindow();
                AppConfigHelper.SetUserSetting("TalentWindowVisible", true);


            }
            else
            {
                HideTalentWindow();

                AppConfigHelper.SetUserSetting("TalentWindowVisible", false);
            }
        	
        }

        protected void ShowTalentWindow()
        {
            if(_talentWindow == null)
            {
                _talentWindow = new TalentWindow { Owner = Application.Current.MainWindow, Topmost = false};
            	
                _talentWindow.Closed += _talentWindow_Closed;
                _talentWindow.PreviewKeyDown += KeyDown;
                _talentWindow.PreviewKeyUp += KeyUp;

                _talentWindow.Loaded += _talentWindow_Loaded;
                _talentWindow.DataContext = this;
            }


            _talentWindow.Show();

            ToggleTalentWindowCaption = "Hide talent window";
            
        }

        private void _talentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (SystemInformation.MonitorCount <= 1)
            {
                _talentWindow.WindowState = WindowState.Normal;
                return;
            }

			MoveTalentWindowToDisplay(SelectedTalentWindowDisplay);
            Rectangle workingArea = Screen.AllScreens[1].WorkingArea;

           // _talentWindow.Left = PixelConverter.ToUnits(workingArea.Left);
           // _talentWindow.Top = PixelConverter.ToUnits(workingArea.Top);
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
            ToggleTalentWindowCaption = "Show talent window";

        }

        #endregion
    }
}
