using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Threading;

namespace SpotlightDownloader
{
    /// <summary>
    /// Represents a Spotlight image
    /// </summary>
    class SpotlightImage
    {
        public string Uri { get; set; }
        public string Sha256 { get; set; }
        public int FileSize { get; set; }
        public string Title { get; set; }
        public string Copyright { get; set; }

        /// <summary>
        /// Get image path to which the image will be downloaded
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <param name="outputName">Specify output file name, or let SpotlightImage generate Guid from sha256 hash</param>
        /// <param name="extension">File extension, defaults to .jpg since image is a JPEG file</param>
        /// <returns>Image path on disk</returns>
        public string GetFilePath(string outputDir, string outputName = null, string extension = ".jpg")
        {
            if (outputName == null)
            {
                byte[] sha256 = System.Convert.FromBase64String(Sha256);
                byte[] guid = new byte[16];
                for (int i = 0; i < guid.Length && i < sha256.Length; i++)
                    guid[i] = sha256[i];
                outputName = new Guid(guid).ToString();
            }
            return outputDir + Path.DirectorySeparatorChar + outputName + extension;
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
            string outputFile = GetFilePath(outputDir, outputName, ".jpg");

            (new WebClient()).DownloadFile(Uri, outputFile);

            if (integrityCheck)
            {
                if (new FileInfo(outputFile).Length != FileSize)
                {
                    File.Delete(outputFile);
                    throw new InvalidDataException("SpotlightImage: File returned by server does not have the expected size: " + Uri + " - Expected size: " + FileSize);
                }
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

            if (metadata)
            {
                string outputMeta = GetFilePath(outputDir, outputName, ".txt");
                File.WriteAllLines(outputMeta, new[]{
                    "[SpotlightImage]",
                    "uri=" + Uri,
                    "sha256=" + Sha256,
                    "filesize=" + FileSize,
                    "title=" + Title,
                    "copyright=" + Copyright
                }, Encoding.UTF8);
            }

            return outputFile;
        }
    }
}
