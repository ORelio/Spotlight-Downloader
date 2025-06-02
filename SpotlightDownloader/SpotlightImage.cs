using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Drawing;

namespace SpotlightDownloader
{
    /// <summary>
    /// Represents a Spotlight image
    /// </summary>
    class SpotlightImage
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
            return Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + ".txt");
        }

        /// <summary>
        /// Load a SpotlightImage object from a Metatada file
        /// </summary>
        /// <param name="metadataFile">File to load information from</param>
        /// <exception cref="System.IO.IOException">Thrown if an error occurs when accessing the specified file</exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown if the specified file is not a valid metadata file</exception>
        /// <returns>SpotlightImage object</returns>
        public static SpotlightImage LoadMeta(string metadataFile)
        {
            string[] lines = File.ReadAllLines(metadataFile);
            if (lines.Length > 0 && lines[0] == MetaHeader)
            {
                SpotlightImage image = new SpotlightImage();
                foreach (string line in lines)
                {
                    int equalIndex = line.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        string value = "";
                        string key = line.Substring(0, equalIndex);
                        if (line.Length > (equalIndex + 1))
                            value = line.Substring(equalIndex + 1);
                        switch (key)
                        {
                            case "uri":
                                image.Uri = value;
                                break;
                            case "sha256":
                                image.Sha256 = value;
                                break;
                            case "filesize":
                                int fileSize;
                                if (int.TryParse(value, out fileSize))
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
            else throw new InvalidDataException("Not a SpotlightImage metadata file: " + metadataFile);
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
                    byte[] sha256 = System.Convert.FromBase64String(Sha256);
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
                        if (!String.IsNullOrEmpty(extensionTmp))
                            extension = extensionTmp;
                    }
                }
            }

            if (extension == null)
                extension = ".jpg";

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
        /// <exception cref="System.Net.WebException">Thrown if download fails</exception>
        /// <exception cref="System.IO.IOException">Thrown if writing file to disk fails</exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown if data sent by server do not pass integrity check</exception>
        /// <returns>Output file path</returns>
        public string DownloadToFile(string outputDir, bool integrityCheck = true, bool metadata = false, string outputName = null, int attempts = 1)
        {
            while (true)
            {
                try
                {
                    attempts--;
                    return DownloadToFileSingleAttempt(outputDir, integrityCheck, metadata, outputName);
                }
                catch (Exception e)
                {
                    if (attempts > 0)
                    {
                        Console.Error.WriteLine("SpotlightImage: " + e.GetType() + ": " + e.Message + " - Waiting 10 seconds before retrying...");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
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
        /// <exception cref="System.Net.WebException">Thrown if download fails</exception>
        /// <exception cref="System.IO.IOException">Thrown if writing file to disk fails</exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown if data sent by server do not pass integrity check</exception>
        /// <returns>Output file path</returns>
        private string DownloadToFileSingleAttempt(string outputDir, bool integrityCheck = true, bool metadata = false, string outputName = null)
        {
            string outputFile = GetFilePath(outputDir, outputName);

            (new WebClient()).DownloadFile(Uri, outputFile);

            if (integrityCheck)
            {
                if (FileSize != null && new FileInfo(outputFile).Length != FileSize.Value)
                {
                    File.Delete(outputFile);
                    throw new InvalidDataException("SpotlightImage: File returned by server does not have the expected size: " + Uri + " - Expected size: " + FileSize.Value);
                }
                if (Sha256 != null)
                {
                    using (FileStream fileStream = File.OpenRead(outputFile))
                    {
                        byte[] hash = (new SHA256Managed()).ComputeHash(fileStream);
                        string hashString = Convert.ToBase64String(hash);
                        if (hashString != Sha256)
                        {
                            File.Delete(outputFile);
                            throw new InvalidDataException("SpotlightImage: File returned by server does not have the expected sha256 hash: " + Uri + " - Expected Hash: " + Sha256);
                        }
                    }
                }
                if (FileSize == null || Sha256 == null)
                {
                    try
                    {
                        using (Image newImage = Image.FromFile(outputFile))
                        {
                            // Seems like the image is valid since there was no exception loading it
                        }
                    }
                    catch (Exception e)
                    {
                        throw new InvalidDataException("SpotlightImage: File returned by server does not seems to be a valid image: " + Uri + " - " + e.GetType() + ": " + e.Message);
                    }
                }
            }

            if (metadata)
            {
                string outputMeta = GetFilePath(outputDir, outputName, ".txt");
                File.WriteAllLines(outputMeta, new[]{
                    MetaHeader,
                    "uri=" + Uri,
                    "sha256=" + Sha256,
                    "filesize=" + FileSize,
                    "filename=" + FileName,
                    "title=" + Title,
                    "copyright=" + Copyright
                }, Encoding.UTF8);
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
            //Windows 7 or lower cannot use PNG as wallpaper or will convert PNG to JPG, better use BMP for best quality
            string fileExtension = ".bmp";
            var imageFormat = System.Drawing.Imaging.ImageFormat.Bmp;

            //Windows 8 and 10 will convert BMP to JPG, better use PNG for best quality
            if ((WindowsVersion.WinMajorVersion == 6 && WindowsVersion.WinMinorVersion >= 2)
                || WindowsVersion.WinMajorVersion >= 10)
            {
                fileExtension = ".png";
                imageFormat = System.Drawing.Imaging.ImageFormat.Png;
            }

            if (String.IsNullOrEmpty(outputName))
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
                    fontSize = (int)((float)fontSize * Desktop.GetScalingFactor());
                Font font = new Font("Segoe UI Semilight", fontSize);
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
        private static Image FixedImageResize(Image image, int Width, int Height, bool needToFill)
        {
            int sourceWidth = image.Width;
            int sourceHeight = image.Height;
            int sourceX = 0;
            int sourceY = 0;
            double destX = 0;
            double destY = 0;

            double nScale = 0;
            double nScaleW = 0;
            double nScaleH = 0;

            nScaleW = ((double)Width / (double)sourceWidth);
            nScaleH = ((double)Height / (double)sourceHeight);

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

            System.Drawing.Bitmap bmPhoto = null;

            try
            {
                bmPhoto = new System.Drawing.Bitmap(destWidth + (int)Math.Round(2 * destX), destHeight + (int)Math.Round(2 * destY));
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("destWidth:{0}, destX:{1}, destHeight:{2}, desxtY:{3}, Width:{4}, Height:{5}",
                    destWidth, destX, destHeight, destY, Width, Height), ex);
            }

            using (System.Drawing.Graphics grPhoto = System.Drawing.Graphics.FromImage(bmPhoto))
            {
                grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                grPhoto.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                grPhoto.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                Rectangle to = new System.Drawing.Rectangle((int)Math.Round(destX), (int)Math.Round(destY), destWidth, destHeight);
                Rectangle from = new System.Drawing.Rectangle(sourceX, sourceY, sourceWidth, sourceHeight);
                //Console.WriteLine("From: " + from.ToString());
                //Console.WriteLine("To: " + to.ToString());
                grPhoto.DrawImage(image, to, from, System.Drawing.GraphicsUnit.Pixel);

                return bmPhoto;
            }
        }

        /// <summary>
        /// Resize image to a fixed destination size without preserving aspect ratio
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="Width">Destination Width</param>
        /// <param name="Height">Destination Height</param>
        private static Image SimpleImageResize(Image image, int Width, int Height)
        {
            Bitmap bmPhoto = new System.Drawing.Bitmap(Width, Height);

            using (System.Drawing.Graphics grPhoto = System.Drawing.Graphics.FromImage(bmPhoto))
            {
                grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                grPhoto.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                grPhoto.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Rectangle to = new System.Drawing.Rectangle(0, 0, Width, Height);
                Rectangle from = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
                grPhoto.DrawImage(image, to, from, System.Drawing.GraphicsUnit.Pixel);
                return bmPhoto;
            }
        }
    }
}
