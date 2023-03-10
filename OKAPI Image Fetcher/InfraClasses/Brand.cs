using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Options;
using NLog;
using OKAPI.Models;
using System.Runtime.InteropServices;

namespace OKAPI.InfraClasses
{
    public interface IBrand
    {
        string? GetOKAPIBrandCode(string? brandName);
        string ResolveImageFiletype(string? brandName);
        string? ResolveImageName(string originalName, string brandName, string modelCode);
    }
    public class Brand : IBrand
    {
        private AppSettings AppSettings;
        public Brand(IOptions<AppSettings> settings)
        {
            AppSettings = settings.Value;
        }
        public string? GetOKAPIBrandCode(string? brandName)
        {
            string? OKAPIBrandCode = brandName;

            if(OKAPIBrandCode != null)
            {
                switch (OKAPIBrandCode.ToLower())
                {
                    case "audi":
                        OKAPIBrandCode = "AU";
                        break;
                    case "seat":
                    case "cupra":
                        OKAPIBrandCode = "SE";
                        break;
                    case "volkswagen":
                        OKAPIBrandCode = "VW";
                        break;
                }
            }

            return OKAPIBrandCode;
        }

        public string ResolveImageFiletype(string? brandName)
        {
            //set png as default
            string? filetype = "png";

            if (brandName != null)
            {
                switch (brandName.ToLower())
                {
                    case "audi":
                        filetype = AppSettings.Image_filetype_Audi;
                        break;
                    case "seat":
                    case "cupra":
                        filetype = AppSettings.Image_filetype_Seat;
                        break;
                    case "volkswagen":
                        filetype = AppSettings.Image_filetype_Volkswagen;
                        break;
                }
            }

            return filetype;
        }

        public string? ResolveImageName(string originalName, string? brandName, string modelCode)
        {
            string? finalName = null;

            string? conversionList = null;
            string? preventImageList = null;
            if (brandName != null)
            {
                switch (brandName.ToLower())
                {
                    case "audi":
                        conversionList = AppSettings.Image_naming_conversion_Audi;
                        preventImageList = AppSettings.Prevent_images_Audi;
                        break;
                    case "seat":
                    case "cupra":
                        conversionList = AppSettings.Image_naming_conversion_Seat;
                        preventImageList = AppSettings.Prevent_images_Seat;
                        break;
                    case "volkswagen":
                        conversionList = AppSettings.Image_naming_conversion_Volkswagen;
                        preventImageList = AppSettings.Prevent_images_Volkswagen;
                        break;
                }
            }

            if(conversionList != null)
            {
                string? converted = null;
                Dictionary<string, string> dic = new Dictionary<string, string>();  
                foreach(var item in conversionList.Split(";"))
                {
                    dic.Add(item.Split("=")[0], item.Split("=")[1]);
                }
                dic.TryGetValue(originalName, out converted);
                if(converted != null)
                    finalName = "img_" + modelCode.Replace(" ", "_") + "_" + converted;
                else
                    if(preventImageList == null || (preventImageList != null && !preventImageList.Contains(originalName)))
                        finalName = "img_" + modelCode.Replace(" ", "_") + "_zzz_" + originalName;
               
            }

            return finalName;
        }    
    }
}
