using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods
{
    /// <summary>
    ///  Provides extension methods for options related to an <see cref="IWpfTextView"/>.
    /// </summary>
    public static class WpfViewOptionExtensions
    {
        #region Extension methods

        /// <summary>
        /// Determines whether the option to highlight the current line is enabled.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if the highlight option was enabled, otherwise <c>false</c>.</returns>
        public static bool IsHighlightCurrentLineEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            return options.GetOptionValue<bool>(DefaultWpfViewOptions.EnableHighlightCurrentLineId);
        }

        /// <summary>
        /// Determines whether the option to draw a gradient selection is enabled.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if the draw selection gradient option was enabled, otherwise <c>false</c>.</returns>
        public static bool IsSimpleGraphicsEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            return options.GetOptionValue<bool>(DefaultWpfViewOptions.EnableSimpleGraphicsId);
        }

        /// <summary>
        ///  Determines whether to allow mouse wheel zooming
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the mouse wheel zooming is enabled, otherwise <c>false</c>.</returns>
        /// <remarks>Disabling the mouse wheel zooming does NOT turn off Zooming (it disables zooming using mouse wheel)</remarks>
        public static bool IsMouseWheelZoomEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            return options.GetOptionValue<bool>(DefaultWpfViewOptions.EnableMouseWheelZoomId);
        }

        /// <summary>
        /// Specifies the appearance category.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns>The appearance category, which determines where to look up font properties and colors.</returns>
        public static string AppearanceCategory(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            return options.GetOptionValue<string>(DefaultWpfViewOptions.AppearanceCategory);
        }

        /// <summary>
        /// Specifies the persisted zoomlevel.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns>The zoomlevel, which scales the view up or down.</returns>
        public static double ZoomLevel(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            return options.GetOptionValue<double>(DefaultWpfViewOptions.ZoomLevelId);
        }
        #endregion
    }
}

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents common <see cref="IWpfTextView"/> options.
    /// </summary>
    public static class DefaultWpfViewOptions
    {
        #region Option identifiers
        /// <summary>
        /// Determines whether to enable the highlight current line adornment.
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableHighlightCurrentLineId = new EditorOptionKey<bool>(EnableHighlightCurrentLineName);
        public const string EnableHighlightCurrentLineName = "Adornments/HighlightCurrentLine/Enable";

        /// <summary>
        /// Determines whether to enable the highlight current line adornment.
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableSimpleGraphicsId = new EditorOptionKey<bool>(EnableSimpleGraphicsName);
        public const string EnableSimpleGraphicsName = "Graphics/Simple/Enable";

        /// <summary>
        /// Determines whether the opacity of text markers and selection is reduced in high contrast mode.
        /// </summary>
        public static readonly EditorOptionKey<bool> UseReducedOpacityForHighContrastOptionId = new EditorOptionKey<bool>(UseReducedOpacityForHighContrastOptionName);
        public const string UseReducedOpacityForHighContrastOptionName = "UseReducedOpacityForHighContrast";

        /// <summary>
        /// Determines whether to enable mouse wheel zooming
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableMouseWheelZoomId = new EditorOptionKey<bool>(EnableMouseWheelZoomName);
        public const string EnableMouseWheelZoomName = "TextView/MouseWheelZoom";

        /// <summary>
        /// Determines the appearance category of a view, which selects a ClassificationFormatMap and EditorFormatMap.
        /// </summary>
        public static readonly EditorOptionKey<string> AppearanceCategory = new EditorOptionKey<string>(AppearanceCategoryName);
        public const string AppearanceCategoryName = "Appearance/Category";

        /// <summary>
        /// Determines the view zoom level.
        /// </summary>
        public static readonly EditorOptionKey<double> ZoomLevelId = new EditorOptionKey<double>(ZoomLevelName);
        public const string ZoomLevelName = "TextView/ZoomLevel";
        #endregion
    }

    /// <summary>
    /// Represents the option to highlight the current line.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultWpfViewOptions.EnableHighlightCurrentLineName)]
    public sealed class HighlightCurrentLineOption : WpfViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default { get { return false; }}

        /// <summary>
        /// Gets the key for the highlight current line option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultWpfViewOptions.EnableHighlightCurrentLineId; } }
    }

    /// <summary>
    /// Represents the option to draw a selection gradient as opposed to a solid color selection.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultWpfViewOptions.EnableSimpleGraphicsName)]
    public sealed class SimpleGraphicsOption : WpfViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the key for the simple graphics option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultWpfViewOptions.EnableSimpleGraphicsId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionName)]
    public sealed class UseReducedOpacityForHighContrastOption : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the key for the use reduced opacity option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId; } }
    }

    /// <summary>
    /// Defines the option to enable the mouse wheel zoom
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultWpfViewOptions.EnableMouseWheelZoomName)]
    public sealed class MouseWheelZoomEnabled : WpfViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the wpf text view  value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultWpfViewOptions.EnableMouseWheelZoomId; } }
    }

    /// <summary>
    /// Defines the appearance category.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultWpfViewOptions.AppearanceCategoryName)]
    public sealed class AppearanceCategoryOption : WpfViewOptionDefinition<string>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override string Default { get { return "text"; } }

        /// <summary>
        /// Gets the key for the appearance category option.
        /// </summary>
        public override EditorOptionKey<string> Key { get { return DefaultWpfViewOptions.AppearanceCategory; } }
    }

    /// <summary>
    /// Defines the zoomlevel.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultWpfViewOptions.ZoomLevelName)]
    public sealed class ZoomLevel : WpfViewOptionDefinition<double>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override double Default { get { return (int)ZoomConstants.DefaultZoom; } }

        /// <summary>
        /// Gets the key for the text view zoom level.
        /// </summary>
        public override EditorOptionKey<double> Key { get { return DefaultWpfViewOptions.ZoomLevelId; } }
    }
}
