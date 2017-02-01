using System.Collections.Generic;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// Class used to consume data provided by an <see cref="ITableDataSource"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ITableDataSource"/> can have multiple subscribers and each subscriber will have its own <see cref="ITableDataSink"/>.
    /// </para>
    /// </remarks>
    public interface ITableDataSink
    {
        /// <summary>
        /// Indicates whether the results reported to the sink are stable.
        /// </summary>
        /// <remarks>
        /// <para>This property should be set to false whenever the source supplying the sink is likely to be posting changes frequently. It should be set to true when no changes are expected. For example, setting this to false
        /// at the start of a build and to true when the build has completed.</para>
        /// <para>This flag has no effect of the behavior of the sink itself but the table control displaying data associated with the sink may display some type of "working" UI to indicate that
        /// the results are likely to change.</para>
        /// </remarks>
        bool IsStable { get; set; }

        /// <summary>
        /// Add the specified entries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <paramref name="newEntries"/> must be immutable/callable from any thread.
        /// </para>
        /// <para>
        /// In general, any call to an <see cref="ITableDataSink"/> will not take effect immediately. Consumers may
        /// batch changes up and process them, after a delay, on a background thread.
        /// </para>
        /// </remarks>
        void AddEntries(IReadOnlyList<ITableEntry> newEntries, bool removeAllEntriesAndSnapshots = false);

        /// <summary>
        /// Remove the specified entries.
        /// </summary>
        /// <remarks><paramref name="oldEntries"/> must be immutable/callable from any thread.</remarks>
        void RemoveEntries(IReadOnlyList<ITableEntry> oldEntries);

        /// <summary>
        /// Remove <paramref name="oldEntries"/> and add <paramref name="newEntries"/>.
        /// </summary>
        /// <remarks><paramref name="oldEntries"/> and <paramref name="newEntries"/> must be immutable/callable from any thread.</remarks>
        void ReplaceEntries(IReadOnlyList<ITableEntry> oldEntries, IReadOnlyList<ITableEntry> newEntries);

        /// <summary>
        /// Add the specified snapshot.
        /// </summary>
        /// <remarks><paramref name="newSnapshot"/> must be immutable/callable from any thread.</remarks>
        void AddSnapshot(ITableEntriesSnapshot newSnapshot, bool removeAllEntriesAndSnapshots = false);

        /// <summary>
        /// Remove the specified snapshot.
        /// </summary>
        /// <remarks><paramref name="oldSnapshot"/> must be immutable/callable from any thread.</remarks>
        void RemoveSnapshot(ITableEntriesSnapshot oldSnapshot);

        /// <summary>
        /// Remove <paramref name="oldSnapshot"/> and add <paramref name="newSnapshot"/>.
        /// </summary>
        /// <remarks><paramref name="oldSnapshot"/> and <paramref name="newSnapshot"/> must be immutable/callable from any thread.</remarks>
        void ReplaceSnapshot(ITableEntriesSnapshot oldSnapshot, ITableEntriesSnapshot newSnapshot);

        /// <summary>
        /// Add the specified factory.
        /// </summary>
        /// <remarks><paramref name="newFactory"/> must be callable from any thread.</remarks>
        void AddFactory(ITableEntriesSnapshotFactory newFactory, bool removeAllEntriesAndSnapshots = false);

        /// <summary>
        /// Remove the specified factory.
        /// </summary>
        /// <remarks><paramref name="oldFactory"/> must be callable from any thread.</remarks>
        void RemoveFactory(ITableEntriesSnapshotFactory oldFactory);

        /// <summary>
        /// Remove <paramref name="oldFactory"/> and add <paramref name="newFactory"/>.
        /// </summary>
        /// <remarks><paramref name="oldFactory"/> and <paramref name="newFactory"/> must be callable from any thread.</remarks>
        void ReplaceFactory(ITableEntriesSnapshotFactory oldFactory, ITableEntriesSnapshotFactory newFactory);

        /// <summary>
        /// Indicate that the <see cref="ITableEntriesSnapshotFactory.GetCurrentSnapshot"/> for <paramref name="factory"/> has changed.
        /// </summary>
        void FactoryUpdated(ITableEntriesSnapshotFactory factory);

        /// <summary>
        /// Make a change that doesn't conform to one of the options above (e.g. replace a list of entries with a snapshot).
        /// </summary>
        void PostChange(IReadOnlyList<ITableEntry> oldEntries = null,
                        IReadOnlyList<ITableEntry> newEntries = null,
                        ITableEntriesSnapshot oldSnapshot = null,
                        ITableEntriesSnapshot newSnapshot = null,
                        ITableEntriesSnapshotFactory oldFactory = null,
                        ITableEntriesSnapshotFactory newFactory = null,
                        bool removeAllEntriesAndSnapshots = false);
    }
}