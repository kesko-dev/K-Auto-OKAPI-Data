using System.Threading.Tasks;

namespace OKAPI.Services
{
    public interface ISchedulerJob
    {
        Task Execute();
    }
}