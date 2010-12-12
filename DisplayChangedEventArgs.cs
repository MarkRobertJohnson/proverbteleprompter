using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProverbTeleprompter.Helpers;

namespace ProverbTeleprompter
{
    public class DisplayChangedEventArgs : EventArgs
    {
        public Win32.DEV_BROADCAST_DEVICEINTERFACE RawDisplayInfo { get; set; }

        public string FriendlyName { get; set; }

        public DisplayDetails DisplayDetails { get; set; }
    }
}
