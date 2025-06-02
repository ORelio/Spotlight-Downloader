using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
        public enum ApiVersion
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
            RegionInfo currentRegion = new RegionInfo(currentCulture.Name);
            string region = currentRegion.TwoLetterISORegionName.ToLower();

            if (locale == null)
                locale = currentCulture.Name;
            else if (locale.Length > 2 && locale.Contains("-"))
                region = locale.Split('-')[1].ToLower();

            string request = null;

            if (apiver == ApiVersion.v4)
            {
                // Windows 11 requests the API with "fd.api.iris.microsoft.com" hostname instead of "arc.msn.com" but both work
                // Note: disphorzres and dispvertres parameters are ignored by this API version, need to scale images client-side
                // Note: no fileSize or sha256 in this API versio so cannot easily check image integrity after download
                request = String.Format(
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
                request = String.Format(
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
            List<SpotlightImage> images = new List<SpotlightImage>();
            Json.JSONData imageData = Json.ParseJson(PerformApiRequest(maxres, locale, apiver));

            if (portrait == null)
                portrait = Screen.PrimaryScreen.Bounds.Height > Screen.PrimaryScreen.Bounds.Width;

            if (imageData.Type == Json.JSONData.DataType.Object && imageData.Properties.ContainsKey("batchrsp"))
            {
                imageData = imageData.Properties["batchrsp"];
                if (imageData.Type == Json.JSONData.DataType.Object && imageData.Properties.ContainsKey("items"))
                {
                    if (!imageData.Properties.ContainsKey("ver") || imageData.Properties["ver"].StringValue != "1.0")
                        Console.Error.WriteLine("SpotlightAPI: Unknown or missing API response version. Errors may occur.");
                    imageData = imageData.Properties["items"];
                    if (imageData.Type == Json.JSONData.DataType.Array)
                    {
                        foreach (Json.JSONData element in imageData.DataArray)
                        {
                            if (element.Type == Json.JSONData.DataType.Object && element.Properties.ContainsKey("item"))
                            {
                                Json.JSONData item = element.Properties["item"];
                                if (item.Type == Json.JSONData.DataType.String)
                                    item = Json.ParseJson(item.StringValue);
                                if (item.Type == Json.JSONData.DataType.Object && item.Properties.ContainsKey("ad") && item.Properties["ad"].Type == Json.JSONData.DataType.Object)
                                {
                                    item = item.Properties["ad"];

                                    if (apiver == ApiVersion.v4)
                                    {
                                        // The equivalent to "title_text" from APIv3 can be found in first line of" iconHoverText". APIv4 "title" does not describe the picture.
                                        string title = item.Properties.ContainsKey("iconHoverText") ? item.Properties["iconHoverText"].StringValue.Split('\r')[0].Split('\n')[0] : null;
                                        if (String.IsNullOrEmpty(title) && item.Properties.ContainsKey("title") && !String.IsNullOrEmpty(item.Properties["title"].StringValue))
                                            title = item.Properties["title"].StringValue;
                                        string copyright = item.Properties.ContainsKey("copyright") ? item.Properties["copyright"].StringValue : null;
                                        string urlField = portrait.Value ? "portraitImage" : "landscapeImage";
                                        if (item.Properties.ContainsKey(urlField)
                                            && item.Properties[urlField].Properties.ContainsKey("asset")
                                            && !String.IsNullOrEmpty(item.Properties[urlField].Properties["asset"].StringValue)
                                            && item.Properties[urlField].Properties["asset"].StringValue.ToLowerInvariant().StartsWith("https://"))
                                        {
                                            string item_url = item.Properties[urlField].Properties["asset"].StringValue;
                                            string item_name = item_url.Split('/').Last().Replace("?ver=", "").Replace('?', '_').Replace('=', '_');
                                            images.Add(new SpotlightImage()
                                            {
                                                Uri = item_url,
                                                Sha256 = null,
                                                FileSize = null,
                                                FileName = item_name,
                                                Title = title,
                                                Copyright = copyright
                                            });
                                        }
                                        else Console.Error.WriteLine("SpotlightAPI: Ignoring item image with missing or invalid uri.");
                                    }
                                    else // ApiVersion.v3
                                    {
                                        string title;
                                        Json.JSONData titleObj = item.Properties.ContainsKey("title_text") ? item.Properties["title_text"] : null;
                                        if (titleObj != null && titleObj.Type == Json.JSONData.DataType.Object && titleObj.Properties.ContainsKey("tx"))
                                            title = titleObj.Properties["tx"].StringValue;
                                        else title = null;

                                        string copyright;
                                        Json.JSONData copyrightObj = item.Properties.ContainsKey("copyright_text") ? item.Properties["copyright_text"] : null;
                                        if (copyrightObj != null && copyrightObj.Type == Json.JSONData.DataType.Object && copyrightObj.Properties.ContainsKey("tx"))
                                            copyright = copyrightObj.Properties["tx"].StringValue;
                                        else copyright = null;

                                        string urlField = portrait.Value ? "image_fullscreen_001_portrait" : "image_fullscreen_001_landscape";
                                        if (item.Properties.ContainsKey(urlField) && item.Properties[urlField].Type == Json.JSONData.DataType.Object)
                                        {
                                            item = item.Properties[urlField];
                                            if (item.Properties.ContainsKey("u") && item.Properties.ContainsKey("sha256") && item.Properties.ContainsKey("fileSize")
                                                && item.Properties["u"].Type == Json.JSONData.DataType.String
                                                && item.Properties["sha256"].Type == Json.JSONData.DataType.String
                                                && item.Properties["fileSize"].Type == Json.JSONData.DataType.String)
                                            {
                                                int fileSizeParsed = 0;
                                                if (int.TryParse(item.Properties["fileSize"].StringValue, out fileSizeParsed))
                                                {
                                                    SpotlightImage image = new SpotlightImage()
                                                    {
                                                        Uri = item.Properties["u"].StringValue,
                                                        Sha256 = item.Properties["sha256"].StringValue,
                                                        FileSize = fileSizeParsed,
                                                        FileName = null,
                                                        Title = title,
                                                        Copyright = copyright
                                                    };
                                                    try
                                                    {
                                                        System.Convert.FromBase64String(image.Sha256);
                                                    }
                                                    catch
                                                    {
                                                        image.Sha256 = null;
                                                    }
                                                    if (!String.IsNullOrEmpty(image.Uri)
                                                        && !String.IsNullOrEmpty(image.Sha256)
                                                        && image.FileSize > 0)
                                                    {
                                                        images.Add(image);
                                                    }
                                                    else Console.Error.WriteLine("SpotlightAPI: Ignoring image with empty uri, hash and/or file size less or equal to 0.");
                                                }
                                                else Console.Error.WriteLine("SpotlightAPI: Ignoring image with invalid, non-number file size.");
                                            }
                                            else Console.Error.WriteLine("SpotlightAPI: Ignoring item image uri with missing 'u', 'sha256' and/or 'fileSize' field(s).");
                                        }
                                        else Console.Error.WriteLine("SpotlightAPI: Ignoring item image with missing uri.");
                                    }
                                }
                                else Console.Error.WriteLine("SpotlightAPI: Ignoring item with missing 'ad' object.");
                            }
                            else Console.Error.WriteLine("SpotlightAPI: Ignoring non-object item while parsing 'batchrsp/items' field in JSON API response.");
                        }
                    }
                    else throw new InvalidDataException("SpotlightAPI: 'batchrsp/items' field in JSON API response is not an array.");
                }
                else throw new InvalidDataException("SpotlightAPI: Missing 'batchrsp/items' field in JSON API response." + (locale != null ? " Locale '" + locale + "' may be invalid." : ""));
            }
            else throw new InvalidDataException("SpotlightAPI: API did not return a 'batchrsp' JSON object.");

            return images.ToArray();
        }
    }
}
