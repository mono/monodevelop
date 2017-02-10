#if CODE_ANALYSIS_BASELINE
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tagger", Scope = "type", Target = "Microsoft.VisualStudio.Text.Tagging.ITaggerMetadata", Justification = "This isn't misspelled.")]

//ToDo: To be looked at
[module: SuppressMessage("Microsoft.Design","CA1020:AvoidNamespacesWithFewTypes", Scope="namespace", Target="Microsoft.VisualStudio.Text.Tagging", Justification="ToDo: To be looked at")]

#endif

