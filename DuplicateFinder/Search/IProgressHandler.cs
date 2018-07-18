using System.Threading.Tasks;

namespace DuplicateFinder.Search
{
    public interface IProgressHandler
    {
        Task ReportCurrentAsync(string message, int foldersQueued, int foldersProcessed, int filesProcessed);
        Task ReportStateAsync(string message);
        Task ReportCompletedAsync();
    }
}
