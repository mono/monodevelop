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
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    [Export(typeof(IFileExtensionRegistryService))]
    [Export(typeof(IFileExtensionRegistryService2))]
    internal sealed class FileExtensionRegistryImpl : IFileExtensionRegistryService2
    {
        [Import]
        internal IContentTypeRegistryService ContentTypeRegistry { get; set; }

        [ImportMany]
        internal List<Lazy<FileExtensionToContentTypeDefinition, IFileExtensionToContentTypeMetadata>> ExtensionToContentTypeExtensionsProductions { get; set; }

        [ImportMany]
        internal List<Lazy<FileExtensionToContentTypeDefinition, IFileNameToContentTypeMetadata>> FileNameToContentTypeProductions { get; set; }

        // UNDONE: the usage of a simple lock causes all readers of the registry to be serialized.
        // Ideally the registry should be switched to some kind of reader-writer locking.
        private readonly object syncLock = new object();

        private StringToContentTypesMap extensionMap;

        private StringToContentTypesMap fileNameMap;

        #region IFileExtensionRegistryService Members

        public IContentType GetContentTypeForExtension(string extension)
        {
            // Check here so that the argument name matches the expected.
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            lock (this.syncLock)
            {
                EnsureInitialized();

                return this.extensionMap.GetContentTypeForString(RemoveExtensionDot(extension)) ?? ContentTypeRegistry.UnknownContentType;
            }
        }

        public IEnumerable<string> GetExtensionsForContentType(IContentType contentType)
        {
            lock (this.syncLock)
            {
                EnsureInitialized();

                return this.extensionMap.GetStringsForContentType(contentType);
            }
        }

        public void AddFileExtension(string extension, IContentType contentType)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException();
            }

            lock (this.syncLock)
            {
                EnsureInitialized();

                this.extensionMap.AddMapping(RemoveExtensionDot(extension), contentType);
            }
        }

        public void RemoveFileExtension(string extension)
        {
            // Check here so that the argument name matches the expected.
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            lock (this.syncLock)
            {
                EnsureInitialized();

                this.extensionMap.RemoveMapping(RemoveExtensionDot(extension));
            }
        }

        #endregion

        #region IFileExtensionRegistryService2 Members

        public IContentType GetContentTypeForFileName(string name)
        {
            // Check here so that the argument name matches the expected.
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            lock (this.syncLock)
            {
                EnsureInitialized();

                return this.fileNameMap.GetContentTypeForString(name) ?? ContentTypeRegistry.UnknownContentType;
            }
        }

        public IContentType GetContentTypeForFileNameOrExtension(string name)
        {
            // No need to lock, we are calling locking public method.
            var fileNameContentType = this.GetContentTypeForFileName(name);

            // Attempt to use extension as fallback ContentType if file name isn't recognized.
            if (fileNameContentType == ContentTypeRegistry.UnknownContentType)
            {
                var extension = Path.GetExtension(name);

                if (!string.IsNullOrEmpty(extension))
                {
                    // No need to lock, we are calling locking public method.
                    return this.GetContentTypeForExtension(extension);
                }
            }

            return fileNameContentType;
        }

        public IEnumerable<string> GetFileNamesForContentType(IContentType contentType)
        {
            lock (this.syncLock)
            {
                EnsureInitialized();

                return this.fileNameMap.GetStringsForContentType(contentType);
            }
        }

        public void AddFileName(string name, IContentType contentType)
        {
            // Disallow nonsense inputs.
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            lock (this.syncLock)
            {
                EnsureInitialized();

                this.fileNameMap.AddMapping(name, contentType);
            }
        }

        public void RemoveFileName(string name)
        {
            // Check here so that the argument name matches the expected.
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            lock (this.syncLock)
            {
                EnsureInitialized();

                this.fileNameMap.RemoveMapping(name);
            }
        }

        #endregion

        /// <summary>
        /// Performs lazy initialization. Callers must first lock this.syncLock.
        /// </summary>
        private void EnsureInitialized()
        {
            if (this.extensionMap == null)
            {
                this.extensionMap = new StringToContentTypesMap(GetExtensionToContentTypeMappings());
                this.fileNameMap = new StringToContentTypesMap(GetFileNameToContentTypeMappings());
            }
        }

        private IEnumerable<Tuple<string, IContentType>> GetExtensionToContentTypeMappings()
        {
            foreach (var fileExtensionToContentTypeProduction in ExtensionToContentTypeExtensionsProductions)
            {
                // MEF ensures that there will be at least one content type in the metadata. We take the first one. 
                // We prefer this over defining a different attribute from ContentType[] for this purpose.
                IEnumerator<string> cts = fileExtensionToContentTypeProduction.Metadata.ContentTypes.GetEnumerator();
                cts.MoveNext();

                IContentType contentType = ContentTypeRegistry.GetContentType(cts.Current);

                if (contentType != null)
                {
                    yield return Tuple.Create(RemoveExtensionDot(fileExtensionToContentTypeProduction.Metadata.FileExtension), contentType);
                }
            }
        }

        private IEnumerable<Tuple<string, IContentType>> GetFileNameToContentTypeMappings()
        {
            foreach (var fileNameToContentTypeProduction in FileNameToContentTypeProductions)
            {
                // MEF ensures that there will be at least one content type in the metadata. We take the first one. 
                // We prefer this over defining a different attribute from ContentType[] for this purpose.
                IEnumerator<string> cts = fileNameToContentTypeProduction.Metadata.ContentTypes.GetEnumerator();
                cts.MoveNext();

                IContentType contentType = ContentTypeRegistry.GetContentType(cts.Current);

                if (contentType != null)
                {
                    yield return Tuple.Create(fileNameToContentTypeProduction.Metadata.FileName, contentType);
                }
            }
        }

        private static string RemoveExtensionDot(string extension)
        {
            if (extension.StartsWith("."))
            {
                return extension.TrimStart('.');
            }
            else
            {
                return extension;
            }
        }
    }
}
