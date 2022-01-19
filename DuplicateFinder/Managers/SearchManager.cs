using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private static readonly ConcurrentDictionary<(long Size, string extension), List<string>> Files
            = new ConcurrentDictionary<(long Size, string extension), List<string>>();
        private static readonly ConcurrentDictionary<(long Size, string extension), object> Locks
            = new ConcurrentDictionary<(long Size, string extension), object>();

        private static string[] _extensionsToUse;

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

                await FillResultFileDuplicatesAsync(progressHandler, result);

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
            _filesProcessed = 0;
            Files.Clear();
            Locks.Clear();
            _extensionsToUse = SearchSettings.Instance.ExtensionsToUse?.Split(new []{ ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => "." + e.Trim().ToUpperInvariant()).ToArray();
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
                foreach (var file in arg.directory.EnumerateFiles())
                {
                    AddFile(file);
                    Interlocked.Increment(ref _filesProcessed);
                }

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

        private static void AddFile(FileInfo fileInfo)
        {
            var extension = GetExtension(fileInfo);
            if (ShouldSkipByExtensions(extension))
                return;
            var key = (fileInfo.Length, extension);
            var path = fileInfo.FullName;

            var currentLock = Locks.GetOrAdd(key, _ => new object());
            
            Files.AddOrUpdate(key, tuple => new List<string> {path}, (tuple, list) =>
            {
                lock (currentLock)
                {
                    list.Add(path);
                    return list;
                }
            });
        }

        private static bool ShouldSkipByExtensions(string extension)
        {
            return _extensionsToUse != null
                   && _extensionsToUse.Length != 0
                   && !_extensionsToUse.Contains(extension);
        }

        private static async Task FillResultFileDuplicatesAsync(IProgressHandler progressHandler, SearchResult result)
        {
            var duplicatePairs = Files.Where(kv => kv.Value.Count > 1).ToArray();
            var count = duplicatePairs.Length;
            for (var i = 0; i < duplicatePairs.Length; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var pair = duplicatePairs[i];
                await progressHandler.ReportStateAsync($"Processing duplicate entry {i + 1} out of {count}");

                if (!SearchSettings.Instance.ExactMatch)
                {
                    result.FileDuplicates.Add(new FileDuplicateEntry
                    {
                        Size = pair.Key.Size,
                        Paths = pair.Value
                    });
                }
                else
                {
                    var dict = pair.Value.ToDictionary(p => p, GetMd5);
                    foreach (var kv in dict.GroupBy(kv => kv.Value))
                    {
                        result.FileDuplicates.Add(new FileDuplicateEntry
                        {
                            Size = pair.Key.Size,
                            Paths = kv.Select(p => p.Key).ToList()
                        });
                    }
                }
            }
        }

        private static string GetMd5(string path)
        {
            try
            {
                using (var provider = new MD5CryptoServiceProvider())
                {
                    using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return string.Concat(provider.ComputeHash(file).Select(b => b.ToString("X2")));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogEntry("SEARCH", LogLevel.Warning, $"Failed to check file {path}: {e.Message}");
                return null;
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
            if (!string.IsNullOrWhiteSpace(SearchSettings.Instance.Drive))
            {
                var drives = DriveInfo.GetDrives();
                var result = new List<DirectoryInfo>();
                var targetDrive = drives.First(d => d.Name == SearchSettings.Instance.Drive + ":\\");
                try
                {
                    result.Add(targetDrive.RootDirectory);
                    Logger.LogEntry("SEARCH", LogLevel.Info, $"Adding drive {targetDrive.Name}");
                }
                catch (Exception e)
                {
                    Logger.LogEntry("SEARCH", LogLevel.Warning, $"Failed to process drive {targetDrive.Name}: {e.Message}");
                }
                return result.ToArray();
            }

            return new DirectoryInfo[0];
        }

        private static string GetExtension(FileInfo fileInfo)
        {
            return SearchSettings.Instance.IgnoreExtensions
                ? "U"
                : fileInfo.Extension.ToUpperInvariant();
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
