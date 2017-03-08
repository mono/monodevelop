//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
#if CODE_ANALYSIS_BASELINE
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "Tabify", Scope = "resource", Target = "Microsoft.VisualStudio.UI.Text.EditorOperations.Implementation.Strings.resources", Justification = "These names match the accepted terms for the operations")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "Untabify", Scope = "resource", Target = "Microsoft.VisualStudio.UI.Text.EditorOperations.Implementation.Strings.resources", Justification = "These names match the accepted terms for the operations")]
[module: SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#_textBufferUndoManager")]
[module: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope="type", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations")]
[module: SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#DeleteHorizontalWhitespace(Microsoft.VisualStudio.Text.ITextEdit,System.Collections.Generic.ICollection`1<Microsoft.VisualStudio.Text.Span>,System.Collections.Generic.ICollection`1<Microsoft.VisualStudio.Text.Span>,System.Collections.Generic.ICollection`1<Microsoft.VisualStudio.Text.Span>)")]
[module: SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#InsertText(System.String,System.Boolean,System.String)", Justification="This is, unfortunately, a rather complicated method.")]
[module: SuppressMessage("Microsoft.Maintainability","CA1506:AvoidExcessiveClassCoupling", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#InsertText(System.String,System.Boolean,System.String)", Justification="This is, unfortunately, a rather complicated method.")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="TextTransactionMergePolicy", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.TextTransactionMergePolicy.#CanMerge(Microsoft.VisualStudio.Text.Operations.ITextUndoTransaction,Microsoft.VisualStudio.Text.Operations.ITextUndoTransaction)", Justification="This is valid")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="MergePolicy", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.TextTransactionMergePolicy.#CanMerge(Microsoft.VisualStudio.Text.Operations.ITextUndoTransaction,Microsoft.VisualStudio.Text.Operations.ITextUndoTransaction)", Justification="This is valid")]

//ToDo: To be looked at
[module: SuppressMessage("Microsoft.Globalization","CA1303:Do not pass literals as localized parameters", MessageId="Insert", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#Untabify()", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Globalization","CA1303:Do not pass literals as localized parameters", MessageId="Insert", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#Tabify()", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#Unindent()", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Maintainability","CA1502:AvoidExcessiveComplexity", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#Backspace()", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1822:MarkMembersAsStatic", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#MegaBytesRequiredForCurrentLineText(Microsoft.VisualStudio.Text.Editor.DisplayTextRange)", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#ReplaceHelper(Microsoft.VisualStudio.Text.NormalizedSpanCollection,System.String)", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations.#ReplaceHelper(Microsoft.VisualStudio.Text.VirtualSnapshotSpan,System.String)", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.TextStructureNavigatorSelectorService.#set__contentTypeRegistryService(Microsoft.VisualStudio.Utilities.IContentTypeRegistryService)", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.TextStructureNavigatorSelectorService.#set__guardedOperations(Microsoft.VisualStudio.Text.Utilities.GuardedOperations)", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.Operations.Implementation.TextStructureNavigatorSelectorService.#set__textStructureNavigatorProviders(System.Collections.Generic.List`1<System.Lazy`2<Microsoft.VisualStudio.Text.Operations.ITextStructureNavigatorProvider,Microsoft.VisualStudio.Text.Utilities.IContentTypeMetadata>>)", Justification="ToDo: To be looked at")]

#endif
