using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

using Microsoft.Win32;

using Windows.Storage;
using Windows.System.UserProfile;
using System.Collections.Generic;

namespace SpotlightDownloader
{
    static class LockScreenHelper
    {
        private const string DefaultLockscreenFolder = @"C:\Windows\Web\Screen";

        /// <summary>
        /// Replace the user lockscreen image for Windows 10 and later though Personalization API.
        /// </summary>
        /// <remarks>May requires Windows activation.</remarks>
        /// <param name="path">Path to the new image file for lockscreen, or null to reset to default lockscreen image</param>
        public static async Task<bool> SetUserImage(string imagePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("User lockscreen change is not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            if (!UserProfilePersonalizationSettings.IsSupported())
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                Console.WriteLine("User lockscreen change is not supported on this system.");
#pragma warning restore CA1303
                return false;
            }

            if (imagePath == null)
            {
                if (Directory.Exists(DefaultLockscreenFolder))
                {
                    // Look for valid image in system folder (usually named img100.jpg)
                    var defaultLockScreenCandidates = new List<string>();
                    defaultLockScreenCandidates.AddRange(Directory.GetFiles(DefaultLockscreenFolder, "*.jpg"));
                    defaultLockScreenCandidates.AddRange(Directory.GetFiles(DefaultLockscreenFolder, "*.png"));
                    var defaultLockscreen = defaultLockScreenCandidates.FirstOrDefault();

                    if (defaultLockscreen != null)
                    {
                        Console.WriteLine(defaultLockscreen);
                        var file = await StorageFile.GetFileFromPathAsync(defaultLockscreen);
                        Console.WriteLine($"Setting User LockScreen: {defaultLockscreen}");
                        await LockScreen.SetImageFileAsync(file);
                    }
                    else
                    {
                        throw new FileNotFoundException(Path.Combine(DefaultLockscreenFolder, "img100.jpg"));
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException(DefaultLockscreenFolder);
                }
            }
            else if (File.Exists(imagePath))
            {
                imagePath = Path.GetFullPath(imagePath);
                var file = await StorageFile.GetFileFromPathAsync(imagePath);
                Console.WriteLine($"Setting User LockScreen: {imagePath}");
                await LockScreen.SetImageFileAsync(file);
            }
            else
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync($"File not found: {imagePath}").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            return true;
        }

        private static readonly RegistryKey HiveUserSettingsLockscreen = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        private const string PathUserSettingsLockscreen = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        private static readonly string[] ValuesUserSettingsLockcreenTips = [
            "SubscribedContent-338389Enabled",
            "SubscribedContent-338388Enabled",
            "SubscribedContent-338387Enabled",
        ];

        /// <summary>
        /// Disable "Get fun facts, tips, tricks, and more on your lock screen" setting for the current user
        /// </summary>
        public static async Task<bool> DisableTipsCurrentUser()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("User lockscreen settings are not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            var keyUserSettingsLockscreen =
                HiveUserSettingsLockscreen.OpenSubKey(PathUserSettingsLockscreen, true)
                ?? HiveUserSettingsLockscreen.CreateSubKey(PathUserSettingsLockscreen, true);

            foreach (var value in ValuesUserSettingsLockcreenTips)
            {
                keyUserSettingsLockscreen.SetValue(value, 0);
            }

            return true;
        }

        private static readonly string PathLocalPolicyEditor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "gpedit.msc");

        /// <summary>
        /// Check if local policy is supported by the current Windows Edition.
        /// Windows Home edition has no local policy editor tool and will ignore policies if set in registry.
        /// </summary>
        /// <returns>TRUE if the system supports policies</returns>
        public static bool LocalPolicySupported()
        {
            return File.Exists(PathLocalPolicyEditor);
        }

        /// <summary>
        /// Check if the current process is running with elevated/administrator permissions
        /// </summary>
        /// <returns>TRUE if running elevated</returns>
        public static bool IsAdmin()
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static readonly RegistryKey HiveDisableSpotlight = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        private const string PathDisableSpotlight = @"Software\Policies\Microsoft\Windows\CloudContent";
        private const string ValueDisableSpotlight = "DisableWindowsSpotlightFeatures";

        /// <summary>
        /// Enable or Disable Spotlight features system-wide using Local Policy (GPO)
        /// </summary>
        /// <remarks>Only works for Enterprise and Education editions</remarks>
        /// <param name="enabled">True: Enable Spotlight features (default). False: Disable spotlight features</param>
        public static bool PolicySetSpotlightEnabled(bool enabled)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && LocalPolicySupported() && IsAdmin())
            {
                var keyDisableSpotlight = HiveDisableSpotlight.OpenSubKey(PathDisableSpotlight, true) ?? HiveDisableSpotlight.CreateSubKey(PathDisableSpotlight, true);

                if (enabled)
                {
                    Console.WriteLine($"HKLM\\{PathDisableSpotlight} -> {'-'}{ValueDisableSpotlight}");
                    keyDisableSpotlight.DeleteValue(ValueDisableSpotlight, false);
                }
                else
                {
                    Console.WriteLine($"HKLM\\{PathDisableSpotlight} -> {ValueDisableSpotlight}={1}");
                    keyDisableSpotlight.SetValue(ValueDisableSpotlight, 1);
                }

                return true;
            }
            return false;
        }

        private static readonly RegistryKey HiveSystemPolicyLockscreen = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        private const string PathSystemPolicyLockscreen = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
        private const string ValueSystemPolicyLockcreen = "LockScreenImagePath";

        /// <summary>
        /// Replace the system lockscreen image for Windows 10 and later through local policy.
        /// </summary>
        /// <remarks>Require admin privileges. Policy only works for Enterprise and Education editions.</remarks>
        /// <param name="path">Path to the new image file for lockscreen, or null to remove config</param>
        public static async Task<bool> SetSystemImage(string imagePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("System lockscreen change is not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            if (!LocalPolicySupported())
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("System lockscreen change is not supported on Windows Home edition.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            if (!IsAdmin())
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("Not running as administrator: Cannot set system-wide lockscreen image.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            var keyDefaultLockscreen =
                HiveSystemPolicyLockscreen.OpenSubKey(PathSystemPolicyLockscreen, true)
                ?? HiveSystemPolicyLockscreen.CreateSubKey(PathSystemPolicyLockscreen, true);

            if (imagePath == null)
            {
                Console.WriteLine($"HKLM\\{PathSystemPolicyLockscreen} -> {'-'}{ValueSystemPolicyLockcreen}");
                keyDefaultLockscreen.DeleteValue(ValueSystemPolicyLockcreen, false);
            }
            else if (File.Exists(imagePath))
            {
                Console.WriteLine($"HKLM\\{PathSystemPolicyLockscreen} -> {ValueSystemPolicyLockcreen}={Path.GetFullPath(imagePath)}");
                keyDefaultLockscreen.SetValue(ValueSystemPolicyLockcreen, Path.GetFullPath(imagePath));
            }
            else
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync($"File not found: {imagePath}").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            return true;
        }
    }
}
