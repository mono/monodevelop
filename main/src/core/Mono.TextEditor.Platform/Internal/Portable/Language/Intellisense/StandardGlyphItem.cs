////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Describes icons or glyphs that are used in statement completion.
    /// </summary>
    public enum StandardGlyphItem
    {
        /// <summary>
        /// Describes a public symbol.
        /// </summary>
        GlyphItemPublic,

        /// <summary>
        /// Describes an internal symbol.
        /// </summary>
        GlyphItemInternal,

        /// <summary>
        /// Describes a friend symbol.
        /// </summary>
        GlyphItemFriend,

        /// <summary>
        /// Describes a protected symbol.
        /// </summary>
        GlyphItemProtected,
        
        /// <summary>
        /// Describes a private symbol.
        /// </summary>
        GlyphItemPrivate,
        
        /// <summary>
        /// Describes a shortcut symbol.
        /// </summary>
        GlyphItemShortcut,
        
        /// <summary>
        /// Describes a symbol that has all (or none) of the standard attributes.
        /// </summary>
        TotalGlyphItems
    }
}