// Copyright (C) Microsoft Corporation.  All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Utilities.Implementation
{
    public interface IContentTypeDefinitionMetadata
    {
        string Name { get; }

        [System.ComponentModel.DefaultValue(null)]
        IEnumerable<string> BaseDefinition { get; }
    }

    [Export(typeof(IContentTypeRegistryService))]
    internal sealed partial class ContentTypeRegistryImpl: IContentTypeRegistryService
    {
        [ImportMany]
        internal List<Lazy<ContentTypeDefinition, IContentTypeDefinitionMetadata>> ContentTypeDefinitions { get; set; }

        [ImportMany]
        internal List<IContentTypeDefinitionSource> ExternalSources { get; set; }

        [Import]
        internal IFileExtensionRegistryService FileExtensionRegistry { get; set; }

        private Dictionary<string, ContentTypeImpl> contentTypes;
        
        /// <summary>
        /// The name of the unknown content type, guaranteed to exists no matter what other content types are produced
        /// </summary>
        private const string UnknownContentTypeName = "UNKNOWN";
        private static ContentTypeImpl unknownContentType = new ContentTypeImpl(ContentTypeRegistryImpl.UnknownContentTypeName);
        
        // The lock is used for thread synchronization 
        private object syncLock = new object();

        /// <summary>
        /// Builds the list of available content types
        /// Note: This function must be called after acquiring a lock on syncLock
        /// </summary>
        /// <remarks>
        /// Building the content type mappings should not throw exceptions, but should rather be logging issues 
        /// with some kind of common error reporting service and try to recover by ignoring the asset productions 
        /// that are deemed to cause the problem.
        /// </remarks>
        private void BuildContentTypes()
        {
            if (this.contentTypes == null)
            {
                this.contentTypes = new Dictionary<string, ContentTypeImpl>();
                
                // Add the singleton Unknown content type to the dictionary
                this.contentTypes.Add(ContentTypeRegistryImpl.UnknownContentTypeName, ContentTypeRegistryImpl.unknownContentType);

                // For each content type provision, create an IContentType.
                foreach (Lazy<ContentTypeDefinition, IContentTypeDefinitionMetadata> contentTypeDefinition in ContentTypeDefinitions)
                {
                    string contentTypeName = contentTypeDefinition.Metadata.Name;
                    if (!string.IsNullOrEmpty(contentTypeName))
                    {
                        // If there is at least one base type specified, iterate the base types collection
                        IEnumerable<string> baseTypes = contentTypeDefinition.Metadata.BaseDefinition;
                        if (baseTypes != null)
                        {
                            foreach (string baseType in baseTypes)
                            {
                                TryAddContentTypeDefinition(contentTypeName, baseType);
                            }
                        }
                        else
                        {
                            // Add the content type without a base type                        
                            TryAddContentTypeDefinition(contentTypeName, null);
                        }
                    }
                }

                // Now consider the external sources. This allows us to consider legacy content types together with MEF-defined
                // content types.
                foreach (IContentTypeDefinitionSource source in this.ExternalSources)
                {
                    if (source.Definitions != null)
                    {
                        foreach (IContentTypeDefinition metadata in source.Definitions)
                        {
                            string contentTypeName = metadata.Name;
                            if (!string.IsNullOrEmpty(contentTypeName))
                            {
                                // If there is at least one base type specified, iterate the base types collection
                                IEnumerable<string> baseTypes = metadata.BaseDefinitions;
                                if (baseTypes != null)
                                {
                                    foreach (string baseType in baseTypes)
                                    {
                                        TryAddContentTypeDefinition(contentTypeName, baseType);
                                    }
                                }
                                else
                                {
                                    // Add the content type without a base type                        
                                    TryAddContentTypeDefinition(contentTypeName, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates and adds a new content type with the specified name, or returns the existing type
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="type">The type created or existent</param>
        /// <returns>true if the type already existed</returns>
        private bool GetOrCreateContentType(string typeName, out ContentTypeImpl type)
        {
            // Get existing or create new content type
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(Strings.ContentTypeRegistry_CannotAddTypeWithEmptyTypeName);
            }

            string uppercaseTypeName = typeName.ToUpperInvariant();
            // no one can produce another unknown content type
            if (uppercaseTypeName == ContentTypeRegistryImpl.UnknownContentTypeName)
            {
                throw new InvalidOperationException(Strings.ContentTypeRegistry_CannotProduceAnotherUnknownType);
            }

            bool isExistingType = this.contentTypes.TryGetValue(uppercaseTypeName, out type);
            if (!isExistingType)
            {
                type = new ContentTypeImpl(typeName);
            }

            return isExistingType;
        }

        /// <summary>
        /// Creates if necessary the base type and update the base type list of the specified type
        /// </summary>
        /// <param name="baseTypeName">Base type name to create and add</param>
        /// <param name="type">The type whose base type list needs updating</param>
        /// <param name="isExistingType">Whether the type is a new type or existent one</param>
        private void AddBaseTypeOfType(string baseTypeName, ContentTypeImpl type, bool isExistingType)
        {
            if (baseTypeName != null)
            {
                string uppercaseBaseTypeName = baseTypeName.ToUpperInvariant();

                // no content type can derive from unknown
                // Do this check before GetOrCreateContentType because it provides a more meaningful exception message
                if (baseTypeName == ContentTypeRegistryImpl.UnknownContentTypeName)
                {
                    throw new InvalidOperationException(String.Format(System.Globalization.CultureInfo.CurrentUICulture, 
                                                        Strings.ContentTypeRegistry_ContentTypesCannotDeriveFromUnknown, type.TypeName));
                }

                // Get existing or create new base content type if any base type is specified
                ContentTypeImpl baseType;
                bool isExistingBaseType = GetOrCreateContentType(baseTypeName, out baseType);

                // Only a new mapping between two existing nodes can introduce a cycle in the graph.
                if (isExistingType && isExistingBaseType && this.CausesCycle(type, baseType))
                {
                    throw new InvalidOperationException(String.Format(System.Globalization.CultureInfo.CurrentUICulture, Strings.ContentTypeRegistry_CausesCycles, baseType.TypeName, type.TypeName));
                }

                if (!isExistingBaseType)
                {
                    this.contentTypes.Add(uppercaseBaseTypeName, baseType);
                }

                type.AddBaseType(baseType);
            }
        }

        // Attempts to add content type declarations to the registry
        private bool TryAddContentTypeDefinition(string typeName, string baseTypeName)
        {
            try
            {
                this.AddContentTypeDefinition(typeName, baseTypeName);
            }
            catch (InvalidOperationException)
            {
                // TODO: figure out a way of reporting invalid productions that we're going to ignore
                return false;
            }
            catch (ArgumentException)
            {
                // TODO: figure out a way of reporting invalid productions that we're going to ignore
                return false;
            }

            return true;
        }

        // Attempts to add content type declarations to the registry
        private void AddContentTypeDefinition(string typeName, string baseTypeName)
        {
            // Get existing or create new content type that needs to be added
            ContentTypeImpl type;
            bool isExistingType = GetOrCreateContentType(typeName, out type);

            if (baseTypeName != null && baseTypeName.ToUpperInvariant() != typeName.ToUpperInvariant())
            {
                AddBaseTypeOfType(baseTypeName, type, isExistingType);
            }

            if (!isExistingType)
            {
                this.contentTypes.Add(type.TypeName.ToUpperInvariant(), type);
            }
        }

        // States of the nodes while searching for cycles
        private enum VisitState
        {
            NotVisited = 0, // The node hasn't been visited yet
            Visiting,       // The node (or one of its children) is being visited
            Visited         // The node and its children have been visited before
        }

        // Verifies whether adding a mapping between the given nodes would create a cycle in the content type hierarchy graph.
        private bool CausesCycle(IContentType newMappingType, IContentType newMappingBaseType)
        {
            // The input parameters can never be null
            if (newMappingType == null)
            {
                throw new InvalidOperationException("newMappingType is null");
            }
            if (newMappingBaseType == null)
            {
                throw new InvalidOperationException("newMappingBaseType is null");
            }

            // Mark the type as being visited and walk the newly proposed base type looking for a cycle
            Dictionary<IContentType, VisitState> states = new Dictionary<IContentType, VisitState>();
            states[newMappingType] = VisitState.Visiting;
            return HasCycle(newMappingBaseType, states);
        }

        // Walks the given node and its children searching for a cycle
        private bool HasCycle(IContentType node, Dictionary<IContentType, VisitState> states)
        {
            VisitState nodeState;
            if (!states.TryGetValue(node, out nodeState))
            {
                nodeState = VisitState.NotVisited;
            }
            // Successfully walked the node's hierarchy before.
            if (nodeState == VisitState.Visited)
            {
                return false;
            }
            // Got to the node while visiting its children - this is a cycle!
            if (nodeState == VisitState.Visiting)
            {
                return true;
            }
            // Start visiting the node
            states[node] = VisitState.Visiting;
            foreach (IContentType baseNode in node.BaseTypes)
            {
                if (HasCycle(baseNode, states))
                {
                    return true;
                }
            }

            // Done with the node
            states[node] = VisitState.Visited;
            return false;
        }

        /// <summary>
        /// Checks whether the specified type is base type for another content type
        /// </summary>
        /// <param name="typeToCheck">The type to check for being a base type</param>
        /// <param name="derivedType">An out parameter to receive the first discovered derived type</param>
        /// <returns><c>True</c> if the given <paramref name="typeToCheck"/> content type is a base type</returns>
        private bool IsBaseType(ContentTypeImpl typeToCheck, out ContentTypeImpl derivedType)
        {
            derivedType = null;

            foreach (ContentTypeImpl type in contentTypes.Values)
            {
                if (type != typeToCheck)
                {
                    foreach (IContentType baseType in type.BaseTypes)
                    {
                        if (String.Compare(baseType.TypeName, typeToCheck.TypeName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            derivedType = type;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #region IContentTypeRegistryService Members

        public IContentType GetContentType(string typeName)
        {
            ContentTypeImpl contentType = null;
            lock (this.syncLock)
            {
                this.BuildContentTypes();
                this.contentTypes.TryGetValue(typeName.ToUpperInvariant(), out contentType);
            }

            return contentType;
        }

        public IContentType UnknownContentType
        {
            get { return ContentTypeRegistryImpl.unknownContentType; }
        }

        public IEnumerable<IContentType> ContentTypes
        {
            get
            {
                IList<ContentTypeImpl> types;
                lock (this.syncLock)
                {
                    // Get a copy of the content types known at this moment
                    this.BuildContentTypes();
                    types = new List<ContentTypeImpl>(this.contentTypes.Values);
                }

                foreach (IContentType contentType in types)
                {
                    yield return contentType;
                }
            }
        }

        public IContentType AddContentType(string typeName, IEnumerable<string> baseTypeNames)
        {
            lock (this.syncLock)
            {
                // Make sure first the content type map is built
                BuildContentTypes();

                ContentTypeImpl type;
                if (GetOrCreateContentType(typeName, out type))
                {
                    // Cannot dynamically add a new content type if a content type with the same name already exists
                    throw new ArgumentException(String.Format(System.Globalization.CultureInfo.CurrentUICulture, Strings.ContentTypeRegistry_CannotAddExistentType, typeName));
                }

                if (baseTypeNames != null)
                {
                    foreach (string baseTypeName in baseTypeNames)
                    {
                        AddBaseTypeOfType(baseTypeName, type, false);
                    }
                }

                // and add this new type
                this.contentTypes.Add(type.TypeName.ToUpperInvariant(), type);

                return type;
            }
        }

        public void RemoveContentType(string typeName)
        {
            lock (this.syncLock)
            {
                // Make sure first the content type map is built
                BuildContentTypes();
            }

            // Since file extension registry will call back the content type registry when it gets initialized,
            // make sure we force the initialization before obtaining the lock. This will guarantee we won't deadlock 
            // when we'll ask the registered extension later, after we obtained the content types lock
            FileExtensionRegistry.GetContentTypeForExtension("");
            string uppercaseTypeName = typeName.ToUpperInvariant();

            lock (this.syncLock)
            {
                ContentTypeImpl type = null;
                if (!this.contentTypes.TryGetValue(uppercaseTypeName, out type))
                {
                    // If the type was not registered, removal is successful
                    return;
                }

                // Check if the type to be removed is not the Unknown content type
                if (uppercaseTypeName == ContentTypeRegistryImpl.UnknownContentTypeName)
                {
                    throw new InvalidOperationException(Strings.ContentTypeRegistry_CannotRemoveTheUnknownType);
                }

                // Check if the type is base type for another registered type
                ContentTypeImpl derivedType;
                if (IsBaseType(type, out derivedType))
                {
                    throw new InvalidOperationException(String.Format(System.Globalization.CultureInfo.CurrentUICulture, Strings.ContentTypeRegistry_CannotRemoveBaseType, type.TypeName, derivedType.TypeName));
                }

                // If there are file extensions using this content type we won't allow removing it
                IEnumerable<string> extensions = FileExtensionRegistry.GetExtensionsForContentType(type);
                if (extensions != null && extensions.GetEnumerator().MoveNext())
                {
                    throw new InvalidOperationException(String.Format(System.Globalization.CultureInfo.CurrentUICulture, Strings.ContentTypeRegistry_CannotRemoveTypeUsedByFileExtensions, type.TypeName));
                }

                // Remove the content type
                this.contentTypes.Remove(uppercaseTypeName);
            }
        }

        #endregion
    }
}
