using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Options applicable to CodeLens.
    /// </summary>
    public static class CodeLensOptions
    {
        /// <summary>
        /// The option that determines whether or not CodeLens as a whole is enabled.  When disabled,
        /// CodeLens UI does not appear inside any documents.
        /// </summary>
        public const string IsCodeLensEnabledOptionId = "IsCodeLensEnabled";
        public static readonly EditorOptionKey<bool> IsCodeLensEnabledOptionKey = new EditorOptionKey<bool>(IsCodeLensEnabledOptionId);

        /// <summary>
        /// The option that determines which specific CodeLens providers are disabled.  This option's value
        /// should be an array of names which correspond to ICodeLensDataPointProviders which should not be loaded.
        /// </summary>
        public const string CodeLensDisabledProvidersOptionId = "CodeLensDisabledProviders";
        public static readonly EditorOptionKey<string[]> CodeLensDisabledProvidersOptionKey = new EditorOptionKey<string[]>(CodeLensDisabledProvidersOptionId);
    }
}
