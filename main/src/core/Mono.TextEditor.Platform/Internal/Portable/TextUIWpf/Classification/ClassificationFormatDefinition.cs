// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Provides classification format information for a particular classification type.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be exported as:
    /// [Export(typeof(EditorFormatDefinition))]</para>
    /// <para>
    /// Exporters must provide the attributes ClassificationTypeAttribute and NameAttribute. The attributes OrderAttribute, 
    /// DisplayNameAttribute and UserVisibleAttribute may be provided optionally.
    /// </para>
    /// </remarks>
    public abstract class ClassificationFormatDefinition : EditorFormatDefinition
    {
        /// <summary>
        /// Defines the string used to look up the bold value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string IsBoldId = "IsBold";

        /// <summary>
        /// Defines the string used to look up the italic value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string IsItalicId = "IsItalic";

        /// <summary>
        /// Defines the string used to look up the font hinting size value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string FontHintingSizeId = "FontHintingSize";

        /// <summary>
        /// Defines the string used to look up the font rendering size value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string FontRenderingSizeId = "FontRenderingSize";

        /// <summary>
        /// Defines the string used look up the text effects value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string TextEffectsId = "TextEffects";

        /// <summary>
        /// Defines the string used to look up the text decorations value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string TextDecorationsId = "TextDecorations";

        /// <summary>
        /// Defines the string used o look up the typeface value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string TypefaceId = "Typeface";

        /// <summary>
        /// Defines the string used o look up the foreground opacity value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string ForegroundOpacityId = "ForegroundOpacity";

        /// <summary>
        /// Defines the string used to look up the background opacity value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string BackgroundOpacityId = "BackgroundOpacity";

        /// <summary>
        /// Defines the default opacity used for the background color/brush if no <see cref="BackgroundOpacityId"/> entities are defined.
        /// </summary>
        public const double DefaultBackgroundOpacity = 0.8;

        /// <summary>
        /// Defines the default opacity used for the background color/brush in high contrast themes.
        /// </summary>
        public const double DefaultHighContrastBackgroundOpacity = 0.5;

        /// <summary>
        /// Defines the string used to look up the <see cref="CultureInfo"/> value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string CultureInfoId = "CultureInfo";

        /// <summary>
        /// Gets or sets the <see cref="CultureInfo"/> for this classification format.
        /// </summary>
        public CultureInfo CultureInfo { get; protected set; }

        /// <summary>
        /// Gets or sets the the font hinting size for this classification format.
        /// </summary>
        public double? FontHintingSize { get; protected set; }

        /// <summary>
        /// Gets or sets the the font rendering size for this classification format.
        /// </summary>
        public double? FontRenderingSize { get; protected set; }

        /// <summary>
        /// Gets or sets the the <see cref="TextEffectCollection"/> for this classification format.
        /// </summary>
        public TextEffectCollection TextEffects { get; protected set; }

        /// <summary>
        /// Gets or sets the <see cref="TextDecorationCollection"/> for this classification format.
        /// </summary>
        public TextDecorationCollection TextDecorations { get; protected set; }

        /// <summary>
        /// Gets or sets the <see cref="Typeface"/> for this classification format.
        /// </summary>
        /// <remarks>
        /// Certain values (bold, italic) in this typeface can be overridden by
        /// other format definitions that have a higher priority.
        /// </remarks>
        public Typeface FontTypeface { get; protected set; }

        /// <summary>
        /// Gets or sets the opacity of the foreground.
        /// </summary>
        /// <remarks>
        /// This value overrides the opacity settings in the 
        /// ForegroundBrush property if this classification format has a higher priority.
        /// </remarks>
        public double? ForegroundOpacity { get; protected set; }

        /// <summary>
        /// Gets or sets the opacity of the background.
        /// </summary>
        /// <remarks>
        /// This value will override the opacity settings in the 
        /// BackgroundBrush property if this classification format has a higher priority.
        /// </remarks>
        public double? BackgroundOpacity { get; protected set; }

        /// <summary>
        /// Determines whether the text should be bold.
        /// </summary>
        /// <remarks>
        /// This value overrides the bold settings in the 
        /// <see cref="FontTypeface"/> property if this classification format has a higher priority.
        /// </remarks>
        public bool? IsBold { get; protected set; }

        /// <summary>
        /// Determines whether the text should be italic.
        /// </summary>
        /// <remarks>
        /// This value will override the italic settings in the 
        /// <see cref="FontTypeface"/> property if this classification format has a higher priority.
        /// </remarks>
        public bool? IsItalic { get; protected set; }

        /// <summary>
        /// Creates a <see cref="ResourceDictionary"/> using this definition.
        /// </summary>
        /// <returns>A <see cref="ResourceDictionary"/> with the values from this definition.</returns>
        protected override ResourceDictionary CreateResourceDictionaryFromDefinition()
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary();

            AddOverridableProperties(resourceDictionary);

            if (this.ForegroundBrush != null)
            {
                resourceDictionary[EditorFormatDefinition.ForegroundBrushId] = this.ForegroundBrush;

                // Only set Opacity if it isn't the default value of 1.0
                if (this.ForegroundBrush.Opacity != 1.0)
                    resourceDictionary[ClassificationFormatDefinition.ForegroundOpacityId] = this.ForegroundBrush.Opacity;
            }
            if (this.BackgroundBrush != null)
            {
                resourceDictionary[EditorFormatDefinition.BackgroundBrushId] = this.BackgroundBrush;

                // Only set Opacity if it isn't the default value of 1.0
                if (this.BackgroundBrush.Opacity != 1.0)
                    resourceDictionary[ClassificationFormatDefinition.BackgroundOpacityId] = this.BackgroundBrush.Opacity;
            }
            if (this.FontTypeface != null)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.TypefaceId, this.FontTypeface);

                // Only set bold/italic to true if they are true in the typeface
                if (this.FontTypeface.Weight == FontWeights.Bold)
                    resourceDictionary[ClassificationFormatDefinition.IsBoldId] = true;
                if (this.FontTypeface.Style == FontStyles.Italic)
                    resourceDictionary[ClassificationFormatDefinition.IsItalicId] = true;
                
            }
            if (this.FontRenderingSize.HasValue)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.FontRenderingSizeId, this.FontRenderingSize.Value);
            }
            if (this.FontHintingSize.HasValue)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.FontHintingSizeId, this.FontHintingSize.Value);
            }

            if (this.TextDecorations != null)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.TextDecorationsId, this.TextDecorations);
            }
            if (this.TextEffects != null)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.TextEffectsId, this.TextEffects);
            }

            if (this.CultureInfo != null)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.CultureInfoId, this.CultureInfo);
            }

            return resourceDictionary;
        }

        /// <summary>
        /// Adds properties to a resource dictionary from a <see cref="ClassificationFormatDefinition"/> that can be overridden by other properties.
        /// </summary>
        private void AddOverridableProperties(ResourceDictionary resourceDictionary)
        {
            if (this.ForegroundOpacity.HasValue)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.ForegroundOpacityId, this.ForegroundOpacity.Value);
            }
            if (this.BackgroundOpacity.HasValue)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.BackgroundOpacityId, this.BackgroundOpacity.Value);
            }
            if (this.IsBold.HasValue)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.IsBoldId, this.IsBold.Value);
            }
            if (this.IsItalic.HasValue)
            {
                resourceDictionary.Add(ClassificationFormatDefinition.IsItalicId, this.IsItalic.Value);
            }
            if (this.ForegroundColor.HasValue)
            {
                resourceDictionary[EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush(this.ForegroundColor.Value);
            }
            if (this.BackgroundColor.HasValue)
            {
                resourceDictionary[EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush(this.BackgroundColor.Value);
            }
        }
    }
}
