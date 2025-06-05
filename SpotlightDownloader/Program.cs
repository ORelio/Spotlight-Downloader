using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SpotlightDownloader.CommandLineHelper;

namespace SpotlightDownloader
{
    /// <summary>
    /// Download Microsoft Spotlight images - By ORelio (c) 2018-2024 - CDDL 1.0
    /// </summary>
    sealed class Program
    {
        public const string Name = "SpotlightDL";
        public const string Version = "1.5.0";

        static async Task Main(string[] args)
        {
            ParsedArguments cmd = null;
            try
            {
                cmd = ArgumentParser.Parse(args);
            }
            catch (ArgumentException ex)
            {
                if (ex.Message == "Show help")
                {
                    foreach (string str in new[]{
                        $" ==== {Name} v{Version} - https://github.com/ORelio/Spotlight-Downloader ====",
                        "",
                        "Retrieve Windows Spotlight images by requesting the Microsoft Spotlight API.",
                        $"{Name} can also define images as wallpaper and system-wide lockscreen image.",
                        "",
                        "Usage:",
                        $"  {Name}.exe <action> [arguments]",
                        "  Only one action must be provided, as first argument",
                        "  Then, provide any number of arguments from the list below.",
                        "",
                        "Actions:",
                        "  urls                Query Spotlight API and print image URLs to standard output",
                        "  download            Download all images and print file path to standard output",
                        "  wallpaper           Download one random image and define it as wallpaper",
                        "  lockscreen          Download one random image and define it as global lockscreen",
                        "",
                        "Arguments:",
                        "  --help              Show detailed program usage (this message)",
                        "  --single            Print only one random url or download only one image as spotlight.jpg",
                        "  --many              Try downloading as much images as possible by calling API many times",
                        "  --amount <n>        Stop downloading after <n> images successfully downloaded, implies --many",
                        "  --cache-size <n>    Only keep <n> most recent images in the output directory, delete others",
                        "  --portrait          Force portrait image instead of autodetecting from current screen res",
                        "  --landscape         Force landscape image instead of autodetecting from current screen res",
                        "  --locale <xx-XX>    Force specified locale, e.g. en-US, instead of autodetecting from system",
                        "  --all-locales       Attempt to download images for all known Spotlight locales, implies --many",
                        "  --outdir <dir>      Set output directory instead of defaulting to working directory",
                        "  --outname <name>    Set output file name as <name>.ext for --single or --embed-meta",
                        "  --api-tries <n>     Amount of unsuccessful API calls before giving up. Default is 3.",
                        "  --api-version <v>   Spotlight API version: 3 (Windows 10) or 4 (Windows 11). Default is 4",
                        "  --skip-integrity    Skip integrity check: file size and sha256 (API v3) or parse image (API v4)",
                        "  --metadata          Also save image metadata such as title & copyright as <image-name>.txt",
                        "  --embed-meta        When available, embed metadata into wallpaper or lockscreen image",
                        "  --from-file         Set the specified file as wallpaper/lockscreen instead of downloading",
                        "  --from-dir          Set a random image from the specified directory as wallpaper/lockscreen",
                        "  --verbose           Display additional status messages while downloading images from API",
                        "",
                        "Exit codes:",
                        "  0                   Success",
                        "  1                   Invalid arguments",
                        "  2                   Spotlight API request failure",
                        "  3                   Image download failure or Failed to write image to disk",
                        "  4                   Failed to define image as wallpaper or lock screen image"
                    })
                    {
                        await Console.Error.WriteLineAsync(str).ConfigureAwait(false);
                    }
                    Environment.Exit(1);
                }
                else
                {
                    await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                    Environment.Exit(1);
                }
            }

            // Main logic using parsed arguments
            try
            {
                Queue<string> remainingLocales = new();

                if (cmd.AllLocales)
                {
                    remainingLocales = new Queue<string>(Locales.AllKnownSpotlightLocales);
                    await Console.Error.WriteLineAsync($"Starting download using {remainingLocales.Count} locales").ConfigureAwait(false);
                    cmd.Locale = remainingLocales.Dequeue();
                    await Console.Error.WriteLineAsync($"Switching to {cmd.Locale} - {remainingLocales.Count + 1} locales remaining").ConfigureAwait(false);
                }

                int downloadCount = 0;
                int noNewImgCount = 0;

                do
                {
                    try
                    {
                        SpotlightImage[] images = (cmd.FromFile != null && (cmd.Action == "wallpaper" || cmd.Action == "lockscreen"))
                            ? [new SpotlightImage()]
                            : await SpotlightApi.GetImageUrlsAsync(cmd.Portrait, cmd.Locale, cmd.ApiTryCount, cmd.ApiVersion).ConfigureAwait(false);

                        if (images.Length < 1)
                        {
                            await Console.Error.WriteLineAsync($"{Name} received an empty image set from Spotlight API.").ConfigureAwait(false);
                            Environment.Exit(2);
                        }

                        Random rng = new();
#pragma warning disable CA5394
                        // false alarm: just randomizing the order of the images, no need for cryptographic purposes
                        SpotlightImage randomImage = images[rng.Next(images.Length)];
#pragma warning restore CA5394

                        if (cmd.Action == "urls")
                        {
                            if (cmd.SingleImage)
                            {
                                Console.WriteLine(randomImage.Uri);
                            }
                            else
                            {
                                foreach (SpotlightImage image in images)
                                {
                                    Console.WriteLine(image.Uri);
                                }
                            }
                            Environment.Exit(0);
                        }

                        try
                        {
                            if (cmd.SingleImage || cmd.Action == "wallpaper" || cmd.Action == "lockscreen")
                            {
                                string imageFile = cmd.FromFile ?? await randomImage.DownloadToFile(cmd.OutputDir, cmd.IntegrityCheck, cmd.Metadata, cmd.OutputName, cmd.ApiTryCount).ConfigureAwait(false);

                                if (!Path.IsPathRooted(imageFile))
                                {
                                    imageFile = Path.GetFullPath(imageFile);
                                }

                                Console.WriteLine(imageFile);

                                if (cmd.EmbedMetadata)
                                {
                                    imageFile = SpotlightImage.EmbedMetadata(
                                        imageFile,
                                        Path.GetDirectoryName(imageFile),
                                        cmd.OutputName ?? Path.GetFileNameWithoutExtension(imageFile)
                                    );
                                }
                                if (cmd.Action == "wallpaper")
                                {
                                    try
                                    {
                                        WallpaperHelper.SetWallpaper(imageFile);
#pragma warning disable CA1303
                                        // no plans for localization yet so temporary disable CA1303
                                        Console.WriteLine("Wallpaper set successfully.");
#pragma warning restore CA1303
                                    }
                                    catch (Exception e)
                                    {
                                        await Console.Error.WriteLineAsync("Failed to set wallpaper: " + e.Message).ConfigureAwait(false);
                                        Environment.Exit(4);
                                        throw;
                                    }
                                }
                                else if (cmd.Action == "lockscreen")
                                {
                                    try
                                    {
                                        await LockScreenHelper.SetLockScreen(imageFile).ConfigureAwait(false);
#pragma warning disable CA1303
                                        // no plans for localization yet so temporary disable CA1303
                                        Console.WriteLine("Lockscreen set successfully.");
#pragma warning restore CA1303
                                    }
                                    catch (Exception e)
                                    {
                                        await Console.Error.WriteLineAsync("Failed to set lockscreen: " + e.Message).ConfigureAwait(false);
                                        Environment.Exit(4);
                                        throw;
                                    }
                                }
                                Environment.Exit(0);
                            }

                            downloadCount = 0;

                            foreach (SpotlightImage image in images)
                            {
                                string imagePath = image.GetFilePath(cmd.OutputDir);
                                if (!File.Exists(imagePath))
                                {
                                    try
                                    {
                                        Console.WriteLine(image.DownloadToFile(cmd.OutputDir, cmd.IntegrityCheck, cmd.Metadata, null, cmd.ApiTryCount));
                                        downloadCount++;
                                        cmd.DownloadAmount--;
                                        if (cmd.DownloadAmount <= 0)
                                            break;
                                    }
                                    catch (InvalidDataException)
                                    {
                                        await Console.Error.WriteLineAsync($"Skipping invalid image: {image.Uri}").ConfigureAwait(false);
                                    }
                                }
                            }

                            if (cmd.Verbose)
                            {
                                await Console.Error.WriteLineAsync($"Successfully downloaded: {downloadCount} images.").ConfigureAwait(false);
                                await Console.Error.WriteLineAsync($"Already downloaded: {images.Length - downloadCount} images.").ConfigureAwait(false);
                            }

                            if (downloadCount == 0)
                                noNewImgCount++;
                            else noNewImgCount = 0;
                        }
                        catch (Exception e)
                        {
                            await Console.Error.WriteLineAsync($"{e.GetType()}: {e.Message}").ConfigureAwait(false);
                            Environment.Exit(3);
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        if (cmd.AllLocales && remainingLocales.Count > 0)
                        {
                            // Force switch to next locale
                            noNewImgCount = int.MaxValue;
                        }
                        else throw;
                    }

                    if (cmd.AllLocales && noNewImgCount >= 50 && remainingLocales.Count > 0)
                    {
                        noNewImgCount = 0;
                        cmd.Locale = remainingLocales.Dequeue();
                        await Console.Error.WriteLineAsync($"Switching to {cmd.Locale} - {remainingLocales.Count + 1} locales remaining").ConfigureAwait(false);
                    }

                } while (cmd.DownloadMany && (downloadCount > 0 || noNewImgCount < 50) && cmd.DownloadAmount > 0);

                if (cmd.CacheSize < int.MaxValue && cmd.CacheSize > 0)
                {
                    foreach (FileInfo imgToDelete in
                        Directory.GetFiles(cmd.OutputDir, "*.jpg", SearchOption.TopDirectoryOnly)
                        .Select(filePath => new FileInfo(filePath))
                        .OrderByDescending(fileInfo => fileInfo.CreationTime)
                        .Skip(cmd.CacheSize))
                    {
                        string metadataFile = SpotlightImage.GetMetaLocation(imgToDelete.FullName);
                        if (File.Exists(metadataFile))
                            File.Delete(metadataFile);
                        imgToDelete.Delete();
                    }
                }

                Environment.Exit(0);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"{e.GetType()}: {e.Message}").ConfigureAwait(false);
                Environment.Exit(2);
                throw;
            }
        }
    }
}
