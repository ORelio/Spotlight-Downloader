using System;
using System.Runtime.InteropServices;

namespace SpotlightDownloader
{
    internal static class Desktop
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        public static (bool Success, string ErrorMessage) SetWallpaper(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                    return (false, $"File not found: {path}");

                int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                if (result == 0)
                    return (false, "Failed to set wallpaper via SystemParametersInfo");

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }
    }
}