//
// RoslynClassificationHighlighting.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public class RoslynClassificationHighlighting : ISyntaxHighlighting
	{
		readonly DocumentId documentId;
		readonly MonoDevelopWorkspace workspace;
		readonly string defaultScope;
		readonly ScopeStack defaultScopeStack;

		readonly Dictionary<string, string> classificationMap = new Dictionary<string, string> ();

		public DocumentId DocumentId => documentId;

		public RoslynClassificationHighlighting (MonoDevelopWorkspace workspace, DocumentId documentId, string defaultScope)
		{
			this.workspace = workspace;
			this.documentId = documentId;
			this.defaultScope = defaultScope;
			this.defaultScopeStack = new ScopeStack (defaultScope);

			classificationMap [ClassificationTypeNames.Comment] = "comment." + defaultScope;
			classificationMap [ClassificationTypeNames.ExcludedCode] = "comment.excluded." + defaultScope;
			classificationMap [ClassificationTypeNames.Identifier] = defaultScope;
			classificationMap [ClassificationTypeNames.Keyword] = "keyword." + defaultScope;
			classificationMap [ClassificationTypeNames.NumericLiteral] = "constant.numeric." + defaultScope;
			classificationMap [ClassificationTypeNames.Operator] = defaultScope;
			classificationMap [ClassificationTypeNames.PreprocessorKeyword] = "meta.preprocessor." + defaultScope;
			classificationMap [ClassificationTypeNames.StringLiteral] = "string." + defaultScope;
			classificationMap [ClassificationTypeNames.WhiteSpace] = "text." + defaultScope;
			classificationMap [ClassificationTypeNames.Text] = "text." + defaultScope;

			classificationMap [ClassificationTypeNames.PreprocessorText] = "meta.preprocessor.region.name." + defaultScope;
			classificationMap [ClassificationTypeNames.Punctuation] = "punctuation." + defaultScope;
			classificationMap [ClassificationTypeNames.VerbatimStringLiteral] = "string.verbatim." + defaultScope;

			classificationMap [ClassificationTypeNames.ClassName] = "entity.name.class." + defaultScope;
			classificationMap [ClassificationTypeNames.DelegateName] = "entity.name.delegate." + defaultScope;
			classificationMap [ClassificationTypeNames.EnumName] = "entity.name.enum." + defaultScope;
			classificationMap [ClassificationTypeNames.InterfaceName] = "entity.name.interface." + defaultScope;
			classificationMap [ClassificationTypeNames.ModuleName] = "entity.name.module." + defaultScope;
			classificationMap [ClassificationTypeNames.StructName] = "entity.name.struct." + defaultScope;
			classificationMap [ClassificationTypeNames.TypeParameterName] = "entity.name.typeparameter." + defaultScope;

			classificationMap [ClassificationTypeNames.XmlDocCommentAttributeName] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentAttributeValue] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentCDataSection] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentComment] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentDelimiter] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentEntityReference] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentName] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = "comment.line.documentation." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlDocCommentText] = "comment.line.documentation." + defaultScope;

			classificationMap [ClassificationTypeNames.XmlLiteralAttributeName] = "entity.other.attribute-name." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralAttributeQuotes] = "punctuation.definition.string." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralAttributeValue] = "string.quoted." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralCDataSection] = "text." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralComment] = "comment.block." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralDelimiter] = defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralEmbeddedExpression] = defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralEntityReference] = defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralName] = "entity.name.tag.localname." + defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralProcessingInstruction] = defaultScope;
			classificationMap [ClassificationTypeNames.XmlLiteralText] = "text." + defaultScope;
		}

		protected virtual void OnHighlightingStateChanged (global::MonoDevelop.Ide.Editor.LineEventArgs e)
		{
			HighlightingStateChanged?.Invoke (this, e);
		}

		public event EventHandler<LineEventArgs> HighlightingStateChanged;

		public async Task<HighlightedLine> GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken)
		{
			List<ColoredSegment> coloredSegments = new List<ColoredSegment> ();

			int offset = line.Offset;
			int length = line.Length;
			var span = new TextSpan (offset, length);

			var classifications = Classifier.GetClassifiedSpans (await workspace.GetDocument (DocumentId).GetSemanticModelAsync (), span, workspace, cancellationToken);

			int lastClassifiedOffsetEnd = offset;
			ScopeStack scopeStack;

			foreach (var curSpan in classifications) {
				if (curSpan.TextSpan.Start > lastClassifiedOffsetEnd) {
					scopeStack = defaultScopeStack.Push (EditorThemeColors.UserTypes);
					ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - offset, curSpan.TextSpan.Start - lastClassifiedOffsetEnd, scopeStack);
					coloredSegments.Add (whitespaceSegment);
				}

				string styleName = GetStyleNameFromClassificationType (curSpan.ClassificationType);
				scopeStack = defaultScopeStack.Push (styleName);
				ColoredSegment curColoredSegment = new ColoredSegment (curSpan.TextSpan.Start - offset, curSpan.TextSpan.Length, scopeStack);
				coloredSegments.Add (curColoredSegment);

				lastClassifiedOffsetEnd = curSpan.TextSpan.End;
			}

			if (offset + length > lastClassifiedOffsetEnd) {
				scopeStack = defaultScopeStack.Push (EditorThemeColors.UserTypes);
				ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - offset, offset + length - lastClassifiedOffsetEnd, scopeStack);
				coloredSegments.Add (whitespaceSegment);
			}

			return new HighlightedLine (line, coloredSegments);
		}

		string GetStyleNameFromClassificationType (string classificationType)
		{
			string result;
			if (classificationMap.TryGetValue (classificationType, out result))
				return result;
			return defaultScope;
		}

		public Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			return Task.FromResult (defaultScopeStack);
		}

		public void Dispose ()
		{
		}
	}
}
