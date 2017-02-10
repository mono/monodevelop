namespace Microsoft.VisualStudio.Text.Utilities
{
    public static class TextModelOptions
    {
        // these values should be set by MEF component in editor options component. defaults
        // are here just in case that doesn't happen in some configuration.
        public static int CompressedStorageFileSizeThreshold = 5 * 1024 * 1024; // 5 MB file (typically 10 MB in memory)
        public static int CompressedStoragePageSize = 1 * 1024 * 1024;          // 1 MB per page (so 10 pages at the low end)
        public static int CompressedStorageMaxLoadedPages = 3;                  // at most 3 pages loaded
        public static bool CompressedStorageGlobalManagement = false;           // per document
        public static bool CompressedStorageRetainWeakReferences = true;        // forces worst case decompression for testing purposes

        public static int StringRebuilderMaxCharactersToConsolidate = 200;      // Combine adjacent pieces when sum of sizes is less than this and
        public static int StringRebuilderMaxLinesToConsolidate = 8;             // Combine adjacent pieces when number of lines is less than this 

        public static int DiffSizeThreshold = 25 * 1024 * 1024;                 // threshold above which to do poor man's diff
    }
}
