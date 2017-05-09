//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides a service to help with the Text Structure Navigation.
    /// </summary>
    [Export(typeof(ITextStructureNavigatorSelectorService))]
    internal sealed class TextStructureNavigatorSelectorService : ITextStructureNavigatorSelectorService
    {
        [Import]
        internal IContentTypeRegistryService _contentTypeRegistryService { get; set; }

        [Import]
        internal GuardedOperations _guardedOperations { get; set; }

        [ImportMany(typeof(ITextStructureNavigatorProvider))]
        internal List<Lazy<ITextStructureNavigatorProvider, IContentTypeMetadata>> _textStructureNavigatorProviders { get; set; }

        public ITextStructureNavigator GetTextStructureNavigator(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            ITextStructureNavigator navigator = null;

            if (textBuffer.Properties.TryGetProperty(typeof(ITextStructureNavigator), out navigator))
            {
                return navigator;
            }

            navigator = CreateNavigator(textBuffer, textBuffer.ContentType);

            // Cache navigator until buffer's content type changes.
            textBuffer.Properties[typeof(ITextStructureNavigator)] = navigator;
            textBuffer.ContentTypeChanged += OnContentTypeChanged;

            return navigator;
        }

        public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, IContentType contentType)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }
            return CreateNavigator(textBuffer, contentType);
        }

        #region Private Helpers

        private ITextStructureNavigator CreateNavigator(ITextBuffer textBuffer, IContentType contentType)
        {
            ITextStructureNavigator navigator =
                _guardedOperations.InvokeBestMatchingFactory
                    (_textStructureNavigatorProviders, 
                     contentType, 
                     (provider) => (provider.CreateTextStructureNavigator(textBuffer)),
                    _contentTypeRegistryService, this);

            // If we're here, and there's no navigator found, we'll create a default one
            if (navigator == null)
            {
                navigator = new DefaultTextNavigator(textBuffer, _contentTypeRegistryService);
            }

            return navigator;
        }

        /// <summary>
        /// Invalidate our cached navigator.
        /// </summary>
        void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            ITextBuffer buffer = e.Before.TextBuffer;
            buffer.Properties.RemoveProperty(typeof(ITextStructureNavigator));
            buffer.ContentTypeChanged -= OnContentTypeChanged;
        }
        #endregion // Private Helpers
    }
}
