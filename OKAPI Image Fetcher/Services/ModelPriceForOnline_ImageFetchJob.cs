using OKAPI.InfraClasses;
using NLog;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using OKAPI.Models;
using OKAPI.Handlers;
using AlertPoster;

namespace OKAPI.Services
{
    /*
     * Each model data source needs its own image fetcher code because of different field names and different field set to use in image fetching itself.
     * This class fetches images for factory order models per price list lines. No vehicle individuals are involved.
     */
    public class ModelPriceForOnline_ImageFetchJob : ISchedulerJob
    {
        private static Logger? logger;
        private IOKAPIHandler okapiHandler;
        private IDatabaseHandler databaseHandler;
        private IImageRepositoryHandler imageRepositoryHandler;
        private AppSettings AppSettings;
        private IBrand brand;
        private EXISTENCETYPES type;

        public ModelPriceForOnline_ImageFetchJob(IOKAPIHandler _okapiHandler, IDatabaseHandler _databaseHandler, IOptions<AppSettings> settings, IImageRepositoryHandler _imageRepositoryHandler, IBrand _brand)
        {
            logger = LogManager.GetCurrentClassLogger();
            okapiHandler = _okapiHandler;
            databaseHandler = _databaseHandler;
            imageRepositoryHandler = _imageRepositoryHandler;   
            AppSettings = settings.Value;           
            brand = _brand;            
        }

        public async Task Execute()
        {
            bool success = true;
            int imgsTotalCount = 0;
            int modelsCount = 0;
            int modelsHandledCount = 0;
            int modelFailures = 0;
            int possibleModelFailures = 0;
            if (logger != null) logger.Info("Starting job for getting OKAPI images.");
            
            try
            {
                // getting model list
                IEnumerable<ModelsData>? models = await databaseHandler.GetModelsDataAsync();

                // do for each model
                if (models != null)
                {                    
                    modelsCount = models.Count();
                    if (logger != null) logger.Info("Going through models, count: " + modelsCount);

                    foreach (ModelsData model in models)
                    {
                        try
                        {
                            int thisImageCount = 0;
                            int thisExistingImageCount = await databaseHandler.GetModelImagesCountAsync(model.ComissionNumber);

                            if (logger != null) logger.Info("  Make: " + model.Make + ", model: " + model.ComissionNumber + ", existing images: " + thisExistingImageCount + (thisExistingImageCount < 3 ? ", trying to fetch more." : ", not fetching."));

                            //check if model already has images in database, assuming over 3 images means that all images exist not only some preview images
                            if (thisExistingImageCount < 3)
                            {
                                // get okapi images by model code and type code
                                OKAPIImageResponse? images = await okapiHandler.GetModelImages(model);

                                // do for each image-url
                                if (images != null && images.data != null && images.meta != null)
                                {
                                    if (logger != null) logger.Info("    - Found: " + images.meta.count + " images.");
                                    foreach (OKAPIImage image in images.data)
                                    {
                                        // image name update by brand naming logic (vs. okapi naming logic)
                                        image.finalName = brand.ResolveImageName(image.name, model.Make, model.ComissionNumber);

                                        //only accepting images that are recognized by naming rules
                                        // -> if we further recognize that different models provide different naming then we should alter the brand naming logic to also allow not identified names and provide them just index above the identified image names.
                                        if (image.finalName != null && image.url != null)
                                        {
                                            if (logger != null) logger.Info("    - Image final name: " + image.finalName);

                                            // add image file to image repository
                                            image.finalUrl = await imageRepositoryHandler.AddModelImage(image.finalName, brand.ResolveImageFiletype(model.Make), image.url) ?? String.Empty;
                                            if (logger != null) logger.Info("    - Image final url: " + (image.finalUrl.Length > 0 ? image.finalUrl : "N/A"));

                                            // add image final url to database with model identifier
                                            if (image.finalUrl.Length > 0)
                                            {
                                                if (await databaseHandler.AddImageToModelDataAsync(model.ComissionNumber, image.finalName, image.finalUrl))
                                                {
                                                    if (logger != null) logger.Info("    - Image added to model data.");
                                                    thisImageCount++;
                                                }
                                                else
                                                    if (logger != null) logger.Info("    - Image not added to model data.");
                                            }
                                        }
                                        else
                                            if (logger != null) logger.Info("NEW IMAGE NAME!!, not accepted, add to naming rules: " + image.name);

                                    }
                                    imgsTotalCount += thisImageCount;
                                    if (logger != null) logger.Info("  - Added " + thisImageCount + "images.");
                                }
                                else
                                {
                                    if (logger != null) logger.Info("  - No images added.");
                                    possibleModelFailures++;
                                }
                                
                                modelsHandledCount++;
                            }
                        }
                        catch(Exception e)
                        {
                            if(logger != null) logger.Error("  Error handling model: "+(model.ComissionNumber ?? "not available")+", error:"+e.Message);
                            modelFailures++;
                        }
                    }
                }
                if (logger != null) logger.Info("Added " + imgsTotalCount + " images.");

               
            }
            catch (Exception e)
            {
                if(logger != null ) logger.Error(e, "Error while handling Image Fetch job: "+e.StackTrace+ ", "+e.InnerException);
                success = false;
            }

            //logging stats
            if (logger != null) logger.Info("Handled models: " + modelsHandledCount + " / " + modelsCount + ", possible OKAPI failures:" + possibleModelFailures + ", model handling errors: " + modelFailures);

            //if several modelfailures
            if(modelFailures > 3)
                success = false;

            if (!success)
            {
                if (await Alert.sendAlertAsync(AppSettings.AlertWebhookUrl, AppSettings.AlertTitle, "OKAPI Image fetcher"))
                    if(logger != null) logger.Info("Sent alert");
                else
                    if(logger != null) logger.Error("Alert sending error!!");
            }
            if(logger != null) logger.Info("Closing job for fetching OKAPI images.");
        }
    }
}
