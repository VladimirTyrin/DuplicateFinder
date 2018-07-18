using System;

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

        public bool EntireMachine { get; set; } = true;

        public int ThreadCount { get; set; } = 1;//Environment.ProcessorCount;
    }
}
