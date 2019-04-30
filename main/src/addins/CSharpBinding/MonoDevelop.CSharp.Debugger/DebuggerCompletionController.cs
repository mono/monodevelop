//
// DebuggerCompletionControllerExtension.cs
//
// Author:
//       Jason Imison <jaimison@microsoft.com>
//
// Copyright (c) 2019 
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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using MonoDevelop.Ide.Gui.Documents;
using Document = Microsoft.CodeAnalysis.Document;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.CSharp.Debugger
{
	[ExportDocumentControllerExtension (MimeType = "*")]
	class DebuggerCompletionController : DocumentControllerExtension
	{
		public override Task<bool> SupportsController (DocumentController controller)
		{
			return Task.FromResult (controller.GetContent<ITextBuffer> () != null);
		}

		protected override object OnGetContent (Type type)
		{
			if (typeof (IDebuggerCompletionProvider).IsAssignableFrom (type)) {
				var textBuffer = Controller.GetContent<ITextBuffer> ();
				var analysisDocument = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges ();
				if (analysisDocument == null)
					return null;
				return new DebuggerCompletionProvider (analysisDocument, textBuffer);
			}
			return base.OnGetContent (type);
		}
	}

	internal class DebuggerCompletionProvider : IDebuggerCompletionProvider
	{
		class DebuggerIntellisenseWorkspace : Workspace
		{
			public DebuggerIntellisenseWorkspace (Solution solution) : base (solution.Workspace.Services.HostServices, "DebuggerIntellisense")
			{
				base.SetCurrentSolution (solution);
			}

			public void OpenDocument (DocumentId documentId, SourceTextContainer textContainer)
			{
				base.OnDocumentOpened (documentId, textContainer, true);
			}
		}

		readonly Document document;
		readonly ITextBuffer textBuffer;

		public DebuggerCompletionProvider (Document document, ITextBuffer textBuffer)
		{
			this.document = document;
			this.textBuffer = textBuffer;
		}

		static async Task<int> GetAdjustedContextPoint (int contextPoint, Document document, CancellationToken cancellationToken)
		{
			var tree = await document.GetSyntaxTreeAsync (cancellationToken).ConfigureAwait(false);

			// Determine the position in the buffer at which to end the tracking span representing
			// the part of the imaginary buffer before the text in the view. 
			var token = tree.FindTokenOnLeftOfPosition (contextPoint, cancellationToken);

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

		public async Task<CompletionData> GetExpressionCompletionData (string exp, StackFrame frame, CancellationToken token)
		{
			var location = frame.SourceLocation;
			if (document == null)
				return null;
			var solution = document.Project.Solution;
			var textSnapshot = textBuffer.CurrentSnapshot;
			var text = textSnapshot.GetText (new Span (0, textSnapshot.Length));
			var insertOffset = await GetAdjustedContextPoint (textSnapshot.GetLineFromLineNumber (location.EndLine - 1).Start.Position + location.EndColumn - 1, document, token).ConfigureAwait(false);
			text = text.Insert (insertOffset, ";" + exp + ";");
			insertOffset++;//advance for 1 which represents `;` before expression
			var newTextBuffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (text, textBuffer.ContentType);
			var snapshot = newTextBuffer.CurrentSnapshot;

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
			var forkedWorkspace = new DebuggerIntellisenseWorkspace (forkedSolution);
			forkedWorkspace.OpenDocument (document.Id, newTextBuffer.AsTextContainer ());
			foreach (var link in document.GetLinkedDocumentIds ()) {
				forkedWorkspace.OpenDocument (link, newTextBuffer.AsTextContainer ());
			}
			var cs = forkedWorkspace.Services.GetLanguageServices (LanguageNames.CSharp).GetService<CompletionService> ();
			var trigger = new CompletionTrigger (CompletionTriggerKind.Invoke, '\0');
			var roslynCompletions = await cs.GetCompletionsAsync (forkedWorkspace.CurrentSolution.GetDocument (document.Id), insertOffset + exp.Length, trigger, cancellationToken: token).ConfigureAwait (false);
			if (roslynCompletions == null)
				return null;
			var result = new Mono.Debugging.Client.CompletionData ();
			foreach (var roslynCompletion in roslynCompletions.Items) {
				if (roslynCompletion.Tags.Contains (WellKnownTags.Snippet))
					continue;
				result.Items.Add (new Mono.Debugging.Client.CompletionItem (roslynCompletion.DisplayText, RoslynTagsToDebuggerFlags (roslynCompletion.Tags)));
			}
			result.ExpressionLength = roslynCompletions.Span.Length;
			return result;
		}

		static ObjectValueFlags RoslynTagsToDebuggerFlags (ImmutableArray<string> tags)
		{
			var result = ObjectValueFlags.None;
			foreach (var tag in tags) {
				switch (tag) {
				case WellKnownTags.Public:
					result |= ObjectValueFlags.Public;
					break;
				case WellKnownTags.Protected:
					result |= ObjectValueFlags.Protected;
					break;
				case WellKnownTags.Private:
					result |= ObjectValueFlags.Private;
					break;
				case WellKnownTags.Internal:
					result |= ObjectValueFlags.Internal;
					break;
				case WellKnownTags.File:
				case WellKnownTags.Project:
				case WellKnownTags.Folder:
				case WellKnownTags.Assembly:
				case WellKnownTags.Intrinsic:
				case WellKnownTags.Keyword:
				case WellKnownTags.Label:
				case WellKnownTags.Snippet:
				case WellKnownTags.Error:
				case WellKnownTags.Warning:
				case WellKnownTags.Module:
				case WellKnownTags.Operator:
					break;
				case WellKnownTags.Local:
				case WellKnownTags.Constant:
				case WellKnownTags.RangeVariable:
				case WellKnownTags.Reference:
					result |= ObjectValueFlags.Variable;
					break;
				case WellKnownTags.Class:
				case WellKnownTags.Enum:
				case WellKnownTags.Delegate:
				case WellKnownTags.Interface:
				case WellKnownTags.Structure:
				case WellKnownTags.TypeParameter:
					result |= ObjectValueFlags.Type;
					break;
				case WellKnownTags.EnumMember:
				case WellKnownTags.Event:
				case WellKnownTags.Field:
					result |= ObjectValueFlags.Field;
					break;
				case WellKnownTags.Namespace:
					result |= ObjectValueFlags.Namespace;
					break;
				case WellKnownTags.ExtensionMethod:
				case WellKnownTags.Method:
					result |= ObjectValueFlags.Method;
					break;
				case WellKnownTags.Parameter:
					result |= ObjectValueFlags.Parameter;
					break;
				case WellKnownTags.Property:
					result |= ObjectValueFlags.Property;
					break;
				}
			}
			return result;
		}
	}
}
