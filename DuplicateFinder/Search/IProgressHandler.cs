using System.Threading.Tasks;

namespace DuplicateFinder.Search
{
    public interface IProgressHandler
    {
        Task ReportCurrentAsync(string message);
        Task ReportCompletedAsync();
    }
}
