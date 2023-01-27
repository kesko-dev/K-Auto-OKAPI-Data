using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OKAPI.InfraClasses;
using OKAPI.Models;
using RestSharp;
using System.Net;

namespace OKAPI.Handlers
{
    public interface IOKAPIHandler
    {
        Task<OKAPIImageResponse?> GetModelImages(string? brandCode, string? modelCodeLong);
    }
    public class OKAPIHandler : IOKAPIHandler
    {
        private static Logger? logger;
        private AppSettings AppSettings;
        private string? access_token;
        private string? token_type;
        private IBrand brand;

        public OKAPIHandler(IOptions<AppSettings> settings, IBrand _brand)
        {

            AppSettings = settings.Value;
            brand = _brand;

            if (AppSettings.useLogging)
                logger = LogManager.GetCurrentClassLogger();

        }


        private async Task<bool> FetchToken(bool forceNew)
        {
            bool success = false;

            try
            {
                if (forceNew)
                    access_token = null;

                if (access_token == null)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var client = new RestClient();

                    var authRequest = new RestRequest(AppSettings.OKAPI_apiAuthAddress, Method.Post);
                    authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                    authRequest.AddParameter("grant_type", "client_credentials");
                    authRequest.AddParameter("client_id", AppSettings.OKAPI_client_id);
                    authRequest.AddParameter("client_secret", AppSettings.OKAPI_client_secret);

                    if(logger != null) logger.Info("    Fetching OKAPI token for client_id: "+AppSettings.OKAPI_client_id);  

                    var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);

                    if (authResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var jObject = JObject.Parse(authResponse.Content);
                        access_token = jObject.GetValue("access_token").ToString();
                        token_type = jObject.GetValue("token_type").ToString();
                        success = true;
                        if (logger != null) logger.Info("    Fetched OKAPI token: " + access_token.Substring(0,10)+"... and token-type: "+token_type);
                    }
                    else
                    {
                        if (logger != null) logger.Error("    Http status error fetching OKAPI token" + authResponse.StatusCode + ", " + authResponse.StatusDescription);
                    }
                }
                else
                    success = true;

            }
            catch (Exception e)
            {
                if (logger != null) logger.Error("    Error fetching OKAPI token: " + e.Message);
            }
            return success;
        }

        private async Task<string> GetOKAPIModelCode(string? modelCodeLong)
        {
            string OKAPIModelCode = "";

            try
            {
                //token
                if (access_token == null)
                    await FetchToken(false);

                bool success = false;
                bool refreshedToken = false;
                bool apierror = false;

                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();
                string url = AppSettings.OKAPI_type_endpoint_url + "TYPE:" + modelCodeLong;

                while (!success && !refreshedToken && !apierror)
                {
                    try
                    {
                        if (logger != null) logger.Info("    Fetching OKAPI model code with request: " + url);

                        var request = new RestRequest(url, Method.Get);
                        request.AddHeader("Authorization", token_type + " " + access_token);
                        request.AddHeader("Accept", "*/*");
                        request.AddHeader("Content-Type", "application/json");
                        var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var jObject = JObject.Parse(response.Content);
                            OKAPIModelCode = (string)jObject.SelectToken("data[0].model_code");
                            
                            if(OKAPIModelCode != null)
                            {
                                if (logger != null) logger.Info("      Fetched OKAPI model code, result: " + OKAPIModelCode);
                                success = true;
                            }                            
                        }
                        else
                        {
                            if (logger != null) logger.Error("    Http status error getting OKAPI model code: " + response.StatusCode + ", " + response.StatusDescription);
                            apierror = true;
                        }
                    }
                    catch (Exception e)
                    {
                        if (logger != null) logger.Error("    Error getting OKAPI model code." + (refreshedToken ? "Not trying anymore." : "Refreshing the token and trying one more time.") + " Error: " + e.Message);
                        if (!refreshedToken)
                            if (await FetchToken(true))
                                refreshedToken = true;
                    }
                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error("    Outer Error getting OKAPI model code, error:" + e.Message + ".");
            }

            return OKAPIModelCode;
        }

        public async Task<OKAPIImageResponse?> GetModelImages(string? brandCode, string? modelCodeLong)
        {
            OKAPIImageResponse? images = null;

            try
            {
                //token
                if(access_token == null)
                    await FetchToken(false);

                string OKAPIModelCode = await GetOKAPIModelCode(modelCodeLong);
                bool success = false;
                bool refreshedToken = false;
                bool apierror = false;

                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();

                if (OKAPIModelCode.Length > 0)
                {
                    while (!success && !refreshedToken && !apierror)
                    {
                        try
                        {
                            string format = brand.ResolveImageFiletype(brandCode);
                            string apiUrl = AppSettings.OKAPI_image_endpoint_url + (format != null ? "?format="+format : "" ) + (AppSettings.Use_Image_Width != null ? "?wid=" + AppSettings.Use_Image_Width : "");
                            var request = new RestRequest(apiUrl, Method.Post);
                            request.AddHeader("authorization", token_type + " " + access_token);
                            request.AddHeader("Accept", "*/*");
                            request.AddHeader("OKAPI-PROCESSING-TYPE", "BATCH");
                            request.AddHeader("Content-Type", "application/json");

                            string requestBody = "{\"brand_code\": \"" + brand.GetOKAPIBrandCode(brandCode) + "\",\"model_code\": \"" + OKAPIModelCode + "\",\"options\":[{\"category\": \"TYPE\",\"code\": \"TYPE:" + modelCodeLong + "\"}]}";
                            if (logger != null) logger.Info("      Trying to fetch images with request body: " + requestBody);
                            request.AddParameter("application/json",requestBody,ParameterType.RequestBody);                                                                                

                            var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                            if (response.StatusCode == HttpStatusCode.OK)
                            {                                
                                images = JsonConvert.DeserializeObject<OKAPIImageResponse>(response.Content);
                                if(images != null)
                                {                                    
                                    success = true;                                    
                                }
                            }
                            else
                            {
                                if (logger != null) logger.Error("    Http status error getting OKAPI images: " + response.StatusCode + ", " + response.StatusDescription);
                                apierror = true;
                            }
                        }
                        catch (Exception e)
                        {
                            if (logger != null) logger.Error("    Error getting OKAPI images." + (refreshedToken ? "Not trying anymore." : "Refreshing the token and trying one more time.") + " Error: " + e.Message);
                            if (!refreshedToken)
                                if (await FetchToken(true))
                                    refreshedToken = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error("    Outer Error getting OKAPI images, error:" + e.Message + ".");
            }

            return images;
        }
    }
}
