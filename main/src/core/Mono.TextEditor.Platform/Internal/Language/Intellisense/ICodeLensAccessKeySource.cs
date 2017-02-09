using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a source for access keys related to a particular data point provider.  This
    /// source updates dynamically as access keys assigned to providers change.
    /// </summary>
    public interface ICodeLensAccessKeySource : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the current access key for this source.
        /// </summary>
        string AccessKey { get; }
    }
}
