using System;
using System.Runtime.InteropServices;

namespace SpotlightDownloader
{
    /// <summary>
    /// Programatically interact with the Desktop using Windows API
    /// By ORelio - (c) 2018 - Available under the CDDL-1.0 license
    /// </summary>

    public static class Desktop
    {
        //Code by Ondrej Balas
        //Source : stackoverflow.com/questions/6402834/

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_COMMAND = 0x111;

        public static void ToggleDesktopIcons()
        {
            var toggleDesktopCommand = new IntPtr(0x7402);
            IntPtr hWnd = GetWindow(FindWindow("Progman", null), GetWindow_Cmd.GW_CHILD);
            SendMessage(hWnd, WM_COMMAND, toggleDesktopCommand, IntPtr.Zero);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            private int _Left;
            private int _Top;
            private int _Right;
            private int _Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WINDOWINFO(Boolean? filler)
                : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            }

        }

        public static bool AreIconsVisible
        {
            get
            {
                IntPtr hWnd = GetWindow(GetWindow(FindWindow("Progman", "Program Manager"), GetWindow_Cmd.GW_CHILD), GetWindow_Cmd.GW_CHILD);
                WINDOWINFO info = new();
                info.cbSize = (uint)Marshal.SizeOf(info);
                GetWindowInfo(hWnd, ref info);
                return (info.dwStyle & 0x10000000) == 0x10000000;
            }
        }

        public static string GetCurrentWallpaper()
        {
            int SPI_GETDESKWALLPAPER = 0x73;
            int MAX_PATH = 260;
            string wallpaper = new('\0', (int)MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaper, 0);
            return wallpaper.Substring(0, wallpaper.IndexOf('\0'));
        }

        public static int SetWallpaper(string path, bool permanent = true)
        {
            int SPIF_UPDATEINIFILE = 0x01;
            int SPIF_SENDWININICHANGE = 0x02;
            int SPI_SETDESKWALLPAPER = 20;
            return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, System.IO.Path.GetFullPath(path), permanent ? SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE : 0);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UpdatePerUserSystemParameters();

        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        public static void RefreshDesktop()
        {
            SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
            System.Diagnostics.Process.Start("RUNDLL32.EXE", "USER32.DLL,UpdatePerUserSystemParameters 1, True");
            UpdatePerUserSystemParameters();
        }

        //stackoverflow.com/questions/5977445/

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }

        public static float GetScalingFactor()
        {
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;
            return ScreenScalingFactor; // 1.25 = 125%
        }
    }
}
