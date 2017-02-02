using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Text.Utilities
{
    public interface IContentTypeMetadata
    {
        IEnumerable<string> ContentTypes { get; }
    }
}
