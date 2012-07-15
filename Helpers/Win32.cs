﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace ProverbTeleprompter.Helpers
{
    public class Win32
    {
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;

        public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;

        public const int SERVICE_CONTROL_STOP = 1;
        public const int SERVICE_CONTROL_DEVICEEVENT = 11;
        public const int SERVICE_CONTROL_SHUTDOWN = 5;

        public static Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");



        public const uint GENERIC_READ = 0x80000000;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_SHARE_READ = 1;
        public const uint FILE_SHARE_WRITE = 2;
        public const uint FILE_SHARE_DELETE = 4;
        public const uint FILE_ATTRIBUTE_NORMAL = 128;
        public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        public const int DBT_DEVTYP_HANDLE = 6;

        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEQUERYREMOVE = 0x8001;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
              
        const int DBT_DEVTYP_VOLUME = 0x00000002;  // logical volume   

        public const int WM_DEVICECHANGE = 0x219;
    	public const int WM_DISPLAYCHANGE = 0x7E;




        public delegate int ServiceControlHandlerEx(int control, int eventType, IntPtr eventData, IntPtr context);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandlerEx cbex, IntPtr context);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVolumePathNamesForVolumeNameW(
                [MarshalAs(UnmanagedType.LPWStr)]
					string lpszVolumeName,
                [MarshalAs(UnmanagedType.LPWStr)]
					string lpszVolumePathNames,
                uint cchBuferLength,
                ref UInt32 lpcchReturnLength);

        [DllImport("kernel32.dll")]
        public static extern bool GetVolumeNameForVolumeMountPoint(string
           lpszVolumeMountPoint, [Out] StringBuilder lpszVolumeName,
           uint cchBufferLength);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr IntPtr, IntPtr NotificationFilter, Int32 Flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint UnregisterDeviceNotification(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(
              string FileName,                    // file name
              uint DesiredAccess,                 // access mode
              uint ShareMode,                     // share mode
              uint SecurityAttributes,            // Security Attributes
              uint CreationDisposition,           // how to create
              uint FlagsAndAttributes,            // file attributes
              int hTemplateFile                   // handle to template file
              );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEV_BROADCAST_DEVICEINTERFACE
        {
            //public int dbcc_size;
            //public int dbcc_devicetype;
            //public int dbcc_reserved;
            //[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            //public byte[] dbcc_classguid;
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            //public char[] dbcc_name;
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HDR
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HANDLE
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
            public IntPtr dbch_handle;
            public IntPtr dbch_hdevnotify;
            public Guid dbch_eventguid;
            public long dbch_nameoffset;
            public byte dbch_data;
            public byte dbch_data1;
        }

        public static string GetDeviceName(DEV_BROADCAST_DEVICEINTERFACE dvi)
        {
            string[] Parts = dvi.dbcc_name.Split('#'); if (Parts.Length >= 3)
            {
                string DevType = Parts[0].Substring(Parts[0].IndexOf(@"?\") + 2); 
                string DeviceInstanceId = Parts[1]; 
                string DeviceUniqueID = Parts[2]; 
                string RegPath = @"SYSTEM\CurrentControlSet\Enum\" + DevType + "\\" + DeviceInstanceId + "\\" + DeviceUniqueID; 
                RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath); 
                if (key != null) { 
                    object result = key.GetValue("FriendlyName"); 
                    if (result != null)                
                        return result.ToString(); 
                    result = key.GetValue("DeviceDesc"); 
                    if (result != null)                
                        return result.ToString(); 
                }
            } 
            return String.Empty;
        }

        public static string GetDeviceInfo(DEV_BROADCAST_DEVICEINTERFACE dvi)
        {
            string[] Parts = dvi.dbcc_name.Split('#'); if (Parts.Length >= 3)
            {
                string DevType = Parts[0].Substring(Parts[0].IndexOf(@"?\") + 2);
                string DeviceInstanceId = Parts[1];
                string DeviceUniqueID = Parts[2];
                Guid deviceGuid;
                if(Parts.Length > 3)
                {
                    deviceGuid = Guid.Parse(Parts[3]);
                }

            }
            return String.Empty;
        }
    }
}
