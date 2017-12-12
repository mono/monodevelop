////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    [Export(typeof(IIntellisenseSessionStackMapService))]
    internal sealed class IntellisenseSessionStackMapService : IIntellisenseSessionStackMapService
    {
        [Import]
        internal IToolTipProviderFactory ToolTipProviderFactory { get; set; }

        [Import(AllowDefault = true)]
        internal IObscuringTipManager TipManager { get; set; }

#if DEBUG
        [ImportMany]
        internal List<Lazy<IObjectTracker>> ObjectTrackers { get; set; }
#endif

        public IIntellisenseSessionStack GetStackForTextView(ITextView textView)
        {
            if (textView == null)
            {
                return (null);
            }

            IIntellisenseSessionStack stack = null;
            if (!textView.Properties.TryGetProperty<IIntellisenseSessionStack>(typeof(IIntellisenseSessionStack), out stack))
            {
                var wpfTextView = textView as IMdTextView;
                if (wpfTextView != null)
                {
                    stack = new IntellisenseSessionStack(wpfTextView, this.TipManager);

#if DEBUG
                    Helpers.TrackObject(this.ObjectTrackers, "Intellisense Session Stacks", stack);
#endif

                    wpfTextView.Properties.AddProperty(typeof(IIntellisenseSessionStack), stack);
                    wpfTextView.Closed += this.OnTextViewClosed;
                }
            }

            return (stack);
        }

        void OnTextViewClosed(object sender, EventArgs e)
        {
            ITextView textView = sender as ITextView;
            if (textView != null)
            {
                textView.Properties.RemoveProperty(typeof(IIntellisenseSessionStack));
                textView.Closed -= this.OnTextViewClosed;
            }
        }
    }
}
