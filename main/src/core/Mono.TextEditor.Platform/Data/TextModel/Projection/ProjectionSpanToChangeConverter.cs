namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;

    internal class ProjectionSpanToNormalizedChangeConverter
    {
        private INormalizedTextChangeCollection normalizedChanges;
        private bool computed = false;
        private int textPosition;
        private ProjectionSpanDiffer differ;
        private ITextSnapshot currentSnapshot;

        public ProjectionSpanToNormalizedChangeConverter(ProjectionSpanDiffer differ, 
                                                         int textPosition, 
                                                         ITextSnapshot currentSnapshot)
        {
            this.differ = differ;
            this.textPosition = textPosition;
            this.currentSnapshot = currentSnapshot;
        }

        public INormalizedTextChangeCollection NormalizedChanges
        {
            get 
            {
                if (!computed)
                {
                    ConstructChanges();
                    computed = true;
                }
                return this.normalizedChanges; 
            }
        }

        #region Private helpers

        private void ConstructChanges()
        {
            IDifferenceCollection<SnapshotSpan> diffs = differ.GetDifferences();

            List<TextChange> changes = new List<TextChange>();
            int pos = this.textPosition;

            // each difference generates a text change
            foreach (Difference diff in diffs)
            {
                pos += GetMatchSize(differ.DeletedSpans, diff.Before);
                TextChange change = new TextChange(pos,
                                                   ReferenceChangeString.CreateChangeString(differ.DeletedSpans, diff.Left),
                                                   ReferenceChangeString.CreateChangeString(differ.InsertedSpans, diff.Right),
                                                   this.currentSnapshot);
                changes.Add(change);
                pos += change.OldLength;
            }
            this.normalizedChanges = NormalizedTextChangeCollection.Create(changes);
        }

        private static int GetMatchSize(ReadOnlyCollection<SnapshotSpan> spans, Match match)
        {
            int size = 0;
            if (match != null)
            {
                Span extent = match.Left;
                for (int s = extent.Start; s < extent.End; ++s)
                {
                    size += spans[s].Length;
                }
            }
            return size;
        }

        #endregion
    }
}
