#if CODE_ANALYSIS_BASELINE
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Scope="", Target="microsoft.visualstudio.logic.text.classification.lookup.implementation.dll", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationTypeRegistryService.#CreateTransientClassificationType(Microsoft.VisualStudio.Text.Classification.IClassificationType[])", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationTypeRegistryService.#CreateTransientClassificationType(System.Collections.Generic.IEnumerable`1<Microsoft.VisualStudio.Text.Classification.IClassificationType>)", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope="member", Target="Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationTypeRegistryService.#singleton", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope="member", Target="Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationTypeRegistryService.#textContribution", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope="member", Target="Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationTypeRegistryService.#transientClassificationType", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Performance","CA1812:AvoidUninstantiatedInternalClasses", Scope="type", Target="Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationTypeRegistryService", Justification="Instantiated by the component model")]

[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.VisualStudio.Text.Classification.Implementation")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "LookUp", Scope = "module", Target = "microsoft.visualstudio.logic.text.classification.lookup.implementation.dll")]

#endif
