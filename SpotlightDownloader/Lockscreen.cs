using System;
using System.IO;
using System.Linq;

using Microsoft.Win32;

namespace SpotlightDownloader
{
    /// <summary>
    /// Wrapper around the Windows lockscreen image
    /// By ORelio (c) 2018 - CDDL 1.0
    /// </summary>
    class Lockscreen
    {
        /// <summary>
        /// Replace the system lockscreen image for Windows 7, 8 and 10.
        /// </summary>
        /// <remarks>Administrator permissions are required</remarks>
        /// <param name="path">Path to the new .jpg file for global lockscreen</param>
        public static void SetGlobalLockscreen(string path)
        {
            if (WindowsVersion.IsMono)
            {
                throw new NotImplementedException("Lockscreen handling is not implemented for Mac and Linux using the Mono framework.");
            }
            else if (WindowsVersion.WinMajorVersion == 6 && WindowsVersion.WinMinorVersion == 1 /* Windows 7 */)
            {
                string lockscreenDir = Win7_InitDir();
                string lockscreenPic = String.Concat(lockscreenDir, Path.DirectorySeparatorChar, "backgroundDefault.jpg");
                PerformBackupReplace(lockscreenDir, true);
                ImageEncoder.CreateJpeg(path, lockscreenPic, 256000);
                Win7_RegistryKey(true);

                //Create a dummy backup if the machine contains no OEM background image
                if (!File.Exists(lockscreenPic + ".bak"))
                    File.Create(lockscreenPic + ".bak").Close();
            }
            else if ((WindowsVersion.WinMajorVersion == 6 && WindowsVersion.WinMajorVersion >= 2) /* Windows 8 and 10 */
                || WindowsVersion.WinMajorVersion >= 10 /* Windows 10 and future Windows versions - might not work on newer versions */)
            {
                string lockscreenDir = Win10_InitDir();
                PerformBackupReplace(lockscreenDir, false, path);
                Win10_ClearCache();
            }
            else
            {
                throw new NotImplementedException(
                    String.Format(
                        "Lockscreen handling is not implemented for Windows NT {0}.{1}",
                        WindowsVersion.WinMajorVersion,
                        WindowsVersion.WinMinorVersion
                    )
                );
            }
        }

        /// <summary>
        /// Restore the system default lockscreen image for Windows 7, 8 and 10.
        /// </summary>
        /// <remarks>Administrator permissions are required</remarks>
        public static void RestoreDefaultGlobalLockscreen()
        {
            if (WindowsVersion.IsMono)
            {
                throw new NotImplementedException("Lockscreen handling is not implemented for Mac and Linux using the Mono framework.");
            }
            else if (WindowsVersion.WinMajorVersion == 6 && WindowsVersion.WinMinorVersion == 1 /* Windows 7 */)
            {
                string lockscreenDir = Win7_InitDir();
                string lockscreenPic = String.Concat(lockscreenDir, Path.DirectorySeparatorChar, "backgroundDefault.jpg");
                File.Delete(lockscreenPic);
                RestoreBackup(lockscreenDir);

                //Dummy backup indicates there was no original OEM image on this machine, we need to disable OEMBackground
                if (File.Exists(lockscreenPic) && new FileInfo(lockscreenPic).Length == 0)
                {
                    File.Delete(lockscreenPic);
                    Win7_RegistryKey(false);
                }
            }
            else if ((WindowsVersion.WinMajorVersion == 6 && WindowsVersion.WinMinorVersion >= 2) /* Windows 8 and 10 */
                || WindowsVersion.WinMajorVersion >= 10 /* Windows 10 and future Windows versions - might not work on newer versions */)
            {
                string lockscreenDir = Win10_InitDir();
                RestoreBackup(lockscreenDir);
                Win10_ClearCache();
            }
            else
            {
                throw new NotImplementedException(
                    String.Format(
                        "Lockscreen handling is not implemented for Windows NT {0}.{1}",
                        WindowsVersion.WinMajorVersion,
                        WindowsVersion.WinMinorVersion
                    )
                );
            }
        }

        /// <summary>
        /// Initialize permissions for the Windows 7 lock screen directory and return its path
        /// </summary>
        /// <returns>Windows 7 lock screen directory path</returns>
        private static string Win7_InitDir()
        {
            string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + (Environment.Is64BitOperatingSystem ? @"\Sysnative\oobe\" : @"\System32\oobe\");
            string lockscreenDir = systemDir + @"info\backgrounds";

            if (!Directory.Exists(lockscreenDir))
            {
                FileSystemAdmin.GrantAll(systemDir, false);
                Directory.CreateDirectory(lockscreenDir);
            }

            FileSystemAdmin.GrantAll(lockscreenDir, true);
            return lockscreenDir;
        }

        /// <summary>
        /// Set the OEMBackground registry key for Windows 7
        /// </summary>
        /// <param name="enable">TRUE to enable OEMBackground</param>
        private static void Win7_RegistryKey(bool enable)
        {
            RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
            RegistryKey regLogonSettings = localMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Background");
            regLogonSettings.SetValue("OEMBackground", enable ? 1 : 0);
        }

        /// <summary>
        /// Initialize permissions for the Windows 8/10 lock screen directory and return its path
        /// </summary>
        /// <returns>Windows 8/10 lock screen directory path</returns>
        private static string Win10_InitDir()
        {
            string lockscreenDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Web\Screen";
            FileSystemAdmin.GrantAll(lockscreenDir, true);
            return lockscreenDir;
        }

        /// <summary>
        /// Clear the Windows 8/10 lock screen cache to force lock screen refresh
        /// </summary>
        private static void Win10_ClearCache()
        {
            string systemDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Microsoft\Windows\SystemData";
            FileSystemAdmin.GrantAll(systemDataDir, true);
            foreach (string cacheDir in Directory.EnumerateDirectories(systemDataDir, "LockScreen_*", SearchOption.AllDirectories))
                foreach (string cacheFile in Directory.EnumerateFiles(cacheDir, "*.jpg", SearchOption.AllDirectories))
                    File.Delete(cacheFile);
        }

        /// <summary>
        /// Perform backups of jpg and png files in the specified directory. Files with an existing backup are ignored.
        /// </summary>
        /// <param name="targetDir">Target directory</param>
        /// <param name="move">If enabled, remove the original files after performing a backup</param>
        /// <param name="overwriteWithImage">If specified, also replace all the original images with the specified image file</param>
        public static void PerformBackupReplace(string targetDir, bool removeOriginal, string overwriteWithImage = null)
        {
            foreach (string picture in Directory.GetFiles(targetDir, "*.jpg").Union(Directory.GetFiles(targetDir, "*.png")))
            {
                string pictureBackup = picture + ".bak";

                if (!File.Exists(pictureBackup))
                {
                    if (removeOriginal)
                    {
                        File.Move(picture, pictureBackup);
                    }
                    else File.Copy(picture, pictureBackup);
                }

                if (overwriteWithImage != null)
                {
                    if (!File.Exists(overwriteWithImage))
                        throw new ArgumentException("The specified image file does not exist.", "overwriteWithImage");
                    ImageEncoder.AutoCopyImageFile(overwriteWithImage, picture);
                }
            }
        }

        /// <summary>
        /// Restore backups of jpg and png files in the specified directory
        /// </summary>
        /// <param name="targetDir">Target directory</param>
        private static void RestoreBackup(string targetDir)
        {
            foreach (string pictureBackup in Directory.GetFiles(targetDir, "*.jpg.bak").Union(Directory.GetFiles(targetDir, "*.png.bak")))
            {
                string picture = pictureBackup.Substring(0, pictureBackup.Length - 4);
                if (File.Exists(picture))
                    File.Delete(picture);
                File.Move(pictureBackup, picture);
            }
        }
    }
}
