using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Interface used by a MEF component to register a listener for CodeLensAdornment creation and destruction, e.g.
    /// 
    ///   [Export(typeof(ICodeLensAdornmentCreationListener))]
    ///   class ListenerImpl : ICodeLensAdornmentCreationListener
    /// </summary>
    public interface ICodeLensAdornmentCreationListener
    {
        /// <summary>
        /// Called each time an adornment FrameworkElement is placed into the editor.
        /// </summary>
        /// <param name="adornment">The UI element representing the new adornment.</param>
        void AdornmentShown(FrameworkElement adornment);

        /// <summary>
        /// Called each time an adornment FrameworkElement is removed from the editor.
        /// </summary>
        /// <param name="adornment">The UI element representing the destroyed adornment.</param>
        void AdornmentHidden(FrameworkElement adornment);
    }
}
