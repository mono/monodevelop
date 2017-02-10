#if CODE_ANALYSIS_BASELINE
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Design","CA1062:Validate arguments of public methods", MessageId="0", Scope="member", Target="Microsoft.VisualStudio.Text.EditorOptions.Implementation.EditorOptionsFactoryService.#GetOptions(Microsoft.VisualStudio.Utilities.IPropertyOwner)", Justification="No need to validate in this case")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.EditorOptions.Implementation.EditorOptionsFactoryService.#set_OptionImports(System.Collections.Generic.List`1<System.Lazy`1<Microsoft.VisualStudio.Text.Editor.EditorOptionDefinition>>)", Justification="ToDo: To be looked at")]

#endif
