//
// CSharpFormatter.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects.Policies;
using Roslyn.Utilities;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.CSharp.OptionProvider;
using Microsoft.VisualStudio.CodingConventions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using MonoDevelop.Ide.Completion.Presentation;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpFormatter : AbstractCodeFormatter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public override bool SupportsOnTheFlyFormatting { get { return true; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override bool SupportsPartialDocumentFormatting { get { return true; } }

		protected override void CorrectIndentingImplementation (PolicyContainer policyParent, TextEditor editor, int line)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			CorrectIndentingImplementationAsync (editor, doc, line, line, default).Wait ();
		}

		protected async override Task CorrectIndentingImplementationAsync (TextEditor editor, DocumentContext context, int startLine, int endLine, CancellationToken cancellationToken)
		{
			if (editor.IndentationTracker == null)
				return;
			var startSegment = editor.GetLine (startLine);
			if (startSegment == null)
				return;
			var endSegment = startLine != endLine ? editor.GetLine (endLine) : startSegment;
			if (endSegment == null)
				return;

			try {
				var document = context.AnalysisDocument;

				var formattingService = document.GetLanguageService<IEditorFormattingService> ();
				if (formattingService == null || !formattingService.SupportsFormatSelection)
					return;

				var formattingRules = new List<IFormattingRule> ();
				formattingRules.Add (ContainedDocumentPreserveFormattingRule.Instance);
				formattingRules.AddRange (Formatter.GetDefaultFormattingRules (document));

				var workspace = document.Project.Solution.Workspace;
				var root = await document.GetSyntaxRootAsync (cancellationToken);
				var options = await document.GetOptionsAsync (cancellationToken);
				var changes = Formatter.GetFormattedTextChanges (
					root, new TextSpan [] { new TextSpan (startSegment.Offset, endSegment.EndOffset - startSegment.Offset) },
					workspace, options, formattingRules, cancellationToken);

				if (changes == null)
					return;
				editor.ApplyTextChanges (changes);
				editor.FixVirtualIndentation ();
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting", e);
			}
		}
		protected override async void OnTheFlyFormatImplementation (TextEditor editor, DocumentContext context, int startOffset, int length)
		{
			var doc = context.AnalysisDocument;

			var formattingService = doc.GetLanguageService<IEditorFormattingService> ();
			if (formattingService == null || !formattingService.SupportsFormatSelection)
				return;

			var changes = await formattingService.GetFormattingChangesAsync (doc, new TextSpan (startOffset, length), default (System.Threading.CancellationToken));
			if (changes == null)
				return;
			editor.ApplyTextChanges (changes);
			editor.FixVirtualIndentation ();
		}

		public static string FormatText (Microsoft.CodeAnalysis.Options.OptionSet optionSet, string input, int startOffset, int endOffset)
		{
			var inputTree = CSharpSyntaxTree.ParseText (input);

			var root = inputTree.GetRoot ();
			var doc = Formatter.Format (root, new TextSpan (startOffset, endOffset - startOffset), TypeSystemService.Workspace, optionSet);
			var result = doc.ToFullString ();
			return result.Substring (startOffset, endOffset + result.Length - input.Length - startOffset);
		}

		protected override ITextSource FormatImplementation (PolicyContainer policyParent, string mimeType, ITextSource input, int startOffset, int length)
		{
			var chain = DesktopService.GetMimeTypeInheritanceChain (mimeType);
			var policy = policyParent.Get<CSharpFormattingPolicy> (chain);
			var textPolicy = policyParent.Get<TextStylePolicy> (chain);
			var optionSet = policy.CreateOptions (textPolicy);

			if (input is IReadonlyTextDocument doc) {
				try {
					var conventions = EditorConfigService.GetEditorConfigContext (doc.FileName).WaitAndGetResult ();
					if (conventions != null)
						optionSet = new FormattingDocumentOptionSet (optionSet, new CSharpDocumentOptionsProvider.DocumentOptions (optionSet, conventions.CurrentConventions));
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading coding conventions.", e);
				}
			}

			return new StringTextSource (FormatText (optionSet, input.Text, startOffset, startOffset + length));
		}

		sealed class FormattingDocumentOptionSet : OptionSet
		{
			readonly OptionSet fallbackOptionSet;
			readonly CSharpDocumentOptionsProvider.DocumentOptions optionsProvider;

			internal FormattingDocumentOptionSet (OptionSet fallbackOptionSet, CSharpDocumentOptionsProvider.DocumentOptions optionsProvider)
			{
				this.fallbackOptionSet = fallbackOptionSet;
				this.optionsProvider = optionsProvider;
			}

			public override object GetOption (OptionKey optionKey)
			{
				if (optionsProvider.TryGetDocumentOption (optionKey, fallbackOptionSet, out object value))
					return value;
				return fallbackOptionSet.GetOption (optionKey);
			}

			public override OptionSet WithChangedOption (OptionKey optionAndLanguage, object value) => throw new InvalidOperationException ();

			internal override IEnumerable<OptionKey> GetChangedOptions (OptionSet optionSet) => throw new InvalidOperationException ();
		}

	}
}
