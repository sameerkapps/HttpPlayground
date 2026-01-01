using HttpPlaygroundServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlaygorundTestApp.FunctionalTesting.Clients
{
    internal class PhotoClient
    {
        internal const string RelativeURL = "/PhotoURLs";

        public string BaseUrl { get; set; } = "https://catphotos.com";

        public int Port { get; set; }

        public static PhotoClient Instance { get; } = Instance ?? new PhotoClient();

        private PhotoClient()
        {
        }

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
