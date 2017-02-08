namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Describe a version number and a position in that version. Used in the implementation
    /// of high fidelity tracking points and spans.
    /// </summary>
    internal struct VersionNumberPosition
    {
        public int VersionNumber;
        public int Position;
        public VersionNumberPosition(int versionNumber, int position)
        {
            this.VersionNumber = versionNumber;
            this.Position = position;
        }
    }

    internal class VersionNumberPositionComparer : System.Collections.Generic.IComparer<VersionNumberPosition>
    {
        public int Compare(VersionNumberPosition x, VersionNumberPosition y)
        {
            return x.VersionNumber - y.VersionNumber; // both values are nonnegative, no overflow possible
        }

        static public VersionNumberPositionComparer Instance = new VersionNumberPositionComparer();
    }
}
