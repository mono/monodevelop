using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// TODO Move to editor\impl.
    /// </summary>
    public interface ITableManagerSourceMetadata
    {
        [DefaultValue(null)]
        IEnumerable<string> ManagerIdentifiers { get; }
    }
}
