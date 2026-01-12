////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////

using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using System.Text.Json;

namespace PlaygorundTestApp.FunctionalTesting
{
    internal class MultiServerSimulator : HttpRequestProcessor
    {
        /// <summary>
        /// This wiill simulate server logic for both cat and photo server.
        /// This is just a crude simulator fo test/demo purpose and no way close 
        /// to any good logic.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override Task<ResponseModel> SimulateServerHandling(RequestModel rs)
        {
            // Redirect for cat or photo based on the URL content
            if(rs.URL.Contains("cat"))
            {
                return RetreiveCatResponse(rs);
            }

            return RetreivePhotoResponse(rs);
        }

        /// <summary>
        /// Here is the simulation
        /// Cat with catid = -1 is bad request
        /// Cat with catid = 3 has photo url
        /// Cat with anu other CatId does not have photo
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        private Task<ResponseModel> RetreivePhotoResponse(RequestModel rs)
        {
            ResponseModel rm = new ResponseModel();
            rm.Headers.Add("Content-Type", "plain/text; charset=utf-8");

            int catIdInd = rs.URL.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1;
            string catId = rs.URL.Substring(catIdInd);

            if(catId == "-1")
            {
                rm.Body = "Not valid";
                rm.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }
            else if(catId == "3")
            {
                rm.Body = $"https://yourcatphoto/Cat{catId}";
                rm.StatusCode = System.Net.HttpStatusCode.OK;
            }
            else
            {
                rm.StatusCode = System.Net.HttpStatusCode.NotFound;
                rm.Body = null;
            }

            var ret = new Task<ResponseModel>(() => rm);
            ret.RunSynchronously();

            return ret;
        }

        /// <summary>
        /// Here is the simulation for cat response
        /// GET
        /// Cat with catid = -1 is bad request
        /// Any other cat id works
        /// 
        /// POST
        /// Cat with catid = -1 is bad request
        /// Any other cat id works
        /// 
        /// PATCH
        /// Any cat ID works
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        private Task<ResponseModel> RetreiveCatResponse(RequestModel rs)
        {
            // response is based on method
            if(rs.Verb == HttpMethod.Get.Method)
            {
                // get the id of the cat
                int catEndInd = rs.URL.IndexOf("cats", StringComparison.InvariantCultureIgnoreCase) + 4;
                // if cat id is -1, return 404
                // FYI rs.URL.Remove(catEndInd) is used to remove cat id from the URL. So all response files can stay in one folder
                if (rs.URL.Contains("-1"))
                {
                    return base.RetrieveByFile("404.json", rs.URL.Remove(catEndInd));
                }
                else
                {
                    return base.RetrieveByFile("CatGet.json", rs.URL.Remove(catEndInd));
                }
            }
            else if(rs.Verb == HttpMethod.Post.Method)
            {
                // when creating a cat, check the id of the cat from the body.
                // and return appropriate file

                CatModel? catModel = JsonSerializer.Deserialize<CatModel?>(rs.Body);

                string responseFile = (catModel?.Id == -1) ? "400.json" : "CatPost.json";

                return base.RetrieveByFile(responseFile);
            }
            else
            {
                // else patch is called.
                return base.RetrieveByFile("Patch.json");
            }
        }
    }
}
