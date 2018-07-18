using System.Collections.Generic;

namespace DuplicateFinder.Search
{
    public class FileDuplicateEntry
    {
        public long Size { get; set; }
        public List<string> Paths { get; set; }
    }
}
