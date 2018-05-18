//
// CSharpCompletionTextEditorExtension.IDebuggerCompletionProvider.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Debugger;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Completion
{
	partial class CSharpCompletionTextEditorExtension : IDebuggerCompletionProvider
	{
		class DebuggerIntelliSenseWorkspace : Workspace
		{
			public DebuggerIntelliSenseWorkspace (Solution solution) : base (solution.Workspace.Services.HostServices, "DebbugerIntellisense")
			{
				base.SetCurrentSolution (solution);
			}

			public void OpenDocument (DocumentId documentId, SourceTextContainer textContainer)
			{
				base.OnDocumentOpened (documentId, textContainer, true);
			}
		}

		static int GetAdjustedContextPoint (int contextPoint, Document document)
		{
			// Determine the position in the buffer at which to end the tracking span representing
			// the part of the imaginary buffer before the text in the view. 
			var tree = document.GetSyntaxTreeSynchronously (CancellationToken.None);
			var token = tree.FindTokenOnLeftOfPosition (contextPoint, CancellationToken.None);

			// Special case to handle class designer because it asks for debugger IntelliSense using
			// spans between members.
			if (contextPoint > token.Span.End &&
				token.IsKindOrHasMatchingText (SyntaxKind.CloseBraceToken) &&
				token.Parent.IsKind (SyntaxKind.Block) &&
				token.Parent.Parent is MemberDeclarationSyntax) {
				return contextPoint;
			}

			if (token.IsKindOrHasMatchingText (SyntaxKind.CloseBraceToken) &&
				token.Parent.IsKind (SyntaxKind.Block)) {
				return token.SpanStart;
			}

			return token.FullSpan.End;
		}

		public async Task<Mono.Debugging.Client.CompletionData> GetExpressionCompletionData (string exp, Mono.Debugging.Client.StackFrame frame, CancellationToken token)
		{
			var location = frame.SourceLocation;
			var document = DocumentContext.AnalysisDocument;
			if (document == null)
				return null;
			var solution = DocumentContext.RoslynWorkspace.CurrentSolution;
			var originalSnapshot = Editor.TextView.TextBuffer.CurrentSnapshot;
			var text = originalSnapshot.GetText (new Span (0, originalSnapshot.Length));
			var insertOffset = GetAdjustedContextPoint (originalSnapshot.GetLineFromLineNumber (location.EndLine - 1).Start.Position + location.EndColumn - 1, document);
			text = text.Insert (insertOffset, ";" + exp + ";");
			insertOffset++;//advance for 1 which represents `;` before expression
			var textBuffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (text, Editor.TextView.TextBuffer.ContentType);
			var snapshot = textBuffer.CurrentSnapshot;

			try {
				//Workaround Mono bug: https://github.com/mono/mono/issues/8700
				snapshot.AsText ();
			} catch (Exception) {
			}

			// Fork the solution using this new primary buffer for the document and all of its linked documents.
			var forkedSolution = solution.WithDocumentText (document.Id, snapshot.AsText (), PreservationMode.PreserveIdentity);
			foreach (var link in document.GetLinkedDocumentIds ()) {
				forkedSolution = forkedSolution.WithDocumentText (link, snapshot.AsText (), PreservationMode.PreserveIdentity);
			}

			// Put it into a new workspace, and open it and its related documents
			// with the projection buffer as the text.
			var forkedWorkspace = new DebuggerIntelliSenseWorkspace (forkedSolution);
			forkedWorkspace.OpenDocument (document.Id, textBuffer.AsTextContainer ());
			foreach (var link in document.GetLinkedDocumentIds ()) {
				forkedWorkspace.OpenDocument (link, textBuffer.AsTextContainer ());
			}
			var cs = forkedWorkspace.Services.GetLanguageServices (LanguageNames.CSharp).GetService<CompletionService> ();
			var trigger = new CompletionTrigger (CompletionTriggerKind.Invoke, '\0');
			var roslynCompletions = await cs.GetCompletionsAsync (forkedWorkspace.CurrentSolution.GetDocument (document.Id), insertOffset + exp.Length, trigger, cancellationToken: token).ConfigureAwait (false);
			if (roslynCompletions == null)
				return null;
			var result = new Mono.Debugging.Client.CompletionData ();
			foreach (var roslynCompletion in roslynCompletions.Items) {
				if (roslynCompletion.Tags.Contains (CompletionTags.Snippet))
					continue;
				result.Items.Add (new Mono.Debugging.Client.CompletionItem (roslynCompletion.DisplayText, RoslynTagsToDebuggerFlags (roslynCompletion.Tags)));
			}
			result.ExpressionLength = roslynCompletions.Span.Length;
			return result;
		}

		static Mono.Debugging.Client.ObjectValueFlags RoslynTagsToDebuggerFlags (ImmutableArray<string> tags)
		{
			var result = Mono.Debugging.Client.ObjectValueFlags.None;
			foreach (var tag in tags) {
				switch (tag) {
				case CompletionTags.Public:
					result |= Mono.Debugging.Client.ObjectValueFlags.Public;
					break;
				case CompletionTags.Protected:
					result |= Mono.Debugging.Client.ObjectValueFlags.Protected;
					break;
				case CompletionTags.Private:
					result |= Mono.Debugging.Client.ObjectValueFlags.Private;
					break;
				case CompletionTags.Internal:
					result |= Mono.Debugging.Client.ObjectValueFlags.Internal;
					break;
				case CompletionTags.File:
				case CompletionTags.Project:
				case CompletionTags.Folder:
				case CompletionTags.Assembly:
				case CompletionTags.Intrinsic:
				case CompletionTags.Keyword:
				case CompletionTags.Label:
				case CompletionTags.Snippet:
				case CompletionTags.Error:
				case CompletionTags.Warning:
				case CompletionTags.Module:
				case CompletionTags.Operator:
					break;
				case CompletionTags.Local:
				case CompletionTags.Constant:
				case CompletionTags.RangeVariable:
				case CompletionTags.Reference:
					result |= Mono.Debugging.Client.ObjectValueFlags.Variable;
					break;
				case CompletionTags.Class:
				case CompletionTags.Enum:
				case CompletionTags.Delegate:
				case CompletionTags.Interface:
				case CompletionTags.Structure:
				case CompletionTags.TypeParameter:
					result |= Mono.Debugging.Client.ObjectValueFlags.Type;
					break;
				case CompletionTags.EnumMember:
				case CompletionTags.Event:
				case CompletionTags.Field:
					result |= Mono.Debugging.Client.ObjectValueFlags.Field;
					break;
				case CompletionTags.Namespace:
					result |= Mono.Debugging.Client.ObjectValueFlags.Namespace;
					break;
				case CompletionTags.ExtensionMethod:
				case CompletionTags.Method:
					result |= Mono.Debugging.Client.ObjectValueFlags.Method;
					break;
				case CompletionTags.Parameter:
					result |= Mono.Debugging.Client.ObjectValueFlags.Parameter;
					break;
				case CompletionTags.Property:
					result |= Mono.Debugging.Client.ObjectValueFlags.Property;
					break;
				}
			}
			return result;
		}
	}
}
