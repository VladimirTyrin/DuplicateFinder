using System.Collections.Generic;

namespace DuplicateFinder.Settings
{
    public class SearchSettings
    {
        private static readonly object Lock = new object();
        private static volatile SearchSettings _instance;

        private SearchSettings()
        {

        }

        public static SearchSettings Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (Lock)
                {
                    if (_instance != null)
                        return _instance;

                    return _instance = new SearchSettings();
                }
            }
        }

        public bool EntireMachine { get; set; } = false;

        public bool ExactMatch { get; set; } = false;

        public int ThreadCount { get; set; } = 1;

        public int UpdateInterval { get; set; } = 10;

        public bool IgnoreExtensions { get; set; } = false;

        public string ExtensionsToUse { get; set; } = string.Join(",", new List<string>
        {
            "mkv"
        });

        public string Drive { get; set; } = "C";
    }
}
