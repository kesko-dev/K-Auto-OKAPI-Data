namespace OKAPI.InfraClasses
{
    public class AppSettings
    {
        public string OKAPI_apiAuthAddress { get; set; }
        public string OKAPI_client_id { get; set; }
        public string OKAPI_client_secret { get; set; }
        public string OKAPI_type_endpoint_url {get;set;}
        public string OKAPI_image_endpoint_url { get; set; }            
        public bool useLogging { get; set; }
        public string image_naming_conversion_Seat { get; set; }
        public string image_naming_conversion_Audi { get; set; }
        public string image_naming_conversion_Volkswagen { get; set; }
        public string AlertWebhookUrl { get; set; }
        public string AlertTitle { get; set; }
    }
}
