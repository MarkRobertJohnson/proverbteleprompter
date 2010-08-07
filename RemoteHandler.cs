using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProverbTeleprompter
{
    public static class RemoteHandler
    {
        private const int WM_KEYDOWN = 0x0100;

        private const int WM_APPCOMMAND = 0x0319;


        public static event EventHandler<RemoteButtonPressedEventArgs> RemoteButtonPressed;
        public static event EventHandler<KeyPressEventArgs> KeyDown;


        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            int iChar;

            // Handle the navigation and numeric buttons.

            if (msg == WM_KEYDOWN)
            {
                iChar = wParam.ToInt32();



                if(KeyDown != null)
                {
                    KeyDown.Invoke(null, new KeyPressEventArgs((char)iChar));
                }


                switch (iChar)
                {
                    case (int)Keys.D0:
                        // Handle 0 key here.  
                        break;
                    // Insert more cases here.

                }  // End switch.

            }    // End key messages.
            if (msg == WM_APPCOMMAND)
            {
                var command = Tools.API.Messages.lParam.Macros.GET_APPCOMMAND_LPARAM(lParam.ToInt32());

            
                if(RemoteButtonPressed != null)
                {
                    RemoteButtonPressed.Invoke(null, new RemoteButtonPressedEventArgs { AppCommand = command});
                }

            }

            return IntPtr.Zero;
        }



        
                
    }

  
}
