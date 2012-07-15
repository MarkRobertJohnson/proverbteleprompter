using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ProverbTeleprompter.Helpers;
using Tools.API.Messages.wParam;
using System.Management;
using System.Linq;

namespace ProverbTeleprompter
{
    public static class SystemHandler
    {
        private const int WM_KEYDOWN = 0x0100;

        private const int WM_APPCOMMAND = 0x0319;


        public static event EventHandler<RemoteButtonPressedEventArgs> RemoteButtonPressed;
        public static event EventHandler<KeyPressEventArgs> KeyDown;

        public static event EventHandler<DisplayChangedEventArgs> DisplayAttached;
        public static event EventHandler<DisplayChangedEventArgs> DisplayRemoved;

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size; 
            public int dbcv_devicetype; 
            public int dbcv_reserved; 
            public int dbcv_unitmask;
        } 
        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr IntPtr, IntPtr NotificationFilter, Int32 Flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint UnregisterDeviceNotification(IntPtr hHandle);

        public static void RegisterHidNotification(IntPtr handle)
          {
            Win32.DEV_BROADCAST_DEVICEINTERFACE dbi = new
            Win32.DEV_BROADCAST_DEVICEINTERFACE();
            int size = Marshal.SizeOf(dbi);
            dbi.dbcc_size = size;
            dbi.dbcc_devicetype = Win32.DBT_DEVTYP_DEVICEINTERFACE;
            dbi.dbcc_reserved = 0;
            dbi.dbcc_classguid = Win32.GUID_DEVINTERFACE_HID;
            dbi.dbcc_name = "";
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(dbi, buffer, true);
            IntPtr r = Win32.RegisterDeviceNotification(handle, buffer,
            Win32.DEVICE_NOTIFY_WINDOW_HANDLE | Win32.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
            if(r == IntPtr.Zero){

            }
        }

        public static DisplayDetails GetMonitorInformation(string dbcc_name)
        {



            DisplayDetails  details = DisplayDetails.GetMonitorDetails().Where(x => dbcc_name.ToLower().Contains(x.PnPID.ToLower())).FirstOrDefault();




            return details;

        }


                
    }

  
}
