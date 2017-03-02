//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.Win32;

    [Export(typeof(IOutliningManagerService))]
    internal class OutliningManagerService : IOutliningManagerService
    {
        [Import]
        internal IBufferTagAggregatorFactoryService TagAggregatorFactory { get; set; }

        [Import]
        internal IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }

        public IOutliningManager GetOutliningManager(ITextView textView)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");

            if (!textView.Roles.Contains(PredefinedTextViewRoles.Structured))
                return null;

            return textView.Properties.GetOrCreateSingletonProperty(delegate
            {
                var tagAggregator = TagAggregatorFactory.CreateTagAggregator<IOutliningRegionTag>(textView.TextBuffer);
                var manager = new OutliningManager(textView.TextBuffer, tagAggregator, EditorOptionsFactoryService.GlobalOptions);
                textView.Closed += delegate { manager.Dispose(); };
                return manager;
            });
        }
    }
}
