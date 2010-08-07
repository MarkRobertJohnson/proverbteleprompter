using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tools.API.Messages.lParam;

namespace ProverbTeleprompter
{
    public class RemoteButtonPressedEventArgs : EventArgs
    {
        public WM_APPCOMMANDCommands AppCommand { get; set; }
    }
}
