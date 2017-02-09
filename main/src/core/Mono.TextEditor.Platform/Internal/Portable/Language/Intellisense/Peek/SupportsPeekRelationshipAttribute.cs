// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Use this attribute to specify that an <see cref="IPeekableItemSourceProvider"/> supports a specific <see cref="IPeekRelationship"/>.
    /// </summary>
    public sealed class SupportsPeekRelationshipAttribute : MultipleBaseMetadataAttribute
    {
        /// <summary>
        /// Construct a new instance of the attribute.
        /// </summary>
        /// <param name="relationshipName">The name of the relationship that we want to mark as supported.</param>
        /// <exception cref="ArgumentNullException"><paramref name="relationshipName"/> is null or empty.</exception>
        public SupportsPeekRelationshipAttribute(string relationshipName)
        {
            if (string.IsNullOrEmpty(relationshipName))
            {
                throw new ArgumentNullException("relationship");
            }

            this.SupportedRelationships = relationshipName;
        }

        /// <summary>
        /// The supported relationship
        /// </summary>
        public string SupportedRelationships
        {
            get; private set;
        }
    }
}
