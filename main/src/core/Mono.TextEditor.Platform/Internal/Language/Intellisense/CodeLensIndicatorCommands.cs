using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public static class CodeLensIndicatorCommands
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "RoutedCommands cannot be modified")]
        public static readonly ICommand ShowDetails = new RoutedCommand("ShowDetails", typeof(CodeLensIndicatorCommands));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "RoutedCommands cannot be modified")]
        public static readonly ICommand HideDetails = new RoutedCommand("HideDetails", typeof(CodeLensIndicatorCommands));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "RoutedCommands cannot be modified")]
        public static readonly ICommand PinDetails = new RoutedCommand("PinDetails", typeof(CodeLensIndicatorCommands));

    }
}
