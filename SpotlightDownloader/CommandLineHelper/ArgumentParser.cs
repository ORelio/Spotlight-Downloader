using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpotlightDownloader.CommandLineHelper
{
    internal static partial class ArgumentParser
    {
        public static ParsedArguments Parse(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help"))
                throw new ArgumentException("Show help");

            var normalizedArgs = args.Select(a => a.ToLower(CultureInfo.CurrentCulture)).ToArray();

            var pArgs = new ParsedArguments
            {
                Action = normalizedArgs[0] switch
                {
                    "urls" or "download" or "wallpaper" or "lockscreen" => normalizedArgs[0],
                    _ => throw new ArgumentException($"Unknown action: {normalizedArgs[0]}"),
                }
            };
            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    switch (normalizedArgs[i])
                    {
                        case "--single":
                            pArgs.SingleImage = true;
                            break;
                        case "--many":
                            pArgs.DownloadMany = true;
                            break;
                        case "--amount":
                            i++;
                            pArgs.DownloadMany = true;
                            if (i >= args.Length)
                                throw new ArgumentException("--amount expects an additional argument.");
                            if (!int.TryParse(args[i], out int downloadAmount) || downloadAmount < 0)
                                throw new ArgumentException("Download amount must be a valid and positive number.");
                            pArgs.DownloadAmount = downloadAmount == 0 ? int.MaxValue : downloadAmount;
                            break;
                        case "--cache-size":
                            i++;
                            if (i >= args.Length)
                                throw new ArgumentException("--cache-size expects an additional argument.");
                            if (!int.TryParse(args[i], out int cacheSize) || cacheSize <= 0)
                                throw new ArgumentException("Cache size must be a valid and strictly positive number.");
                            pArgs.CacheSize = cacheSize;
                            break;
                        case "--portrait":
                            pArgs.Portrait = true;
                            break;
                        case "--landscape":
                            pArgs.Portrait = false;
                            break;
                        case "--locale":
                            i++;
                            if (i < args.Length)
                                pArgs.Locale = args[i];
                            else
                                throw new ArgumentException("--locale expects an additional argument.");
                            if (!LocaleRegex().Match(pArgs.Locale).Success)
                                Console.Error.WriteLine($"--locale expected format is xx-XX, e.g. en-US. Locale '{pArgs.Locale}' might not work.");
                            break;
                        case "--all-locales":
                            pArgs.AllLocales = true;
                            pArgs.DownloadMany = true;
                            break;
                        case "--outdir":
                            i++;
                            if (i < args.Length)
                                pArgs.OutputDir = args[i];
                            else
                                throw new ArgumentException("--outdir expects an additional argument.");
                            if (!Directory.Exists(pArgs.OutputDir))
                                throw new ArgumentException($"Output directory '{pArgs.OutputDir}' does not exist.");
                            break;
                        case "--outname":
                            i++;
                            if (i < args.Length)
                                pArgs.OutputName = args[i];
                            else
                                throw new ArgumentException("--outname expects an additional argument.");
                            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                                if (pArgs.OutputName.Contains(invalidChar, StringComparison.Ordinal))
                                    throw new ArgumentException($"Invalid character '{invalidChar}' in specified output file name.");
                            break;
                        case "--api-tries":
                            i++;
                            if (i >= args.Length)
                                throw new ArgumentException("--api-tries expects an additional argument.");
                            if (!int.TryParse(args[i], out int apiTryCount) || apiTryCount <= 0)
                                throw new ArgumentException("API tries must be a valid and strictly positive number.");
                            pArgs.ApiTryCount = apiTryCount;
                            break;
                        case "--api-version":
                            i++;
                            if (i >= args.Length)
                                throw new ArgumentException("--api-version expects an additional argument.");
                            if (!int.TryParse(args[i], out int apiVerInt) || apiVerInt < 3 || apiVerInt > 4)
                                throw new ArgumentException("Must set a supported API version: 3 or 4");
                            pArgs.ApiVersion = apiVerInt == 3 ? SpotlightApi.Version.v3 : SpotlightApi.Version.v4;
                            break;
                        case "--skip-integrity":
                            pArgs.IntegrityCheck = false;
                            break;
                        case "--metadata":
                            pArgs.Metadata = true;
                            break;
                        case "--inconsistent-metadata":
                            pArgs.Metadata = true;
                            pArgs.MetadataAllowInconsistent = true;
                            break;
                        case "--embed-meta":
                            pArgs.EmbedMetadata = true;
                            break;
                        case "--from-file":
                            i++;
                            if (i < args.Length)
                                pArgs.FromFile = args[i];
                            else
                                throw new ArgumentException("--from-file expects an additional argument.");
                            if (!File.Exists(pArgs.FromFile))
                                throw new ArgumentException($"Input file '{pArgs.FromFile}' does not exist.");
                            break;
                        case "--from-dir":
                            i++;
                            if (i < args.Length)
                                pArgs.FromFile = args[i];
                            else
                                throw new ArgumentException("--from-dir expects an additional argument.");
                            if (Directory.Exists(pArgs.FromFile))
                            {
                                string[] jpegFiles = [.. Directory.EnumerateFiles(pArgs.FromFile, "*.jpg", SearchOption.AllDirectories)];
                                if (jpegFiles.Length != 0)
                                {
                                    // False alarm: we don't need to use a secure random here
#pragma warning disable CA5394
                                    pArgs.FromFile = jpegFiles[Random.Shared.Next(0, jpegFiles.Length)];
#pragma warning restore CA5394
                                }
                                else
                                    throw new ArgumentException($"Input directory '{pArgs.FromFile}' does not contain JPG files.");
                            }
                            else
                                throw new ArgumentException($"Input directory '{pArgs.FromFile}' does not exist.");
                            break;
                        case "--restore":
                            if (pArgs.Action == "lockscreen")
                                pArgs.Restore = true;
                            else
                                throw new ArgumentException($"Invalid --restore argument for action: {pArgs.Action}");
                            break;
                        case "--verbose":
                            pArgs.Verbose = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {args[i]}");
                    }
                }

                if (pArgs.DownloadMany && pArgs.CacheSize < pArgs.DownloadAmount)
                {
                    Console.Error.WriteLine($"Download amount ({(pArgs.DownloadAmount == int.MaxValue ? "MAX" : pArgs.DownloadAmount.ToString(CultureInfo.InvariantCulture))}) is greater than cache size ({pArgs.CacheSize}). Reducing download amount to {pArgs.CacheSize}.");
                    pArgs.DownloadAmount = pArgs.CacheSize;
                }

                if (pArgs.DownloadMany && pArgs.Metadata && pArgs.AllLocales && !pArgs.MetadataAllowInconsistent)
                {
                    throw new ArgumentException("--metadata combined with --all-locales will produce random metadata languages. Please relaunch with --inconsistent-metadata if you really intend to do this.");
                }
            }

            return pArgs;
        }

        [GeneratedRegex("^[a-z]{2}-[A-Z]{2}$")]
        private static partial Regex LocaleRegex();
    }
}