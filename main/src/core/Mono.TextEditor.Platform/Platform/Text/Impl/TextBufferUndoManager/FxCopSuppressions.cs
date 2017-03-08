//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
#if CODE_ANALYSIS_BASELINE
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Scope="", Target="microsoft.visualstudio.logic.text.bufferundomanager.implementation.dll", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#.ctor(Microsoft.VisualStudio.Text.ITextBuffer,System.Collections.Generic.IList`1<Microsoft.VisualStudio.Text.ITextChange>)", MessageId="", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope="type", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferUndoManagerProvider", Justification="BASELINE: Original port of VisualStudio to ToolPlat")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="TextBufferChangeUndoPrimitive", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#get_TextBuffer()", Justification="Member name")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="ITextUndoHistory", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#get_TextBuffer()", Justification="Member name")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="ITextBuffer", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#get_TextBuffer()", Justification="Member name")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="AttachedToNewBuffer", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#set_AttachedToNewBuffer(System.Boolean)", Justification="Member name")]

//ToDo: To be looked at
[module: SuppressMessage("Microsoft.Reliability","CA2000:Dispose objects before losing scope", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferUndoManagerProvider.#GetTextBufferUndoManager(Microsoft.VisualStudio.Text.ITextBuffer)", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="readonly", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#Do()", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Naming","CA2204:Literals should be spelled correctly", MessageId="readonly", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferChangeUndoPrimitive.#Undo()", Justification="ToDo: To be looked at")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferUndoManagerProvider.#set__undoHistoryRegistry(Microsoft.VisualStudio.Text.Operations.ITextUndoHistoryRegistry)", Justification="ToDo: To be looked at")]

#endif
