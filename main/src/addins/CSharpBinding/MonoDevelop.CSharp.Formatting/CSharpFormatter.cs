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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Core.Text;
using MonoDevelop.CSharp.OptionProvider;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Completion.Presentation;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.CSharp.OptionProvider;
using Microsoft.VisualStudio.CodingConventions;
using System.Linq;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpFormatter : AbstractCodeFormatter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public override bool SupportsOnTheFlyFormatting { get { return true; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override bool SupportsPartialDocumentFormatting { get { return true; } }

		protected override void CorrectIndentingImplementation (PolicyContainer policyParent, Ide.Editor.TextEditor editor, int line)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			CorrectIndentingImplementationAsync (editor, doc.DocumentContext, line, line, default).Ignore ();
		}

		protected async override Task CorrectIndentingImplementationAsync (Ide.Editor.TextEditor editor, DocumentContext context, int startLine, int endLine, CancellationToken cancellationToken)
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

				var formattingRules = new List<AbstractFormattingRule> ();
				formattingRules.Add (ContainedDocumentPreserveFormattingRule.Instance);
				formattingRules.AddRange (Formatter.GetDefaultFormattingRules (document));

				var workspace = document.Project.Solution.Workspace;
				var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
				var options = await document.GetOptionsAsync (cancellationToken).ConfigureAwait (false);
				var changes = Formatter.GetFormattedTextChanges (
					root, new TextSpan [] { new TextSpan (startSegment.Offset, endSegment.EndOffset - startSegment.Offset) },
					workspace, options, formattingRules, cancellationToken);

				if (changes == null)
					return;
				await Runtime.RunInMainThread (delegate {
					editor.ApplyTextChanges (changes);
					editor.FixVirtualIndentation ();
				});
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting", e);
			}
		}
		protected override async void OnTheFlyFormatImplementation (Ide.Editor.TextEditor editor, DocumentContext context, int startOffset, int length)
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
			var doc = Formatter.Format (root, new TextSpan (startOffset, endOffset - startOffset), IdeApp.TypeSystemService.Workspace, optionSet);
			var result = doc.ToFullString ();
			return result.Substring (startOffset, endOffset + result.Length - input.Length - startOffset);
		}

		protected override ITextSource FormatImplementation (PolicyContainer policyParent, string mimeType, ITextSource input, int startOffset, int length)
		{
			var chain = IdeServices.DesktopService.GetMimeTypeInheritanceChain (mimeType);
			var policy = policyParent.Get<CSharpFormattingPolicy> (chain);
			var textPolicy = policyParent.Get<TextStylePolicy> (chain);
			var optionSet = policy.CreateOptions (textPolicy);

			if (input is IReadonlyTextDocument doc) {
				try {
					var conventions = EditorConfigService.GetEditorConfigContext (doc.FileName).WaitAndGetResult ();
					if (conventions != null)
						optionSet = new FormattingDocumentOptionSet (optionSet, new DocumentOptions (optionSet, conventions.CurrentConventions));
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading coding conventions.", e);
				}
			}

			return new StringTextSource (FormatText (optionSet, input.Text, startOffset, startOffset + length));
		}

		sealed class DocumentOptions : IDocumentOptions
		{
			readonly OptionSet optionSet;
			readonly ICodingConventionsSnapshot codingConventionsSnapshot;
			private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>> s_convertedDictionaryCache =
				new ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>> ();


			public DocumentOptions (OptionSet optionSet, ICodingConventionsSnapshot codingConventionsSnapshot)
			{
				this.optionSet = optionSet;
				this.codingConventionsSnapshot = codingConventionsSnapshot;
			}

			public bool TryGetDocumentOption (OptionKey option, out object value)
			{
				if (codingConventionsSnapshot != null) {
					var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation> ().SingleOrDefault ();
					if (editorConfigPersistence != null) {
						// Temporarly map our old Dictionary<string, object> to a Dictionary<string, string>. This can go away once we either
						// eliminate the legacy editorconfig support, or we change IEditorConfigStorageLocation.TryGetOption to take
						// some interface that lets us pass both the Dictionary<string, string> we get from the new system, and the
						// Dictionary<string, object> from the old system.
						//
						// We cache this with a conditional weak table so we're able to maintain the assumptions in EditorConfigNamingStyleParser
						// that the instance doesn't regularly change and thus can be used for further caching
						var allRawConventions = s_convertedDictionaryCache.GetValue (
							codingConventionsSnapshot.AllRawConventions,
							d => ImmutableDictionary.CreateRange (d.Select (c => KeyValuePairUtil.Create (c.Key, c.Value.ToString ()))));

						try {
							if (editorConfigPersistence.TryGetOption (allRawConventions, option.Option.Type, out value))
								return true;
						} catch (Exception ex) {
							LoggingService.LogError ("Error while getting editor config preferences.", ex);
						}
					}
				}

				var result = optionSet.GetOption (option);
				value = result;
				return true;
			}
		}

		sealed class FormattingDocumentOptionSet : OptionSet
		{
			readonly OptionSet fallbackOptionSet;
			readonly IDocumentOptions optionsProvider;

			internal FormattingDocumentOptionSet (OptionSet fallbackOptionSet, IDocumentOptions optionsProvider)
			{
				this.fallbackOptionSet = fallbackOptionSet;
				this.optionsProvider = optionsProvider;
			}

			public override object GetOption (OptionKey optionKey)
			{
				if (optionsProvider.TryGetDocumentOption (optionKey, out object value))
					return value;
				return fallbackOptionSet.GetOption (optionKey);
			}

			public override OptionSet WithChangedOption (OptionKey optionAndLanguage, object value) => throw new InvalidOperationException ();

			internal override IEnumerable<OptionKey> GetChangedOptions (OptionSet optionSet) => throw new InvalidOperationException ();
		}

	}
}
