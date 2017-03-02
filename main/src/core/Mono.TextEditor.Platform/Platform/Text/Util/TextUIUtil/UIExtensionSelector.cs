//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Helper class to perform ContentType and TextViewRole match against a set of extensions. 
    /// </summary>
    public static class UIExtensionSelector
    {
        /// <summary>
        /// Given a list of extensions that provide text view roles, filter the list and return that
        /// subset which matches at least one of the roles in the provided set of roles.
        /// </summary>
        public static List<Lazy<TProvider, TMetadataView>> SelectMatchingExtensions<TProvider, TMetadataView>
                    (IEnumerable<Lazy<TProvider, TMetadataView>> providerHandles,
                     ITextViewRoleSet viewRoles)
            where TMetadataView : ITextViewRoleMetadata          // text view role is required
        {
            var result = new List<Lazy<TProvider, TMetadataView>>();
            foreach (var providerHandle in providerHandles)
            {
                IEnumerable<string> providerRoles = providerHandle.Metadata.TextViewRoles;
                if (viewRoles.ContainsAny(providerRoles))
                {
                    result.Add(providerHandle);
                }
            }
            return result;
        }

        /// <summary>
        /// Given a list of extensions that provide text view roles and content types, filter the list and return that
        /// subset which matches the content types and at least one of the roles in the provided set of roles.
        /// </summary>
        public static List<Lazy<TProvider, TMetadataView>> SelectMatchingExtensions<TProvider, TMetadataView>
                    (IEnumerable<Lazy<TProvider, TMetadataView>> providerHandles,
                     IContentType documentContentType,
                     IContentType excludedContentType,
                     ITextViewRoleSet viewRoles)
            where TMetadataView : IContentTypeAndTextViewRoleMetadata          // both content type and text view role are required
        {
            var result = new List<Lazy<TProvider, TMetadataView>>();
            foreach (var providerHandle in providerHandles)
            {
                // first, check content type match
                if ((excludedContentType == null || !ExtensionSelector.ContentTypeMatch(excludedContentType, providerHandle.Metadata.ContentTypes)) && 
                    ExtensionSelector.ContentTypeMatch(documentContentType, providerHandle.Metadata.ContentTypes) &&
                    viewRoles.ContainsAny(providerHandle.Metadata.TextViewRoles))
                {
                    result.Add(providerHandle);
                }
            }
            return result;
        }

        /// <summary>
        /// Given a list of extensions that provide text view roles and content types, return the
        /// instantiated extension which best matches the given content type and matches at least one of the roles.
        /// </summary>
        public static TExtensionInstance InvokeBestMatchingFactory<TExtensionInstance, TExtensionFactory, TMetadataView>
                    (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> providerHandles,
                     IContentType dataContentType,
                     ITextViewRoleSet viewRoles,
                     Func<TExtensionFactory, TExtensionInstance> getter,
                     IContentTypeRegistryService contentTypeRegistryService,
                     GuardedOperations guardedOperations,
                     object errorSource)
            where TMetadataView : IContentTypeAndTextViewRoleMetadata          // both content type and text view role are required
            where TExtensionFactory : class
            where TExtensionInstance : class
        {
            var roleMatchingProviderHandles = SelectMatchingExtensions(providerHandles, viewRoles);
            return guardedOperations.InvokeBestMatchingFactory(roleMatchingProviderHandles, dataContentType, getter, contentTypeRegistryService, errorSource);
        }
    }
}
