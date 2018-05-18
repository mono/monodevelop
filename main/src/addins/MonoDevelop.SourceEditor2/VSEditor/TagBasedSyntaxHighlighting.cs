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

namespace Microsoft.VisualStudio.Platform
{
	[Export (typeof (ITagBasedSyntaxHighlightingFactory))]
	internal sealed class TagBasedSyntaxHighlightingFactory : ITagBasedSyntaxHighlightingFactory
	{
		public ISyntaxHighlighting CreateSyntaxHighlighting (ITextView textView)
		{
			return new TagBasedSyntaxHighlighting (textView, null);
		}

		public ISyntaxHighlighting CreateSyntaxHighlighting (ITextView textView, string defaultScope)
		{
			return new TagBasedSyntaxHighlighting (textView, defaultScope);
		}
	}

	internal sealed class TagBasedSyntaxHighlighting : ISyntaxHighlighting
	{
		private ITextView textView { get; }
		private IAccurateClassifier classifier { get; set; }
		readonly Dictionary<string, ScopeStack> classificationMap;
		private MonoDevelop.Ide.Editor.ITextDocument textDocument { get; }

		internal TagBasedSyntaxHighlighting (ITextView textView, string defaultScope)
		{
			this.textView = textView;
			this.textDocument = textView.GetTextEditor ();
			if (defaultScope != null)
				classificationMap = GetClassificationMap (defaultScope);
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
					scopeStack = new ScopeStack (EditorThemeColors.Foreground);
					ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - start, curSpan.Span.Start - lastClassifiedOffsetEnd, scopeStack);
					coloredSegments.Add (whitespaceSegment);
				}

				scopeStack = GetScopeStackFromClassificationType (curSpan.ClassificationType);
				ColoredSegment curColoredSegment = new ColoredSegment (curSpan.Span.Start - start, curSpan.Span.Length, scopeStack);
				coloredSegments.Add (curColoredSegment);

				lastClassifiedOffsetEnd = curSpan.Span.End;
			}

			if (end > lastClassifiedOffsetEnd) {
				scopeStack = new ScopeStack (EditorThemeColors.Foreground);
				ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - start, end - lastClassifiedOffsetEnd, scopeStack);
				coloredSegments.Add (whitespaceSegment);
			}

			return Task.FromResult(new HighlightedLine (line, coloredSegments));
		}

		public Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			return Task.FromResult (ScopeStack.Empty);
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

        private void OnClassificationChanged (object sender, ClassificationChangedEventArgs args)
		{
			var handler = _highlightingStateChanged;
			if (handler != null) {
				foreach (Mono.TextEditor.MdTextViewLineCollection.MdTextViewLine line in textView.TextViewLines) {
					if (line.Start.Position > args.ChangeSpan.End.Position || line.End.Position < args.ChangeSpan.Start)
						continue;
					var oldSegments = line.layoutWrapper.HighlightedLine.Segments;
					var newSegments = GetHighlightedLineAsync (line.line, CancellationToken.None).Result.Segments;
					if (oldSegments.Count != newSegments.Count) {
						handler (this, new LineEventArgs (line.line));
						continue;
					}
					for (int i = 0; i < oldSegments.Count; i++) {
						if (newSegments [i].ColorStyleKey != oldSegments [i].ColorStyleKey) {
							handler (this, new LineEventArgs (line.line));
							break;
						}
					}
				}
			}
		}

		Dictionary<IClassificationType, ScopeStack> classificationTypeToScopeCache = new Dictionary<IClassificationType, ScopeStack> ();
		static ScopeStack defaultScopeStack = new ScopeStack (EditorThemeColors.Foreground);

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
				//styleName = "punctuation.section.embedded.begin"; // suggested by mike, does nothing
				//styleName = "punctuation.section.embedded.begin.cs"; // suggested by mike, does nothing
				styleName = "meta.preprocessor.source.cs"; // TODO: Find a name to use here
														   //styleName = style.HtmlServerSideScript.Name;
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
		static Dictionary<string, ScopeStack> GetClassificationMap (string scope)
		{
			Dictionary<string, ScopeStack> result;
			if (classificationMapCache.TryGetValue (scope, out result))
				return result;
			var defaultScopeStack = new ScopeStack (scope);
			result = new Dictionary<string, ScopeStack> {
				[ClassificationTypeNames.Comment] = MakeScope (defaultScopeStack, "comment." + scope),
				[ClassificationTypeNames.ExcludedCode] = MakeScope (defaultScopeStack, "comment.excluded." + scope),
				[ClassificationTypeNames.Identifier] = MakeScope (defaultScopeStack, scope),
				[ClassificationTypeNames.Keyword] = MakeScope (defaultScopeStack, "keyword." + scope),
				[ClassificationTypeNames.NumericLiteral] = MakeScope (defaultScopeStack, "constant.numeric." + scope),
				[ClassificationTypeNames.Operator] = MakeScope (defaultScopeStack, scope),
				[ClassificationTypeNames.PreprocessorKeyword] = MakeScope (defaultScopeStack, "meta.preprocessor." + scope),
				[ClassificationTypeNames.StringLiteral] = MakeScope (defaultScopeStack, "string." + scope),
				[ClassificationTypeNames.WhiteSpace] = MakeScope (defaultScopeStack, "text." + scope),
				[ClassificationTypeNames.Text] = MakeScope (defaultScopeStack, "text." + scope),

				[ClassificationTypeNames.PreprocessorText] = MakeScope (defaultScopeStack, "meta.preprocessor.region.name." + scope),
				[ClassificationTypeNames.Punctuation] = MakeScope (defaultScopeStack, "punctuation." + scope),
				[ClassificationTypeNames.VerbatimStringLiteral] = MakeScope (defaultScopeStack, "string.verbatim." + scope),

				[ClassificationTypeNames.ClassName] = MakeScope (defaultScopeStack, "entity.name.class." + scope),
				[ClassificationTypeNames.DelegateName] = MakeScope (defaultScopeStack, "entity.name.delegate." + scope),
				[ClassificationTypeNames.EnumName] = MakeScope (defaultScopeStack, "entity.name.enum." + scope),
				[ClassificationTypeNames.InterfaceName] = MakeScope (defaultScopeStack, "entity.name.interface." + scope),
				[ClassificationTypeNames.ModuleName] = MakeScope (defaultScopeStack, "entity.name.module." + scope),
				[ClassificationTypeNames.StructName] = MakeScope (defaultScopeStack, "entity.name.struct." + scope),
				[ClassificationTypeNames.TypeParameterName] = MakeScope (defaultScopeStack, "entity.name.typeparameter." + scope),

				[ClassificationTypeNames.FieldName] = MakeScope (defaultScopeStack, "entity.name.field." + scope),
				[ClassificationTypeNames.EnumMemberName] = MakeScope (defaultScopeStack, "entity.name.enummember." + scope),
				[ClassificationTypeNames.ConstantName] = MakeScope (defaultScopeStack, "entity.name.constant." + scope),
				[ClassificationTypeNames.LocalName] = MakeScope (defaultScopeStack, "entity.name.local." + scope),
				[ClassificationTypeNames.ParameterName] = MakeScope (defaultScopeStack, "entity.name.parameter." + scope),
				[ClassificationTypeNames.ExtensionMethodName] = MakeScope (defaultScopeStack, "entity.name.extensionmethod." + scope),
				[ClassificationTypeNames.MethodName] = MakeScope (defaultScopeStack, "entity.name.function." + scope),
				[ClassificationTypeNames.PropertyName] = MakeScope (defaultScopeStack, "entity.name.property." + scope),
				[ClassificationTypeNames.EventName] = MakeScope (defaultScopeStack, "entity.name.event." + scope),

				[ClassificationTypeNames.XmlDocCommentAttributeName] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentAttributeQuotes] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentAttributeValue] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentCDataSection] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentComment] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentDelimiter] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentEntityReference] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentName] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentProcessingInstruction] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),
				[ClassificationTypeNames.XmlDocCommentText] = MakeScope (defaultScopeStack, "comment.line.documentation." + scope),

				[ClassificationTypeNames.XmlLiteralAttributeName] = MakeScope (defaultScopeStack, "entity.other.attribute-name." + scope),
				[ClassificationTypeNames.XmlLiteralAttributeQuotes] = MakeScope (defaultScopeStack, "punctuation.definition.string." + scope),
				[ClassificationTypeNames.XmlLiteralAttributeValue] = MakeScope (defaultScopeStack, "string.quoted." + scope),
				[ClassificationTypeNames.XmlLiteralCDataSection] = MakeScope (defaultScopeStack, "text." + scope),
				[ClassificationTypeNames.XmlLiteralComment] = MakeScope (defaultScopeStack, "comment.block." + scope),
				[ClassificationTypeNames.XmlLiteralDelimiter] = MakeScope (defaultScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralEmbeddedExpression] = MakeScope (defaultScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralEntityReference] = MakeScope (defaultScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralName] = MakeScope (defaultScopeStack, "entity.name.tag.localname." + scope),
				[ClassificationTypeNames.XmlLiteralProcessingInstruction] = MakeScope (defaultScopeStack, scope),
				[ClassificationTypeNames.XmlLiteralText] = MakeScope (defaultScopeStack, "text." + scope),
			};
			classificationMapCache = classificationMapCache.SetItem (scope, result);

			return result;
		}

		public void Dispose ()
		{
		}
	}
}