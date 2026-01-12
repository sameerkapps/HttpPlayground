////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////

using PlaygorundTestApp.FunctionalTesting.Clients;
using PlaygorundTestApp.FunctionalTesting.Models;
using System.Net;

namespace PlaygorundTestApp.FunctionalTesting
{
    /// <summary>
    /// This class is intended to demo Functional testing. It conditionally makes multiple API calls.
    /// So one can simulate and test if the functional flow is correct.
    /// In Get, it will make calls to one backend API and another third party API and return the combined values.
    /// In Create, it will create cat, attempt to retrive it's 
    /// </summary>
    internal class CatManager
    {
        /// <summary>
        /// This will get the profile of a cat based on catid. The profile includes the details and PhotoURL.
        /// If cat is not found, it will return null
        /// If cat is found; but photo is not found, it will return nulll for PhotoURI
        /// </summary>
        /// <param name="catId">id of the cat</param>
        /// <returns>Cat details and photo url</returns>
        internal async Task<CatProfileModel?> GetCatProfile(int catId)
        {
            CatModel? catModel = await CatClient.Instance.GetCatInfo(catId).ConfigureAwait(false);
            if(catModel == null)
            {
                return null;
            }

            CatProfileModel catProfile = new();
            catProfile.Id = catModel.Id;
            catProfile.Name = catModel.Name;

            catProfile.PhotoUrl = await PhotoClient.Instance.GetCatPhoto(catId).ConfigureAwait(false);

            return catProfile;
        }

        /// <summary>
        /// This method will create a cat. Then retrieve its PhotoUri using PhotoClient.
        /// If the PhotoURI is not null, it will update the cat with URI
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
                // retrieve it's photo
                string? photoUrl = await PhotoClient.Instance.GetCatPhoto(catId).ConfigureAwait(false);

                // if it has value, update the value
                if(!string.IsNullOrEmpty(photoUrl))
                {
                    result = await CatClient.Instance.UpdateCatPhoto(catId, photoUrl).ConfigureAwait(false);
                }
            }

            return result;
        }
    }
}
