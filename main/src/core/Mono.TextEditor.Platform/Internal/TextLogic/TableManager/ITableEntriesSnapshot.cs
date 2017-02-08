using System;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// An abstraction for a fixed set of <see cref="ITableEntry"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="ITableEntriesSnapshot"/> and its virtual entries must be immutable and callable from any thread. The one exception is that the snapshot's Dispose() method will be called
    /// when the snapshot is no longer being used (at which point there should not be any calls to get data from the snapshot or its entries).
    /// </para>
    /// </remarks>
    public interface ITableEntriesSnapshot : IDisposable
    {
        /// <summary>
        /// Number of entries in this snapshot.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Get the version number associated with the snapshot.
        /// </summary>
        /// <remarks>
        /// <para>This property is only used by <see cref="ITableEntriesSnapshot"/> created by an <see cref="ITableEntriesSnapshotFactory"/> and, in that case, the VersionNumbers must be different if the contents are different.</para>
        /// <para>The VersionNumber should always be >= 0 with the exception that an empty snapshot's VersionNumber can (but does not have to) be -1.</para>
        /// </remarks>
        int VersionNumber { get; }

        /// <summary>
        /// Hint to the snapshot that there will be a lot of access to the snapshot's data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The model for using and releasing entries from snapshots works as follows. For snapshots that were directly added to an <see cref="ITableDataSink"/>:
        /// </para>
        /// <para>
        /// <see cref="StartCaching"/> is called from a background thread at the start of an update pass and before any calls to <see cref="TryGetValue(int, string, out object)"/>.
        /// </para>
        /// <para>
        /// <see cref="StopCaching"/> will be called from a background thread at the end of an update pass
        /// </para>
        /// <para>
        /// <see cref="IDisposable.Dispose"/> will be called at the end of an update pass if either the snapshot had previously been removed or if the snapshot
        /// was a snapshot managed by an <see cref="ITableEntriesSnapshotFactory"/> and none of its entries are visible in table.
        /// </para>
        /// </remarks>
        void StartCaching();

        /// <summary>
        /// Hint to the snapshot that the snapshot's entries will no longer be accessed.
        /// </summary>
        void StopCaching();

        /// <summary>
        /// Get the data for the <paramref name="columnName"/> of the entry at <paramref name="index"/>.
        /// </summary>
        /// <returns>true if successful.</returns>
        bool TryGetValue(int index, string columnName, out object content);

        /// <summary>
        /// Returns an object that uniquely identifies the entry at <paramref name="index"/>.
        /// </summary>
        object Identity(int index);

        // TODO remove
        /// <summary>
        /// Returns an object that uniquely identifies the snapshot.
        /// </summary>
        object SnapshotIdentity { get; }

        /// <summary>
        /// Returns the index of the entry associated with <paramref name="currentIndex"/> in a different
        /// <see cref="ITableEntriesSnapshot"/>. <paramref name="newerSnapshot"/> will always correspond to
        /// a snapshot that was created after this snapshot.
        /// </summary>
        /// <returns>The index of the corresponding entry in <paramref name="newerSnapshot"/> or -1 if there is no corresponding entry.</returns>
        int TranslateTo(int currentIndex, ITableEntriesSnapshot newerSnapshot);
    }

    // TODO remove abstract qualifier
    /// <summary>
    /// Helper class for those that want to implement only part of the <see cref="ITableEntriesSnapshot"/> interface.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public abstract class TableEntriesSnapshotBase : ITableEntriesSnapshot
    {
        /// <summary>
        /// Number of entries in this snapshot.
        /// </summary>
        public virtual int Count { get { return 0; } }

        /// <summary>
        /// Get the version number associated with the snapshot.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Intentially return an invalid VersionNumber so that this snapshot will be different than any valid snapshot.
        /// </para>
        /// </remarks>
        public virtual int VersionNumber { get { return -1; } }

        /// <summary>
        /// Hint to the snapshot that there will be a lot of access to the snapshot's data.
        /// </summary>
        public virtual void StartCaching() { }

        /// <summary>
        /// Hint to the snapshot that the snapshot's entries will no longer be accessed.
        /// </summary>
        public virtual void StopCaching() { }

        /// <summary>
        /// Get the data for the <paramref name="columnName"/> of the entry at <paramref name="index"/>.
        /// </summary>
        /// <returns>true if successful.</returns>
        public virtual bool TryGetValue(int index, string columnName, out object content)
        {
            content = null;
            return false;
        }

        /// <summary>
        /// Returns an object that uniquely identifies the entry at <paramref name="index"/>.
        /// </summary>
        public virtual object Identity(int index)
        {
            return null;
        }

        // TODO remove
        /// <summary>
        /// Returns an object that uniquely identifies the snapshot.
        /// </summary>
        public virtual object SnapshotIdentity { get { return null; } }

        /// <summary>
        /// Returns the index of the entry associated with <paramref name="currentIndex"/> in a different
        /// <see cref="ITableEntriesSnapshot"/>. <paramref name="newerSnapshot"/> will always correspond to
        /// a snapshot that was created after this snapshot.
        /// </summary>
        /// <returns>The index of the corresponding entry in <paramref name="newerSnapshot"/> or -1 if there is no corresponding entry.</returns>
        public virtual int TranslateTo(int currentIndex, ITableEntriesSnapshot newerSnapshot)
        {
            return -1;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
