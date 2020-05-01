using System;
using System.Linq;
using System.IO;
using SharpTools;
using System.Diagnostics;
using System.Collections.Generic;

namespace SpotlightDownloader
{
    /// <summary>
    /// Download Microsoft Spotlight images - By ORelio (c) 2018-2020 - CDDL 1.0
    /// </summary>
    class Program
    {
        public const string Name = "SpotlightDL";
        public const string Version = "1.4.3";

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string action = null;
                bool singleImage = false;
                bool maximumRes = false;
                bool? portrait = false;
                string locale = null;
                bool allLocales = false;
                string outputDir = ".";
                string outputName = "spotlight";
                bool integrityCheck = true;
                bool downloadMany = false;
                int downloadAmount = int.MaxValue;
                int cacheSize = int.MaxValue;
                bool metadata = false;
                bool metadataAllowInconsistent = false;
                bool embedMetadata = false;
                string fromFile = null;
                int apiTryCount = 3;
                bool verbose = false;

                switch (args[0].ToLower())
                {
                    case "urls":
                    case "download":
                    case "wallpaper":
                    case "lockscreen":
                        action = args[0].ToLower();
                        break;
                    default:
                        Console.Error.WriteLine("Unknown action: " + args[0]);
                        Environment.Exit(1);
                        break;
                }

                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        switch (args[i].ToLower())
                        {
                            case "--single":
                                singleImage = true;
                                break;
                            case "--many":
                                downloadMany = true;
                                break;
                            case "--amount":
                                i++;
                                downloadMany = true;
                                if (i >= args.Length)
                                {
                                    Console.Error.WriteLine("--amount expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (!int.TryParse(args[i], out downloadAmount) || downloadAmount < 0)
                                {
                                    Console.Error.WriteLine("Download amount must be a valid and positive number.");
                                    Environment.Exit(1);
                                }
                                if (downloadAmount == 0)
                                    downloadAmount = int.MaxValue;
                                break;
                            case "--cache-size":
                                i++;
                                if (i >= args.Length)
                                {
                                    Console.Error.WriteLine("--cache-size expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (!int.TryParse(args[i], out cacheSize) || cacheSize <= 0)
                                {
                                    Console.Error.WriteLine("Cache size must be a valid and strictly positive number.");
                                    Environment.Exit(1);
                                }
                                break;
                            case "--maxres":
                                maximumRes = true;
                                break;
                            case "--portrait":
                                portrait = true;
                                break;
                            case "--landscape":
                                portrait = false;
                                break;
                            case "--locale":
                                i++;
                                if (i < args.Length)
                                    locale = args[i];
                                else
                                {
                                    Console.Error.WriteLine("--locale expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (!System.Text.RegularExpressions.Regex.Match(locale, "^[a-z]{2}-[A-Z]{2}$").Success)
                                {
                                    Console.Error.WriteLine("--locale expected format is xx-XX, e.g. en-US.");
                                    Environment.Exit(1);
                                }
                                break;
                            case "--all-locales":
                                allLocales = true;
                                downloadMany = true;
                                break;
                            case "--outdir":
                                i++;
                                if (i < args.Length)
                                    outputDir = args[i];
                                else
                                {
                                    Console.Error.WriteLine("--outdir expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (!Directory.Exists(outputDir))
                                {
                                    Console.Error.WriteLine("Output directory '" + outputDir + "' does not exist.");
                                    Environment.Exit(1);
                                }
                                break;
                            case "--outname":
                                i++;
                                if (i < args.Length)
                                    outputName = args[i];
                                else
                                {
                                    Console.Error.WriteLine("--outname expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                foreach (char invalidChar in Path.GetInvalidFileNameChars())
                                {
                                    if (outputName.Contains(invalidChar))
                                    {
                                        Console.Error.WriteLine("Invalid character '" + invalidChar + "' in specified output file name.");
                                        Environment.Exit(1);
                                    }
                                }
                                break;
                            case "--skip-integrity":
                                integrityCheck = false;
                                break;
                            case "--api-tries":
                                i++;
                                if (i >= args.Length)
                                {
                                    Console.Error.WriteLine("--api-tries expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (!int.TryParse(args[i], out apiTryCount) || apiTryCount <= 0)
                                {
                                    Console.Error.WriteLine("API tries must be a valid and strictly positive number.");
                                    Environment.Exit(1);
                                }
                                break;
                            case "--metadata":
                                metadata = true;
                                break;
                            case "--inconsistent-metadata":
                                metadata = true;
                                metadataAllowInconsistent = true;
                                break;
                            case "--embed-meta":
                                embedMetadata = true;
                                break;
                            case "--from-file":
                                i++;
                                if (i < args.Length)
                                    fromFile = args[i];
                                else
                                {
                                    Console.Error.WriteLine("--from-file expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (!File.Exists(fromFile))
                                {
                                    Console.Error.WriteLine("Input file '" + fromFile + "' does not exist.");
                                    Environment.Exit(1);
                                }
                                break;
                            case "--from-dir":
                                i++;
                                if (i < args.Length)
                                    fromFile = args[i];
                                else
                                {
                                    Console.Error.WriteLine("--from-dir expects an additional argument.");
                                    Environment.Exit(1);
                                }
                                if (Directory.Exists(fromFile))
                                {
                                    string[] jpegFiles = Directory.EnumerateFiles(fromFile, "*.jpg", SearchOption.AllDirectories).ToArray();
                                    if (jpegFiles.Any())
                                    {
                                        fromFile = jpegFiles[new Random().Next(0, jpegFiles.Length)];
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("Input directory '" + fromFile + "' does not contain JPG files.");
                                        Environment.Exit(1);
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine("Input directory '" + fromFile + "' does not exist.");
                                    Environment.Exit(1);
                                }
                                break;
                            case "--restore":
                                if (action == "lockscreen")
                                {
                                    if (FileSystemAdmin.IsAdmin())
                                    {
                                        try
                                        {
                                            Lockscreen.RestoreDefaultGlobalLockscreen();
                                            Environment.Exit(0);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                                            Environment.Exit(4);
                                        }
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("This program must run as administrator to restore the global lockscreen.");
                                        Environment.Exit(4);
                                    }
                                }
                                break;
                            case "--verbose":
                                verbose = true;
                                break;
                            default:
                                Console.Error.WriteLine("Unknown argument: " + args[i]);
                                Environment.Exit(1);
                                break;
                        }
                    }

                    if (downloadMany && cacheSize < downloadAmount)
                    {
                        Console.Error.WriteLine(
                            "Download amount ({0}) is greater than cache size ({1}). Reducing download amount to {1}.",
                            downloadAmount == int.MaxValue ? "MAX" : downloadAmount.ToString(),
                            cacheSize
                        );
                        downloadAmount = cacheSize;
                    }

                    if (downloadMany && metadata && allLocales && !metadataAllowInconsistent)
                    {
                        Console.Error.WriteLine("--metadata combined with --all-locales will produce random metadata languages.");
                        Console.Error.WriteLine("Please relaunch with --inconsistent-metadata if you really intend to do this.");
                        Environment.Exit(1);
                    }
                }

                try
                {
                    Queue<string> remainingLocales = new Queue<string>();

                    if (allLocales)
                    {
                        remainingLocales = new Queue<string>(Locales.AllKnownSpotlightLocales);
                        Console.Error.WriteLine(String.Format("Starting download using {0} locales", remainingLocales.Count));
                        locale = remainingLocales.Dequeue();
                        Console.Error.WriteLine(String.Format("Switching to {0} - {1} locales remaining", locale, remainingLocales.Count + 1));
                    }

                    int downloadCount = 0;
                    int noNewImgCount = 0;

                    do
                    {
                        SpotlightImage[] images = (fromFile != null && (action == "wallpaper" || action == "lockscreen"))
                            ? new[] { new SpotlightImage() } // Skip API request, we'll use a local file
                            : Spotlight.GetImageUrls(maximumRes, portrait, locale, apiTryCount);

                        if (images.Length < 1)
                        {
                            Console.Error.WriteLine(Program.Name + " received an empty image set from Spotlight API.");
                            Environment.Exit(2);
                        }

                        SpotlightImage randomImage = images.OrderBy(p => new Guid()).First();

                        if (action == "urls")
                        {
                            if (singleImage)
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
                            if (singleImage || action == "wallpaper" || action == "lockscreen")
                            {
                                string imageFile = fromFile ?? randomImage.DownloadToFile(outputDir, integrityCheck, metadata, outputName, apiTryCount);
                                if (embedMetadata)
                                    imageFile = SpotlightImage.EmbedMetadata(imageFile, outputDir, outputName);
                                Console.WriteLine(imageFile);
                                if (action == "wallpaper")
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
                                else if (action == "lockscreen")
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
                                string imagePath = image.GetFilePath(outputDir);
                                if (!File.Exists(imagePath))
                                {
                                    try
                                    {
                                        Console.WriteLine(image.DownloadToFile(outputDir, integrityCheck, metadata, null, apiTryCount));
                                        downloadCount++;
                                        downloadAmount--;
                                        if (downloadAmount <= 0)
                                            break;
                                    }
                                    catch (InvalidDataException)
                                    {
                                        Console.Error.WriteLine("Skipping invalid image: " + image.Uri);
                                    }
                                }
                            }

                            if (verbose)
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

                        if (allLocales && noNewImgCount >= 50 && remainingLocales.Count > 0)
                        {
                            noNewImgCount = 0;
                            locale = remainingLocales.Dequeue();
                            Console.Error.WriteLine(String.Format("Switching to {0} - {1} locales remaining", locale, remainingLocales.Count + 1));
                        }

                    } while (downloadMany && (downloadCount > 0 || noNewImgCount < 50) && downloadAmount > 0);

                    if (cacheSize < int.MaxValue && cacheSize > 0)
                    {
                        foreach (FileInfo imgToDelete in
                            Directory.GetFiles(outputDir, "*.jpg", SearchOption.TopDirectoryOnly)
                            .Select(filePath => new FileInfo(filePath))
                            .OrderByDescending(fileInfo => fileInfo.CreationTime)
                            .Skip(cacheSize))
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

            foreach (string str in new[]{
                    " ==== " + Program.Name + " v" + Program.Version + " - By ORelio - Microzoom.fr ====",
                    "",
                    "Retrieve Windows Spotlight images by requesting the Microsoft Spotlight API.",
                    Program.Name + " can also define images as wallpaper and system-wide lockscreen image.",
                    "",
                    "Usage:",
                    "  " + Program.Name + ".exe <action> [arguments]",
                    "  Only one action must be provided, as first argument",
                    "  Then, provide any number of arguments from the list below.",
                    "",
                    "Actions:",
                    "  urls               Query Spotlight API and print image URLs to standard output",
                    "  download           Download all images and print file path to standard output",
                    "  wallpaper          Download one random image and define it as wallpaper",
                    "  lockscreen         Download one random image and define it as global lockscreen",
                    "",
                    "Arguments:",
                    "  --single           Print only one random url or download only one image as spotlight.jpg",
                    "  --many             Try downloading as much images as possible by calling API many times",
                    "  --amount <n>       Stop downloading after <n> images successfully downloaded, implies --many",
                    "  --cache-size <n>   Only keep <n> most recent images in the output directory, delete others",
                    "  --maxres           Force maximum image resolution instead of tailoring to current screen res",
                    "  --portrait         Force portrait image instead of autodetecting from current screen res",
                    "  --landscape        Force landscape image instead of autodetecting from current screen res",
                    "  --locale <xx-XX>   Force specified locale, e.g. en-US, instead of autodetecting from system",
                    "  --all-locales      Attempt to download images for all known Spotlight locales, implies --many",
                    "  --outdir <dir>     Set output directory instead of defaulting to working directory",
                    "  --outname <name>   Set output file name as <name>.ext for --single or --embed-meta",
                    "  --skip-integrity   Skip integrity check of downloaded files: file size and sha256 hash",
                    "  --api-tries <n>    Amount of unsuccessful API calls before giving up. Default is 3.",
                    "  --metadata         Also save image metadata such as title & copyright as <image-name>.txt",
                    "  --embed-meta       When available, embed metadata into wallpaper or lockscreen image",
                    "  --from-file        Set the specified file as wallpaper/lockscreen instead of downloading",
                    "  --from-dir         Set a random image from the specified directory as wallpaper/lockscreen",
                    "  --restore          Restore the default lockscreen image, has no effect with other actions",
                    "  --verbose          Display additional status messages while downloading images from API",
                    "",
                    "Exit codes:",
                    "  0                  Success",
                    "  1                  Invalid arguments",
                    "  2                  Spotlight API request failure",
                    "  3                  Image download failure or Failed to write image to disk",
                    "  4                  Failed to define image as wallpaper or lock screen image"
                })
            {
                Console.Error.WriteLine(str);
            }
            Environment.Exit(1);
        }
    }
}
