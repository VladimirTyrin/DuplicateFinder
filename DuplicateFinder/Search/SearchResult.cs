using System.Collections.Generic;

namespace DuplicateFinder.Search
{
    public class SearchResult
    {
        public List<FileDuplicateEntry> FileDuplicates { get; set; } = new List<FileDuplicateEntry>();
        public List<DirectoryDuplicateEntry> DirectoryDuplicates { get; set; } = new List<DirectoryDuplicateEntry>();
        public List<string> SkippedPaths { get; set; } = new List<string>();
        public bool Canceled { get; set; }

        public static SearchResult FromCanceled()
        {
            return new SearchResult
            {
                Canceled = true
            };
        }
    }
}
