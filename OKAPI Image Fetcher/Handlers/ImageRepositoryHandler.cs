using OKAPI.InfraClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace OKAPI.Handlers
{
    /*
     * ImageRepositoryHandler. Named after the usage. If possible, the interface keeps the same even if the image repository and thus  
     * the handler itself changes. 
     * Default at this stage is the Kesko kuvakakku using Imgix https://docs.imgix.com/
     */
    public interface IImageRepositoryHandler
    {
        Task<string>? AddImage(string? finalName, string? originalUrl);

    }
    public class ImageRepositoryHandler : IImageRepositoryHandler
    {
        private static Logger? logger;
        private AppSettings AppSettings;

        public ImageRepositoryHandler(IOptions<AppSettings> settings)
        {

            AppSettings = settings.Value;

            if (AppSettings.useLogging)
                logger = LogManager.GetCurrentClassLogger();
        }


        public async Task<string?> AddImage(string? finalName, string? originalUrl)
        {
            string? finalUrl = originalUrl;

            /*
            IEnumerable<JatkoLeasing_Vehicle> list = null;

            try
            {
                //asetukset
                
                var cancellationTokenSource = new CancellationTokenSource();                
                var client = new RestClient();                

                var authRequest = new RestRequest(AppSettings.JatkoLeasing_apiAuthAddress, Method.POST);
                authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                authRequest.AddParameter("grant_type", "password");
                authRequest.AddParameter("userName", AppSettings.JatkoLeasing_apiUser);
                authRequest.AddParameter("password", AppSettings.JatkoLeasing_apiPw);
                
                var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);              
                
                if (authResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(authResponse.Content);
                    string access_token = jObject.GetValue("access_token").ToString();
                    string token_type = jObject.GetValue("token_type").ToString();

                    var request = new RestRequest(AppSettings.JatkoLeasing_apiReqAddress, Method.GET);
                    request.AddHeader("authorization", token_type + " " + access_token);
                    var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //if (logger != null) logger.Info("Sisältö:" + response.Content);
                        list = JsonConvert.DeserializeObject<IEnumerable<JatkoLeasing_Vehicle>>(response.Content);
                    }
                    else
                    {
                        if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                    }                    
                }

            }
            catch (Exception e)
            {
                if (logger != null) logger.Error(e + ", autojen haku ei onnistunut.");
            }
            */

            return finalUrl;
        }


    }
}
