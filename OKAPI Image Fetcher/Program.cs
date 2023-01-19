using OKAPI.InfraClasses;
using OKAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using OKAPI.Services;
using OKAPI.Handlers;


namespace TiNet_Leasing_Import
{
    internal class Program
    {
        private static Logger? logger = LogManager.GetCurrentClassLogger(); 
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, args);

            var serviceProvider = serviceCollection.BuildServiceProvider();
                        
            if(logger != null) logger.Info("Starting console application...");

            if (logger != null) logger.Info($"Arguments count={args.Length}");
            foreach (var arg in args)
            {
                if (logger != null) logger.Info($"Passed argument {arg}");
            }

            var importRunner = serviceProvider.GetService<ImportRunner>();

            try
            {
                importRunner.Start().Wait();
            }
            catch (Exception e)
            {
                if (logger != null) logger.Error(e, "Exception while running main application.");
            }

            if (logger != null) logger.Info("Stopping console application...");
            LogManager.Flush();
        }

        private static void ConfigureServices(IServiceCollection services, string[] args)
        {

            var configuration = new ConfigurationBuilder()                               
                .AddJsonFile("appsettings.json")
                .Build();
            services.Configure<AppSettings>(configuration.GetRequiredSection("AppSettings"));

            services.AddSingleton<IConfiguration>(configuration);

            services.AddSingleton<ImportRunner>();

            services.AddScoped<IOKAPIHandler, OKAPIHandler>();
            services.AddScoped<IImageRepositoryHandler, ImageRepositoryHandler>();  
            services.AddScoped<IDatabaseHandler, DatabaseHandler>();
            services.AddScoped<IBrand, Brand>();

            services.AddDbContext<ModelDataDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("ModelDataConnection")));
            
            services.AddTransient<ISchedulerJob, ModelPriceForOnline_ImageFetchJob>();

        }

    }
}
