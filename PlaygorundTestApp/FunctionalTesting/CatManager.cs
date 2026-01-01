using PlaygorundTestApp.FunctionalTesting.Clients;
using PlaygorundTestApp.FunctionalTesting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlaygorundTestApp.FunctionalTesting
{
    internal class CatManager
    {
        /// <summary>
        /// This will get the profile of a cat based on catid. The profile includes the details and PhotoURL.
        /// If a photo is not found, it will return default
        /// if cat is not found, it will return null
        /// </summary>
        /// <param name="catId">id of the cat</param>
        /// <returns>Cat details and photo url</returns>
        internal async Task<CatProfieModel?> GetCatProfile(int catId)
        {
            CatModel? catModel = await CatClient.Instance.GetCatInfo(catId).ConfigureAwait(false);
            if(catModel == null)
            {
                return null;
            }

            CatProfieModel catProfile = new();
            catProfile.Id = catModel.Id;
            catProfile.Name = catModel.Name;

            catProfile.PhotoUrl = await PhotoClient.Instance.GetCatPhoto(catId).ConfigureAwait(false);

            return catProfile;
        }

        /// <summary>
        /// This method will create a cat. Then retrieve its PhotoUri
        /// if the PhotoURI is not null, it will send patch request to update the entity
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="catName"></param>
        /// <returns></returns>
        internal async Task<HttpStatusCode> CreateCatWithPhoto(int catId, string catName)
        {
            CatModel meow = new CatModel()
            {
                Id = catId,
                Name = catName
            };

            // create cat
            HttpStatusCode result = await CatClient.Instance.GreateCatInfo(catId, catName).ConfigureAwait(false);
            if(result == HttpStatusCode.Created)
            {
                string? photoUrl = await PhotoClient.Instance.GetCatPhoto(catId).ConfigureAwait(false);

                if(!string.IsNullOrEmpty(photoUrl))
                {
                    result = await CatClient.Instance.UpdateCatPhoto(catId, photoUrl).ConfigureAwait(false);
                }
            }

            return result;
        }
    }
}
