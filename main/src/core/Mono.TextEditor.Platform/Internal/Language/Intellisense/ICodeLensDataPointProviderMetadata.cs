using System.ComponentModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Metadata interface used for consuming ICodeLensDataPointProvider imports.
    /// </summary>
    public interface ICodeLensDataPointProviderMetadata
    {
        /// <summary>
        /// Gets the uniquely-identifying name of the data point provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the localized name of the data point provider.
        /// </summary>
        [DefaultValue(null)]
        string LocalizedName { get; }

        /// <summary>
        /// Gets the priority of the data point provider.  Lower value items will come first in
        /// the default ordering of data in the UI.
        /// </summary>
        [DefaultValue(int.MaxValue)]
        int Priority { get; }

        /// <summary>
        /// Determines if the user can modify the indicator option setting.
        /// </summary>
        [DefaultValue(true)]
        bool OptionModifiable { get; }

        /// <summary>
        /// Determines if the indicator option is visible.
        /// </summary>
        [DefaultValue(true)]
        bool OptionVisible { get; }
    }
}
