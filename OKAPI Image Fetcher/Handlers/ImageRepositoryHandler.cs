using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OKAPI.InfraClasses;
using RestSharp;
using System;
using System.Net;
using System.Web;

namespace OKAPI.Handlers
{
    /*
     * ImageRepositoryHandler. Named after the usage. If possible, the interface keeps the same even if the image repository and thus  
     * the handler itself changes.       
     */
    public interface IImageRepositoryHandler
    {
        Task<string>? AddModelImage(string? finalImageFileName, string imageFiletype, string? originalUrl);

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


        public async Task<string?> AddModelImage(string? finalImageFileName, string imageFiletype, string? originalUrl)
        {
            string? finalUrl = null;
            string modelimagepath = "modelimages/";

            try
            {                               
                var cancellationTokenSource = new CancellationTokenSource();                
                var client = new RestClient();
                                
                var address = AppSettings.Image_repository_url + modelimagepath + finalImageFileName;

                //1. send first the metadata of the image
                var request = new RestRequest(address, Method.Put);
                request.AddHeader("x-token", AppSettings.Image_repository_secret);
                request.AddHeader("Content-Type", "application/json");
                
                string requestBody = "{\"contentType\": \"Image/"+imageFiletype+"\",\"originalFilename\": \"" + finalImageFileName+"."+imageFiletype + "\",\"meta\":{\"target\": \"www\"}}";
                if (logger != null) logger.Info("      Trying to insert image meta data with: " + requestBody);
                request.AddJsonBody(requestBody);
                var response = await client.PutAsync(request, cancellationTokenSource.Token);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(response.Content);
                    string uploadUrl = jObject.GetValue("uploadUrl").ToString(); 
                    if(logger != null) logger.Info("    Trying to insert file with the uploadUrl: "+uploadUrl);

                    //2. send the actual image
                    if(uploadUrl != null)
                    {
                        var requestImage = new RestRequest(uploadUrl, Method.Put);
                        requestImage.AddHeader("x-token", AppSettings.Image_repository_secret);
                        requestImage.AddHeader("content-type", "Image/"+imageFiletype);
                        requestImage.RequestFormat = DataFormat.Binary;
                        //requestImage.AlwaysMultipartFormData = true;

                        using (WebClient imageclient = new WebClient())
                        {
                            string path = "c:\\temp";
                            string imageName = finalImageFileName + "." + imageFiletype;
                            imageclient.DownloadFile(new Uri(originalUrl), path+"\\"+imageName);
                            byte[] bytes = File.ReadAllBytes(path+"\\"+imageName);
                            requestImage.AddParameter("Image/" + imageFiletype, bytes, ParameterType.RequestBody);

                            var responseImage = await client.PutAsync(requestImage, cancellationTokenSource.Token);
                            if (responseImage.StatusCode == HttpStatusCode.OK)
                            {
                                finalUrl = AppSettings.Image_repository_public_url + modelimagepath + finalImageFileName;
                            }
                            else
                            {
                                if (logger != null) logger.Error("    API returned error with image request, status:" + responseImage.StatusCode + ", " + responseImage.StatusDescription);
                            }

                            File.Delete(path+"\\"+imageName); 
                            imageclient.Dispose();
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
