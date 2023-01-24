using OKAPI.InfraClasses;
using OKAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NLog;
using System;
using AlertPoster;

namespace OKAPI.Handlers;

public interface IDatabaseHandler
{
    Task<IEnumerable<ModelsData>?> GetModelsDataAsync();
    Task<int> GetModelImagesCountAsync(string modelCode);
    Task<bool> AddImageToModelDataAsync(string? modelCode, string? finalName, string? finalUrl);
}
public class DatabaseHandler : IDatabaseHandler, IDisposable
{
    private static Logger logger;
    private AppSettings AppSettings;
    private ModelDataDbContext? db;

    public DatabaseHandler(IOptions<AppSettings> settings, ModelDataDbContext _db)
    {
        db = _db;

        AppSettings = settings.Value;

        if (AppSettings.useLogging)
            logger = LogManager.GetCurrentClassLogger();
    }

    public async Task<IEnumerable<ModelsData>?> GetModelsDataAsync()
    {
        IEnumerable<ModelsData>? models = null;

        try
        {
            models = await db.ModelsTable                
                .Select(g => new ModelsData
                {
                    modelCode = g.modelCode,
                    modelCodeLong = g.modelCodeLong,
                    make = g.make
                })
                //Testing with Tarraco
                .Where(m => m.modelCode.StartsWith("KN25"))                
                .ToListAsync();

        }
        catch (Exception e)
        {
            if (logger != null) logger.Error("Not getting models data from database, error: " + e);
        }

        return models;
    }

    public async Task<int> GetModelImagesCountAsync(string? modelCode)
    {
        int count = 0;

        try
        {
            count = await db.ModelsImageTable
                .Where(m => m.modelCode == modelCode)
                .CountAsync();         

        }
        catch (Exception e)
        {
            if (logger != null) logger.Error("Not getting model's images count from database, error: " + e);
        }

        return count;
    }

    public async Task<bool> AddImageToModelDataAsync(string? modelCode, string? finalName, string? finalUrl)
    {
        bool success = false;

        try
        {
            ModelImage image = new ModelImage();
            image.modelCode = modelCode;
            image.imageName = finalName;
            image.imageUrl = finalUrl;

            db.ModelsImageTable.Add(image);
            await db.SaveChangesAsync();
            success = true;
            if (logger != null) logger.Info("    Image: "+finalName+" data created in database.");
        }
        catch (Exception e)
        {
            if (logger != null) logger.Error("    Image data not created in database, error: " + e);               
        }

        return success;
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
            //fail silently
        }
    }
}
