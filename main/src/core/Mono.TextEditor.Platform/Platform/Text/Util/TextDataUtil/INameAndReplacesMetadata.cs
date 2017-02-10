using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Text.Utilities
{
    public interface INameAndReplacesMetadata
    {
        [DefaultValue(null)]
        string Name { get; }

        [DefaultValue(null)]
        IEnumerable<string> Replaces { get; }
    }
}
