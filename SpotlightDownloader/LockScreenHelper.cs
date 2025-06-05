using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
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
                var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(imagePath));
                await LockScreen.SetImageFileAsync(file);
            }
            else
            {
                throw new FileNotFoundException(imagePath);
            }

            return true;
        }

        private static RegistryKey HiveDefaultLockscreen = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        private const string PathDefaultLockscreen = @"Software\Policies\Microsoft\Windows\Personalization";
        private const string ValueDefaultLockcreen = "LockscreenImage";

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

            var keyDefaultLockscreen = HiveDefaultLockscreen.OpenSubKey(PathDefaultLockscreen, true) ?? HiveDefaultLockscreen.CreateSubKey(PathDefaultLockscreen, true);

            if (imagePath == null)
            {
                keyDefaultLockscreen.DeleteValue(ValueDefaultLockcreen, false);
            }
            else if (File.Exists(imagePath))
            {
                keyDefaultLockscreen.SetValue(ValueDefaultLockcreen, Path.GetFullPath(imagePath));
            }
            else
            {
                throw new FileNotFoundException(imagePath);
            }

            return true;
        }

        private static RegistryKey HiveDisableSpotlight = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        private const string PathDisableSpotlight = @"Software\Policies\Microsoft\Windows\CloudContent";
        private const string ValueDisableSpotlight = "DisableWindowsSpotlightFeatures";

        /// <summary>
        /// Enable or Disable Spotlight features for the current user.
        /// </summary>
        /// <remarks>Policy only works for Enterprise and Education editions.</remarks>
        /// <param name="enabled">True: Enable Spotlight features (default). False: Disable spotlight features</param>
        public static async Task<bool> SetSpotlightEnabled(bool enabled)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("System policy change is not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            var keyDisableSpotlight = HiveDisableSpotlight.OpenSubKey(PathDisableSpotlight, true) ?? HiveDisableSpotlight.CreateSubKey(PathDisableSpotlight, true);

            if (enabled)
            {
                keyDisableSpotlight.DeleteValue(ValueDisableSpotlight, false);
            }
            else
            {
                keyDisableSpotlight.SetValue(ValueDisableSpotlight, 1);
            }

            return true;
        }

        /// <summary>
        /// Check if the current process is running with elevated/administrator permissions
        /// </summary>
        /// <returns>TRUE if running elevated</returns>
        public static bool IsAdmin()
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            WindowsPrincipal wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Set-up lockscreen image and settings (user, system and policy)
        /// </summary>
        /// <remarks>
        /// May requires Windows activation for user lockscreen.
        /// System and policy only works with Enterprise, Education editions.
        /// </remarks>
        /// <param name="path">Path to the new image file for lockscreen</param>
        public static async Task<bool> Setup(string imagePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("Lockscreen change is not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException(imagePath);
            }

            var success = true;

            try
            {
                await SetUserImage(imagePath).ConfigureAwait(false);

#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                Console.WriteLine("User lockscreen set successfully.");
#pragma warning restore CA1303
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync("Failed to set user lockscreen: " + e.Message).ConfigureAwait(false);
                throw;
            }

            if (IsAdmin())
            {
                try
                {
                    await SetSpotlightEnabled(false).ConfigureAwait(false);

    #pragma warning disable CA1303
                    // no plans for localization yet so temporary disable CA1303
                    Console.WriteLine("User policy set to disable built-in spotlight features (Enterprise, Education only).");
    #pragma warning restore CA1303
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException || e is SecurityException || e is IOException)
                    {
                        await Console.Error.WriteLineAsync("Failed to set user policy (Enterprise, Education only): " + e.Message).ConfigureAwait(false);
                        success = false;
                    }
                    else
                        throw;
                }

                try
                {
                    await SetSystemImage(imagePath).ConfigureAwait(false);

#pragma warning disable CA1303
                    // no plans for localization yet so temporary disable CA1303
                    Console.WriteLine("System lockscreen set successfully (Enterprise, Education only).");
#pragma warning restore CA1303
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException || e is SecurityException || e is IOException)
                    {
                        await Console.Error.WriteLineAsync("Failed to set system lockscreen (Enterprise, Education only): " + e.Message).ConfigureAwait(false);
                        success = false;
                    }
                    else
                        throw;
                }
            }
            else
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                Console.WriteLine("Not launched as admin: Not touching system lockscreen and policy (Enterprise, Education only).");
#pragma warning restore CA1303
            }

            return success;
        }

        /// <summary>
        /// Restore default lockscreen image and settings (user, system and policy)
        /// </summary>
        /// <remarks>
        /// May requires Windows activation for user lockscreen.
        /// System and policy only works with Enterprise, Education editions.
        /// </remarks>
        /// <param name="path">Path to the new image file for lockscreen</param>
        public static async Task<bool> Restore()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("Lockscreen change is not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            var success = true;

            try
            {
                await SetUserImage(null).ConfigureAwait(false);

#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                Console.WriteLine("User lockscreen successfully reset.");
#pragma warning restore CA1303
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException)
                {
                    await Console.Error.WriteLineAsync("Failed to reset user lockscreen: Could not find default lock screen image").ConfigureAwait(false);
                    success = false;
                }
                else
                {
                    await Console.Error.WriteLineAsync("Failed to reset user lockscreen: " + e.Message).ConfigureAwait(false);
                    throw;
                }
            }

            if (IsAdmin())
            {
                try
                {
                    await SetSpotlightEnabled(true).ConfigureAwait(false);

    #pragma warning disable CA1303
                    // no plans for localization yet so temporary disable CA1303
                    Console.WriteLine("User policy reset to allow built-in spotlight features (Enterprise, Education only).");
    #pragma warning restore CA1303
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException || e is SecurityException || e is IOException)
                    {
                        await Console.Error.WriteLineAsync("Failed to reset user policy (Enterprise, Education only): " + e.Message).ConfigureAwait(false);
                        success = false;
                    }
                    else
                        throw;
                }

                try
                {
                    await SetSystemImage(null).ConfigureAwait(false);

#pragma warning disable CA1303
                    // no plans for localization yet so temporary disable CA1303
                    Console.WriteLine("System lockscreen reset successfully (Enterprise, Education only).");
#pragma warning restore CA1303
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException || e is SecurityException || e is IOException)
                    {
                        await Console.Error.WriteLineAsync("Failed to reset system lockscreen (Enterprise, Education only): " + e.Message).ConfigureAwait(false);
                        success = false;
                    }
                    else
                        throw;
                }
            }
            else
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                Console.WriteLine("Not launched as admin: Not touching system lockscreen and policy (Enterprise, Education only).");
#pragma warning restore CA1303
            }

            return success;
        }
    }
}
