//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ISmartIndentationService))]
    internal sealed class SmartIndentationService : ISmartIndentationService, ISmartIndent
    {
        [ImportMany]
        internal List<Lazy<ISmartIndentProvider, IContentTypeMetadata>> SmartIndentProviders { get; set; }

        [Import]
        internal GuardedOperations GuardedOperations { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        public int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line)
        {
            return GetSmartIndent(textView).GetDesiredIndentation(line);
        }

        private ISmartIndent GetSmartIndent(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty<ISmartIndent>(typeof(SmartIndentationService), delegate
                    {
                        EventHandler<TextDataModelContentTypeChangedEventArgs> onContentTypeChanged = null;
                        EventHandler onClosed = null;

                        Action disconnect = delegate()
                        {
                            ISmartIndent currentSmartIndent = (ISmartIndent)textView.Properties[typeof(SmartIndentationService)];
                            textView.Properties.RemoveProperty(typeof(SmartIndentationService));
                            currentSmartIndent.Dispose();

                            textView.TextDataModel.ContentTypeChanged -= onContentTypeChanged;
                            textView.Closed -= onClosed;
                        };

                        onContentTypeChanged = delegate(object sender, TextDataModelContentTypeChangedEventArgs e)
                        {
                            disconnect();
                        };

                        onClosed = delegate(object sender, EventArgs e)
                        {
                            disconnect();
                        };

                        textView.TextDataModel.ContentTypeChanged += onContentTypeChanged;
                        textView.Closed += onClosed;

                        return CreateSmartIndent(textView);
                    });
        }

        ISmartIndent CreateSmartIndent(ITextView textView)
        {
            if (textView.IsClosed)
                return this;
            else
                return GuardedOperations.InvokeBestMatchingFactory
                                            (SmartIndentProviders, textView.TextDataModel.ContentType,
                                             (provider) => (provider.CreateSmartIndent(textView)), ContentTypeRegistryService, this) 
                       ?? this;
        }

        /// <summary>
        /// This is the vacuous implementation for ContentTypes that have no provided ISmartIndent 
        /// </summary>
        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            return null;
        }

        /// <summary>
        /// This is for the vacuous ISmartIndent
        /// </summary>
        public void Dispose()
        {
        }
    }
}
