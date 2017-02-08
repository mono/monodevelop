using System;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// An entry that corresponds to a row of data in a table control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All methods on this interface can be called from either the main thread or a background thread.
    /// </para>
    /// </remarks> 
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public interface ITableEntry
    {
        /// <summary>
        /// Get the data associated with the specified column (if this entry has data associated with that column).
        /// </summary>
        /// <returns>true if the entry has data associated with the column.</returns>
        bool TryGetValue(string keyName, out object content);

        /// <summary>
        /// Set the data associated with the specified column (if this entry has data associated with that column).
        /// </summary>
        /// <returns>true if the value was changed.</returns>
        bool TrySetValue(string keyName, object content);

        /// <summary>
        /// Can the data associated with the specified column be set?
        /// </summary>
        /// <remarks>This method returning true is not a guarantee that <see cref="TrySetValue(string, object)"/> will work for <paramref name="keyName"/>.</remarks>
        bool CanSetValue(string keyName);

        /// <summary>
        /// Returns an object that uniquely identifies the entry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property (and the related properties in <see cref="ITableEntriesSnapshot"/> are used to persist various attributes like selection state
        /// when an <see cref="ITableEntry"/> is replaced with a new <see cref="ITableEntry"/>. Entries that replace an existing entry will have their
        /// attributes set based on the attributes of the replaced entry.
        /// </para>
        /// <para>
        /// When <see cref="ITableDataSink.ReplaceEntries(System.Collections.Generic.IReadOnlyList{ITableEntry}, System.Collections.Generic.IReadOnlyList{ITableEntry})"/> is called, every entry in the
        /// list of old entries is checked to see if it has state and there is a corresponding entry among the added entries. If there is,
        /// then the two entries are considered equivalent and the old entry's attributes are copied to the new entry.
        /// </para>
        /// <para>
        /// When <see cref="ITableDataSink.ReplaceSnapshot(ITableEntriesSnapshot, ITableEntriesSnapshot)"/> is called, every entry in the old snapshot is checked to see if it has state and a corresponding entry
        /// is found by first calling <see cref="ITableEntriesSnapshot.TranslateTo(int, ITableEntriesSnapshot)"/> to find the corresponding entry. If that fails to find a corresponding index in the new snapshot, then <see cref="ITableEntriesSnapshot.Identity(int)"/>
        /// is used to find a corresponding entry. If there is a corresponding entry, the old entry's attributes are copied to the new entry.
        /// </para>
        /// <para>
        /// When a <see cref="ITableEntriesSnapshotFactory"/> replaces its snapshot with a new version, the entry state is transfered over exactly as if <see cref="ITableDataSink.ReplaceSnapshot(ITableEntriesSnapshot, ITableEntriesSnapshot)"/> had
        /// been called on the factory's old and new snapshots.
        /// </para>
        /// </remarks>
        object Identity { get; }
    }

    /// <summary>
    /// Helper class for those that want to implement only part of the <see cref="ITableEntry"/> interface.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public abstract class TableEntryBase : ITableEntry
    {
        public virtual bool TryGetValue(string keyName, out object content)
        {
            content = null;
            return false;
        }

        public virtual bool TrySetValue(string keyName, object content) { return false; }

        public virtual bool CanSetValue(string keyName) { return false; }

        public virtual object Identity { get { return null; } }
    }

    /// <summary>
    /// Overload class for getting typed data from an <see cref="ITableEntry"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public static class TableEntryHelpers
    {
        /// <summary>
        /// Try to get data of type <typeparamref name="T"/> from an entry.
        /// </summary>
        /// <typeparam name="T">Expected data type.</typeparam>
        /// <returns>true if the <paramref name="entry"/>.TryGetValue(...) returned true and the corresponding data was of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <paramref name="content"/> will be set to default(T) if <paramref name="entry"/>.TryGetValue(...) returns false or the returned data was not a <typeparamref name="T"/>.
        /// </remarks>
        public static bool TryGetValue<T>(this ITableEntry entry, string keyName, out T content)
        {
            return entry.TryGetValue(keyName, out content, () => default(T));
        }

        /// <summary>
        /// Try to get data of type <typeparamref name="T"/> from an entry.
        /// </summary>
        /// <typeparam name="T">Expected data type.</typeparam>
        /// <returns>true if the <paramref name="entry"/>.TryGetValue(...) returned true and the corresponding data was of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <paramref name="content"/> will be set to <paramref name="defaultValue"/> if <paramref name="entry"/>.TryGetValue(...) returns false or the returned data was not a <typeparamref name="T"/>.
        /// </remarks>
        public static bool TryGetValue<T>(this ITableEntry entry, string keyName, out T content, T defaultValue)
        {
            return entry.TryGetValue(keyName, out content, () => defaultValue);
        }

        /// <summary>
        /// Try to get data of type <typeparamref name="T"/> from an entry.
        /// </summary>
        /// <typeparam name="T">Expected data type.</typeparam>
        /// <returns>true if the <paramref name="entry"/>.TryGetValue(...) returned true and the corresponding data was of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <paramref name="content"/> will be set to <paramref name="defaultValue"/>() if <paramref name="entry"/>.TryGetValue(...) returns false or the returned data was not a <typeparamref name="T"/>.
        /// </remarks>
        public static bool TryGetValue<T>(this ITableEntry entry, string keyName, out T content, Func<T> defaultValue)
        {
            object c;
            if (entry.TryGetValue(keyName, out c) && (c is T))
            {
                content = (T)c;

                return (content != null) || (c == null);
            }

            content = defaultValue();
            return false;
        }
    }
}
