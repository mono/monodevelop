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
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

#pragma warning disable 618 // IToolTipProviderFactory is deprecated.
    [Export(typeof(IToolTipProviderFactory))]
    internal sealed class ToolTipProviderFactory : IToolTipProviderFactory
    {
        //Specify the view layer definitions for the view.
        [Export]
        [Name("ToolTip")]
        [Order()]
        internal SpaceReservationManagerDefinition tooltipManager;

        #region IToolTipProviderFactory Members
        public IToolTipProvider GetToolTipProvider(ITextView textView)
        {
            IWpfTextView wpfTextView = textView as IWpfTextView;
            if (wpfTextView == null)
                throw new ArgumentException(Strings.InvalidTextView);

            return CreateToolTipProviderInternal(wpfTextView);
        }
        #endregion

        internal static ToolTipProvider CreateToolTipProviderInternal(IWpfTextView view)
        {
            ToolTipProvider toolTipAdornmentProvider = view.Properties.GetOrCreateSingletonProperty<ToolTipProvider>(delegate
                                                                                                                    {
                                                                                                                        return new ToolTipProvider(view);
                                                                                                                    });

            return toolTipAdornmentProvider;
        }
    }
#pragma warning restore 618
}
