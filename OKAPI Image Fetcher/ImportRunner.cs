//using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKAPI.Services
{
    public class ImportRunner
    {
        private static Logger? logger;
        private readonly IEnumerable<ISchedulerJob> _jobs;

        public ImportRunner(IEnumerable<ISchedulerJob> jobs)
        {
            logger = LogManager.GetCurrentClassLogger();    
            _jobs = jobs;
        }

       
        public async Task Start()
        {
            if (logger != null) logger.Info($"Start leasing importrunner. Job count {_jobs.Count()}");

            foreach (var job in _jobs)
            {
                try
                {
                    if (logger != null) logger.Info($"Execute job {job.GetType().Name}.");

                    await job.Execute();
                }
                catch (Exception ex)
                {
                    if (logger != null) logger.Error(ex, $"Error while executing job {job.GetType().Name}.");
                }
            }

            if (logger != null) logger.Info("Jobs execution done.");
        }
    }
}
