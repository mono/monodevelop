using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides information about nested Peek invocation.
    /// </summary>
    public class NestedPeekTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// Case insensitive name of the relationship that was used to invoke nested Peek.
        /// </summary>
        public string RelationshipName { get; private set; }

        /// <summary>
        /// Gets the collection of <see cref="IPeekableItem"/> objects.
        /// </summary>
        public IEnumerable<IPeekableItem> PeekableItems { get; private set; }

        /// <summary>
        /// Gets the <see cref="ITrackingPoint"/> at which nested Peek was invoked.
        /// </summary>
        public ITrackingPoint TrackingPoint { get; private set; }

        /// <summary>
        /// Creates new instance of the <see cref="NestedPeekTriggeredEventArgs"/>.
        /// </summary>
        /// <param name="relationshipName">Case insensitive name of the relationship that was used to invoke nested Peek.</param>
        /// <param name="peekableItems">The list of the <see cref="IPeekableItem"/> objects that can provide results of the 
        /// nested Peek invocation.</param>
        public NestedPeekTriggeredEventArgs(string relationshipName, ITrackingPoint trackingPoint, IEnumerable<IPeekableItem> peekableItems)
        {
            if (string.IsNullOrWhiteSpace(relationshipName))
            {
                throw new ArgumentException("relationshipName");
            }
            if (peekableItems == null)
            {
                throw new ArgumentNullException("peekableItems");
            }
            if (trackingPoint == null)
            {
                throw new ArgumentNullException("trackingPoint");
            }

            RelationshipName = relationshipName;
            PeekableItems = peekableItems;
            TrackingPoint = trackingPoint;
        }
    }
}
