////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using PlaygorundTestApp.FunctionalTesting.Models;
using System.Net;
using System.Text.Json;

namespace PlaygorundTestApp.FunctionalTesting.Clients
{
    /// <summary>
    /// This class will act as a client that will make calls to Cat's REST API.
    /// It will perform GET, POST, and PATCH operations.
    /// </summary>
    internal class CatClient
    {
        public static CatClient Instance { get; } = Instance ?? new CatClient();

        private CatClient()
        {
        }

        // this get call returns info of the cat.
        // if cat is not found it returns null
        internal async Task<CatModel?> GetCatInfo(int catid)
        {
            RequestSender rs = new();

            (HttpStatusCode status, _, string body) = await rs.SendGet($"/{catid}").ConfigureAwait(false);
            if (status == HttpStatusCode.OK)
            {
                Console.WriteLine($"Cat with id = {catid} found");
                CatModel? cm = JsonSerializer.Deserialize<CatModel?>(body);

                return cm;
            }

            Console.WriteLine($"Cat with id = {catid} NOT found");

            return null;
        }

        // crates cat model and sends it to the backend
        internal async Task<HttpStatusCode> GreateCatInfo(int catid, string catName)
        {
            RequestSender rs = new();

            CatModel meow = new CatModel()
            {
                Id = catid,
                Name = catName
            };

            (HttpStatusCode status, _) = await rs.SendPost(meow);
            if (status == HttpStatusCode.OK)
            {
                Console.WriteLine($"Cat with id = {catid} created");
            }

            return status;
        }


        // This will update photo uri of the cat
        internal async Task<HttpStatusCode> UpdateCatPhoto(int catid, string photoUri)
        {
            RequestSender rs = new();

            CatPhotoUpdateModel catPhoto = new CatPhotoUpdateModel()
            {
                CatId = catid,
                PhotoURI = photoUri
            };

            (HttpStatusCode status, _) = await rs.SendPatch(catPhoto);
            if (status == HttpStatusCode.OK)
            {
                Console.WriteLine($"Cat photo updated");
            }

            return status;
        }
    }
}
