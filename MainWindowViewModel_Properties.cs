using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using Microsoft.Practices.Prism.Commands;
using ProverbTeleprompter.Helpers;
using ProverbTeleprompter.HtmlConverter;
using RichTextBox = System.Windows.Controls.RichTextBox;
using System.Linq;

namespace ProverbTeleprompter
{
    public partial class MainWindowViewModel
    {
        #region Bindable Properties

        private ImageSource _boomarkImage;
        private double _defaultSpeed;
        private double _desiredSpeed;
        private string _documentPath;
        private bool? _editable = false;
        private double _eyelinePosition;
        private bool _flipMainWindowHoriz;
        private bool _flipMainWindowVert;
        private bool _flipTalentWindowHoriz;
        private bool _flipTalentWindowVert;
        private double _fontSize;
        private bool _isBlackOnWhite = true;
        private bool _isDocumentDirty;
        private bool _isWhiteOnBlack;
        private double _lineHeight;
        private FlowDocument _mainDocument;
        private Brush _mainDocumentCaretBrush;
        private double _mainScrollerExtentHeight;
        private double _mainScrollerVerticalOffset;
        private double _mainScrollerViewportHeight;
        private RichTextBox _mainTextBox;
        private double _mainWindowHeight;
        private double _mainWindowLeft;
        private double _mainWindowScaleX = 1;
        private double _mainWindowScaleY = 1;
        private WindowState _mainWindowState;
        private double _mainWindowTop;
        private double _mainWindowWidth;
        private bool _paused;
        private double _percentComplete;
        private Bookmark _selectedBookmark;
        private double _speed;
        private double _speedMax = 8;
        private double _speedMin = -8;
        private double _talentWindowHeight;
        private double _talentWindowLeft;
        private double _talentWindowScaleX = 1;
        private double _talentWindowScaleY = 1;
        private WindowState _talentWindowState;
        private double _talentWindowTop;
        private double _talentWindowWidth;
        private string _tempDocumentPath;
        private string _timeRemaining;
        private string _toggleTalentWindowCaption = "Show talent window";
        private double _toolWindowHeight;
        private double _toolWindowLeft;
        private double _toolWindowTop;
        private double _toolWindowWidth;
        private bool _toolsVisible = true;
        private double _totalBoostAmount;
        private Dictionary<string, FileSystemWatcher> _watchedFiles = new Dictionary<string, FileSystemWatcher>();

        public Dictionary<string, FileSystemWatcher> WatchedFiles
        {
            get { return _watchedFiles; }
            set
            {
                _watchedFiles = value;
                Changed(() => WatchedFiles);
            }
        }

        public bool? Editable
        {
            get { return _editable; }
            set
            {
                _editable = value;
                Paused = Editable.GetValueOrDefault();
                Changed(() => Editable);
            }
        }

        public FlowDocument MainDocument
        {
            get
            {
                if (_mainDocument == null)
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


        public RichTextBox MainTextBox
        {
            get { return _mainTextBox; }
            set
            {
                _mainTextBox = value;
            

                Changed(() => MainTextBox);
            }
        }


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

        public bool ToolsVisible
        {
            get { return _toolsVisible; }
            set
            {
                _toolsVisible = value;
                Changed(() => ToolsVisible);
            }
        }

        public bool IsBlackOnWhite
        {
            get { return _isBlackOnWhite; }
            set
            {
                _isBlackOnWhite = value;
                if (_isBlackOnWhite)
                {
                    SetBlackOnWhite();
                }
                Changed(() => IsBlackOnWhite);
            }
        }

        public double DefaultSpeed
        {
            get { return _defaultSpeed; }
            set
            {
                _defaultSpeed = value;
                Changed(() => DefaultSpeed);
            }
        }

        [DependsUpon("SpeedMax")]
        [DependsUpon("SpeedMin")]
        public double Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                if (_speed < SpeedMin)
                {
                    _speed = SpeedMin;
                }
                if (_speed > SpeedMax)
                {
                    _speed = SpeedMax;
                }

                if (_configInitialized && value != 0)
                    AppConfigHelper.SetUserSetting("Speed", _speed);

                Changed(() => Speed);
            }
        }

        public double SpeedMax
        {
            get { return _speedMax; }
            set
            {
                _speedMax = value;
                Changed(() => SpeedMax);
            }
        }

        public double SpeedMin
        {
            get { return _speedMin; }
            set
            {
                _speedMin = value;
                Changed(() => SpeedMin);
            }
        }

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
                AppConfigHelper.SetUserSetting("DocumentPath", DocumentPath);
            }
        }

        public string TempDocumentPath
        {
            get { return _tempDocumentPath; }
            set
            {
                _tempDocumentPath = value;
                Changed(() => TempDocumentPath);
            }
        }

        public bool IsDocumentDirty
        {
            get { return _isDocumentDirty; }
            set
            {
                _isDocumentDirty = value;
                Changed(() => IsDocumentDirty);
                Changed(() => SaveDocumentCommand);
            }
        }

        public bool FlipTalentWindowVert
        {
            get { return _flipTalentWindowVert; }
            set
            {
                _flipTalentWindowVert = value;
                TalentWindowScaleY = FlipTalentWindowVert ? -1 : 1;
                AppConfigHelper.SetUserSetting("FlipTalentWindowVert", FlipTalentWindowVert);
                Changed(() => FlipTalentWindowVert);
            }
        }

        public bool FlipTalentWindowHoriz
        {
            get { return _flipTalentWindowHoriz; }
            set
            {
                _flipTalentWindowHoriz = value;
                TalentWindowScaleX = FlipTalentWindowHoriz ? -1 : 1;
                AppConfigHelper.SetUserSetting("FlipTalentWindowHoriz", FlipTalentWindowHoriz);
                Changed(() => FlipTalentWindowHoriz);
            }
        }

        public bool FlipMainWindowVert
        {
            get { return _flipMainWindowVert; }
            set
            {
                _flipMainWindowVert = value;
                MainWindowScaleY = FlipMainWindowVert ? -1 : 1;
                AppConfigHelper.SetUserSetting("FlipMainWindowVert", FlipMainWindowVert);
                Changed(() => FlipMainWindowVert);
            }
        }

        public bool FlipMainWindowHoriz
        {
            get { return _flipMainWindowHoriz; }
            set
            {
                _flipMainWindowHoriz = value;
                MainWindowScaleX = FlipMainWindowHoriz ? -1 : 1;
                AppConfigHelper.SetUserSetting("FlipMainWindowHoriz", FlipMainWindowHoriz);
                Changed(() => FlipMainWindowHoriz);
            }
        }

        public double ToolWindowTop
        {
            get { return _toolWindowTop; }
            set
            {
                _toolWindowTop = value;
                if (_toolsWindow != null)
                    _toolsWindow.Top = _toolWindowTop;
                Changed(() => ToolWindowTop);

                AppConfigHelper.SetUserSetting("ToolWindowTop", _toolWindowTop);
            }
        }

        public double ToolWindowLeft
        {
            get { return _toolWindowLeft; }
            set
            {
                _toolWindowLeft = value;
                Changed(() => ToolWindowLeft);
                AppConfigHelper.SetUserSetting("ToolWindowLeft", _toolWindowLeft);
            }
        }

        public double ToolWindowHeight
        {
            get { return _toolWindowHeight; }
            set
            {
                _toolWindowHeight = value;
                Changed(() => ToolWindowHeight);

                AppConfigHelper.SetUserSetting("ToolWindowHeight", _toolWindowHeight);
            }
        }

        public double ToolWindowWidth
        {
            get { return _toolWindowWidth; }
            set
            {
                _toolWindowWidth = value;
                Changed(() => ToolWindowWidth);
                AppConfigHelper.SetUserSetting("ToolWindowWidth", _toolWindowWidth);
            }
        }


        public double TalentWindowTop
        {
			get { return _talentWindowTop; }
            set
            {
                var rect = ScreenHelpers.GetEntireDesktopArea();
                if (value < rect.Top)
                {
                    value = rect.Top;
                }
                _talentWindowTop = value;
                Changed(() => TalentWindowTop);

                AppConfigHelper.SetUserSetting("TalentWindowTop", _talentWindowTop);
            }
        }

        public double TalentWindowLeft
        {
			get
			{

				return _talentWindowLeft;
			}
            set
            {
                var rect = ScreenHelpers.GetEntireDesktopArea();
                if (value < rect.Left)
                {
                    value = rect.Left;
                }
                _talentWindowLeft = value;
                Changed(() => TalentWindowLeft);
                AppConfigHelper.SetUserSetting("TalentWindowLeft", _talentWindowLeft);
            }
        }

        public double TalentWindowHeight
        {
			get { return _talentWindowHeight; }
            set
            {
                
                //Ensure the height doe not exceed the bottom of the desktop
                var rect = ScreenHelpers.GetEntireDesktopArea();
                if ((TalentWindowTop + value) > rect.Bottom)
                {
                    value = (rect.Bottom - TalentWindowTop);
                }
                if (value < 300)
                {
                    value = 300;
                }


                _talentWindowHeight = value;
                Changed(() => TalentWindowHeight);

                AppConfigHelper.SetUserSetting("TalentWindowHeight", _talentWindowHeight);
            }
        }

        public double TalentWindowWidth
        {
			get { return _talentWindowWidth; }
            set
            {
                var rect = ScreenHelpers.GetEntireDesktopArea();
                if ((TalentWindowLeft + value) > rect.Right)
                {
                    value = (rect.Right - TalentWindowLeft);
                }

                if (value < 400)
                {
                    value = 400;
                }
                _talentWindowWidth = value;
                Changed(() => TalentWindowWidth);
                AppConfigHelper.SetUserSetting("TalentWindowWidth", _talentWindowWidth);
            }
        }


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

        public Brush MainDocumentCaretBrush
        {
            get { return _mainDocumentCaretBrush; }
            set
            {
                _mainDocumentCaretBrush = value;
                Changed(() => MainDocumentCaretBrush);
            }
        }

        public bool Paused
        {
            get { return _paused; }
            set
            {
                _paused = value;
                if (Paused)
                {
                    DesiredSpeed = Speed;
                    Speed = 0;
                }
                else
                {
                    Speed = DesiredSpeed;
                }
                Changed(() => Paused);
            }
        }

        public double PercentComplete
        {
            get { return _percentComplete; }
            set
            {
                _percentComplete = value;
                Changed(() => PercentComplete);
            }
        }

        public string TimeRemaining
        {
            get { return _timeRemaining; }
            set
            {
                _timeRemaining = value;
                Changed(() => TimeRemaining);
            }
        }

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

        public double MainScrollerExtentHeight
        {
            get { return _mainScrollerExtentHeight; }
            set
            {
                _mainScrollerExtentHeight = value;
                Changed(() => MainScrollerExtentHeight);
            }
        }

        public double MainScrollerViewportHeight
        {
            get { return _mainScrollerViewportHeight; }
            set
            {
                _mainScrollerViewportHeight = value;
                Changed(() => MainScrollerViewportHeight);
            }
        }

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

        [DependsUpon("FlipTalentWindowHoriz")]
        public double TalentWindowScaleX
        {
            get { return _talentWindowScaleX; }
            set
            {
                _talentWindowScaleX = value ;
                Changed(() => TalentWindowScaleX);
            }
        }

        [DependsUpon("FlipTalentWindowVert")]
        public double TalentWindowScaleY
        {
            get { return _talentWindowScaleY ; }
            set
            {
                _talentWindowScaleY = value;
                Changed(() => TalentWindowScaleY);
            }
        }

        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;

                DocumentHelpers.ChangePropertyValue(MainDocument, TextElement.FontSizeProperty, FontSize);
                if (_configInitialized)
                    AppConfigHelper.SetUserSetting("FontSize", FontSize);

                Changed(() => FontSize);
            }
        }

        [DependsUpon("FontSize")]
        public double LineHeight
        {
            get { return _lineHeight; }
            set
            {


                MainDocument.SetValue(Block.LineHeightProperty, value * FontSize);
                if (_configInitialized)
                    AppConfigHelper.SetUserSetting("LineHeight", value);
                _lineHeight = value;

                
                Changed(() => LineHeight);
            }
        }

        public Bookmark SelectedBookmark
        {
            get { return _selectedBookmark; }
            set
            {
                _selectedBookmark = value;
                Changed(() => SelectedBookmark);
            }
        }

        public double MainWindowHeight
        {
            get { return _mainWindowHeight; }
            set
            {
                _mainWindowHeight = value;
                Changed(() => MainWindowHeight);
            }
        }

        public double MainWindowWidth
        {
            get { return _mainWindowWidth; }
            set
            {
                _mainWindowWidth = value;

                Changed(() => MainWindowWidth);
            }
        }

        public double MainWindowLeft
        {
            get { return _mainWindowLeft; }
            set
            {
                _mainWindowLeft = value;

                Changed(() => MainWindowLeft);
            }
        }

        public double MainWindowTop
        {
            get { return _mainWindowTop; }
            set
            {
                _mainWindowTop = value;

                Changed(() => MainWindowTop);
            }
        }

        public ImageSource BookmarkImage
        {
            get { return _boomarkImage; }
            set
            {
                _boomarkImage = value;
                Changed(() => BookmarkImage);
            }
        }

        public string ToggleTalentWindowCaption
        {
            get { return _toggleTalentWindowCaption; }
            set
            {
                _toggleTalentWindowCaption = value;
                Changed(() => ToggleTalentWindowCaption);
            }
        }

        public WindowState MainWindowState
        {
            get { return _mainWindowState; }
            set
            {
                _mainWindowState = value;
                AppConfigHelper.SetUserSetting("MainWindowState", MainWindowState);
                Changed(() => MainWindowState);
            }
        }

        public WindowState TalentWindowState
        {
            get { return _talentWindowState; }
            set
            {
                _talentWindowState = value;
                AppConfigHelper.SetUserSetting("TalentWindowState", TalentWindowState);
            	_fullScreenTalentWindow = value == WindowState.Maximized;
                Changed(() => TalentWindowState);
            }
        }

        private bool _captureKeyboard = true;
        public bool CaptureKeyboard
        {
            get
            {
                return _captureKeyboard;
            }
            set
            {
                _captureKeyboard = value;
                Changed(() => CaptureKeyboard);
            }
        }

        private bool _receiveGlobalKeystrokes;
        public bool ReceiveGlobalKeystrokes
        {
            get
            {
                return _receiveGlobalKeystrokes;
            }
            set
            {
                _receiveGlobalKeystrokes = value;
                AppConfigHelper.SetUserSetting("ReceiveGlobalKeystrokes", ReceiveGlobalKeystrokes);
                Changed(() => ReceiveGlobalKeystrokes);
            }
        }

        private bool _multipleMonitorsAvailable;
        public bool MultipleMonitorsAvailable
        {
            get
            {
                return _multipleMonitorsAvailable;
            }
            set
            {
                _multipleMonitorsAvailable = value;
                Changed(() => MultipleMonitorsAvailable);
				if (_toggleTalentWindowCommand != null)
					(ToggleTalentWindowCommand as DelegateCommand<object>).RaiseCanExecuteChanged();
				if (_talentWindowsDisplaySelectedCommand != null)
					(TalentWindowsDisplaySelectedCommand as DelegateCommand<string>).RaiseCanExecuteChanged();
            }
        }

    	private ObservableCollection<string> _displays;
    	public ObservableCollection<string> Displays
    	{
    		get { return _displays; }
    		set
    		{
    			_displays = value;
				Changed(() => Displays);
    		}
    	}

    	private string _selectedTalentWindowDisplay;
    	public string SelectedTalentWindowDisplay
    	{
    		get { return _selectedTalentWindowDisplay; }
    		set
    		{
				MoveTalentWindowToDisplay(value);
    			_selectedTalentWindowDisplay = value;
				Changed(() => SelectedTalentWindowDisplay);
				AppConfigHelper.SetUserSetting("SelectedTalentWindowDisplay", value);
    		}
    	}

    	private bool _fullScreenTalentWindow;
		[DependsUpon("TalentWindowState")]
    	public bool FullScreenTalentWindow
    	{
    		get { return _fullScreenTalentWindow; }
    		set
    		{
    			_fullScreenTalentWindow = value;
				Changed(() => FullScreenTalentWindow);
				TalentWindowState = value ? WindowState.Maximized : WindowState.Normal;
				AppConfigHelper.SetUserSetting("FullScreenTalentWindow", value);
    		}
    	}

    	private void MoveTalentWindowToDisplay(string displayName)
    	{
    		bool origFullScreen = false;
			//If moving to a new display
			if(SelectedTalentWindowDisplay != displayName && TalentWindowState == WindowState.Maximized)
			{
				TalentWindowState = WindowState.Normal;
				origFullScreen = true;
			}
			var talentWindowScreen = Screen.AllScreens.SingleOrDefault(x => x.DeviceName == displayName) ?? Screen.PrimaryScreen;
    		TalentWindowLeft = talentWindowScreen.WorkingArea.Left;
			TalentWindowTop = talentWindowScreen.WorkingArea.Top;
			if (FullScreenTalentWindow || origFullScreen)
			{
				TalentWindowState = WindowState.Maximized;
			} else
			{
				TalentWindowState = WindowState.Normal;
			}

			if(string.IsNullOrWhiteSpace(displayName))
			{
				SelectedTalentWindowDisplay = talentWindowScreen.DeviceName;
			}
			
		}



    	public void LoadRandomBibleChapter()
        {
            string content = BibleHelpers.GetRandomBibleChapterHtml();
            if (!string.IsNullOrWhiteSpace(content))
            {
                MainDocument = HtmlToXamlConverter.ConvertHtmlToXaml(content);
                MainDocument.ContentStart.InsertLineBreak();
                MainDocument.ContentStart.InsertLineBreak();
                MainDocument.ContentStart.InsertLineBreak();

                SetDocumentConfig();
            }
        }

        #endregion
    }
}