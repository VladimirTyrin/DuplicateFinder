using System;
using System.Threading.Tasks;
using DuplicateFinder.Search;
using ITCC.Logging.Core;

namespace DuplicateFinder.Managers
{
    public static class SearchManager
    {
        public static async Task<SearchResult> SearchForDuplicatesAsync(IProgressHandler progressHandler)
        {
            try
            {
                var result = new SearchResult();

                return result;
            }
            catch (Exception e)
            {
                Logger.LogException("SEARCH", LogLevel.Error, e);
                return null;
            }
            finally
            {
                await progressHandler.ReportCompletedAsync();
            }
        }

        public static async Task StopAsync()
        {

        }
    }
}
