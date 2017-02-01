using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// A manager for tabular data of a particular type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is intended to manage data from multiple data sources, each of which can provide tens of thousands discrete entries.
    /// </para>
    /// <para>
    /// All methods on this interface can be called from either the main thread or a background thread.
    /// </para>
    /// </remarks>
    public interface ITableManager
    {
        /// <summary>
        /// Identifier of the table manager.
        /// </summary>
        /// <remarks>
        /// <para>This property will not change over the lifetime of the <see cref="ITableManager"/>.</para>
        /// </remarks>
        Guid Identifier { get; }

        /// <summary>
        /// Add <paramref name="source"/> to the list of sources associated with the table manager.
        /// </summary>
        /// <param name="source">Table data source.</param>
        /// <param name="columns">Indicates the columns that could be displayed by a table containing data from <paramref name="source"/>.</param>
        /// <returns>true if <paramref name="source"/> was added to the table manager's Sources. Returns false if it was not (because it was already one of the table manager's sources).</returns>
        /// <remarks>
        /// <para>This method can be called from any thread.</para>
        /// <para><paramref name="columns"/> must be immutable and callable from any thread.</para>
        /// <para>Adding a source may cause <paramref name="source"/>'s Subscribe() to be called immediately (before AddSource() returns).</para>
        /// </remarks>
        bool AddSource(ITableDataSource source, IReadOnlyCollection<string> columns);

        /// <summary>
        /// Add <paramref name="source"/> to the list of sources associated with the table manager.
        /// </summary>
        /// <param name="source">Table data source.</param>
        /// <param name="columns">Indicates the columns that could be displayed by a table containing data from <paramref name="source"/>.</param>
        /// <returns>true if <paramref name="source"/> was added to the table manager's Sources. Returns false if it was not (because it was already one of the table manager's sources).</returns>
        /// <remarks>
        /// <para>This method can be called from any thread.</para>
        /// <para>Adding a source may cause <paramref name="source"/>'s Subscribe() to be called immediately (before AddSource() returns).</para>
        /// </remarks>
        bool AddSource(ITableDataSource source, params string[] columns);

        /// <summary>
        /// Remove <paramref name="source"/> from the list of sources associated with this table manager.
        /// </summary>
        /// <returns>true if <paramref name="source"/> was removed from the table manager. Returns false if it was not (because it was not one of the table manager's Sources).</returns>
        /// <remarks>
        /// <para>Removing a source may cause <paramref name="source"/>'s to unnsubscribe (e.g. the call Dispose() on the subscription obkect) immediately (before RemoveSource() returns).</para>
        /// </remarks>
        bool RemoveSource(ITableDataSource source);

        /// <summary>
        /// The list of sources currently associated with the table manager.
        /// </summary>
        /// <remarks>The returned list is immutable and can be used by any thread.</remarks>
        IReadOnlyList<ITableDataSource> Sources { get; }

        /// <summary>
        /// Get the union of all columns provided by any of the data sources in <paramref name="sources"/>.
        /// </summary>
        /// <remarks>The returned list is immutable and can be used by any thread.</remarks>
        IReadOnlyList<string> GetColumnsForSources(IEnumerable<ITableDataSource> sources);

        /// <summary>
        /// Raised whenever sources are added or removed from this table manager.
        /// </summary>
        /// <remarks>This event can be raised on any thread.</remarks>
        event EventHandler SourcesChanged;
    }
}