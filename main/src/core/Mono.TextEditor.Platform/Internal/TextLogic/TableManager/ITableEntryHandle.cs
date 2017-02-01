namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// A wrapper for an <see cref="ITableEntry"/> or a "virtual" entry created from an <see cref="ITableEntriesSnapshot"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TODO consider moving to TextUiWpf/TableControl.
    /// </para>
    /// </remarks>
    public interface ITableEntryHandle : ITableEntry
    {
        /// <summary>
        /// Gets <see cref="ITableEntry"/> associated with this <see cref="ITableEntryHandle"/>.
        /// </summary>
        /// <returns>true if this was created from an <see cref="ITableEntry"/>.</returns>
        bool TryGetEntry(out ITableEntry tableEntry);

        /// <summary>
        /// Gets <see cref="ITableEntriesSnapshot"/> and index associated with this <see cref="ITableEntryHandle"/>.
        /// </summary>
        /// <returns>true if this was created from an <see cref="ITableEntriesSnapshot"/> and that snapshot has been pinned (<see cref="ITableEntryHandle.PinSnapshot"/>).</returns>
        /// <remarks>
        /// <para>Snapshots created by an <see cref="ITableEntriesSnapshotFactory"/> are</para>
        /// </remarks>
        bool TryGetSnapshot(out ITableEntriesSnapshot snapshot, out int index);

        /// <summary>
        /// Gets the <see cref="ITableEntriesSnapshotFactory"/>, version number and index associated with this <see cref="ITableEntryHandle"/>.
        /// The entry's snapshot will be returned if it is being held by the table control.
        /// </summary>
        /// <returns>true if this was created from an <see cref="ITableEntriesSnapshot"/> that, in turn, was created by an <see cref="ITableEntriesSnapshotFactory"/>.</returns>
        bool TryGetFactory(out ITableEntriesSnapshotFactory factory, out int versionNumber, out int index);

        /// <summary>
        /// Pin the snapshot for this <see cref="ITableEntryHandle"/>.
        /// </summary>
        /// <returns>The <see cref="ITableEntriesSnapshot"/> used to create this entry or null if it or its equivalent no longer exists.</returns>
        /// <remarks>
        /// <para>All calls to <see cref="ITableEntryHandle.PinSnapshot"/> should be matched by calls to <see cref="ITableEntryHandle.UnpinSnapshot"/>.</para>
        /// <para>This will return null (and have no effect) on handles created from <see cref="ITableEntry"/>s.</para>
        /// <para>If the <see cref="ITableEntriesSnapshot"/> used to create this handle was directly added to the <see cref="ITableDataSink"/>, then
        /// this method will return that snapshot (but have no effect otherwise).</para>
        /// <para>If the <see cref="ITableEntriesSnapshot"/> used to create this handle is managed by an <see cref="ITableEntriesSnapshotFactory"/>, then this method will return its cached snapshot if
        /// still exists or it will ask the factory to recreate it if it does not. The factory may not be able to recreate the snapshot and, in that case, this method will return null.</para>
        /// </remarks>
        ITableEntriesSnapshot PinSnapshot();

        /// <summary>
        /// Unpin the snapshot for this <see cref="ITableEntryHandle"/>.
        /// </summary>
        /// <remarks>
        /// <para>All calls to <see cref="ITableEntryHandle.UnpinSnapshot"/> should be matched with an earlier call to <see cref="ITableEntryHandle.PinSnapshot"/>.</para>
        /// <para>This will have no effect on handles created from <see cref="ITableEntry"/>s or ones created from an <see cref="ITableEntriesSnapshot"/> that was directly added to an <see cref="ITableDataSink"/>.</para>
        /// <para>If the <see cref="ITableEntriesSnapshot"/> used to create this handle is managed by an <see cref="ITableEntriesSnapshotFactory"/>, then this method will decrement is "pinned" count and, if that count goes
        /// to zero, release its cached snapshot.</para>
        /// </remarks>
        void UnpinSnapshot();

        /// <summary>
        /// Ensure that the entry is visible in the table control, scrolling the contents of the table control if needed.
        /// </summary>
        void EnsureVisible();

        /// <summary>
        /// Gets or sets whether the entry this <see cref="ITableEntryHandle"/> is associated with is selected.
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets whether details of the entry this <see cref="ITableEntryHandle"/> is associated with are being shown.
        /// </summary>
        bool AreDetailsShown { get; set; }

        /// <summary>
        /// Gets whether the entry this <see cref="ITableEntryHandle"/> is associated with is selected can show details.
        /// </summary>
        bool CanShowDetails { get; }

        /// <summary>
        /// Gets or sets whether the entry this <see cref="ITableEntryHandle"/> is associated with is expanded.
        /// </summary>
        /// <remarks>
        /// TODO remove -- obsoleted by AreDetailsShown.
        /// </remarks>
        bool IsExpanded { get; set; }

        /// <summary>
        /// Gets whether the entry this <see cref="ITableEntryHandle"/> is associated with can be expanded.
        /// </summary>
        /// <remarks>
        /// TODO remove -- obsoleted by CanShowDetails.
        /// </remarks>
        bool CanBeExpanded { get; }

        /// <summary>
        /// Gets whether the entry this <see cref="ITableEntryHandle"/> is associated with has vertical content.
        /// </summary>
        bool HasVerticalContent { get; }

        /// <summary>
        /// Navigates to the data the entry this <see cref="ITableEntryHandle"/> is associated with represents.
        /// </summary>
        /// <remarks>
        /// <para>Calling this is equivalent to the user initiating an navigation action from the table control itself.</para>
        /// </remarks>
        bool NavigateTo(bool isPreview);

        /// <summary>
        /// Navigates to the help link the entry this <see cref="ITableEntryHandle"/> is associated with represents.
        /// </summary>
        /// <remarks>
        /// <para>Calling this is equivalent to the user initiating an navigate to help action from the table control itself.</para>
        /// </remarks>
        bool NavigateToHelp();

        /// <summary>
        /// Is the entry visible in the table control.
        /// </summary>
        bool IsVisible { get; }
    }
}