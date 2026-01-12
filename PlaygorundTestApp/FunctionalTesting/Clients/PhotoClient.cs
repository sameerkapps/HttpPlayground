////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////

namespace PlaygorundTestApp.FunctionalTesting.Clients
{
    /// <summary>
    /// This is a class simulating client for third party APIs.
    /// </summary>
    internal class PhotoClient
    {
        internal const string RelativeURL = "/PhotoURLs";

        public string BaseUrl { get; set; } = "https://catphotos.com";

        public int Port { get; set; }

        public static PhotoClient Instance { get; } = Instance ?? new PhotoClient();

        private PhotoClient()
        {
        }

        /// It will retrieve photo URI for the given catId
        internal async Task<string?> GetCatPhoto(int catid)
        {
            using var httpClient = new HttpClient();
            try
            {
                UriBuilder uriBuilder = new UriBuilder("http", BaseUrl, Port, RelativeURL + $"/{catid}");

                string photoUri = await httpClient.GetStringAsync(uriBuilder.Uri.AbsoluteUri);

                return photoUri;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Photo Request error: {ex.Message}");

                return null;
            }
        }
    }
}
