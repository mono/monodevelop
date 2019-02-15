//
// ImportSymbolCompletionData.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using System.Text;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Fonts;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Completion
{
	class ImportSymbolCompletionData : CompletionData
	{
		CSharpCompletionTextEditorExtension completionExt;
		readonly Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.CSharpSyntaxContext ctx;
		ISymbol type;
		string displayText;//This is just for caching, because Sorting completion list can call DisplayText many times
		bool useFullName;

		public ISymbol Symbol { get { return type; } }

		public override IconId Icon {
			get {
				return type.GetStockIcon ();
			}
		}
		static CompletionItemRules rules = CompletionItemRules.Create (matchPriority: -10000);
        public override CompletionItemRules Rules => rules;
		public override string DisplayText {
			get {
				if (displayText == null) {
					if (!CommonCompletionUtilities.TryRemoveAttributeSuffix (type, ctx, out displayText)) {
						displayText = type.Name;
					}
				}
				return displayText;
			}
		}
		public override string CompletionText { get =>  useFullName ? type.ContainingNamespace.GetFullName () + "." + type.Name : DisplayText; }

        public override int PriorityGroup { get { return int.MinValue; } }

		public ImportSymbolCompletionData (CSharpCompletionTextEditorExtension ext, Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.CSharpSyntaxContext ctx, ISymbol type, bool useFullName)
		{
			this.completionExt = ext;
			this.ctx = ctx;
			this.useFullName = useFullName;
			this.type = type;
			this.DisplayFlags |= DisplayFlags.IsImportCompletion;
		}

		bool initialized = false;
		bool generateUsing, insertNamespace;

		void Initialize ()
		{
			if (initialized)
				return;
			initialized = true;
			if (type.ContainingNamespace == null)
				return;
			generateUsing = !useFullName;
			insertNamespace = useFullName;
		}

		public override string GetDisplayTextMarkup ()
		{
			return useFullName ? type.ToDisplayString (Ambience.NameFormat) : DisplayText;
		}

		static string GetDefaultDisplaySelection (string description, bool isSelected)
		{
			if (!isSelected)
				return "<span foreground=\"darkgray\">" + description + "</span>";
			return description;
		}

		string displayDescription = null;
		public override string GetDisplayDescription (bool isSelected)
		{
			if (displayDescription == null) {
				Initialize ();
				if (generateUsing || insertNamespace) {
					displayDescription = string.Format (GettextCatalog.GetString ("(from '{0}')"), type.ContainingNamespace.GetFullName ());
				} else {
					displayDescription = "";
				}
			}
			return GetDefaultDisplaySelection (displayDescription, isSelected);
		}

		#region IActionCompletionData implementation

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, MonoDevelop.Ide.Editor.Extension.KeyDescriptor descriptor)
		{
			Initialize ();
			var doc = completionExt.DocumentContext;
			var offset = completionExt.CurrentCompletionContext.TriggerOffset;
			base.InsertCompletionText (window, ref ka, descriptor);

			using (var undo = completionExt.Editor.OpenUndoGroup ()) {
				if (!window.WasShiftPressed && generateUsing) {
					AddGlobalNamespaceImport (completionExt.Editor, doc, type.ContainingNamespace.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat));
				} else {
					doc.AnalysisDocument.GetSemanticModelAsync ().ContinueWith (t => {
						Runtime.RunInMainThread (delegate {
							completionExt.Editor.InsertText (offset, type.ContainingNamespace.ToMinimalDisplayString (t.Result, offset, SymbolDisplayFormat.CSharpErrorMessageFormat) + ".");
						});
					});
				}
			}
			ka |= KeyActions.Ignore;
		}

		static async void AddGlobalNamespaceImport (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, string nsName)
		{
			var unit = await context.AnalysisDocument.GetSemanticModelAsync ();
			if (unit == null)
				return;

			int offset = SearchUsingInsertionPoint (unit.SyntaxTree.GetRoot ());

			var text = StringBuilderCache.Allocate ();
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			text.Append (editor.EolMarker);

			editor.InsertText (offset, StringBuilderCache.ReturnAndFree (text));
		}

		static int SearchUsingInsertionPoint (SyntaxNode parent)
		{
			var result = 0;
			foreach (SyntaxNode node in parent.ChildNodes ()) {
				if (node.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingDirective)) {
					result = node.FullSpan.End;
					continue;
				}
				SyntaxTrivia last = new SyntaxTrivia ();

				foreach (var trivia in node.GetLeadingTrivia ()) {
					if (last.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineCommentTrivia)||
						last.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.DefineDirectiveTrivia) ||
						last.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiLineCommentTrivia) ||
						last.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia))
						result = trivia.Span.End;
					last = trivia;
				}
				break;
			}
			return result;
		}

		const string commitChars = ".";

		public override bool IsCommitCharacter (char keyChar, string partialWord)
		{
			return commitChars.IndexOf (keyChar) >= 0;
		}

		public override async Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken cancelToken)
		{
			CompletionDescription description;
			var doc = completionExt.DocumentContext;
			var completionService = doc.AnalysisDocument.GetLanguageService<CompletionService> ();
			if (completionService == null)
				return null;
			var semanticModel = await doc.AnalysisDocument.GetSemanticModelAsync (cancelToken);
			description = await CommonCompletionUtilities.CreateDescriptionAsync (doc.RoslynWorkspace, semanticModel, completionExt.Editor.CaretOffset, new [] { type }, null, cancelToken).ConfigureAwait (false);

			var markup = StringBuilderCache.Allocate ();
			var theme = SyntaxHighlightingService.GetIdeFittingTheme (DefaultSourceEditorOptions.Instance.GetEditorTheme ());
			var taggedParts = description.TaggedParts;
			if (taggedParts.Length == 0) {
				return null;
			}
			int i = 0;
			while (i < taggedParts.Length) {
				if (taggedParts [i].Tag == TextTags.LineBreak)
					break;
				i++;
			}
			if (i + 1 >= taggedParts.Length) {
				markup.AppendTaggedText (theme, taggedParts);
			} else {
				markup.AppendTaggedText (theme, taggedParts.Take (i));
				markup.Append ("<span font='");
				markup.Append (IdeServices.FontService.SansFontName);
				markup.Append ("' size='small'>");
				markup.AppendLine ();
				markup.AppendLine ();
				markup.AppendTaggedText (theme, taggedParts.Skip (i + 1));
				markup.Append ("</span>");
			}
			markup.AppendLine ();
			markup.AppendLine ();
			markup.Append ("<span font='");
			markup.Append (IdeServices.FontService.SansFontName);
			markup.Append ("' size='small'>");
			markup.AppendLine (GettextCatalog.GetString ("Note: creates an using for namespace:"));
			markup.Append ("<b>");
			markup.Append (Ambience.EscapeText (type.ContainingNamespace.ToDisplayString ()));
			markup.Append ("</b>");
			markup.Append ("</span>");

			return new TooltipInformation {
				SignatureMarkup = StringBuilderCache.ReturnAndFree (markup)
			};
		}

		#endregion
	}
}

