using KAutoLeasing.InfraClasses;
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

namespace KAutoLeasing.Models
{
    public interface IDataHandler
    {
        Task<IEnumerable<JatkoLeasing_Vehicle>> getJatkoLeasingVehicleData();
        Task<Brands> getLeasingModelsWithToken();
        Task<Trims> getLeasingTrimsWithToken(int makeId);
        Task<Trims> getLeasingTrimsByParameterWithToken(string trimIds);
        Task<Trims> getLeasingTrimsByModelcodeParameterWithToken(string modelCodes);
        Task<Trims> getLeasingTrimsByModelWithToken(int makeId, int modelId);
        Task<LeasingCacheItem> getLeasingCacheItem(string id);
        void setLeasingCacheItem(string id, string json);
    }
    public class OKAPIHandler : IDataHandler, IDisposable
    {
        private static Logger logger;
        private AppSettings AppSettings;

        private LeasingCacheDbContext db;


        public OKAPIHandler(IOptions<AppSettings> settings, LeasingCacheDbContext _db)
        {
            db = _db;

            AppSettings = settings.Value;

            if (AppSettings.useLogging)
                logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<LeasingCacheItem> getLeasingCacheItem(string id)
        {
            try
            {
                return await db.LeasingCacheTable
                    .FindAsync(id);
            }
            catch (Exception ex)
            {
                if (logger != null) logger.Error("Cachen saanti ei onnistunut, " + ex.Message );
            }
            return null;
        }
        public void setLeasingCacheItem(string id, string json)
        {            
            try
            {
                LeasingCacheItem c = new LeasingCacheItem
                {
                    ID = id,
                    json = json
                };

                db.LeasingCacheTable.Add(c);
                db.SaveChanges();                
            }
            catch(Exception ex)
            {
                if (logger != null) logger.Error("Cachen kirjoitus ei onnistunut, " + ex.Message);
            }         
        }

        
        public async Task<IEnumerable<JatkoLeasing_Vehicle>> getJatkoLeasingVehicleData()
        {
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
            return list;
        }


        
        public async Task<Brands> getLeasingModelsWithToken()
        {
            Brands list = null;
            try
            {
                //asetukset

                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();
                                
                var authRequest = new RestRequest(AppSettings.Leasing_apiAuthAddress, Method.POST);
                authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                authRequest.AddParameter("grant_type", "password");
                authRequest.AddParameter("userName", AppSettings.Leasing_apiUser);
                authRequest.AddParameter("password", AppSettings.Leasing_apiPw);

                var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);

                if (authResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(authResponse.Content);
                    string access_token = jObject.GetValue("access_token").ToString();
                    string token_type = jObject.GetValue("token_type").ToString();

                    var request = new RestRequest(AppSettings.Leasing_apiReqAddress_Models_WithToken, Method.GET);
                    request.AddHeader("authorization", token_type + " " + access_token);
                    var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //if (logger != null) logger.Info("Sisältö:" + response.Content);
                        list = JsonConvert.DeserializeObject<Brands>(response.Content);
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
            return list;
        }

        

        

        public async Task<Trims> getLeasingTrimsWithToken(int makeId)
        {
            Trims list = null;

            try
            {
                //asetukset
                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();

                var authRequest = new RestRequest(AppSettings.Leasing_apiAuthAddress, Method.POST);
                authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                authRequest.AddParameter("grant_type", "password");
                authRequest.AddParameter("userName", AppSettings.Leasing_apiUser);
                authRequest.AddParameter("password", AppSettings.Leasing_apiPw);

                var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);

                //Random random = new Random(DateTime.Now.Millisecond);
                //string r = random.Next().ToString();

                if (authResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(authResponse.Content);
                    string access_token = jObject.GetValue("access_token").ToString();
                    string token_type = jObject.GetValue("token_type").ToString();

                    string addr = AppSettings.Leasing_apiReqAddress_Trims_WithToken + "/" + makeId; // + "?" + r;
                    var request = new RestRequest(addr, Method.GET);
                    request.AddHeader("authorization", token_type + " " + access_token);
                    var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //if (logger != null) logger.Info("Sisältö:" + response.Content);
                        list = JsonConvert.DeserializeObject<Trims>(response.Content);
                    }
                    else
                    {
                        if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                    }
                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error(e + ", trimmien haku ei onnistunut.");
            }
            
            return list;
        }

        public async Task<Trims> getLeasingTrimsByParameterWithToken(string trimIds)
        {
            Trims list = null;

            try
            {
                //asetukset
                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();

                var authRequest = new RestRequest(AppSettings.Leasing_apiAuthAddress, Method.POST);
                authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                authRequest.AddParameter("grant_type", "password");
                authRequest.AddParameter("userName", AppSettings.Leasing_apiUser);
                authRequest.AddParameter("password", AppSettings.Leasing_apiPw);

                var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);

                if (authResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(authResponse.Content);
                    string access_token = jObject.GetValue("access_token").ToString();
                    string token_type = jObject.GetValue("token_type").ToString();

                    //haetaan ensin mallit
                    var request = new RestRequest(AppSettings.Leasing_apiReqAddress_Models_WithToken, Method.GET);
                    request.AddHeader("authorization", token_type + " " + access_token);
                    var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);
                    Brands brands = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {                        
                        brands = JsonConvert.DeserializeObject<Brands>(response.Content);
                        Trims thislist = null;
                        list = new Trims();
                        list.trims = new List<Trim>();
                        foreach(var brand in brands.brands)
                        {
                            int makeId = brand.id;
                            //haetaan brändin trimmit listaan
                            request = new RestRequest(AppSettings.Leasing_apiReqAddress_Trims_WithToken + "/" + makeId, Method.GET);
                            request.AddHeader("authorization", token_type + " " + access_token);
                            response = await client.ExecuteAsync(request, cancellationTokenSource.Token);
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                thislist = JsonConvert.DeserializeObject<Trims>(response.Content);
                                list.trims.AddRange(thislist.trims);
                            }
                            else
                            {
                                if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                            }
                        }

                        int[] reqTrims = Array.ConvertAll(trimIds.Split(','), s => int.Parse(s));

                        list.trims = list.trims.Where(t => reqTrims.Contains(t.id)).ToList();

                    }
                    else
                    {
                        if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                    }         
                   

                    
                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error(e + ", trimmien haku ei onnistunut.");
            }

            return list;
        }

        public async Task<Trims> getLeasingTrimsByModelcodeParameterWithToken(string modelCodes)
        {
            Trims list = null;

            try
            {
                //asetukset
                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();

                var authRequest = new RestRequest(AppSettings.Leasing_apiAuthAddress, Method.POST);
                authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                authRequest.AddParameter("grant_type", "password");
                authRequest.AddParameter("userName", AppSettings.Leasing_apiUser);
                authRequest.AddParameter("password", AppSettings.Leasing_apiPw);

                var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);

                if (authResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(authResponse.Content);
                    string access_token = jObject.GetValue("access_token").ToString();
                    string token_type = jObject.GetValue("token_type").ToString();

                    //haetaan ensin mallit
                    var request = new RestRequest(AppSettings.Leasing_apiReqAddress_Models_WithToken, Method.GET);
                    request.AddHeader("authorization", token_type + " " + access_token);
                    var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);
                    Brands brands = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        brands = JsonConvert.DeserializeObject<Brands>(response.Content);
                        Trims thislist = null;
                        list = new Trims();
                        list.trims = new List<Trim>();
                        foreach (var brand in brands.brands)
                        {
                            int makeId = brand.id;
                            //haetaan brändin trimmit listaan
                            request = new RestRequest(AppSettings.Leasing_apiReqAddress_Trims_WithToken + "/" + makeId, Method.GET);
                            request.AddHeader("authorization", token_type + " " + access_token);
                            response = await client.ExecuteAsync(request, cancellationTokenSource.Token);
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                thislist = JsonConvert.DeserializeObject<Trims>(response.Content);
                                list.trims.AddRange(thislist.trims);
                            }
                            else
                            {
                                if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                            }
                        }

                        //int[] reqTrims = Array.ConvertAll(modelCodes.Split(','), s => int.Parse(s));

                        list.trims = list.trims.Where(t => modelCodes.Contains(t.technicalInformation.modelCode)).ToList();

                    }
                    else
                    {
                        if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                    }



                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error(e + ", trimmien haku ei onnistunut.");
            }

            return list;
        }


        public async Task<Trims> getLeasingTrimsByModelWithToken(int makeId, int modelId)
        {
            Trims list = null;

            try
            {
                //asetukset
                var cancellationTokenSource = new CancellationTokenSource();
                var client = new RestClient();

                var authRequest = new RestRequest(AppSettings.Leasing_apiAuthAddress, Method.POST);
                authRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
                authRequest.AddParameter("grant_type", "password");
                authRequest.AddParameter("userName", AppSettings.Leasing_apiUser);
                authRequest.AddParameter("password", AppSettings.Leasing_apiPw);

                var authResponse = await client.ExecuteAsync(authRequest, cancellationTokenSource.Token);

                if (authResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jObject = JObject.Parse(authResponse.Content);
                    string access_token = jObject.GetValue("access_token").ToString();
                    string token_type = jObject.GetValue("token_type").ToString();

                    string addr = AppSettings.Leasing_apiReqAddress_Trims_WithToken + "/" + makeId;
                    var request = new RestRequest(addr, Method.GET);
                    request.AddHeader("authorization", token_type + " " + access_token);
                    var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //if (logger != null) logger.Info("Sisältö:" + response.Content);
                        list = JsonConvert.DeserializeObject<Trims>(response.Content);
                        
                        list.trims = list.trims.Where(t => t.modelId == modelId).ToList();
                    }
                    else
                    {
                        if (logger != null) logger.Info("status:" + response.StatusCode + ", " + response.StatusDescription);
                    }
                }
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error(e + ", trimmien haku ei onnistunut.");
            }

            return list;
        }

        public void Dispose()
        {
            try
            {
                db.Dispose();
                db = null;
            }
            catch (Exception e)
            {

            }
        }
    }
}
