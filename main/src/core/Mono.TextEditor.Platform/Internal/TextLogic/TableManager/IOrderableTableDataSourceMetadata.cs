using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// TODO Move to editor\impl.
    /// </summary>
    public interface IOrderableTableDataSourceMetadata : ITableDataSourceMetadata, ITableDataSourceTypeMetadata, ITableManagerSourceMetadata, IOrderable
    {
    }
}
