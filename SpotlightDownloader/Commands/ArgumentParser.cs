using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpotlightDownloader.Commands
{
    internal static partial class ArgumentParser
    {
        public static ParsedArguments Parse(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help"))
                throw new ArgumentException("Show help");

            var normalizedArgs = args.Select(a => a.ToLower(CultureInfo.CurrentCulture)).ToArray();

            var parsed = new ParsedArguments
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
                            parsed.SingleImage = true;
                            break;
                        case "--many":
                            parsed.DownloadMany = true;
                            break;
                        case "--amount":
                            i++;
                            parsed.DownloadMany = true;
                            if (i >= args.Length)
                                throw new ArgumentException("--amount expects an additional argument.");
                            if (!int.TryParse(args[i], out int downloadAmount) || downloadAmount < 0)
                                throw new ArgumentException("Download amount must be a valid and positive number.");
                            parsed.DownloadAmount = downloadAmount == 0 ? int.MaxValue : downloadAmount;
                            break;
                        case "--cache-size":
                            i++;
                            if (i >= args.Length)
                                throw new ArgumentException("--cache-size expects an additional argument.");
                            if (!int.TryParse(args[i], out int cacheSize) || cacheSize <= 0)
                                throw new ArgumentException("Cache size must be a valid and strictly positive number.");
                            parsed.CacheSize = cacheSize;
                            break;
                        case "--maxres":
                            parsed.MaximumRes = true;
                            break;
                        case "--portrait":
                            parsed.Portrait = true;
                            break;
                        case "--landscape":
                            parsed.Portrait = false;
                            break;
                        case "--locale":
                            i++;
                            if (i < args.Length)
                                parsed.Locale = args[i];
                            else
                                throw new ArgumentException("--locale expects an additional argument.");
                            if (!LocaleRegex().Match(parsed.Locale).Success)
                                Console.Error.WriteLine($"--locale expected format is xx-XX, e.g. en-US. Locale '{parsed.Locale}' might not work.");
                            break;
                        case "--all-locales":
                            parsed.AllLocales = true;
                            parsed.DownloadMany = true;
                            break;
                        case "--outdir":
                            i++;
                            if (i < args.Length)
                                parsed.OutputDir = args[i];
                            else
                                throw new ArgumentException("--outdir expects an additional argument.");
                            if (!Directory.Exists(parsed.OutputDir))
                                throw new ArgumentException($"Output directory '{parsed.OutputDir}' does not exist.");
                            break;
                        case "--outname":
                            i++;
                            if (i < args.Length)
                                parsed.OutputName = args[i];
                            else
                                throw new ArgumentException("--outname expects an additional argument.");
                            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                                if (parsed.OutputName.Contains(invalidChar, StringComparison.Ordinal))
                                    throw new ArgumentException($"Invalid character '{invalidChar}' in specified output file name.");
                            break;
                        case "--api-tries":
                            i++;
                            if (i >= args.Length)
                                throw new ArgumentException("--api-tries expects an additional argument.");
                            if (!int.TryParse(args[i], out int apiTryCount) || apiTryCount <= 0)
                                throw new ArgumentException("API tries must be a valid and strictly positive number.");
                            parsed.ApiTryCount = apiTryCount;
                            break;
                        case "--api-version":
                            i++;
                            if (i >= args.Length)
                                throw new ArgumentException("--api-version expects an additional argument.");
                            if (!int.TryParse(args[i], out int apiVerInt) || apiVerInt < 3 || apiVerInt > 4)
                                throw new ArgumentException("Must set a supported API version: 3 or 4");
                            parsed.ApiVersion = apiVerInt == 3 ? Spotlight.ApiVersion.v3 : Spotlight.ApiVersion.v4;
                            break;
                        case "--skip-integrity":
                            parsed.IntegrityCheck = false;
                            break;
                        case "--metadata":
                            parsed.Metadata = true;
                            break;
                        case "--inconsistent-metadata":
                            parsed.Metadata = true;
                            parsed.MetadataAllowInconsistent = true;
                            break;
                        //case "--embed-meta":
                        //    parsed.EmbedMetadata = true;
                        //    break;
                        case "--from-file":
                            i++;
                            if (i < args.Length)
                                parsed.FromFile = args[i];
                            else
                                throw new ArgumentException("--from-file expects an additional argument.");
                            if (!File.Exists(parsed.FromFile))
                                throw new ArgumentException($"Input file '{parsed.FromFile}' does not exist.");
                            break;
                        case "--from-dir":
                            i++;
                            if (i < args.Length)
                                parsed.FromFile = args[i];
                            else
                                throw new ArgumentException("--from-dir expects an additional argument.");
                            if (Directory.Exists(parsed.FromFile))
                            {
                                string[] jpegFiles = [.. Directory.EnumerateFiles(parsed.FromFile, "*.jpg", SearchOption.AllDirectories)];
                                if (jpegFiles.Length != 0)
                                {
                                    // False alarm: we don't need to use a secure random here
#pragma warning disable CA5394
                                    parsed.FromFile = jpegFiles[Random.Shared.Next(0, jpegFiles.Length)];
#pragma warning restore CA5394
                                }
                                else
                                    throw new ArgumentException($"Input directory '{parsed.FromFile}' does not contain JPG files.");
                            }
                            else
                                throw new ArgumentException($"Input directory '{parsed.FromFile}' does not exist.");
                            break;
                        case "--verbose":
                            parsed.Verbose = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {args[i]}");
                    }
                }

                if (parsed.DownloadMany && parsed.CacheSize < parsed.DownloadAmount)
                {
                    Console.Error.WriteLine($"Download amount ({(parsed.DownloadAmount == int.MaxValue ? "MAX" : parsed.DownloadAmount.ToString(CultureInfo.InvariantCulture))}) is greater than cache size ({parsed.CacheSize}). Reducing download amount to {parsed.CacheSize}.");
                    parsed.DownloadAmount = parsed.CacheSize;
                }

                if (parsed.DownloadMany && parsed.Metadata && parsed.AllLocales && !parsed.MetadataAllowInconsistent)
                {
                    throw new ArgumentException("--metadata combined with --all-locales will produce random metadata languages. Please relaunch with --inconsistent-metadata if you really intend to do this.");
                }
            }

            return parsed;
        }

        [GeneratedRegex("^[a-z]{2}-[A-Z]{2}$")]
        private static partial Regex LocaleRegex();
    }
}