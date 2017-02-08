using System;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// Provider for <see cref="ITableManager"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEF export. To get an instance of an <see cref="ITableManagerProvider"/>, use the following pattern:
    /// <code>
    /// [Import]
    /// internal ITableManagerProvider tableManagerProvider  { get; private set; }
    /// </code>
    /// </para>
    /// </remarks>
    public interface ITableManagerProvider
    {
        /// <summary>
        /// Get the <see cref="ITableManager"/> with the specified <paramref name="identifier"/>.
        /// </summary>
        /// <remarks>
        /// <para>Common identifiers can be found in <see cref="StandardTables"/>.</para>
        /// <para>This method can be called from any thread.</para>
        /// </remarks>
        ITableManager GetTableManager(Guid identifier);
    }
}
