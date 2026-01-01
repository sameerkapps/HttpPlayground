////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

using HttpPlaygroundServer.Model;

namespace HttpPlaygroundServer
{
    /// <summary>
    /// This class store requests and retreives the responses from the storeage folder
    /// </summary>
    internal class StorageManager
    {
        private const string RequestsFolder = "Requests";
        private const string ResponsesFolder = "Responses";

        private string _strRequestsFolder;
        private string _strResponsesFolder;

        internal async Task StoreRequest(RequestModel rs)
        {
            // from URL, build storage folder
            CreateFoldersFromUrl(rs.URL);

            // build file name
            string fileName = $"{rs.Verb}-{DateTime.Now.ToString("yyyyMMdd-HHmmss.fff")}.json";
            // full file path and name
            string fullPath = Path.Combine(_strRequestsFolder, fileName);

            // Write the results
            string jsonOutput = JsonSerializer.Serialize<RequestModel>(rs, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            // WriteAllTextAsync is not available .net standard 2.0
            await Task.Run(() => File.WriteAllText(fullPath, jsonOutput)).ConfigureAwait(false);
        }

        internal async Task<ResponseModel> RetrieveResponseByFileName(string filename, string url = null)
        {
            // full file path and name
            string folderPath;
            if (url == null)
            {
                folderPath = _strResponsesFolder;
            }
            else
            {
                string basePath = BuildBaseFolderPath(url);
                folderPath = Path.Combine(basePath, ResponsesFolder);
            }
            
            string filePath = Path.Combine(folderPath, filename);
            if (File.Exists(filePath))
            {
                string strResponse = await Task<string>.Run(() => File.ReadAllText(filePath)).ConfigureAwait(false);

                return JsonSerializer.Deserialize<ResponseModel>(strResponse);
            }
            else
            {
                return new ResponseModel() { StatusCode = HttpStatusCode.NotFound };
            }
        }

        internal async Task<ResponseModel> RetrieveResponseByVerb(string verb)
        {
            // build default file name based on verb
                // if the file exists return the contents of the file
            // else return 200 or 201 or 204

            string verbFileName = $"{verb}.json";
            // full file path and name
            string filePath = Path.Combine(_strResponsesFolder, verbFileName);
            if (File.Exists(filePath))
            {
                string strResponse = await Task<string>.Run(() => File.ReadAllText(filePath)).ConfigureAwait(false);

                return JsonSerializer.Deserialize<ResponseModel>(strResponse);
            }
            else
            {
                HttpStatusCode code = HttpStatusCode.OK;
                if (HttpMethod.Post.Method.Equals(verb, StringComparison.InvariantCultureIgnoreCase))
                {
                    code = HttpStatusCode.Created;
                }
                else if (HttpMethod.Delete.Method.Equals(verb, StringComparison.InvariantCultureIgnoreCase))
                {
                    code = HttpStatusCode.NoContent;
                }

                return new ResponseModel() { StatusCode = code };
            }
        }

        /// <summary>
        /// Parses a URL and creates a corresponding folder structure on disk.
        /// </summary>
        /// <param name="url">The URL to parse.</param>
        /// <param name="baseFolder">The base folder where the structure should be created.</param>
        /// <returns>The full path to the created folder.</returns>
        internal void CreateFoldersFromUrl(string url, bool createRequestsFolder = true)
        {
            string fullPath = BuildBaseFolderPath(url);

            CreateFolders(fullPath, createRequestsFolder);
        }

        private void CreateFolders(string fullPath, bool createRequestsFolder)
        {
            // Create rest folder if missing
            Directory.CreateDirectory(fullPath);

            // Create requests folders
            if (createRequestsFolder)
            {
                _strRequestsFolder = Path.Combine(fullPath, RequestsFolder);
                Directory.CreateDirectory(_strRequestsFolder);
            }

            // Create responses folders
            _strResponsesFolder = Path.Combine(fullPath, ResponsesFolder);
            Directory.CreateDirectory(_strResponsesFolder);
        }

        private static string BuildBaseFolderPath(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));

            if (string.IsNullOrWhiteSpace(ServerConfig.StorageFolder))
                throw new ArgumentException("Base folder cannot be null or empty.", nameof(ServerConfig.StorageFolder));

            // Ensure base folder exists
            Directory.CreateDirectory(ServerConfig.StorageFolder);

            // Parse URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new UriFormatException($"Invalid URL format: {url}");

            // Clean up the path (remove leading slash, convert to folder path)
            string relativePath = uri.AbsolutePath
                .Trim('/')
                .Replace('/', Path.DirectorySeparatorChar);

            // Combine base folder + host + path
            return Path.Combine(ServerConfig.StorageFolder, relativePath);
        }
    }
}
