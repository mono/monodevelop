using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Represents the set of differences between two projection snapshots.
    /// </summary>
    public class ProjectionSpanDifference
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProjectionSpanDifference"/>.
        /// </summary>
        /// <param name="differenceCollection">The collection of snapshot spans that include the differences.</param>
        /// <param name="insertedSpans">A read-only collection of the inserted snapshot spans.</param>
        /// <param name="deletedSpans">A read-only collection of the deleted snapshot spans.</param>
        public ProjectionSpanDifference(IDifferenceCollection<SnapshotSpan> differenceCollection, ReadOnlyCollection<SnapshotSpan> insertedSpans, ReadOnlyCollection<SnapshotSpan> deletedSpans)
        {
            DifferenceCollection = differenceCollection;
            InsertedSpans = insertedSpans;
            DeletedSpans = deletedSpans;
        }

        /// <summary>
        /// The collection of differences between the two snapshots.
        /// </summary>
        public IDifferenceCollection<SnapshotSpan> DifferenceCollection { get; private set; }

        /// <summary>
        /// The read-only collection of inserted snapshot spans.
        /// </summary>
        public ReadOnlyCollection<SnapshotSpan> InsertedSpans { get; private set; }

        /// <summary>
        /// The read-only collection of deleted snapshot spans.
        /// </summary>
        public ReadOnlyCollection<SnapshotSpan> DeletedSpans { get; private set; }
    }
}
