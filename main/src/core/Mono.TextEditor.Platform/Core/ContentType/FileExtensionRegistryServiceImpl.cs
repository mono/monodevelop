// Copyright (C) Microsoft Corporation.  All Rights Reserved.

using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Utilities.Implementation
{
    public interface IFileExtensionToContentTypeMetadata
    {
        string FileExtension { get; }
        IEnumerable<string> ContentTypes { get; }
    }

    [Export(typeof(IFileExtensionRegistryService))]
    internal sealed class FileExtensionRegistryImpl : IFileExtensionRegistryService
    {
        [Import]
        internal IContentTypeRegistryService ContentTypeRegistry { get; set; }

        [ImportMany]
        internal List<Lazy<FileExtensionToContentTypeDefinition, IFileExtensionToContentTypeMetadata>> ExtensionToContentTypeExtensionsProductions { get; set; }

        // UNDONE: the usage of a simple lock causes all readers of the registry to be serialized.
        // Ideally the registry should be switched to some kind of reader-writer locking.
        private object syncLock = new object();

        // Map of extensions to their correspondent content types
        private Dictionary<string, IContentType> extensionMap;

        // Map of a content type to a set of extensions
        // keyed by lowercase dotless form of extension
        // value is case-preserved extension
        private Dictionary<IContentType, Dictionary<string, string>> contentTypeMap;

        private Dictionary<string, IContentType> ExtensionMap
        {
            get
            {
                if (null == this.extensionMap)
                {
                    BuildExtensionMap();
                }

                return this.extensionMap;
            }
        }

        private Dictionary<IContentType, Dictionary<string, string>> ContentTypeMap
        {
            get
            {
                if (this.contentTypeMap == null)
                {
                    BuildExtensionMap();
                }

                return this.contentTypeMap;
            }
        }

        /// <summary>
        /// Builds the map of available extensions to content types
        /// Note: This function must be called after acquiring a lock on syncLock
        /// </summary>
        private void BuildExtensionMap()
        {
            if ( null == this.extensionMap )
            {
                this.extensionMap = new Dictionary<string, IContentType>();
                this.contentTypeMap = new Dictionary<IContentType, Dictionary<string, string>>();

                foreach (var fileExtensionToContentTypeProduction in ExtensionToContentTypeExtensionsProductions)
                {
                    // MEF ensures that there will be at least one content type in the metadata. We take the first one. 
                    // We prefer this over defining a different attribute from ContentType[] for this purpose.
                    IEnumerator<string> cts = fileExtensionToContentTypeProduction.Metadata.ContentTypes.GetEnumerator();
                    cts.MoveNext();
                    IContentType contentType = ContentTypeRegistry.GetContentType(cts.Current);
                    if (contentType != null)
                    {
                        TryAddFileExtension(fileExtensionToContentTypeProduction.Metadata.FileExtension, contentType);
                        // For now simply ignore the conflicting extension declarations.
                        // Later the issues should probably be logged with some kind of composition error reporting services.
                    }
                }
            }
        }

        public IContentType GetContentTypeForExtension(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }

            lock (this.syncLock)
            {
                IContentType contentType;

                if (!this.ExtensionMap.TryGetValue(AsKey(extension), out contentType))
                {
                    return ContentTypeRegistry.UnknownContentType;
                }

                return contentType;
            }
        }

        public IEnumerable<string> GetExtensionsForContentType(IContentType contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            lock (this.syncLock)
            {
                Dictionary<string, string> extensions;
                if (this.ContentTypeMap.TryGetValue(contentType, out extensions))
                {
                    return extensions.Values;
                }
                else
                {
                    return new string[0];
                }
            }
        }

        public void AddFileExtension(string extension, IContentType contentType)
        {
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            lock (this.syncLock)
            {
                if (this.extensionMap == null)
                {
                    BuildExtensionMap();
                }

                if (!TryAddFileExtension(extension, contentType))
                {
                    throw new InvalidOperationException
                                (String.Format(System.Globalization.CultureInfo.CurrentUICulture,
                                    Strings.FileExtension_NoMultipleContentTypes, extension));
                }
            }
        }

        public void RemoveFileExtension(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }

            string extensionAsKey = AsKey(extension);

            lock (this.syncLock)
            {
                if (this.extensionMap == null)
                {
                    BuildExtensionMap();
                }

                IContentType contentType;
                if (this.extensionMap.TryGetValue(extensionAsKey, out contentType))
                {
                    this.extensionMap.Remove(extensionAsKey);
                }
                else
                {
                    return;
                }

                Dictionary<string, string> extensions;
                if (!this.contentTypeMap.TryGetValue(contentType, out extensions))
                {
                    extensions = new Dictionary<string, string>();
                    this.contentTypeMap.Add(contentType, extensions);
                }

                if (extensions.ContainsKey(extensionAsKey))
                {
                    extensions.Remove(extensionAsKey);

                    if (extensions.Count == 0)
                    {
                        this.contentTypeMap.Remove(contentType);
                    }
                }
            }
        }

        // Strips dot from extension but preserves case
        private static string StripDot(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }

            return extension.TrimStart('.');
        }

        // Strips dot from extension and guarantees case so can be used as key
        private static string AsKey(string extension)
        {
            return StripDot(extension).ToLower(new CultureInfo(CultureInfo.InvariantCulture.LCID));
        }

        // Attempts to add a new file extension to the. Returns false if an existing mapping for 
        private bool TryAddFileExtension(string extension, IContentType contentType)
        {
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (this.extensionMap == null)
            {
                throw new InvalidOperationException("extensionMap is null");
            }

            string extensionWithoutDot = StripDot(extension);
            string extensionAsKey = AsKey(extension);

            // Check if the file extension is already registered
            IContentType existingContentType;
            if (this.extensionMap.TryGetValue(extensionAsKey, out existingContentType))
            {
                if (contentType != existingContentType)
                {
                    // A conflicting declaration has been detected.
                    return false;
                }
                // Else: Nothing to do - the same file extension with the same content type have been registered before.
            }
            else
            {
                // A new file extension - lets add it to the map
                this.extensionMap.Add(extensionAsKey, contentType);
            }

            // Update content types map
            Dictionary<string, string> extensions;
            if (!this.contentTypeMap.TryGetValue(contentType, out extensions))
            {
                extensions = new Dictionary<string, string>();
                this.contentTypeMap.Add(contentType, extensions);
            }
            if (!extensions.ContainsKey(extensionAsKey))
            {
                extensions.Add(extensionAsKey, extensionWithoutDot); // value preserves case
            }

            return true; // Success
        }
    }
}

