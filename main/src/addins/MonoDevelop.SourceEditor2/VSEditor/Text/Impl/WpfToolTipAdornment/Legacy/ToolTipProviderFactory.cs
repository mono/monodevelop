//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

    [Export(typeof(IToolTipProviderFactory))]
    [Obsolete]
    internal sealed class ToolTipProviderFactory : IToolTipProviderFactory
    {
        //Specify the view layer definitions for the view.
        [Export]
        [Name("ToolTip")]
        [Order()]
        internal SpaceReservationManagerDefinition tooltipManager;

        public IToolTipProvider GetToolTipProvider(ITextView textView)
        {
            var wpfTextView = textView as IMdTextView;
            if (wpfTextView == null)
                throw new ArgumentException("Invalid TextView");

            return CreateToolTipProviderInternal(wpfTextView);
        }

        internal static ToolTipProvider CreateToolTipProviderInternal(IMdTextView view)
        {
            ToolTipProvider toolTipAdornmentProvider = view.Properties.GetOrCreateSingletonProperty (
                delegate {
                    return new ToolTipProvider (view);
                });

            return toolTipAdornmentProvider;
        }
    }
}
