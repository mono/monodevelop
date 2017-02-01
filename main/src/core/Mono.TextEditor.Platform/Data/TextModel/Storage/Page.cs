using System.Diagnostics;
namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Base class for a page that participates in an MRU list.
    /// </summary>
    internal abstract class Page
    {
        internal Page More { get; set; }
        internal Page Less { get; set; }

        public abstract bool UnloadWhileLocked();
        public abstract string Id { get; }
    }
}
