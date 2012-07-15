using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using ProverbTeleprompter.Helpers;

namespace ProverbTeleprompter
{
    public partial class MainWindowViewModel
    {
        #region Commands

        private ICommand _bookmarkSelectedCommand;
        private ICommand _deleteBookmarkCommand;
        private ICommand _editInWordpadCommand;
        private ICommand _loadDocumentCommand;
        private ICommand _renameBookmarkCommand;
        private ICommand _saveDocumentAsCommand;
        private ICommand _saveDocumentCommand;


        private ICommand _setBookmarkCommand;
        private ICommand _setDefaultSpeedCommand;

        private DelegateCommand<object> _toggleTalentWindowCommand;

        public ICommand BookmarkSelectedCommand
        {
            get
            {
                return _bookmarkSelectedCommand ??
                       (_bookmarkSelectedCommand = new DelegateCommand<Bookmark>(JumpToBookmark));
            }
        }

        public ICommand SetBookmarkCommand
        {
            get
            {
                return _setBookmarkCommand ??
                       (_setBookmarkCommand = new RelayCommand(x => InsertBookmarkAtCurrentEyelineMark()));
            }
        }

        public ICommand ToggleTalentWindowCommand
        {
            
            get
            {
                
                return _toggleTalentWindowCommand ??
                       (_toggleTalentWindowCommand = new DelegateCommand<object>(
                                                         x => ToggleTalentWindow()));
            }
        }

        public ICommand SetDefaultSpeedCommand
        {
            get
            {
                if (_setDefaultSpeedCommand == null)
                {
                    _setDefaultSpeedCommand = new RelayCommand(x => { DefaultSpeed = Speed; });
                }
                return _setDefaultSpeedCommand;
            }
        }

        public ICommand DeleteBookmarkCommand
        {
            get
            {
                if (_deleteBookmarkCommand == null)
                {
                    _deleteBookmarkCommand = new DelegateCommand<ContentControl>(x =>
                                                                                     {
                                                                                         var bookmark =
                                                                                             x.Content as Bookmark;
                                                                                         bookmark.Hyperlink.Inlines.
                                                                                             Clear();
                                                                                         Bookmarks.Remove(bookmark);
                                                                                     });
                }
                return _deleteBookmarkCommand;
            }
        }

        public ICommand RenameBookmarkCommand
        {
            get
            {
                if (_renameBookmarkCommand == null)
                {
                    _renameBookmarkCommand = new RelayCommand(x =>
                                                                  {
                                                                      var listItem = x as DependencyObject;
                                                                      IEnumerable<TextBox> children =
                                                                          listItem.FindChildren<TextBox>();

                                                                      foreach (TextBox bookmarkTextBox in children)
                                                                      {
                                                                          bookmarkTextBox.Focusable = true;
                                                                          bookmarkTextBox.IsEnabled = true;
                                                                          bookmarkTextBox.SelectionStart = 0;
                                                                          bookmarkTextBox.SelectionLength =
                                                                              bookmarkTextBox.Text.Length;
                                                                          bookmarkTextBox.PreviewKeyUp -=
                                                                              bookmarkTextBox_PreviewKeyUp;
                                                                          bookmarkTextBox.PreviewKeyUp +=
                                                                              bookmarkTextBox_PreviewKeyUp;
                                                                          bookmarkTextBox.Focus();
                                                                          break;
                                                                      }
                                                                  });
                }
                return _renameBookmarkCommand;
            }
        }

        public ICommand SaveDocumentAsCommand
        {
            get
            {
                return _saveDocumentAsCommand ??
                       (_saveDocumentAsCommand = new RelayCommand(x => SaveDocumentAs(DocumentPath)));
            }
        }

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
                                                                }, (a) => IsDocumentDirty);
                }
                return _saveDocumentCommand;
            }
        }

        public ICommand LoadDocumentCommand
        {
            get
            {
                return _loadDocumentCommand ??
                       (_loadDocumentCommand = new RelayCommand(x => LoadDocumentDialog(DocumentPath)));
            }
        }

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

        private ICommand _closeApplicationCommand;

        public ICommand CloseApplicationCommand
        {
            get
            {
                return _closeApplicationCommand ??
                       (_closeApplicationCommand = new RelayCommand(x =>
                                                         {
                                                             App.Current.MainWindow.Close();

                                                         }));
            }
        }

        private ICommand _bookmarkClickedCommand;
        public ICommand BookmarkClickedCommand
        {
            get
            {
                if (_bookmarkClickedCommand == null)
                {
                    _bookmarkClickedCommand = new DelegateCommand<ContentControl>(x =>
                    {
                        var bookmark =
                            x.Content as Bookmark;
                        JumpToBookmark(bookmark);
                       
                    });
                }
                return _bookmarkClickedCommand;
            }
        }

    	private ICommand _talentWindowsDisplaySelectedCommand;

    	public ICommand TalentWindowsDisplaySelectedCommand
    	{
			get
			{
				if (_talentWindowsDisplaySelectedCommand != null) return _talentWindowsDisplaySelectedCommand;
				
				_talentWindowsDisplaySelectedCommand = new DelegateCommand<string>(x =>
				{


				}, y => MultipleMonitorsAvailable);

				return _talentWindowsDisplaySelectedCommand;
			}

    	}

    	private void bookmarkTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var box = (sender as TextBox);
                box.Focusable = false;
                box.IsEnabled = false;
            }
        }

        #endregion  Commands    
    }
}