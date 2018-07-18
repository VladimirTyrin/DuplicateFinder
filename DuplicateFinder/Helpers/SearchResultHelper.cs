using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DuplicateFinder.Search;
using ITCC.Logging.Core;

namespace DuplicateFinder.Helpers
{
    public static class SearchResultHelper
    {
        public static async Task<bool> SaveResultAsync(string filePath, SearchResult result)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var writer = new StreamWriter(fileStream))
                    {
                        await SaveResultInternalAsync(writer, result);
                        await writer.FlushAsync();
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogException("SAVE", LogLevel.Error, exception);
                return false;
            }
        }

        private static async Task SaveResultInternalAsync(StreamWriter writer, SearchResult result)
        {
            await writer.WriteLineAsync("#### Directories");
            await writer.WriteLineAsync();
            if (result.DirectoryDuplicates != null && result.DirectoryDuplicates.Any())
            {
                foreach (var directoryDuplicate in result.DirectoryDuplicates)
                {
                    await writer.WriteLineAsync();
                    foreach (var path in directoryDuplicate.Paths)
                    {
                        await writer.WriteLineAsync(path);
                    }
                }
            }
            else
            {
                await writer.WriteLineAsync("No directory duplicates");
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("#### Files ");
            await writer.WriteLineAsync();
            if (result.FileDuplicates != null && result.FileDuplicates.Any())
            {
                foreach (var fileDuplicate in result.FileDuplicates)
                {
                    await writer.WriteLineAsync();
                    foreach (var path in fileDuplicate.Paths)
                    {
                        await writer.WriteLineAsync(path);
                    }
                }
            }
            else
            {
                await writer.WriteLineAsync("No file duplicates");
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("#### Skipped ");
            await writer.WriteLineAsync();
        }
    }
}
