//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System.ComponentModel;
    using System.Windows;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Utilities;

    public interface IWpfTextViewMarginMetadata : IOrderable, IContentTypeAndTextViewRoleMetadata
    {
        /// <summary>
        /// Gets the name of the margin that contains this margin.
        /// </summary>
        string MarginContainer { get; }

        [DefaultValue(null)]
        IEnumerable<string> Replaces { get; }

        /// <summary>
        /// Optional OptionName that controls creation and visibility of the margin.
        /// </summary>
        [DefaultValue(null)]
        string OptionName { get; }

        /// <summary>
        /// Gets the grid unit type to be used for drawing of this element in the container margin's grid.
        /// </summary>
        [DefaultValue(GridUnitType.Auto)]
        GridUnitType GridUnitType { get; }

        /// <summary>
        /// Gets the size of the grid cell in which the margin should be placed.
        /// </summary>
        [DefaultValue(1.0)]
        double GridCellLength { get; }
    }
}
