using System.Collections.Generic;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// TODO Move to editor\impl.
    /// </summary>
    public interface ITableDataSourceTypeMetadata
    {
        IEnumerable<string> DataSourceTypes { get; }
    }
}
