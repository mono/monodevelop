// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Predefined Peek relationships.
    /// </summary>
    public static class PredefinedPeekRelationships
    {
        /// <summary>
        /// A relationship describing a connection between an <see cref="IPeekableItem"/> and its definition.
        /// </summary>
        public static IPeekRelationship Definitions = new DefinitionRelationship();

        private class DefinitionRelationship : IPeekRelationship
        {
            public string Name
            {
                get { return "IsDefinedBy"; }
            }

            public string DisplayName
            {
                get { return "Is Defined By"; }
            }
        }
    }
}
