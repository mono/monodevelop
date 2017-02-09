////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Globalization;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Describes the icon to use for displaying items in statement completion.
    /// </summary>
    public class IconDescription
    {
        private StandardGlyphGroup group;
        private StandardGlyphItem item;

        /// <summary>
        /// Gets the <see cref="StandardGlyphGroup"/> of the icon to be displayed.
        /// </summary>
        public StandardGlyphGroup Group
        {
            get { return this.group; }
        }

        /// <summary>
        /// Gets the specific <see cref="StandardGlyphItem"/> within the icon group to be displayed.
        /// </summary>
        public StandardGlyphItem Item
        {
            get { return this.item; }
        }

        /// <summary>
        /// Initializes a new instance of an <see cref="IconDescription"/> from a group and an item within the group.
        /// </summary>
        /// <param name="group">The icon group of the icon to be displayed.</param>
        /// <param name="item">The specific icon within the icon group to be displayed.</param>
        public IconDescription(StandardGlyphGroup group, StandardGlyphItem item)
        {
            this.group = group;
            this.item = item;
        }

        /// <summary>
        /// Provides a description of the specific icon. 
        /// </summary>
        /// <returns>Group.Item</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", group.ToString(), item.ToString());
        }
    }
}