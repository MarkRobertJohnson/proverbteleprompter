using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        public event EventHandler<DocumentUpdatedEventArgs> DocumentUpdated;

        public EditWindow()
        {
            InitializeComponent();

            Closing += EditWindow_Closing;
        }

        void EditWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //For performance reasons, we only ever want to hide the window
            e.Cancel = true;
            Visibility = Visibility.Hidden;

        }

        private void UpdateDocument()
        {
            if (DocumentUpdated != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    DocumentHelpers.SaveDocument(ms, RichTextEditor.RichTextBox.Document, DataFormats.Rtf);
                    DocumentUpdated.Invoke(this, new DocumentUpdatedEventArgs(ms, DataFormats.Rtf));
                }

            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDocument();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
        }

        private void CloseAndUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDocument();

            Visibility = Visibility.Hidden;
        }


    }
}
