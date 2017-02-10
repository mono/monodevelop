using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.VisualStudio.Text.Utilities
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class PerformanceBlockMarker
    {
        [ImportMany]
        internal List<Lazy<IPerformanceMarkerBlockProvider>> PerformanceMarkerBlockProviders { get; set; }

        internal IDisposable CreateBlock(string blockName)
        {
            IEnumerable<IDisposable> providedBlocks;

            if (PerformanceMarkerBlockProviders != null)
                providedBlocks = PerformanceMarkerBlockProviders
                    .Select(lazyProvider => lazyProvider.Value.CreateBlock(blockName));
            else
                providedBlocks = Enumerable.Empty<IDisposable>();

            return new Block(providedBlocks);
        }

        private class Block : IDisposable
        {
            private readonly IEnumerable<IDisposable> markers;

            internal Block(IEnumerable<IDisposable> markers)
            {
                this.markers = markers.ToList();
            }

            public void Dispose()
            {
                foreach (var marker in this.markers)
                    if (marker != null)
                        marker.Dispose();
            }
        }
    }
}
