using System;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.System.UserProfile;

namespace SpotlightDownloader
{
    static class LockScreenHelper
    {
        /// <summary>
        /// Replace the system lockscreen image for Windows 10 and later.
        /// </summary>
        /// <remarks>May requires Windows activation.</remarks>
        /// <param name="path">Path to the new .jpg file for lockscreen</param>
        public static async Task<bool> SetLockScreen(string imagePath)
        {
            if (!PlatformHelper.IsWindows())
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                await Console.Error.WriteLineAsync("Lockscreen change is not supported on this platform.").ConfigureAwait(false);
#pragma warning restore CA1303
                return false;
            }

            if (!UserProfilePersonalizationSettings.IsSupported())
            {
#pragma warning disable CA1303
                // no plans for localization yet so temporary disable CA1303
                Console.WriteLine("Lockscreen change is not supported on this system.");
#pragma warning restore CA1303
                return false;
            }
            var file = await StorageFile.GetFileFromPathAsync(imagePath);
            await LockScreen.SetImageFileAsync(file);
            return true;
        }
    }
}
