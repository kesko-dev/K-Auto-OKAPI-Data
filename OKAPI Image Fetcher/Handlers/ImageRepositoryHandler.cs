using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NLog;
using OKAPI.InfraClasses;
using RestSharp;
using System.Net;

namespace OKAPI.Handlers
{
    /*
     * ImageRepositoryHandler. Named after the usage. If possible, the interface keeps the same even if the image repository and thus  
     * the handler itself changes.       
     */
    public interface IImageRepositoryHandler
    {
        Task<string>? AddModelImage(string? finalImageFileName, string? originalUrl);

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


        public async Task<string?> AddModelImage(string? finalImageFileName, string? originalUrl)
        {
            string? finalUrl = null;
            string modelimagepath = "modelimages/";

            try
            {                               
                var cancellationTokenSource = new CancellationTokenSource();                
                var client = new RestClient();

                string fileName = finalImageFileName.Substring(0, finalImageFileName.IndexOf("."));
                string fileType = finalImageFileName.Substring(finalImageFileName.IndexOf(".")+1);

                var address = AppSettings.Image_repository_url + modelimagepath + fileName;

                //1. send first the metadata of the image
                var request = new RestRequest(AppSettings.Image_repository_url, Method.Put);
                request.AddHeader("x-token", AppSettings.Image_repository_secret);
                request.AddHeader("Content-type", "application/json");
                
                string requestBody = "{\"contentType\": \"Image/"+fileType+"\",\"originalFilename\": \"" + finalImageFileName + "\",\"meta\":{}}";
                if (logger != null) logger.Info("      Trying to insert image meta data with: " + requestBody);
                request.AddParameter("application/json", requestBody, ParameterType.RequestBody);

                var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(response.Content);
                    var uploadUrl = jObject.GetValue("uploadUrl").ToString();

                    //2. send the actual image
                    if(uploadUrl != null)
                    {
                        var requestImage = new RestRequest(uploadUrl, Method.Put);
                        requestImage.AddHeader("x-token", AppSettings.Image_repository_secret);
                        requestImage.AddBody(File.ReadAllBytes(originalUrl));

                        var responseImage = await client.ExecuteAsync(requestImage, cancellationTokenSource.Token); 
                        if(responseImage.StatusCode == HttpStatusCode.OK)
                        {
                            finalUrl = AppSettings.Image_repository_public_url + modelimagepath + fileName;
                        }
                        else
                        {
                            if (logger != null) logger.Error("    API returned error with image request, status:" + responseImage.StatusCode + ", " + responseImage.StatusDescription);
                        }
                    }
                    else
                    {
                        if (logger != null) logger.Error("    Couldn't get API image upload url");
                    }
                }
                else
                {
                    if (logger != null) logger.Error("    API returned error with meta request, status:" + response.StatusCode + ", " + response.StatusDescription);
                }                       

            }
            catch (Exception e)
            {
                if (logger != null) logger.Error("    Error in model image adding: "+e.Message);
            }
            
            return finalUrl;
        }
    }
}
