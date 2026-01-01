using PlaygorundTestApp.FunctionalTesting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaygorundTestApp.FunctionalTesting.Clients
{
    internal class CatClient
    {
        public static CatClient Instance { get; } = Instance ?? new CatClient();

        private CatClient()
        {
        }

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
