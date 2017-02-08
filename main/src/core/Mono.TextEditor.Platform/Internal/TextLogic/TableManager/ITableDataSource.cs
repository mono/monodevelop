using System;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// A source for data given to an <see cref="ITableManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All methods on this interface can be called from either the main thread or a background thread.
    /// </para>
    /// </remarks>
    public interface ITableDataSource
    {
        /// <summary>
        /// Identifier that describes the type of entries provided by this source (e.g. <see cref="StandardTableDataSources.CommentTableDataSource"/>)
        /// </summary>
        /// <remarks>
        /// <para>Different sources can have the same identifier (e.g. there could be multiple sources of <see cref="StandardTableDataSources.ErrorTableDataSource"/>).</para>
        /// <para>This identifier cannot change over the lifetimen of the <see cref="ITableDataSource"/>.</para>
        /// </remarks>
        Guid SourceTypeIdentifier { get; }

        /// <summary>
        /// Unique identifier of this data source.
        /// </summary>
        /// <remarks>
        /// <para>This identifier cannot change over the lifetimen of the <see cref="ITableDataSource"/>.</para>
        /// </remarks>
        Guid Identifier { get; }

        /// <summary>
        /// Localized name to identify the source in any UI displayed to the user. Can be null.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Subscribe to <see cref="ITableEntry"/>s created by this data source.
        /// </summary>
        /// <param name="sink">Contains methods called when the <see cref="ITableEntry"/>s provided by the source change.</param>
        /// <returns>A key that controls the lifetime of the subscription. The <see cref="ITableDataSource"/> must continue to provide updates until either the key is disposed
        /// or the source is removed from the table.</returns>
        /// <remarks>
        /// <para>A side-effect of subscribing is that the source call <paramref name="sink"/> to add its current contents (though this can be delayed).</para>
        /// <para>A <see cref="ITableDataSource"/> can have multiple, simultaneous subscribers.</para>
        /// </remarks>
        IDisposable Subscribe(ITableDataSink sink);
    }
}
