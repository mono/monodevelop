// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.ComponentModel.Composition;
    using System.Windows;

    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// This class associates a grid cell size with a MEF export.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value of this attribute will be used by the consumers to decide rendering behavior for the exported
    /// object. The rendering behavior will match the behavior defined in WPF classes (e.g. <see cref="System.Windows.Controls.Grid"/>)
    /// that interact with <see cref="GridLength"/>.
    /// </para>
    /// <para>
    /// This class is used in combination with <see cref="GridUnitTypeAttribute"/> to create a <see cref="GridLength"/> for a cell
    /// in a <see cref="System.Windows.Controls.Grid"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// [Export(typeof(IWpfTextViewMarginProvider))]
    /// [Name(PredefinedMarginNames.VerticalScrollBar)]
    /// [MarginContainer(PredefinedMarginNames.VerticalScrollBarContainerMargin)]
    /// [ContentType("text")]
    /// [TextViewRole(PredefinedTextViewRoles.Interactive)]
    /// [GridUnitType(GridUnitType.Pixel)] //this size is expressed as a pixel using the GridCellLength attribute
    /// [GridCellLength(15)] //15 pixels wide
    /// internal sealed class VerticalScrollBarMarginProvider : IWpfTextViewMarginProvider { */ implementation /* }
    /// </example>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class GridCellLengthAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Gets the grid cell length.
        /// </summary>
        public double GridCellLength { get; private set; }

        /// <summary>
        /// Constructs a <see cref="GridCellLengthAttribute"/>.
        /// </summary>
        /// <param name="cellLength">The length of the grid cell.</param>
        public GridCellLengthAttribute(double cellLength)
        {
            this.GridCellLength = cellLength;
        }
    }
}
