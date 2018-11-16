//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor;

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using static Microsoft.VisualStudio.Language.Intellisense.Implementation.MDUtils;
using Microsoft.VisualStudio.Language.StandardClassification;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Tasks;

namespace Microsoft.VisualStudio.Platform
{
    [Export(typeof(ITagBasedSyntaxHighlightingFactory))]
    internal sealed class TagBasedSyntaxHighlightingFactory : ITagBasedSyntaxHighlightingFactory
    {
        public ISyntaxHighlighting CreateSyntaxHighlighting(ITextView textView)
        {
            return new TagBasedSyntaxHighlighting(textView, null);
        }

        public ISyntaxHighlighting CreateSyntaxHighlighting(ITextView textView, string defaultScope)
        {
            return new TagBasedSyntaxHighlighting(textView, defaultScope);
        }
    }

    internal sealed class TagBasedSyntaxHighlighting : ISyntaxHighlighting2
    {
        private ITextView textView { get; }
        private IAccurateClassifier classifier { get; set; }
        readonly Dictionary<string, ScopeStack> classificationMap;
        Dictionary<IClassificationType, ScopeStack> classificationTypeToScopeCache = new Dictionary<IClassificationType, ScopeStack>();
        ScopeStack defaultScopeStack;
        private MonoDevelop.Ide.Editor.ITextDocument textDocument { get; }
        public bool IsUpdatingOnTextChange { get { return true; } }

        internal TagBasedSyntaxHighlighting (ITextView textView, string defaultScope)
		{
			this.textView = textView;
			this.textDocument = textView.GetTextEditor ();
			if (defaultScope != null)
				classificationMap = GetClassificationMap (defaultScope);
			else
				defaultScopeStack = new ScopeStack (EditorThemeColors.Foreground);
		}

		public Task<HighlightedLine> GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken)
		{
			ITextSnapshotLine snapshotLine = (line as Mono.TextEditor.TextDocument.DocumentLineFromTextSnapshotLine)?.Line;
			if ((this.classifier == null) || (snapshotLine == null)) {
				return Task.FromResult (new HighlightedLine (line, new [] { new ColoredSegment (0, line.Length, ScopeStack.Empty) }));
			}
			List<ColoredSegment> coloredSegments = new List<ColoredSegment> ();

			SnapshotSpan snapshotSpan = new SnapshotSpan(textView.TextBuffer.CurrentSnapshot, snapshotLine.Extent.Span);
			int start = snapshotSpan.Start.Position;
			int end = snapshotSpan.End.Position;

			IList<ClassificationSpan> classifications = this.classifier.GetClassificationSpans (snapshotSpan);

			int lastClassifiedOffsetEnd = start;
			ScopeStack scopeStack;
			foreach (ClassificationSpan curSpan in classifications) {
				if (curSpan.Span.Start > lastClassifiedOffsetEnd) {
					scopeStack = defaultScopeStack;
					ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - start, curSpan.Span.Start - lastClassifiedOffsetEnd, scopeStack);
					coloredSegments.Add (whitespaceSegment);
				}

				scopeStack = GetScopeStackFromClassificationType (curSpan.ClassificationType);
				if (scopeStack.Peek ().StartsWith ("comment", StringComparison.Ordinal)) {
					ScanAndAddComment (coloredSegments, start, scopeStack, curSpan);
				} else {
					var curColoredSegment = new ColoredSegment (curSpan.Span.Start - start, curSpan.Span.Length, scopeStack);
					coloredSegments.Add (curColoredSegment);
				}

				lastClassifiedOffsetEnd = curSpan.Span.End;
			}

			if (end > lastClassifiedOffsetEnd) {
				scopeStack = defaultScopeStack;
				ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - start, end - lastClassifiedOffsetEnd, scopeStack);
				coloredSegments.Add (whitespaceSegment);
			}

			return Task.FromResult(new HighlightedLine (line, coloredSegments));
		}
		#region Tag Comment Scanning

		void ScanAndAddComment (List<ColoredSegment> coloredSegments, int startOffset, ScopeStack commentScopeStack, ClassificationSpan classificationSpan)
		{
			int lastClassifiedOffset = classificationSpan.Span.Start;
			try {
				// Scan comments for tag highlighting
				var text = textView.TextSnapshot.GetText (classificationSpan.Span);
				int idx = 0, oldIdx = 0;

				while ((idx = FindNextCommentTagIndex (text, idx, out string commentTag)) >= 0) {
					var headSpanLength = idx - oldIdx;
					if (headSpanLength > 0) {
						var headSegment = new ColoredSegment (lastClassifiedOffset - startOffset, headSpanLength, commentScopeStack);
						lastClassifiedOffset += headSpanLength;
						coloredSegments.Add (headSegment);
					}
					var highlightSegment = new ColoredSegment (lastClassifiedOffset - startOffset, commentTag.Length, commentScopeStack.Push ("markup.other"));
					coloredSegments.Add (highlightSegment);
					idx += commentTag.Length;
					lastClassifiedOffset += commentTag.Length;
					oldIdx = idx;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while scanning comment tags.", e);
			}
			int tailSpanLength = classificationSpan.Span.End - lastClassifiedOffset;
			if (tailSpanLength > 0) {
				var tailSpan = new ColoredSegment (lastClassifiedOffset - startOffset, tailSpanLength, commentScopeStack);
				coloredSegments.Add (tailSpan);
			}
		}

		static int FindNextCommentTagIndex (string text, int startIndex, out string commentTag)
		{
			var foundIndex = -1;
			commentTag = null;
			foreach (var tag in CommentTag.SpecialCommentTags) {
				var i = text.IndexOf (tag.Tag, startIndex, StringComparison.OrdinalIgnoreCase);
				if (i < 0)
					continue;
				if (i < foundIndex || foundIndex < 0) {
					foundIndex = i;
					commentTag = tag.Tag;
				}
			}
			return foundIndex;
		}

		#endregion

		public async Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			var line = textDocument.GetLineByOffset (offset);
			var highligthedLine = await GetHighlightedLineAsync (line, cancellationToken).ConfigureAwait (false);
			offset -= line.Offset;
			foreach (var segment in highligthedLine.Segments) {
				if (segment.Offset <= offset && segment.EndOffset >= offset)
					return segment.ScopeStack;
			}
			return defaultScopeStack;
		}

		private EventHandler<LineEventArgs> _highlightingStateChanged;
		public event EventHandler<LineEventArgs> HighlightingStateChanged {
			add {
				lock (this) {
					_highlightingStateChanged += value;
				}

				if (this.classifier == null) {
					this.classifier = PlatformCatalog.Instance.ViewClassifierAggregatorService.GetClassifier (this.textView) as IAccurateClassifier;
					this.classifier.ClassificationChanged += this.OnClassificationChanged;
				}
			}

			remove {
				bool dispose = false;
				lock (this) {
					_highlightingStateChanged -= value;
					dispose = _highlightingStateChanged == null;
				}

				if (dispose && (this.classifier != null)) {
					this.classifier.ClassificationChanged -= this.OnClassificationChanged;
					(this.classifier as IDisposable)?.Dispose ();
					this.classifier = null;
				}
			}
		}

        private void OnClassificationChanged(object sender, ClassificationChangedEventArgs args)
        {
            var handler = _highlightingStateChanged;
            if (handler != null)
            {
                foreach (Mono.TextEditor.MdTextViewLineCollection.MdTextViewLine line in textView.TextViewLines)
                {
                    if (line.Start.Position > args.ChangeSpan.End.Position || line.End.Position < args.ChangeSpan.Start)
                        continue;
                    handler(this, new LineEventArgs(line.line));
                }
            }
        }

		private ScopeStack GetScopeStackFromClassificationType (IClassificationType classificationType)
		{
			if (classificationTypeToScopeCache.TryGetValue (classificationType, out var cachedScope))
				return cachedScope;
			ScopeStack scope = null;
			void ProcessClassificationType(IClassificationType classification)
			{
				foreach (var baseType in classification.BaseTypes) {
					ProcessClassificationType (baseType);
				}
				//This comparision with Identifier and Keyword is very hacky
				//what we are doing here is, making sure anything has greater priorty over
				//this two Identifer/Keyword, I came to this two from 
				//https://github.com/dotnet/roslyn/blob/88d1bd1/src/EditorFeatures/Core.Wpf/Classification/ClassificationTypeFormatDefinitions.cs
				//Which orders different classifications by priority...
				//This is needed so Semantical classification for Interface or Class is greater then Identifier from Syntactical classifeer
				if (scope != null && (classification.Classification == PredefinedClassificationTypeNames.Identifier || classification.Classification == PredefinedClassificationTypeNames.Keyword))
					return;
				if (classificationMap != null && classificationMap.TryGetValue (classification.Classification, out var mappedScope)) {
					scope = mappedScope;
					return;
				}
				var styleName = GetStyleNameFromClassificationName (classification.Classification);
				if (styleName == null)
					return;
				scope = new ScopeStack (styleName);
			}
			ProcessClassificationType (classificationType);
			return classificationTypeToScopeCache [classificationType] = scope ?? defaultScopeStack;
		}

		private string GetStyleNameFromClassificationName (string classificationName)
		{
			string styleName = null;

			// MONO: TODO: get this from the EditorFormat?
			switch (classificationName) {
			// MONO: TODO: Make each language MEF export this knowledge?

			// CSS Entries
			case "CSS Comment":
				styleName = "comment.block.css";
				break;
			case "CSS Keyword":
				styleName = "keyword.other.css";
				break;
			case "CSS Selector":
				styleName = "entity.name.tag.css";
				break;
			case "CSS Property Name":
				styleName = "support.type.property-name.css";
				break;
			case "CSS Property Value":
				styleName = "support.constant.property-value.css";
				break;
			case "CSS String Value":
				styleName = "string.quoted.double.css";
				break;

			// HTML Entries
			case "HTML Attribute Name":
				styleName = "entity.other.attribute-name.html";
				break;
			case "HTML Attribute Value":
				styleName = "string.unquoted.html";
				break;
			case "HTML Comment":
				styleName = "comment.block.html";
				break;
			case "HTML Element Name":
				styleName = "entity.name.tag.html";
				break;
			case "HTML Entity":
				styleName = "constant.character.entity.html";
				break;
			case "HTML Operator":
				styleName = "punctuation.separator.key-value.html";
				break;
			case "HTML Server-Side Script":
				styleName = "source.server.html";
				break;
			case "HTML Tag Delimiter":
				styleName = "punctuation.definition.tag.begin.html";
				break;
			case "RazorCode":
				//styleName = style.RazorCode.Name;
				break;
			case "RazorTagHelperAttribute":
				styleName = "markup.bold";
				break;
			case "RazorTagHelperElement":
				styleName = "markup.bold";
				break;

			// JSON Entries
			case "operator":
				styleName = "meta.structure.dictionary.value.json";
				break;
			case "string":
				styleName = "string.quoted.double.json";
				break;
			case "keyword":
				styleName = "constant.language.json";
				break;
			case "number":
				styleName = "constant.numeric.json";
				break;
			case "comment":
				styleName = "comment.block.json";
				break;
			case "JSON Property Name":
				styleName = "support.type.property-name.json";
				break;

			// LESS Entries
			case "LessCssVariableDeclaration":
				styleName = "variable.other.less";
				break;
			case "LessCssVariableReference":
				styleName = "variable.other.less";
				break;
			case "LessCssNamespaceReference":
				styleName = "variable.other.less";
				break;
			case "LessCssMixinReference":
				styleName = "variable.other.less";
				break;
			case "LessCssMixinDeclaration":
				styleName = "variable.other.less";
				break;
			case "LessCssKeyword":
				styleName = "punctuation.definition.keyword.css";
				break;

			// Scss Entries
			case "ScssMixinReference":
				styleName = "variable.other.less";
				break;
			case "ScssMixinDeclaration":
				styleName = "variable.other.less";
				break;
			case "ScssVariableDeclaration":
				styleName = "variable.other.less";
				break;
			case "ScssVariableReference":
				styleName = "variable.other.less";
				break;
			default:
				// If the stylename looks like a textmate style, just use it
				if (classificationName.IndexOf ('.') >= 0) {
					styleName = classificationName;
				}

				break;
			}

			return styleName;
		}

		static ScopeStack MakeScope (ScopeStack defaultScope, string scope)
		{
			return defaultScope.Push (scope);
		}

		static ImmutableDictionary<string, Dictionary<string, ScopeStack>> classificationMapCache = ImmutableDictionary<string, Dictionary<string, ScopeStack>>.Empty;
		Dictionary<string, ScopeStack> GetClassificationMap (string scope)
		{
			Dictionary<string, ScopeStack> result;
			var baseScopeStack = new ScopeStack (scope);
			defaultScopeStack = baseScopeStack.Push (EditorThemeColors.Foreground);
			if (classificationMapCache.TryGetValue (scope, out result))
				return result;
			result = new Dictionary<string, ScopeStack> {
				[ClassificationTypeNames.Comment] = MakeScope (baseScopeStack, "comment." + scope),
				[ClassificationTypeNames.ExcludedCode] = MakeScope (baseScopeStack, "comment.excluded." + scope),
				[ClassificationTypeNames.Identifier] = MakeScope (baseScopeStack, scope),
                [ClassificationTypeNames.Keyword] = MakeScope(baseScopeStack, "keyword." + scope),
                ["identifier - keyword - (TRANSIENT)"] = MakeScope(baseScopeStack, "keyword." + scope), // required for highlighting of some context specific keywords like 'nameof'
                [ClassificationTypeNames.NumericLiteral] = MakeScope (baseScopeStack, "constant.numeric." + scope),
				[ClassificationTypeNames.Operator] = MakeScope (baseScopeStack, scope),
				[ClassificationTypeNames.PreprocessorKeyword] = MakeScope (baseScopeStack, "meta.preprocessor." + scope),
				[ClassificationTypeNames.StringLiteral] = MakeScope (baseScopeStack, "string." + scope),
				[ClassificationTypeNames.WhiteSpace] = MakeScope (baseScopeStack, "text." + scope),
				[ClassificationTypeNames.Text] = MakeScope (baseScopeStack, "text." + scope),

				[ClassificationTypeNames.PreprocessorText] = MakeScope (baseScopeStack, "meta.preprocessor.region.name." + scope),
				[ClassificationTypeNames.Punctuation] = MakeScope (baseScopeStack, "punctuation." + scope),
				[ClassificationTypeNames.VerbatimStringLiteral] = MakeScope (baseScopeStack, "string.verbatim." + scope),

				[ClassificationTypeNames.ClassName] = MakeScope (baseScopeStack, "entity.name.class." + scope),
				[ClassificationTypeNames.DelegateName] = MakeScope (baseScopeStack, "entity.name.delegate." + scope),
				[ClassificationTypeNames.EnumName] = MakeScope (baseScopeStack, "entity.name.enum." + scope),
				[ClassificationTypeNames.InterfaceName] = MakeScope (baseScopeStack, "entity.name.interface." + scope),
				[ClassificationTypeNames.ModuleName] = MakeScope (baseScopeStack, "entity.name.module." + scope),
				[ClassificationTypeNames.StructName] = MakeScope (baseScopeStack, "entity.name.struct." + scope),
				[ClassificationTypeNames.TypeParameterName] = MakeScope (baseScopeStack, "entity.name.typeparameter." + scope),

				[ClassificationTypeNames.FieldName] = MakeScope (baseScopeStack, "entity.name.field." + scope),
				[ClassificationTypeNames.EnumMemberName] = MakeScope (baseScopeStack, "entity.name.enummember." + scope),
				[ClassificationTypeNames.ConstantName] = MakeScope (baseScopeStack, "entity.name.constant." + scope),
				[ClassificationTypeNames.LocalName] = MakeScope (baseScopeStack, "entity.name.local." + scope),
				[ClassificationTypeNames.ParameterName] = MakeScope (baseScopeStack, "entity.name.parameter." + scope),
				[ClassificationTypeNames.ExtensionMethodName] = MakeScope (baseScopeStack, "entity.name.extensionmethod." + scope),
				[ClassificationTypeNames.MethodName] = MakeScope (baseScopeStack, "entity.name.function." + scope),
				[ClassificationTypeNames.PropertyName] = MakeScope (baseScopeStack, "entity.name.property." + scope),
				[ClassificationTypeNames.EventName] = MakeScope (baseScopeStack, "entity.name.event." + scope),

				[ClassificationTypeNames.XmlDocCommentAttributeName] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentAttributeQuotes] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentAttributeValue] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentCDataSection] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
                [ClassificationTypeNames.XmlDocCommentComment] = MakeScope(baseScopeStack, "comment.line.documentation." + scope),
                [ClassificationTypeNames.XmlDocCommentDelimiter] = MakeScope (baseScopeStack, "comment.line.documentation.delimiter." + scope),
				[ClassificationTypeNames.XmlDocCommentEntityReference] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentName] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentProcessingInstruction] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentText] = MakeScope (baseScopeStack, "comment.line.documentation." + scope),

				[ClassificationTypeNames.XmlLiteralAttributeName] = MakeScope (baseScopeStack, "entity.other.attribute-name." + scope),
				[ClassificationTypeNames.XmlLiteralAttributeQuotes] = MakeScope (baseScopeStack, "punctuation.definition.string." + scope),
				[ClassificationTypeNames.XmlLiteralAttributeValue] = MakeScope (baseScopeStack, "string.quoted." + scope),
				[ClassificationTypeNames.XmlLiteralCDataSection] = MakeScope (baseScopeStack, "text." + scope),
				[ClassificationTypeNames.XmlLiteralComment] = MakeScope (baseScopeStack, "comment.block." + scope),
				[ClassificationTypeNames.XmlLiteralDelimiter] = MakeScope (baseScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralEmbeddedExpression] = MakeScope (baseScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralEntityReference] = MakeScope (baseScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralName] = MakeScope (baseScopeStack, "entity.name.tag.localname." + scope),
				[ClassificationTypeNames.XmlLiteralProcessingInstruction] = MakeScope (baseScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralText] = MakeScope (baseScopeStack, "text." + scope),
			};
			classificationMapCache = classificationMapCache.SetItem (scope, result);

			return result;
		}

		public void Dispose ()
		{
		}
	}
}