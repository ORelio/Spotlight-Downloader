using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpotlightDownloader
{
    /// <summary>
    /// Represents a Spotlight image
    /// </summary>
    sealed class SpotlightImage
    {
        private const string MetaHeader = "[SpotlightImage]";

        public string Uri { get; set; }
        public string Sha256 { get; set; }
        public int? FileSize { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Copyright { get; set; }

        /// <summary>
        /// Get Metadata file location from JPG file location
        /// </summary>
        /// <param name="imagePath">Image file location</param>
        /// <returns>Metadata file location</returns>
        public static string GetMetaLocation(string imagePath)
        {
            return Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension($"{imagePath}.txt"));
        }

        /// <summary>
        /// Load a SpotlightImage object from a Metatada file
        /// </summary>
        /// <param name="metadataFile">File to load information from</param>
        /// <exception cref="IOException">Thrown if an error occurs when accessing the specified file</exception>
        /// <exception cref="InvalidDataException">Thrown if the specified file is not a valid metadata file</exception>
        /// <returns>SpotlightImage object</returns>
        public static SpotlightImage LoadMeta(string metadataFile)
        {
            string[] lines = File.ReadAllLines(metadataFile);
            if (lines.Length > 0 && lines[0] == MetaHeader)
            {
                SpotlightImage image = new();
                foreach (string line in lines)
                {
                    int equalIndex = line.IndexOf('=', StringComparison.Ordinal);
                    if (equalIndex > 0)
                    {
                        string value = "";
                        string key = line[..equalIndex];
                        if (line.Length > (equalIndex + 1))
                            value = line[(equalIndex + 1)..];
                        switch (key)
                        {
                            case "uri":
                                image.Uri = value;
                                break;
                            case "sha256":
                                image.Sha256 = value;
                                break;
                            case "filesize":
                                if (int.TryParse(value, out int fileSize))
                                    image.FileSize = fileSize;
                                break;
                            case "filename":
                                image.FileName = value;
                                break;
                            case "title":
                                image.Title = value;
                                break;
                            case "copyright":
                                image.Copyright = value;
                                break;
                        }
                    }
                }
                return image;
            }
            else throw new InvalidDataException($"{MetaHeader}: Not a SpotlightImage metadata file: {metadataFile}");
        }

        /// <summary>
        /// Get image path to which the image will be downloaded
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <param name="outputName">Specify output file name, or let SpotlightImage choose a file name</param>
        /// <param name="extension">File extension, defaults to .jpg since image is a JPEG file</param>
        /// <remarks>
        /// File name depends on data provided by Spotlight API
        /// Api v3 returns a hash => Compute Guid from sha256 hash
        /// Api v4 returns a file name or resource ID in URL => Use that as file name
        /// </remarks>
        /// <returns>Image path on disk</returns>
        public string GetFilePath(string outputDir, string outputName = null, string extension = null)
        {
            if (outputName == null)
            {
                if (FileName == null)
                {
                    byte[] sha256 = Convert.FromBase64String(Sha256);
                    byte[] guid = new byte[16];
                    for (int i = 0; i < guid.Length && i < sha256.Length; i++)
                        guid[i] = sha256[i];
                    outputName = new Guid(guid).ToString();
                }
                else
                {
                    outputName = Path.GetFileNameWithoutExtension(FileName);
                    if (extension == null)
                    {
                        string extensionTmp = Path.GetExtension(FileName);
                        if (!string.IsNullOrEmpty(extensionTmp))
                            extension = extensionTmp;
                    }
                }
            }

            extension ??= ".jpg";

            return Path.Combine(outputDir, outputName + extension);
        }

        /// <summary>
        /// Download the Spotlight image to a specified directory and file name, then perform integrity check
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <param name="integrityCheck">Whether to perform integrity check</param>
        /// <param name="metadata">Whether to save image metadata in a .txt file besides the .jpg file</param>
        /// <param name="outputName">Specify output file name, or let SpotlightImage generate Guid from sha256 hash</param>
        /// <param name="attempts">Amount of download attempts before raising an exception if an error occurs</param>
        /// <exception cref="WebException">Thrown if download fails</exception>
        /// <exception cref="IOException">Thrown if writing file to disk fails</exception>
        /// <exception cref="InvalidDataException">Thrown if data sent by server do not pass integrity check</exception>
        /// <returns>Output file path</returns>
        public async Task<string> DownloadToFile(string outputDir, bool integrityCheck = true, bool metadata = false, string outputName = null, int attempts = 1)
        {
            while (true)
            {
                try
                {
                    attempts--;
                    return await DownloadToFileSingleAttempt(outputDir, integrityCheck, metadata, outputName).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (attempts > 0)
                    {
                        await Console.Error.WriteLineAsync($"{MetaHeader}: {e.GetType()}: {e.Message} - Waiting 10 seconds before retrying...").ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                    }
                    else throw;
                }
            }
        }

        /// <summary>
        /// Download the Spotlight image to a specified directory and file name, then perform integrity check (Single Attempt)
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <param name="integrityCheck">Whether to perform integrity check</param>
        /// <param name="metadata">Whether to save image metadata in a .txt file besides the .jpg file</param>
        /// <param name="outputName">Specify output file name, or let SpotlightImage generate Guid from sha256 hash</param>
        /// <exception cref="WebException">Thrown if download fails</exception>
        /// <exception cref="IOException">Thrown if writing file to disk fails</exception>
        /// <exception cref="InvalidDataException">Thrown if data sent by server do not pass integrity check</exception>
        /// <returns>Output file path</returns>
        private async Task<string> DownloadToFileSingleAttempt(string outputDir, bool integrityCheck = true, bool metadata = false, string outputName = null)
        {
            string outputFile = GetFilePath(outputDir, outputName);

            long? expectedContentLength = null;

            using (HttpClient client = new())
            using (HttpResponseMessage response = await client.GetAsync(new Uri(Uri), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                // Get Content-Length header if available
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    expectedContentLength = response.Content.Headers.ContentLength.Value;
                }

                using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using FileStream fileStream = new(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            if (expectedContentLength.HasValue)
            {
                long actualFileSize = new FileInfo(outputFile).Length;
                if (actualFileSize != expectedContentLength.Value)
                {
                    File.Delete(outputFile);
                    throw new InvalidDataException(
                        $"SpotlightImage: Downloaded file size does not match HTTP Content-Length: {Uri} - Expected: {expectedContentLength.Value}, Actual: {actualFileSize}"
                    );
                }
            }

            if (integrityCheck)
            {
                if (FileSize != null && new FileInfo(outputFile).Length != FileSize.Value)
                {
                    File.Delete(outputFile);
                    throw new InvalidDataException($"SpotlightImage: File returned by server does not have the expected size: {Uri} - Expected size: {FileSize.Value}");
                }
                if (Sha256 != null)
                {
                    using FileStream fileStream = File.OpenRead(outputFile);
                    using var sha256 = SHA256.Create();
                    byte[] hash = await sha256.ComputeHashAsync(fileStream).ConfigureAwait(false);
                    string hashString = Convert.ToBase64String(hash);
                    if (hashString != Sha256)
                    {
                        File.Delete(outputFile);
                        throw new InvalidDataException($"SpotlightImage: File returned by server does not have the expected sha256 hash: {Uri} - Expected Hash: {Sha256}");
                    }
                }
                if (FileSize == null || Sha256 == null)
                {
                    try
                    {
                        using Image newImage = Image.FromFile(outputFile);
                        // Seems like the image is valid since there was no exception loading it
                    }
                    catch (Exception e)
                    {
                        throw new InvalidDataException($"SpotlightImage: File returned by server does not seems to be a valid image: {Uri} - {e.GetType()}: {e.Message}");
                    }
                }
            }

            if (metadata)
            {
                string outputMeta = GetFilePath(outputDir, outputName, ".txt");
                File.WriteAllLines(outputMeta, [
                    MetaHeader,
                    $"uri={Uri}",
                    $"sha256={Sha256}",
                    $"filesize={FileSize}",
                    $"filename={FileName}",
                    $"title={Title}",
                    $"copyright={Copyright}"
                ], Encoding.UTF8);
            }

            return outputFile;
        }

        /// <summary>
        /// Load an input image, adjust to screen res, embed metadata using current desktop scaling factor and save the result into a new image file
        /// </summary>
        /// <param name="inputImageFile">Input image file</param>
        /// <param name="outputDir">Output directory</param>
        /// <param name="outputName">Output file name</param>
        /// <param name="adjustToScreen">Auto adjust image to current screen resolution</param>
        /// <param name="adjustToScaling">Auto adjust text to current UI scaling factor to avoid blurry text</param>
        /// <returns>Output file path</returns>
        public static string EmbedMetadata(string inputImageFile, string outputDir, string outputName, bool adjustToScreen = true, bool adjustToScaling = true)
        {
            string fileExtension = ".png";
            var imageFormat = System.Drawing.Imaging.ImageFormat.Png;

            if (string.IsNullOrEmpty(outputName))
                outputName = Path.GetFileNameWithoutExtension(inputImageFile);
            string outputFile = Path.Combine(outputDir, outputName + fileExtension);

            Image img = Image.FromFile(inputImageFile);

            if (adjustToScreen)
            {
                // Crop image to get correct aspect ratio, optionally downscaling it if screen is smaller
                Rectangle screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                Image img2 = FixedImageResize(img, screen.Width, screen.Height, true);
                img.Dispose();
                img = img2;

                if (adjustToScaling && (img.Width < screen.Width || img.Height < screen.Height))
                {
                    // Source image was smaller than screen: Upscale it to avoid blurry text
                    img2 = SimpleImageResize(img, screen.Width, screen.Height);
                    img.Dispose();
                    img = img2;
                }
            }

            string metadataFile = GetMetaLocation(inputImageFile);
            if (File.Exists(metadataFile))
            {
                SpotlightImage meta = LoadMeta(metadataFile);
                Graphics gfx = Graphics.FromImage(img);

                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                gfx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                int fontSize = 11;
                if (adjustToScaling)
                    fontSize = (int)(fontSize * GetScalingFactor());
                using Font font = new("Segoe UI Semilight", fontSize);
                SizeF titleSize = gfx.MeasureString(meta.Title, font);
                if (titleSize.Width < img.Size.Width - 20)
                {
                    gfx.DrawString(
                        meta.Title,
                        font,
                        Brushes.White,
                        new Rectangle(
                            img.Size.Width - 10 - (int)Math.Ceiling(titleSize.Width),
                            10,
                            (int)Math.Ceiling(titleSize.Width),
                            (int)Math.Ceiling(titleSize.Height)
                        )
                    );
                    gfx.Flush();
                }
            }

            img.Save(outputFile, imageFormat);
            img.Dispose();

            return outputFile;
        }

        /// <summary>
        /// Resize image to a fixed destination size while preserving aspect ratio
        /// </summary>
        /// <remarks>Source: stackoverflow.com/questions/10323633/</remarks>
        /// <param name="image">Input image</param>
        /// <param name="Width">Desized Width</param>
        /// <param name="Height">Desized Height</param>
        /// <param name="needToFill">True = crop to fill, False = fit inside</param>
        /// <returns>Output image</returns>
        private static Bitmap FixedImageResize(Image image, int Width, int Height, bool needToFill)
        {
            int sourceWidth = image.Width;
            int sourceHeight = image.Height;
            int sourceX = 0;
            int sourceY = 0;
            double destX = 0;
            double destY = 0;
            double nScaleW = Width / (double)sourceWidth;
            double nScaleH = Height / (double)sourceHeight;

            double nScale;
            if (!needToFill)
            {
                nScale = Math.Min(nScaleH, nScaleW);
            }
            else
            {
                nScale = Math.Max(nScaleH, nScaleW);
                destY = (Height - sourceHeight * nScale) / 2;
                destX = (Width - sourceWidth * nScale) / 2;
            }

            if (nScale > 1)
                nScale = 1;

            int destWidth = (int)Math.Round(sourceWidth * nScale);
            int destHeight = (int)Math.Round(sourceHeight * nScale);

            Bitmap bmPhoto;
            try
            {
                bmPhoto = new Bitmap(destWidth + (int)Math.Round(2 * destX), destHeight + (int)Math.Round(2 * destY));
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"destWidth:{destWidth}, destX:{destX}, destHeight:{destHeight}, desxtY:{destY}, Width:{Width}, Height:{Height}", ex);
            }

            using Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            grPhoto.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            grPhoto.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            Rectangle to = new((int)Math.Round(destX), (int)Math.Round(destY), destWidth, destHeight);
            Rectangle from = new(sourceX, sourceY, sourceWidth, sourceHeight);
            //Console.WriteLine("From: " + from.ToString());
            //Console.WriteLine("To: " + to.ToString());
            grPhoto.DrawImage(image, to, from, GraphicsUnit.Pixel);

            return bmPhoto;
        }

        /// <summary>
        /// Resize image to a fixed destination size without preserving aspect ratio
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="Width">Destination Width</param>
        /// <param name="Height">Destination Height</param>
        private static Bitmap SimpleImageResize(Image image, int Width, int Height)
        {
            Bitmap bmPhoto = new(Width, Height);

            using Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            grPhoto.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            grPhoto.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            Rectangle to = new(0, 0, Width, Height);
            Rectangle from = new(0, 0, image.Width, image.Height);
            grPhoto.DrawImage(image, to, from, GraphicsUnit.Pixel);
            return bmPhoto;
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }

        public static float GetScalingFactor()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
            float ScreenScalingFactor = PhysicalScreenHeight / (float)LogicalScreenHeight;
            return ScreenScalingFactor; // 1.25 = 125%
        }
    }
}
