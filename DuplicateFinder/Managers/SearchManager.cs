using System;
using System.Collections.Generic;
using System.IO;
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
        private static int _directoriesProcessed;
        private static int _filesProcessed;
        private static int _queued;

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
                    Interlocked.Increment(ref _queued);
                    _searchBlock.Post((directory, progressHandler, result));
                }

                await _searchBlock.Completion;
                Logger.LogEntry("SEARCH", LogLevel.Info, "Search completed");
                await progressHandler.ReportStateAsync("Scan completed");

                return result;
            }
            catch (OperationCanceledException)
            {
                Logger.LogEntry("SEARCH", LogLevel.Info, "Search canceled");
                return SearchResult.FromCanceled();
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
            _directoriesProcessed = 0;
        }

        private static async Task ProcessDirectoryAsync((DirectoryInfo directory, IProgressHandler progressHandler, SearchResult result) arg)
        {
            Interlocked.Decrement(ref _queued);
            var path = arg.directory.FullName;
            if (ShouldSkip(arg.directory))
            {
                Logger.LogEntry("SEARCH", LogLevel.Debug, $"Skipping {path}");
                arg.result.SkippedPaths.Add(path);
                return;
            }

            try
            {
                foreach (var subDirectory in arg.directory.GetDirectories())
                {
                    Interlocked.Increment(ref _queued);
                    _searchBlock.Post((subDirectory, arg.progressHandler, arg.result));
                }

                await arg.progressHandler.ReportCurrentAsync($"Processing {path}", _queued, _directoriesProcessed, _filesProcessed);
                if (_queued <= 0)
                    _searchBlock.Complete();

                Interlocked.Increment(ref _directoriesProcessed);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogEntry("SEARCH", LogLevel.Debug, $"Skipping {path}");
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
            foreach (var skippedPath in SkippedPrefixes)
            {
                if (path.StartsWith(skippedPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            foreach (var part in SkippedParts)
            {
                if (path.Contains(part))
                    return true;
            }

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
                        Logger.LogEntry("SEARCH", LogLevel.Info, $"Adding drive {drive.Name}");
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

        private static readonly string[] SkippedPrefixes = 
        {
            @"C:\Windows",
            @"C:\Program Files",
            @"C:\Program Files (x86)",
        };

        private static readonly string[] SkippedParts = 
        {
            "node_modules"
        };
    }
}
