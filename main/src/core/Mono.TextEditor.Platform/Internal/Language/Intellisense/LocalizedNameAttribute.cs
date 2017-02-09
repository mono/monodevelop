using Microsoft.VisualStudio.Utilities;
using System;
using System.Resources;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an attribute which can provide a localized name as metadata for a MEF extension.
    /// </summary>
    public sealed class LocalizedNameAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Note: the localized name is cached rather than the type to prevent
        /// MEF from referencing the type in its cache.  Types exposed as metadata
        /// cause MEF to load the assembly containing the type during composition.
        /// </summary>
        private readonly string localizedName;

        /// <summary>
        /// Creates an instance of this attribute, which caches the localized name represented
        /// by the given type and resource name.
        /// </summary>
        /// <param name="type">The type from which to load the localized resource.  This should
        /// be a type created by the resource designer.</param>
        /// <param name="resourceId">The name of the localized resource string contained the
        /// resource type.</param>
        public LocalizedNameAttribute(Type type, string resourceId)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (resourceId == null)
            {
                throw new ArgumentNullException("resourceId");
            }

            ResourceManager resourceManager = new ResourceManager(type);
            this.localizedName = resourceManager.GetString(resourceId);
        }

        /// <summary>
        /// Creates an instance of this attribute, which caches the localized name represented
        /// by the given type and resource name.
        /// </summary>
        /// <param name="type">The type from which to load the localized resource.</param>
        /// <param name="resourceStreamName">The base name of the resource stream containing the resource.</param>
        /// <param name="resourceId">The name of the localized resource string contained the
        /// resource type.</param>
        public LocalizedNameAttribute(Type type, string resourceStreamName, string resourceId)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (resourceStreamName == null)
            {
                throw new ArgumentNullException("resourceStreamName");
            }
            if (resourceId == null)
            {
                throw new ArgumentNullException("resourceId");
            }

            ResourceManager resourceManager = new ResourceManager(resourceStreamName, type.Assembly);
            this.localizedName = resourceManager.GetString(resourceId);
        }

        /// <summary>
        /// Gets the localized name specified by the constructor.
        /// </summary>
        public string LocalizedName
        {
            get
            {
                return this.localizedName;
            }
        }
    }
}
