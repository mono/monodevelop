// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Helper class to perform ContentType best-match against a set of extensions. This could
    /// become a public service.
    /// </summary>
    internal static class ExtensionSelector
    {
        /// <summary>
        /// Given a list of extensions that provide content types, filter the list and return that
        /// subset which matches the given content type
        /// </summary>
        public static List<Lazy<TProvider, TMetadataView>> SelectMatchingExtensions<TProvider, TMetadataView>
                    (IEnumerable<Lazy<TProvider, TMetadataView>> providerHandles,
                     IContentType dataContentType)
            where TMetadataView : IContentTypeMetadata          // content type is required
        {
            var result = new List<Lazy<TProvider, TMetadataView>>();
            foreach (var providerHandle in providerHandles)
            {
                if (ContentTypeMatch(dataContentType, providerHandle.Metadata.ContentTypes))
                {
                    result.Add(providerHandle);
                }
            }
            return result;
        }

        /// <summary>
        /// Test whether an extension matches a content type.
        /// </summary>
        /// <param name="dataContentType">Content type (typically of a text buffer) against which to match an extension.</param>
        /// <param name="extensionContentTypes">Content types from extension metadata.</param>
        public static bool ContentTypeMatch(IContentType dataContentType, IEnumerable<string> extensionContentTypes)
        {
            foreach (string contentType in extensionContentTypes)
            {
                if (dataContentType.IsOfType(contentType))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Test whether an extension matches one of a set of content types.
        /// </summary>
        /// <param name="dataContentTypes">Content types (typically of text buffers in a buffer graph) against which to match an extension.</param>
        /// <param name="extensionContentTypes">Content types from extension metadata.</param>
        public static bool ContentTypeMatch(IEnumerable<IContentType> dataContentTypes, IEnumerable<string> extensionContentTypes)
        {
            foreach (IContentType bufferContentType in dataContentTypes)
            {
                if (ContentTypeMatch(bufferContentType, extensionContentTypes))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
