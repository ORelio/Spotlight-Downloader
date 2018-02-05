using System;
using System.Linq;
using System.IO;
using SharpTools;
using System.Diagnostics;

namespace SpotlightDownloader
{
    /// <summary>
    /// Download Microsoft Spotlight images - By ORelio (c) 2018 - CDDL 1.0
    /// </summary>
    class Program
    {
        public const string Name = "SpotlightDL";
        public const string Version = "1.0";

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string action = null;
                bool singleImage = false;
                bool maximumRes = false;
                bool? portrait = false;
                string outputDir = ".";
                string outputName = "spotlight";
                bool integrityCheck = true;
                bool downloadMany = false;
                bool metadata = false;

                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        switch (args[i].ToLower())
                        {
                            case "--single":
                                singleImage = true;
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
                            case "--download-many":
                                downloadMany = true;
                                break;
                            case "--metadata":
                                metadata = true;
                                break;
                            default:
                                Console.Error.WriteLine("Unknown argument: " + args[i]);
                                Environment.Exit(1);
                                break;
                        }
                    }
                }

                switch (args[0].ToLower())
                {
                    case "urls":
                    case "download":
                    case "wallpaper":
                        action = args[0].ToLower();
                        break;
                    default:
                        Console.Error.WriteLine("Unknown action: " + args[0]);
                        Environment.Exit(1);
                        break;
                }

                try
                {
                    int downloadCount = 0;
                    int noNewImgCount = 0;

                    do
                    {
                        SpotlightImage[] images = Spotlight.GetImageUrls(maximumRes, portrait);

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
                                string outputFile = randomImage.DownloadToFile(outputDir, integrityCheck, metadata, outputName);
                                Console.WriteLine(outputFile);
                                if (action == "wallpaper")
                                {
                                    try
                                    {
                                        Desktop.SetWallpaper(outputFile);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.Error.WriteLine(e.GetType() + ": " + e.Message);
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
                                    Console.WriteLine(image.DownloadToFile(outputDir, integrityCheck, metadata));
                                    downloadCount++;
                                }
                            }

                            Console.Error.WriteLine("Successfully downloaded: " + downloadCount + " images.");
                            Console.Error.WriteLine("Already downloaded: " + (images.Length - downloadCount) + " images.");

                            if (downloadCount == 0)
                                noNewImgCount++;
                            else noNewImgCount = 0;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                            Environment.Exit(3);
                        }
                    } while (downloadMany && (downloadCount > 0 || noNewImgCount < 10));
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.GetType() + ": " + e.Message);
                    Environment.Exit(2);
                }
            }

            foreach (string str in new[]{
                    " ==== " + Program.Name + " v" + Program.Version + " - By ORelio ====",
                    "",
                    "Retrieve Spotlight images from Microsoft's Spotlight API.",
                    "By default, " + Program.Name + " downloads images in current directory.",
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
                    "",
                    "Arguments:",
                    "  --single           Print only one random url or download only one image as spotlight.jpg",
                    "  --maxres           Force maximum image resolution instead of tailoring to current screen res",
                    "  --portrait         Force portrait image instead of autodetecting from current screen res",
                    "  --landscape        Force landscape image instead of autodetecting from current screen res",
                    "  --outdir <dir>     Set output directory instead of defaulting to working directory",
                    "  --outname <name>   Set output file name as <name>.jpg in single image mode, ignored otherwise",
                    "  --skip-integrity   Skip integrity check of downloaded files: file size and sha256 hash",
                    "  --download-many    Try downloading as much images as possible by calling API many times.",
                    "  --metadata         Also save image metadata such as title & copyright as <image-name>.txt",
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
