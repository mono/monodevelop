// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification
{

    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Tagging;

    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Provides coloring information for text markers.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be exported as:
    /// [Export(typeof(EditorFormatDefinition))]</para>
    /// <para>
    /// Exporters must provide the attribute NameAttribute.
    /// </para>
    /// <para>
    /// The <see cref="MarkerFormatDefinition"/> is consumed by the default visual manager for the <see cref="TextMarkerTag"/>. The <see cref="TextMarkerTag.Type"/> property
    /// should correspond to the <see cref="NameAttribute"/> of this export so that the desired color can be correctly loaded for the text marker.
    /// </para>
    /// <para>
    /// If you wish your <see cref="MarkerFormatDefinition"/> to interact with Visual Studio, then set the <see cref="UserVisibleAttribute"/> on your export to true
    /// and make sure you set <see cref="EditorFormatDefinition.BackgroundColor"/> and <see cref="EditorFormatDefinition.ForegroundColor"/>. The foreground brush will
    /// be used to draw the border and the background brush will be used to draw the fill.
    /// </para>
    /// <example>
    /// [Export(typeof(EditorFormatDefinition))]
    /// [Name("MarkerFormatDefinition/RedMarker")]
    /// [UserVisible(true)]
    /// private VisualStudioRedMarker : MarkerFormatDefinition
    /// {
    ///     VisualStudioRedMarker() 
    ///     {
    ///         this.BackgroundColor = Colors.Red;
    ///         this.ForegroundColor = Colors.Blue;
    ///         this.DisplayName = "Red Marker"; //this value should be localized
    ///         this.ZOrder = 5;
    ///     }
    /// }
    /// </example>
    /// </remarks>
    public abstract class MarkerFormatDefinition : EditorFormatDefinition
    {
        /// <summary>
        /// Defines the string used to lookup the z-order value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string ZOrderId = "MarkerFormatDefinition/ZOrderId";

        /// <summary>
        /// Defines the string used to lookup the fill brush value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string FillId = "MarkerFormatDefinition/FillId";

        /// <summary>
        /// Defines the string used to look up the border pen value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string BorderId = "MarkerFormatDefinition/BorderId";

        /// <summary>
        /// The Z-Order is used as the Z-Order of the marker when it's drawn on the text marker adornment layer. This property can be used to specify
        /// in which order multiple markers should be drawn when they all overlap the same span of text.
        /// </summary>
        protected int ZOrder { get; set; }

        /// <summary>
        /// The brush is used to paint the inner body of the text marker.
        /// </summary>
        protected Brush Fill { get; set; }

        /// <summary>
        /// The pen is used to draw the border of the text marker.
        /// </summary>
        /// <remarks>
        /// This property is optional and can be null, in which case no pen will be used for the border of the text marker adornment.
        /// </remarks>
        protected Pen Border { get; set; }

        /// <summary>
        /// Creates a <see cref="ResourceDictionary"/> using this definition.
        /// </summary>
        /// <returns>A <see cref="ResourceDictionary"/> with the values from this definition.</returns>
        protected override ResourceDictionary CreateResourceDictionaryFromDefinition()
        {
            ResourceDictionary result = base.CreateResourceDictionaryFromDefinition();

            if (this.Border != null)
                result[MarkerFormatDefinition.BorderId] = this.Border;

            result[MarkerFormatDefinition.FillId] = this.Fill;
            result[MarkerFormatDefinition.ZOrderId] = this.ZOrder;

            return result;
        }
    }
}
