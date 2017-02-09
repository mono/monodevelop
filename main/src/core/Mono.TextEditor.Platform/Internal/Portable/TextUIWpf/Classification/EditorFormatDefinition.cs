// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Windows;
using System.Windows.Media;

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Provides format information for a particular item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEF component part, and should be exported as:
    /// [Export(typeof(EditorFormatDefinition))]
    /// </para>
    /// <para>
    /// Exporters must provide the attribute <see cref="NameAttribute"/>. The attributes <see cref="OrderAttribute"/>, 
    /// <see cref="DisplayNameAttribute"/> and <see cref="UserVisibleAttribute"/> may be provided optionally.
    /// </para>
    /// <para>
    /// Consumers of this attribute may optionally ignore the foreground formatting information, the background information,
    /// or both.
    /// </para>
    /// <para>
    /// It's recommended that exporters of this class prefix the <see cref="NameAttribute"/> with a unique string (e.g.
    /// their package name) to reduce the chance of conflict with another similarly named export.
    /// </para>
    /// </remarks>
    public abstract class EditorFormatDefinition
    {
        /// <summary>
        /// Defines the string used to look up the background brush value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string BackgroundBrushId = "Background";
        /// <summary>
        /// Defines the string used to look up the foreground brush value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string ForegroundBrushId = "Foreground";

        /// <summary>
        /// Defines the string used to look up the background color value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string BackgroundColorId = "BackgroundColor";

        /// <summary>
        /// Defines the string used to look up the foreground color value in the <see cref="ResourceDictionary"/>.
        /// </summary>
        public const string ForegroundColorId = "ForegroundColor";

        /// <summary>
        /// Gets or sets the foreground color for this item.
        /// </summary>
        /// <remarks>
        /// If the foreground brush is set, this color will be ignored.
        /// </remarks>
        public Color? ForegroundColor { get; protected set; }

        /// <summary>
        /// Gets or sets the background color for this item.
        /// </summary>
        /// <remarks>
        /// If the background brush is set, this color will be ignored.
        /// </remarks>
        public Color? BackgroundColor { get; protected set; }

        /// <summary>
        /// Gets or sets the background brush for this item.
        /// </summary>
        /// <remarks>
        /// This brush will override any background color that is set.  
        /// </remarks>
        public Brush BackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets or sets the foreground brush for this item.
        /// </summary>
        /// <remarks>
        /// This brush will override any foreground color that is set.
        /// </remarks>
        public Brush ForegroundBrush { get; protected set; }

        /// <summary>
        /// Determines whether the foreground of this format is customizable.
        /// </summary>
        public bool? ForegroundCustomizable { get; protected set; }

        /// <summary>
        /// Determines whether the background of this format is customizable.
        /// </summary>
        public bool? BackgroundCustomizable { get; protected set; }

        /// <summary>
        /// Defines the string used when displaying this format definition to the user. This property is only used
        /// if the <see cref="UserVisibleAttribute"/> is set to true on this object's export.
        /// </summary>
        public string DisplayName { get; protected set; }

        /// <summary>
        /// Creates a <see cref="ResourceDictionary"/> from this definition.
        /// </summary>
        /// <returns>A <see cref="ResourceDictionary"/> with the values from this definition.</returns>
        public ResourceDictionary CreateResourceDictionary()
        {
            return CreateResourceDictionaryFromDefinition();
        }

        /// <summary>
        /// Creates a <see cref="ResourceDictionary"/> from this definition.
        /// </summary>
        /// <returns>A <see cref="ResourceDictionary"/> with the values from this definition.</returns>
        protected virtual ResourceDictionary CreateResourceDictionaryFromDefinition()
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            Brush foregroundBrush = null;
            Brush backgroundBrush = null;

            if (this.ForegroundColor.HasValue)
            {
                resourceDictionary[EditorFormatDefinition.ForegroundColorId] = this.ForegroundColor;
                foregroundBrush = new SolidColorBrush(this.ForegroundColor.Value);
            }
            if (this.BackgroundColor.HasValue)
            {
                resourceDictionary[EditorFormatDefinition.BackgroundColorId] = this.BackgroundColor;
                backgroundBrush = new SolidColorBrush(this.BackgroundColor.Value);
            }
            if (this.ForegroundBrush != null)
            {
                // Clone so we can freeze it.
                foregroundBrush = this.ForegroundBrush.Clone();
            }
            if (this.BackgroundBrush != null)
            {
                // Clone so we can freeze it.
                backgroundBrush = this.BackgroundBrush.Clone();
            }

            if (foregroundBrush != null)
            {
                foregroundBrush.Freeze();
                resourceDictionary[EditorFormatDefinition.ForegroundBrushId] = foregroundBrush;
            }

            if (backgroundBrush != null)
            {
                backgroundBrush.Freeze();
                resourceDictionary[EditorFormatDefinition.BackgroundBrushId] = backgroundBrush;
            }

            return resourceDictionary;
        }
    }
}
