// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Implementation
{
    [Export(typeof(IObscuringTipManager))]
    public class TipManager : IObscuringTipManager
    {
        public void PushTip(ITextView view, IObscuringTip tip)
        {
            TipManager.PushTipToView(view, tip);
        }

        public void RemoveTip(ITextView view, IObscuringTip tip)
        {
            TipManager.RemoveTipFromView(view, tip);
        }

        internal static void PushTipToView(ITextView view, IObscuringTip tip)
        {
            VsEditorAdaptersFactoryService.GetSimpleTextViewWindowFromTextView(view)?.PushTip(tip);
        }

        internal static void RemoveTipFromView(ITextView view, IObscuringTip tip)
        {
            VsEditorAdaptersFactoryService.GetSimpleTextViewWindowFromTextView(view)?.RemoveTip(tip);
        }
    }
}
