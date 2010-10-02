using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for TalentWindow.xaml
    /// </summary>
    public partial class TalentWindow : Window
    {

        public MainWindowViewModel MainWindowViewModel { get; set; }

        public TalentWindow()
        {
            InitializeComponent();

            MouseLeftButtonDown +=TalentWindow_MouseLeftButtonDown;
            MouseDoubleClick +=TalentWindow_MouseDoubleClick;
            Loaded += TalentWindow_Loaded;
            
        }

        void TalentWindow_Loaded(object sender, RoutedEventArgs e)
        {

            MainWindowViewModel = DataContext as MainWindowViewModel;
        }

        void TalentWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        void TalentWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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



    }
}
