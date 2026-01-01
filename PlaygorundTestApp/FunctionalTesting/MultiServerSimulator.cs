using HttpPlaygroundServer;
using HttpPlaygroundServer.Model;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaygorundTestApp.FunctionalTesting
{
    internal class MultiServerSimulator : HttpRequestProcessor
    {
        /// <summary>
        /// This wiill simulate server logic for cat and photo server
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override Task<ResponseModel> SimulateServerHandling(RequestModel rs)
        {
            // chk for cat
            if(rs.URL.Contains("cat"))
            {
                return RetreiveCatResponse(rs);
            }
            // chk for photo

            return RetreivePhotoResponse(rs);
        }

        /// <summary>
        /// here is the simulation
        /// Cat with catid = -1 is bad request
        /// Cat with catid = 3 has photo url
        /// Other cats do not have photo
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
        /// here is the simulation
        /// POST
        /// Cat with catid = -1 is bad request
        /// Any other cat id works
        /// GET
        /// Cat with catid = -1 is bad request
        /// Any other cat id works
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>

        private Task<ResponseModel> RetreiveCatResponse(RequestModel rs)
        {
            // if it ends with -1, return 400
            // return cat

            if(rs.Verb == HttpMethod.Get.Method)
            {
                int catEndInd = rs.URL.IndexOf("cats", StringComparison.InvariantCultureIgnoreCase) + 4;
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
                CatModel? catModel = JsonSerializer.Deserialize<CatModel?>(rs.Body);

                string responseFile = (catModel?.Id == -1) ? "400.json" : "CatPost.json";

                return base.RetrieveByFile(responseFile);
            }
            else
            {
                return base.RetrieveByFile("Patch.json");
            }
        }
    }
}
