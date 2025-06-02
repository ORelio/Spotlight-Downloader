using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SpotlightDownloader
{
    /// <summary>
    /// Wrapper class for generating images
    /// By ORelio (c) 2018 - CDDL 1.0
    /// <remarks>
    /// Jpeg part of this class is written by Pratik and davidwroxy from StackOverflow
    /// </remarks>
    /// </summary>
    internal static class ImageEncoder
    {
        /// <summary>
        /// Encode an input image to a Jpeg file below the requested size, trying to achieve maximum possible quality.
        /// </summary>
        /// <param name="inputPath">Input image path</param>
        /// <param name="outputPath">Output jpeg image path</param>
        /// <param name="maximumSizeBytes">Maximum size in bytes. If omitted, the Jpeg file is created with maximum available quality</param>
        /// <returns>TRUE if thefile was successfully generated</returns>
        public static bool CreateJpeg(string inputPath, string outputPath, long maximumSizeBytes = Int64.MaxValue)
        {
            return CreateJpeg(Image.FromFile(inputPath), outputPath, maximumSizeBytes);
        }

        /// <summary>
        /// Encode an input image to a Jpeg file below the requested size
        /// </summary>
        /// <param name="inputPath">Input image</param>
        /// <param name="outputPath">Output jpeg image path</param>
        /// <param name="maximumSizeBytes">Maximum size in bytes. If omitted, the Jpeg file is created with maximum available quality</param>
        /// <returns>TRUE if thefile was successfully generated</returns>
        public static bool CreateJpeg(Image image, string outputPath, long maximumSizeBytes = Int64.MaxValue)
        {
            long outputFileSize = Int64.MaxValue;
            int quality = 100;
            do
            {
                SaveJpeg(outputPath, image, quality);
                outputFileSize = new FileInfo(outputPath).Length;
                quality--;
            }
            while (quality >= 0 && outputFileSize > maximumSizeBytes);
            return quality >= 0;
        }

        /// <summary>
        /// Encode an input image to a PNG file
        /// </summary>
        /// <param name="image">Input image path</param>
        /// <param name="outputPath">Output png image path</param>
        public static void CreatePng(string inputPath, string outputPath)
        {
            CreatePng(Image.FromFile(inputPath), outputPath);
        }

        /// <summary>
        /// Encode an input image to a PNG file
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="outputPath">Output png image path</param>
        public static void CreatePng(Image image, string outputPath)
        {
            image.Save(outputPath, ImageFormat.Png);
        }

        /// <summary>
        /// Copy an input image file to an output path, converting image if necessary. Existing output file is overwritten.
        /// </summary>
        /// <param name="inputPath">Input image path</param>
        /// <param name="outputPath">Output image path</param>
        /// <exception cref="NotSupportedException">Thrown if a non-supported output format is requested and image needs converting.</exception>
        public static void AutoCopyImageFile(string inputPath, string outputPath)
        {
            string intputExt = Path.GetExtension(inputPath).ToLower();
            string outputExt = Path.GetExtension(outputPath).ToLower();
            if (intputExt == outputExt)
            {
                File.Copy(inputPath, outputPath, true);
            }
            else if (outputExt == ".jpg")
            {
                CreateJpeg(inputPath, outputPath);
            }
            else if (outputExt == ".png")
            {
                CreatePng(inputPath, outputPath);
            }
            else throw new NotSupportedException(String.Format("Output file format '{0}' is not supported.", outputExt));
        }

        /// <summary>
        /// Saves an image as a jpeg image, with the given quality
        /// </summary>
        /// <param name="path"> Path to which the image would be saved. </param>
        /// <param name="quality"> An integer from 0 to 100, with 100 being the highest quality.</param>
        /// <seealso>https://stackoverflow.com/a/4161930</seealso>
        private static void SaveJpeg(string path, Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");

            // Encoder parameter for image quality 
            EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
            // JPEG image codec 
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;
            img.Save(path, jpegCodec, encoderParams);
        }

        /// <summary>
        /// Returns the image codec with the given mime type
        /// </summary>
        /// <seealso>https://stackoverflow.com/a/4161930</seealso>
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];

            return null;
        }
    }
}
