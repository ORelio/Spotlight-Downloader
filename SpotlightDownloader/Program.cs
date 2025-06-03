using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SpotlightDownloader.Commands;

namespace SpotlightDownloader
{
    /// <summary>
    /// Download Microsoft Spotlight images - By ORelio (c) 2018-2024 - CDDL 1.0
    /// </summary>
    class Program
    {
        public const string Name = "SpotlightDL";
        public const string Version = "1.5.0";

        static void Main(string[] args)
        {
            ParsedArguments parsed = null;
            try
            {
                parsed = ArgumentParser.Parse(args);
            }
            catch (ArgumentException ex)
            {
                if (ex.Message == "Show help")
                {
                    foreach (string str in new[]{
                        " ==== " + Name + " v" + Version + " - By ORelio and its contributors - Microzoom.fr ====",
                        "",
                        "Retrieve Windows Spotlight images by requesting the Microsoft Spotlight API.",
                        Name + " can also define images as wallpaper and system-wide lockscreen image.",
                        "",
                        "Usage:",
                        "  " + Name + ".exe <action> [arguments]",
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
                        "  --maxres            Force maximum image resolution (API v3). API v4 always returns max res.",
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
                        Console.Error.WriteLine(str);
                    }
                    Environment.Exit(1);
                }
                else
                {
                    Console.Error.WriteLine(ex.Message);
                    Environment.Exit(1);
                }
            }

            // Main logic using parsed arguments
            try
            {
                Queue<string> remainingLocales = new();

                if (parsed.AllLocales)
                {
                    remainingLocales = new Queue<string>(Locales.AllKnownSpotlightLocales);
                    Console.Error.WriteLine(string.Format("Starting download using {0} locales", remainingLocales.Count));
                    parsed.Locale = remainingLocales.Dequeue();
                    Console.Error.WriteLine(string.Format("Switching to {0} - {1} locales remaining", parsed.Locale, remainingLocales.Count + 1));
                }

                int downloadCount = 0;
                int noNewImgCount = 0;

                do
                {
                    try
                    {
                        SpotlightImage[] images = (parsed.FromFile != null && (parsed.Action == "wallpaper" || parsed.Action == "lockscreen"))
                            ? [new SpotlightImage()]
                            : Spotlight.GetImageUrlsAsync(parsed.MaximumRes, parsed.Portrait, parsed.Locale, parsed.ApiTryCount, parsed.ApiVersion).GetAwaiter().GetResult();

                        if (images.Length < 1)
                        {
                            Console.Error.WriteLine(Name + " received an empty image set from Spotlight API.");
                            Environment.Exit(2);
                        }

                        SpotlightImage randomImage = images.OrderBy(p => new Guid()).First();

                        if (parsed.Action == "urls")
                        {
                            if (parsed.SingleImage)
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
                            if (parsed.SingleImage || parsed.Action == "wallpaper" || parsed.Action == "lockscreen")
                            {
                                string imageFile = parsed.FromFile ?? randomImage.DownloadToFile(parsed.OutputDir, parsed.IntegrityCheck, parsed.Metadata, parsed.OutputName, parsed.ApiTryCount);
                                if (parsed.EmbedMetadata)
                                    imageFile = SpotlightImage.EmbedMetadata(imageFile, parsed.OutputDir, parsed.OutputName);
                                Console.WriteLine(imageFile);
                                if (parsed.Action == "wallpaper")
                                {
                                    try
                                    {
                                        Desktop.SetWallpaper(imageFile);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                                        Environment.Exit(4);
                                    }
                                }
                                else if (parsed.Action == "lockscreen")
                                {
                                    if (FileSystemAdmin.IsAdmin())
                                    {
                                        try
                                        {
                                            Lockscreen.SetGlobalLockscreen(imageFile);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                                            Environment.Exit(4);
                                        }
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("This program must run as administrator to change the global lockscreen.");
                                        Environment.Exit(4);
                                    }
                                }
                                Environment.Exit(0);
                            }

                            downloadCount = 0;

                            foreach (SpotlightImage image in images)
                            {
                                string imagePath = image.GetFilePath(parsed.OutputDir);
                                if (!File.Exists(imagePath))
                                {
                                    try
                                    {
                                        Console.WriteLine(image.DownloadToFile(parsed.OutputDir, parsed.IntegrityCheck, parsed.Metadata, null, parsed.ApiTryCount));
                                        downloadCount++;
                                        parsed.DownloadAmount--;
                                        if (parsed.DownloadAmount <= 0)
                                            break;
                                    }
                                    catch (InvalidDataException)
                                    {
                                        Console.Error.WriteLine("Skipping invalid image: " + image.Uri);
                                    }
                                }
                            }

                            if (parsed.Verbose)
                            {
                                Console.Error.WriteLine("Successfully downloaded: " + downloadCount + " images.");
                                Console.Error.WriteLine("Already downloaded: " + (images.Length - downloadCount) + " images.");
                            }

                            if (downloadCount == 0)
                                noNewImgCount++;
                            else noNewImgCount = 0;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                            Environment.Exit(3);
                        }
                    }
                    catch (Exception)
                    {
                        if (parsed.AllLocales && remainingLocales.Count > 0)
                        {
                            // Force switch to next locale
                            noNewImgCount = int.MaxValue;
                        }
                        else throw;
                    }

                    if (parsed.AllLocales && noNewImgCount >= 50 && remainingLocales.Count > 0)
                    {
                        noNewImgCount = 0;
                        parsed.Locale = remainingLocales.Dequeue();
                        Console.Error.WriteLine(string.Format("Switching to {0} - {1} locales remaining", parsed.Locale, remainingLocales.Count + 1));
                    }

                } while (parsed.DownloadMany && (downloadCount > 0 || noNewImgCount < 50) && parsed.DownloadAmount > 0);

                if (parsed.CacheSize < int.MaxValue && parsed.CacheSize > 0)
                {
                    foreach (FileInfo imgToDelete in
                        Directory.GetFiles(parsed.OutputDir, "*.jpg", SearchOption.TopDirectoryOnly)
                        .Select(filePath => new FileInfo(filePath))
                        .OrderByDescending(fileInfo => fileInfo.CreationTime)
                        .Skip(parsed.CacheSize))
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
                Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                Environment.Exit(2);
            }
        }
    }
}
