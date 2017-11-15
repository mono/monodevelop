using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Projection;
using MonoDevelop.Ide.Composition;

namespace Microsoft.VisualStudio.Platform
{
	public class MonoDevelopContainedDocument
	{
		private const char RazorExplicit = '@';

		private const string CSharpRazorBlock = "{";
		private const string VBRazorBlock = "code";

		private const string HelperRazor = "helper";
		private const string FunctionsRazor = "functions";

		public ITextBuffer LanguageBuffer { get; private set; }
		public IProjectionBuffer DataBuffer { get; private set; }
		public IMonoDevelopContainedLanguageHost ContainedLanguageHost { get; private set; }
		private Workspace Workspace { get; set; }
		private string Language { get; set; }

		public static MonoDevelopContainedDocument FromDocument(Document document)
		{
			MonoDevelopContainedDocument containedDocument = null;
			if (document.TryGetText(out SourceText sourceText))
			{
				ITextBuffer textBuffer = sourceText.Container.GetTextBuffer();

				containedDocument = textBuffer.Properties.GetProperty<MonoDevelopContainedDocument>(typeof(MonoDevelopContainedDocument));
			}

			return containedDocument;
		}

		public static MonoDevelopContainedDocument AttachToBuffer(ITextBuffer languageBuffer, IProjectionBuffer dataBuffer, IMonoDevelopContainedLanguageHost containedLanguageHost)
		{
			return new MonoDevelopContainedDocument(languageBuffer, dataBuffer, containedLanguageHost);
		}

		public static void DetachFromBuffer(ITextBuffer languageBuffer)
		{
			languageBuffer.Properties.RemoveProperty(typeof(MonoDevelopContainedDocument));
		}

		private MonoDevelopContainedDocument (ITextBuffer languageBuffer, IProjectionBuffer dataBuffer, IMonoDevelopContainedLanguageHost containedLanguageHost)
		{
			Document document = languageBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
			Project project = document.Project;

			Workspace = project.Solution.Workspace;

			LanguageBuffer = languageBuffer;
			DataBuffer = dataBuffer;
			ContainedLanguageHost = containedLanguageHost;
			Language = project.Language;

			languageBuffer.Properties.AddProperty (typeof (MonoDevelopContainedDocument), this);
		}

		public IEnumerable<TextSpan> GetEditorVisibleSpans ()
		{
			return DataBuffer.CurrentSnapshot
				.GetSourceSpans ()
				.Where (ss => ss.Snapshot.TextBuffer == LanguageBuffer)
				.Select (s => s.Span.ToTextSpan ())
				.OrderBy (s => s.Start);
		}

		internal BaseIndentationFormattingRule GetBaseIndentationRule (SyntaxNode root, SourceText text, List<TextSpan> spans, int spanIndex)
		{
			var currentSpanIndex = spanIndex;
			GetVisibleAndTextSpan (text, spans, currentSpanIndex, out var visibleSpan, out var visibleTextSpan);

			var end = visibleSpan.End;
			var current = root.FindToken (visibleTextSpan.Start).Parent;
			while (current != null) {
				if (current.Span.Start == visibleTextSpan.Start) {
					var blockType = GetRazorCodeBlockType (visibleSpan.Start);
					if (blockType == RazorCodeBlockType.Explicit) {
						var baseIndentation = GetBaseIndentation (root, text, visibleSpan);
						return new BaseIndentationFormattingRule (root, TextSpan.FromBounds (visibleSpan.Start, end), baseIndentation);
					}
				}

				if (current.Span.Start < visibleSpan.Start) {
					var blockType = GetRazorCodeBlockType (visibleSpan.Start);
					if (blockType == RazorCodeBlockType.Block || blockType == RazorCodeBlockType.Helper) {
						var baseIndentation = GetBaseIndentation (root, text, visibleSpan);
						return new BaseIndentationFormattingRule (root, TextSpan.FromBounds (visibleSpan.Start, end), baseIndentation);
					}

					if (currentSpanIndex == 0) {
						break;
					}

					GetVisibleAndTextSpan (text, spans, --currentSpanIndex, out visibleSpan, out visibleTextSpan);
					continue;
				}

				current = current.Parent;
			}

			var span = spans[spanIndex];
			var indentation = GetBaseIndentation (root, text, span);
			return new BaseIndentationFormattingRule (root, span, indentation);
		}

		private void GetVisibleAndTextSpan (SourceText text, List<TextSpan> spans, int spanIndex, out TextSpan visibleSpan, out TextSpan visibleTextSpan)
		{
			visibleSpan = spans[spanIndex];

			visibleTextSpan = GetVisibleTextSpan (text, visibleSpan);
			if (visibleTextSpan.IsEmpty) {
				// span has no text in them
				visibleTextSpan = visibleSpan;
			}
		}

		private int GetBaseIndentation (SyntaxNode root, SourceText text, TextSpan span)
		{
			// Is this right?  We should probably get this from the IVsContainedLanguageHost instead.
			var editorOptionsFactory = CompositionManager.GetExportedValue<IEditorOptionsFactoryService> ();
			var editorOptions = editorOptionsFactory.GetOptions (DataBuffer);

			var additionalIndentation = GetAdditionalIndentation (root, text, span);

			// Skip over the first line, since it's in "Venus space" anyway.
			var startingLine = text.Lines.GetLineFromPosition (span.Start);
			for (var line = startingLine; line.Start < span.End; line = text.Lines[line.LineNumber + 1]) {
				ContainedLanguageHost.GetLineIndent (
					line.LineNumber,
					out var baseIndentationString,
					out var parent,
					out var indentSize,
					out var useTabs,
					out var tabSize);

				if (!string.IsNullOrEmpty (baseIndentationString)) {
					return baseIndentationString.GetColumnFromLineOffset (baseIndentationString.Length, editorOptions.GetTabSize ()) + additionalIndentation;
				}
			}

			return additionalIndentation;
		}

		private TextSpan GetVisibleTextSpan (SourceText text, TextSpan visibleSpan, bool uptoFirstAndLastLine = false)
		{
			var start = visibleSpan.Start;
			for (; start < visibleSpan.End; start++) {
				if (!char.IsWhiteSpace (text[start])) {
					break;
				}
			}

			var end = visibleSpan.End - 1;
			if (start <= end) {
				for (; start <= end; end--) {
					if (!char.IsWhiteSpace (text[end])) {
						break;
					}
				}
			}

			if (uptoFirstAndLastLine) {
				var firstLine = text.Lines.GetLineFromPosition (visibleSpan.Start);
				var lastLine = text.Lines.GetLineFromPosition (visibleSpan.End);

				if (firstLine.LineNumber < lastLine.LineNumber) {
					start = (start < firstLine.End) ? start : firstLine.End;
					end = (lastLine.Start < end + 1) ? end : lastLine.Start - 1;
				}
			}

			return (start <= end) ? TextSpan.FromBounds (start, end + 1) : default (TextSpan);
		}

		private int GetAdditionalIndentation (SyntaxNode root, SourceText text, TextSpan span)
		{
			var type = GetRazorCodeBlockType (span.Start);

			// razor block
			if (type == RazorCodeBlockType.Block) {
				// more workaround for csharp razor case. when } for csharp razor code block is just typed, "}" exist
				// in both subject and surface buffer and there is no easy way to figure out who owns } just typed.
				// in this case, we let razor owns it. later razor will remove } from subject buffer if it is something
				// razor owns.
				var textSpan = GetVisibleTextSpan (text, span);
				var end = textSpan.End - 1;
				if (end >= 0 && text[end] == '}') {
					var token = root.FindToken (end);
					var syntaxFact = Workspace.Services.GetLanguageServices (Language).GetService<ISyntaxFactsService> ();
					if (token.Span.Start == end && syntaxFact != null) {
						if (syntaxFact.TryGetCorrespondingOpenBrace (token, out var openBrace) && !textSpan.Contains (openBrace.Span)) {
							return 0;
						}
					}
				}

				return Workspace.Options.GetOption (FormattingOptions.IndentationSize, Language);
			}

			return 0;
		}

		private RazorCodeBlockType GetRazorCodeBlockType (int position)
		{
			var subjectBuffer = (IProjectionBuffer)LanguageBuffer;
			var subjectSnapshot = subjectBuffer.CurrentSnapshot;
			var surfaceSnapshot = DataBuffer.CurrentSnapshot;

			var surfacePoint = surfaceSnapshot.MapFromSourceSnapshot (new SnapshotPoint (subjectSnapshot, position), PositionAffinity.Predecessor);
			if (!surfacePoint.HasValue) {
				// how this can happen?
				return RazorCodeBlockType.Implicit;
			}

			var ch = char.ToLower (surfaceSnapshot[Math.Max (surfacePoint.Value - 1, 0)]);

			// razor block
			if (IsCodeBlock (surfaceSnapshot, surfacePoint.Value, ch)) {
				return RazorCodeBlockType.Block;
			}

			if (ch == RazorExplicit) {
				return RazorCodeBlockType.Explicit;
			}

			if (CheckCode (surfaceSnapshot, surfacePoint.Value, HelperRazor)) {
				return RazorCodeBlockType.Helper;
			}

			return RazorCodeBlockType.Implicit;
		}

		private bool IsCodeBlock (ITextSnapshot surfaceSnapshot, int position, char ch)
		{
			return CheckCode (surfaceSnapshot, position, ch, CSharpRazorBlock) ||
					CheckCode (surfaceSnapshot, position, ch, FunctionsRazor, CSharpRazorBlock);
		}

		private bool CheckCode (ITextSnapshot snapshot, int position, char ch, string tag, bool checkAt = true)
		{
			if (ch != tag[tag.Length - 1] || position < tag.Length) {
				return false;
			}

			var start = position - tag.Length;
			var razorTag = snapshot.GetText (start, tag.Length);
			return string.Equals (razorTag, tag, StringComparison.OrdinalIgnoreCase) && (!checkAt || snapshot[start - 1] == RazorExplicit);
		}

		private bool CheckCode (ITextSnapshot snapshot, int position, string tag)
		{
			int i = position - 1;
			if (i < 0) {
				return false;
			}

			for (; i >= 0; i--) {
				if (!char.IsWhiteSpace (snapshot[i])) {
					break;
				}
			}

			var ch = snapshot[i];
			position = i + 1;

			return CheckCode (snapshot, position, ch, tag);
		}

		private bool CheckCode (ITextSnapshot snapshot, int position, char ch, string tag1, string tag2)
		{
			if (!CheckCode (snapshot, position, ch, tag2, checkAt: false)) {
				return false;
			}

			return CheckCode (snapshot, position - tag2.Length, tag1);
		}

		private enum RazorCodeBlockType
		{
			Block,
			Explicit,
			Implicit,
			Helper
		}
	}
}
