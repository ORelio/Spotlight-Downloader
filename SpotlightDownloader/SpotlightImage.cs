using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
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
    }
}
