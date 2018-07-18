using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using DuplicateFinder.Search;
using DuplicateFinder.Settings;
using ITCC.Logging.Core;

namespace DuplicateFinder.Managers
{
    public static class SearchManager
    {
        private static CancellationTokenSource _cts;
        private static ActionBlock<(DirectoryInfo, IProgressHandler, SearchResult)> _searchBlock;

        public static async Task<SearchResult> SearchForDuplicatesAsync(IProgressHandler progressHandler)
        {
            Reload();
            await Task.Yield();

            try
            {
                var result = new SearchResult();
                
                var directories = GetInitialDirectories();
                foreach (var directory in directories)
                {
                    _searchBlock.Post((directory, progressHandler, result));
                }

                await _searchBlock.Completion;
                Logger.LogEntry("SEARCH", LogLevel.Info, "Search completed");

                return result;
            }
            catch (OperationCanceledException)
            {
                Logger.LogEntry("SEARCH", LogLevel.Info, "Search canceled");
                return null;
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

        public static void CancelSearch()
        {
            _cts.Cancel();
        }

        private static void Reload()
        {
            _cts = new CancellationTokenSource();
            _searchBlock = new ActionBlock<(DirectoryInfo, IProgressHandler, SearchResult)>(ProcessDirectoryAsync, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = SearchSettings.Instance.ThreadCount,
                CancellationToken = _cts.Token
            });
        }

        private static async Task ProcessDirectoryAsync((DirectoryInfo directory, IProgressHandler progressHandler, SearchResult result) arg)
        {
            var path = arg.directory.FullName;
            if (ShouldSkip(arg.directory))
            {
                Logger.LogEntry("SEARCH", LogLevel.Info, $"Skipping {path}");
                arg.result.SkippedPaths.Add(path);
                return;
            }

            try
            {
                await arg.progressHandler.ReportCurrentAsync($"Processing {path}");

                foreach (var subDirectory in arg.directory.GetDirectories())
                {
                    _searchBlock.Post((subDirectory, arg.progressHandler, arg.result));
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogEntry("SEARCH", LogLevel.Info, $"Skipping {path}");
                arg.result.SkippedPaths.Add(path);
            }
            catch (Exception exception)
            {
                Logger.LogException("SEARCH", LogLevel.Warning, exception);
                arg.result.SkippedPaths.Add(path);
            }
        }

        private static bool ShouldSkip(DirectoryInfo directory)
        {
            var path = directory.FullName;
            if (path.StartsWith(@"C:\Windows", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static DirectoryInfo[] GetInitialDirectories()
        {
            if (SearchSettings.Instance.EntireMachine)
            {
                var drives = DriveInfo.GetDrives();
                var result = new List<DirectoryInfo>();
                foreach (var drive in drives)
                {
                    try
                    {
                        result.Add(drive.RootDirectory);
                    }
                    catch (Exception e)
                    {
                        Logger.LogEntry("SEARCH", LogLevel.Warning, $"Failed to process drive {drive.Name}: {e.Message}");
                    }
                }

                return result.ToArray();
            }

            return new DirectoryInfo[0];
        }
    }
}
