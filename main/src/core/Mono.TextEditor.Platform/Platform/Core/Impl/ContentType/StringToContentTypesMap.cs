//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Utilities.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    internal sealed class StringToContentTypesMap
    {
        // Map of extensions to their corresponding content types
        private Dictionary<string, IContentType> stringMap;

        public StringToContentTypesMap(IEnumerable<Tuple<string, IContentType>> mappings)
        {

            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }

            this.stringMap = new Dictionary<string, IContentType>(StringComparer.OrdinalIgnoreCase);

            foreach (var mapping in mappings)
            {
                // Any failures should ideally be logged somehow.
                TryAddString(mapping.Item1, mapping.Item2);
            }
        }

        public IContentType GetContentTypeForString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            IContentType contentType;

            if (!this.stringMap.TryGetValue(str, out contentType))
            {
                return null;
            }

            return contentType;
        }

        public IEnumerable<string> GetStringsForContentType(IContentType contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            // Asymptotically slow, however, after searching the VS code base, we found that
            // looking up extensions for ContentType is only used by tests, and is probably
            // rarely used. This method used be backed by a second map for quick lookup,
            // but for simplicity, we're going to move to a single map, barring any regressions.
            return this.stringMap
                .Where(pair => pair.Value == contentType)
                .Select(pair => pair.Key);
        }

        public void AddMapping(string str, IContentType contentType)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (!TryAddString(str, contentType))
            {
                throw new InvalidOperationException
                            (String.Format(System.Globalization.CultureInfo.CurrentUICulture,
                                Strings.FileExtensionRegistry_NoMultipleContentTypes, str));
            }
        }

        public void RemoveMapping(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            this.stringMap.Remove(str);
        }

        private bool TryAddString(string str, IContentType contentType)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            // Check if the string is already registered
            IContentType existingContentType;
            if (this.stringMap.TryGetValue(str, out existingContentType))
            {
                // Return false if there is a conflict.
                return contentType == existingContentType;
            }
            else
            {
                // A new string - lets add it to the map
                this.stringMap.Add(str, contentType);
            }

            return true;
        }
    }
}

