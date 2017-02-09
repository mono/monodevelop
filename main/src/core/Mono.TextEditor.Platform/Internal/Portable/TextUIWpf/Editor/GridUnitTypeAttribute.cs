// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.ComponentModel.Composition;
    using System.Windows;

    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// This class associates a <see cref="GridUnitType"/> value with a MEF export.
    /// </summary>
    /// <remarks>
    /// The value of this attribute will be used by the consumers to decide rendering behavior for the exported
    /// object. The rendering behavior will match the behavior defined in WPF classes (e.g. <see cref="System.Windows.Controls.Grid"/>)
    /// that interact with <see cref="GridUnitType"/>.
    /// </remarks>
    /// <example>
    /// [Export(typeof(IWpfTextViewMarginProvider))]
    /// [Name(PredefinedMarginNames.VerticalScrollBar)]
    /// [MarginContainer(PredefinedMarginNames.VerticalScrollBarContainerMargin)]
    /// [ContentType("text")]
    /// [TextViewRole(PredefinedTextViewRoles.Interactive)]
    /// [GridUnitType(GridUnitType.Star)] //this size is determined as a weighted proportion of available space
    /// internal sealed class VerticalScrollBarMarginProvider : IWpfTextViewMarginProvider { */ implementation /* }
    /// </example>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class GridUnitTypeAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Gets the <see cref="GridUnitType"/>.
        /// </summary>
        public GridUnitType GridUnitType { get; private set; }

        /// <summary>
        /// Constructs a <see cref="GridUnitTypeAttribute"/>.
        /// </summary>
        /// <param name="gridUnitType">The <see cref="GridUnitType"/>.</param>
        public GridUnitTypeAttribute(GridUnitType gridUnitType)
        {
            this.GridUnitType = gridUnitType;
        }
    }
}
