using System;
using System.Net;
using System.Globalization;
using System.Windows.Forms;
using SharpTools;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SpotlightDownloader
{
    /// <summary>
    /// Wrapper around the Spotlight JSON API
    /// </summary>
    static class Spotlight
    {
        /// <summary>
        /// Request new images from the Spotlight API and return raw JSON response
        /// </summary>
        /// <param name="maxres">Force maximum image resolution. Otherwise, current image resolution is used</param>
        /// <returns>Raw JSON response</returns>
        /// <exception cref="System.Net.WebException">An exception is thrown if the request fails</exception>
        private static string PerformApiRequest(bool maxres)
        {
            WebClient webClient = new WebClient();
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            RegionInfo currentRegion = new RegionInfo(currentCulture.LCID);

            int screenWidth = maxres ? 99999 : Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = maxres ? 99999 : Screen.PrimaryScreen.Bounds.Height;

            string request = String.Format(
                "https://arc.msn.com/v3/Delivery/Cache?pid=209567&fmt=json&rafb=0&ua=WindowsShellClient"
                    +"%2F0&disphorzres={0}&dispvertres={1}&lo=80217&pl={2}&lc={3}&ctry={4}&time={5}",
                    screenWidth,
                    screenHeight,
                currentCulture.Name,
                currentCulture.Name,
                currentRegion.TwoLetterISORegionName.ToLower(),
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            byte[] stringRaw = webClient.DownloadData(request);
            return Encoding.UTF8.GetString(stringRaw);
        }

        /// <summary>
        /// Request new images from the Spotlight API and return image urls
        /// </summary>
        /// <param name="maxres">Force maximum image resolution. Otherwise, current main monitor screen resolution is used</param>
        /// <param name="portrait">Null = Auto detect from current main monitor resolution. True = portrait, False = landscape.</param>
        /// <returns>List of images</returns>
        /// <exception cref="System.Net.WebException">An exception is thrown if the request fails</exception>
        /// <exception cref="System.IO.InvalidDataException">An exception is thrown if the JSON data is invalid</exception>
        public static SpotlightImage[] GetImageUrls(bool maxres = false, bool? portrait = null)
        {
            List<SpotlightImage> images = new List<SpotlightImage>();
            Json.JSONData imageData = Json.ParseJson(PerformApiRequest(maxres));

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
                                else Console.Error.WriteLine("SpotlightAPI: Ignoring item with missing 'ad' object.");
                            }
                            else Console.Error.WriteLine("SpotlightAPI: Ignoring non-object item while parsing 'batchrsp/items' field in JSON API response.");
                        }
                    }
                    else throw new InvalidDataException("SpotlightAPI: 'batchrsp/items' field in JSON API response is not an array.");
                }
                else throw new InvalidDataException("SpotlightAPI: Missing 'batchrsp/items' field in JSON API response.");
            }
            else throw new InvalidDataException("SpotlightAPI: API did not return a 'batchrsp' JSON object.");

            return images.ToArray();
        }
    }
}
