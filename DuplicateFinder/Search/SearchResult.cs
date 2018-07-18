using System.Collections.Generic;

namespace DuplicateFinder.Search
{
    public class SearchResult
    {
        public List<FileDuplicateEntry> FileDuplicates { get; set; }
        public List<DirectoryDuplicateEntry> DirectoryDuplicates { get; set; }
    }
}
