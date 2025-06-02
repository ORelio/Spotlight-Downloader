using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.Json;

using static System.String;

namespace SpotlightDownloader
{
    /// <summary>
    /// Wrapper around the Spotlight JSON API - By ORelio (c) 2018-2024 - CDDL 1.0
    /// </summary>
    static class Spotlight
    {
        /// <summary>
        /// Refers to different versions of the Spotlight API
        /// </summary>
        internal enum ApiVersion
        {
            /// <summary>
            /// API version used by Windows 10 for its Lockscreen.
            /// Default hostname: arc.msn.com
            /// Endpoint: /v3/Delivery/Placement
            /// Maximum resolution: 1080p
            /// File names: No
            /// Metadata: Yes
            /// Server-side scaling: Yes
            /// Integrity checks: Yes
            /// </summary>
            v3,

            /// <summary>
            /// API version used by Windows 11 for its Lockscreen or Wallpaper.
            /// Default hostname: fd.api.iris.microsoft.com
            /// Endpoint: /v4/api/selection
            /// Maximum resolution: 4K
            /// File names: Yes for some images
            /// Metadata: Yes
            /// Server-side scaling: No
            /// Integrity checks: No
            /// </summary>
            v4
        }

        /// <summary>
        /// Request new images from the Spotlight API and return raw JSON response
        /// </summary>
        /// <param name="maxres">Force maximum image resolution. Otherwise, current image resolution is used</param>
        /// <param name="locale">Null = Auto detect from current system, or specify xx-XX value format such as en-US</param>
        /// <param name="apiver">Version of the Spotlight API</param>
        /// <returns>Raw JSON response</returns>
        /// <exception cref="System.Net.WebException">An exception is thrown if the request fails</exception>
        private static string PerformApiRequest(bool maxres, string locale = null, ApiVersion apiver = ApiVersion.v4)
        {
            WebClient webClient = new WebClient();
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            RegionInfo currentRegion = new(currentCulture.Name);
            string region = currentRegion.TwoLetterISORegionName.ToLower();

            if (locale == null)
                locale = currentCulture.Name;
            else if (locale.Length > 2 && locale.Contains("-"))
                region = locale.Split('-')[1].ToLower();

            string request;
            if (apiver == ApiVersion.v4)
            {
                // Windows 11 requests the API with "fd.api.iris.microsoft.com" hostname instead of "arc.msn.com" but both work
                // Note: disphorzres and dispvertres parameters are ignored by this API version, need to scale images client-side
                // Note: no fileSize or sha256 in this API versio so cannot easily check image integrity after download
                request = Format(
                    "https://fd.api.iris.microsoft.com/v4/api/selection?&placement=88000820&bcnt=4&country={0}&locale={1}&fmt=json",
                    region,
                    locale
                );
            }
            else
            {
                int screenWidth = maxres ? 99999 : Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = maxres ? 99999 : Screen.PrimaryScreen.Bounds.Height;

                // Windows 10 requests the API with older hostname "arc.msn.com" instead of "fd.api.iris.microsoft.com" but both work
                // This API supports setting disphorzres and dispvertres to have image scaled server-side and save bandwidth
                // This API also returns fileSize and sha256 to allow verifying image integrity after download
                request = Format(
                    "https://arc.msn.com/v3/Delivery/Placement?pid=338387&fmt=json&ua=WindowsShellClient"
                        + "%2F0&cdm=1&disphorzres={0}&dispvertres={1}&pl={2}&lc={3}&ctry={4}&time={5}",
                        screenWidth,
                        screenHeight,
                    locale,
                    locale,
                    region,
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                );
            }

            byte[] stringRaw = webClient.DownloadData(request);
            return Encoding.UTF8.GetString(stringRaw);
        }

        /// <summary>
        /// Request new images from the Spotlight API and return image urls
        /// </summary>
        /// <param name="maxres">Force maximum image resolution. Otherwise, current main monitor screen resolution is used</param>
        /// <param name="portrait">Null = Auto detect from current main monitor resolution, True = portrait, False = landscape.</param>
        /// <param name="locale">Null = Auto detect from current system, or specify xx-XX value format such as en-US</param>
        /// <param name="apiver">Version of the Spotlight API</param>
        /// <param name="attempts">Amount of API call attempts before raising an exception if an error occurs</param>
        /// <returns>List of images</returns>
        /// <exception cref="System.Net.WebException">An exception is thrown if the request fails</exception>
        /// <exception cref="System.IO.InvalidDataException">An exception is thrown if the JSON data is invalid</exception>
        public static SpotlightImage[] GetImageUrls(bool maxres = false, bool? portrait = null, string locale = null, int attempts = 1, ApiVersion apiver = ApiVersion.v4)
        {
            while (true)
            {
                try
                {
                    attempts--;
                    return GetImageUrlsSingleAttempt(maxres, portrait, locale, apiver);
                }
                catch (Exception e)
                {
                    if (attempts > 0)
                    {
                        Console.Error.WriteLine("SpotlightAPI: " + e.GetType() + ": " + e.Message + " - Waiting 10 seconds before retrying...");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    else throw;
                }
            }
        }

        /// <summary>
        /// Request new images from the Spotlight API and return image urls (Single Attempt)
        /// </summary>
        /// <param name="maxres">Force maximum image resolution. Otherwise, current main monitor screen resolution is used</param>
        /// <param name="portrait">Null = Auto detect from current main monitor resolution, True = portrait, False = landscape.</param>
        /// <param name="locale">Null = Auto detect from current system, or specify xx-XX value format such as en-US</param>
        /// <param name="apiver">Version of the Spotlight API</param>
        /// <returns>List of images</returns>
        /// <exception cref="System.Net.WebException">An exception is thrown if the request fails</exception>
        /// <exception cref="System.IO.InvalidDataException">An exception is thrown if the JSON data is invalid</exception>
        private static SpotlightImage[] GetImageUrlsSingleAttempt(bool maxres = false, bool? portrait = null, string locale = null, ApiVersion apiver = ApiVersion.v4)
        {
            List<SpotlightImage> images = [];
            string rawJson = PerformApiRequest(maxres, locale, apiver);
            var root = Json.ParseJson(rawJson);
            // Console.Error.WriteLine("=== RAW JSON RECEIVED FROM SERVER ==="); // debug purposes
            // Console.Error.WriteLine(rawJson); // debug purposes
            // Console.Error.WriteLine("=== END OF RAW JSON ==="); // debug purposes

            portrait ??= Screen.PrimaryScreen.Bounds.Height > Screen.PrimaryScreen.Bounds.Width;

            if (!root.TryGetProperty("batchrsp", out var batchrsp))
                throw new InvalidDataException("SpotlightAPI: API did not return a 'batchrsp' JSON object.");

            if (!batchrsp.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("SpotlightAPI: 'batchrsp/items' field in JSON API response is not an array.");

            foreach (var itemWrapper in items.EnumerateArray())
            {
                if (!itemWrapper.TryGetProperty("item", out var itemStringElement) || itemStringElement.ValueKind != JsonValueKind.String)
                {
                    Console.Error.WriteLine("SpotlightAPI: Ignoring non-object item while parsing 'batchrsp/items' field in JSON API response.");
                    continue;
                }

                // Parse the nested JSON string
                var item = Json.ParseJson(itemStringElement.GetString());

                // ApiVersion.v4
                if (item.TryGetProperty("ad", out var ad))
                {
                    if (apiver == ApiVersion.v4)
                    {
                        string title = ad.TryGetProperty("iconHoverText", out var hoverText) ? hoverText.GetString()?.Split('\r', '\n')[0] : null;
                        if (IsNullOrEmpty(title) && ad.TryGetProperty("title", out var titleProp))
                            title = titleProp.GetString();
                        string copyright = ad.TryGetProperty("copyright", out var copyrightProp) ? copyrightProp.GetString() : null;
                        string urlField = portrait.Value ? "portraitImage" : "landscapeImage";
                        if (ad.TryGetProperty(urlField, out var imageObj) && imageObj.TryGetProperty("asset", out var assetProp))
                        {
                            string item_url = assetProp.GetString();
                            if (!IsNullOrEmpty(item_url) && item_url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string item_name = item_url.Split('/').Last().Replace("?ver=", "").Replace('?', '_').Replace('=', '_');
                                string item_name = item_url.Split('/').Last().Split('?')[0];
                                images.Add(new SpotlightImage
                                {
                                    Uri = item_url,
                                    Sha256 = null,
                                    FileSize = null,
                                    FileName = item_name,
                                    Title = title,
                                    Copyright = copyright
                                });
                            }
                            else
                            {
                                Console.Error.WriteLine("SpotlightAPI: Ignoring item image with missing or invalid uri.");
                            }
                        }
                    }
                    else // ApiVersion.v3
                    {
                        string title = ad.TryGetProperty("title_text", out var titleObj) && titleObj.TryGetProperty("tx", out var txTitle) ? txTitle.GetString() : null;
                        string copyright = ad.TryGetProperty("copyright_text", out var copyrightObj) && copyrightObj.TryGetProperty("tx", out var txCopyright) ? txCopyright.GetString() : null;
                        string urlField = portrait.Value ? "image_fullscreen_001_portrait" : "image_fullscreen_001_landscape";
                        if (ad.TryGetProperty(urlField, out var imageObj) &&
                            imageObj.TryGetProperty("u", out var uProp) &&
                            imageObj.TryGetProperty("sha256", out var sha256Prop) &&
                            imageObj.TryGetProperty("fileSize", out var fileSizeProp))
                        {
                            string uri = uProp.GetString();
                            string sha256 = sha256Prop.GetString();
                            _ = int.TryParse(fileSizeProp.GetString(), out int fileSizeParsed);
                            if (!IsNullOrEmpty(uri) && !IsNullOrEmpty(sha256) && fileSizeParsed > 0)
                            {
                                images.Add(new SpotlightImage
                                {
                                    Uri = uri,
                                    Sha256 = sha256,
                                    FileSize = fileSizeParsed,
                                    FileName = null,
                                    Title = title,
                                    Copyright = copyright
                                });
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("SpotlightAPI: Ignoring item image uri with missing 'u', 'sha256' and/or 'fileSize' field(s).");
                        }
                    }
                }
            }

            return [.. images];
        }
    }
}
