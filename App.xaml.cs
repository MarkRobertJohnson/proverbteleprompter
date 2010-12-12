using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace ProverbTeleprompter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            base.OnStartup(e);
        }

  //      [PreEmptive.Attributes.Teardown]
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);

            if(e.Exception.InnerException != null)
                MessageBox.Show(e.Exception.InnerException.Message);
        }
    }
}
