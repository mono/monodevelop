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
		readonly ScopeStack defaultScope;
		readonly ScopeStack userScope;

		readonly Dictionary<string, ScopeStack> classificationMap;

		public DocumentId DocumentId => documentId;

		public RoslynClassificationHighlighting (MonoDevelopWorkspace workspace, DocumentId documentId, string defaultScope)
		{
			this.workspace = workspace;
			this.documentId = documentId;
			this.defaultScope = new ScopeStack (defaultScope);
			this.userScope = this.defaultScope.Push (EditorThemeColors.UserTypes);

			classificationMap = new Dictionary<string, ScopeStack> {
				[ClassificationTypeNames.Comment] = MakeScope ("comment." + defaultScope),
				[ClassificationTypeNames.ExcludedCode] = MakeScope ("comment.excluded." + defaultScope),
				[ClassificationTypeNames.Identifier] = MakeScope (defaultScope),
				[ClassificationTypeNames.Keyword] = MakeScope ("keyword." + defaultScope),
				[ClassificationTypeNames.NumericLiteral] = MakeScope ("constant.numeric." + defaultScope),
				[ClassificationTypeNames.Operator] = MakeScope (defaultScope),
				[ClassificationTypeNames.PreprocessorKeyword] = MakeScope ("meta.preprocessor." + defaultScope),
				[ClassificationTypeNames.StringLiteral] = MakeScope ("string." + defaultScope),
				[ClassificationTypeNames.WhiteSpace] = MakeScope ("text." + defaultScope),
				[ClassificationTypeNames.Text] = MakeScope ("text." + defaultScope),

				[ClassificationTypeNames.PreprocessorText] = MakeScope ("meta.preprocessor.region.name." + defaultScope),
				[ClassificationTypeNames.Punctuation] = MakeScope ("punctuation." + defaultScope),
				[ClassificationTypeNames.VerbatimStringLiteral] = MakeScope ("string.verbatim." + defaultScope),

				[ClassificationTypeNames.ClassName] = MakeScope ("entity.name.class." + defaultScope),
				[ClassificationTypeNames.DelegateName] = MakeScope ("entity.name.delegate." + defaultScope),
				[ClassificationTypeNames.EnumName] = MakeScope ("entity.name.enum." + defaultScope),
				[ClassificationTypeNames.InterfaceName] = MakeScope ("entity.name.interface." + defaultScope),
				[ClassificationTypeNames.ModuleName] = MakeScope ("entity.name.module." + defaultScope),
				[ClassificationTypeNames.StructName] = MakeScope ("entity.name.struct." + defaultScope),
				[ClassificationTypeNames.TypeParameterName] = MakeScope ("entity.name.typeparameter." + defaultScope),

				[ClassificationTypeNames.XmlDocCommentAttributeName] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentAttributeQuotes] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentAttributeValue] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentCDataSection] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentComment] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentDelimiter] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentEntityReference] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentName] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentProcessingInstruction] = MakeScope ("comment.line.documentation." + defaultScope),
				[ClassificationTypeNames.XmlDocCommentText] = MakeScope ("comment.line.documentation." + defaultScope),

				[ClassificationTypeNames.XmlLiteralAttributeName] = MakeScope ("entity.other.attribute-name." + defaultScope),
				[ClassificationTypeNames.XmlLiteralAttributeQuotes] = MakeScope ("punctuation.definition.string." + defaultScope),
				[ClassificationTypeNames.XmlLiteralAttributeValue] = MakeScope ("string.quoted." + defaultScope),
				[ClassificationTypeNames.XmlLiteralCDataSection] = MakeScope ("text." + defaultScope),
				[ClassificationTypeNames.XmlLiteralComment] = MakeScope ("comment.block." + defaultScope),
				[ClassificationTypeNames.XmlLiteralDelimiter] = MakeScope (defaultScope),
				[ClassificationTypeNames.XmlLiteralEmbeddedExpression] = MakeScope (defaultScope),
				[ClassificationTypeNames.XmlLiteralEntityReference] = MakeScope (defaultScope),
				[ClassificationTypeNames.XmlLiteralName] = MakeScope ("entity.name.tag.localname." + defaultScope),
				[ClassificationTypeNames.XmlLiteralProcessingInstruction] = MakeScope (defaultScope),
				[ClassificationTypeNames.XmlLiteralText] = MakeScope ("text." + defaultScope),
			};
		}

		ScopeStack MakeScope (string scope)
		{
			return defaultScope.Push (scope);
		}

		protected virtual void OnHighlightingStateChanged (global::MonoDevelop.Ide.Editor.LineEventArgs e)
		{
			HighlightingStateChanged?.Invoke (this, e);
		}

		public event EventHandler<LineEventArgs> HighlightingStateChanged;

		public async Task<HighlightedLine> GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken)
		{
			var document = workspace.GetDocument (DocumentId);
			if (document == null)
				return await DefaultSyntaxHighlighting.Instance.GetHighlightedLineAsync (line, cancellationToken);

			// Empirical testing shows that we end up not reallocating the list if we pre-allocate that we have at least 2 times more colored segments than classifiers per line.
			// Current Roslyn API does not allow for a Count getting without iteration, so leave it with a magic number which yields similar results.
			var coloredSegments = new List<ColoredSegment> (32);

			int offset = line.Offset;
			int length = line.Length;
			var span = new TextSpan (offset, length);


			var classifications = Classifier.GetClassifiedSpans (await document.GetSemanticModelAsync ().ConfigureAwait (false), span, workspace, cancellationToken);

			int lastClassifiedOffsetEnd = offset;
			ScopeStack scopeStack;

			foreach (var curSpan in classifications) {
				if (curSpan.TextSpan.Start > lastClassifiedOffsetEnd) {
					scopeStack = userScope;
					ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - offset, curSpan.TextSpan.Start - lastClassifiedOffsetEnd, scopeStack);
					coloredSegments.Add (whitespaceSegment);
				}

				scopeStack = GetStyleScopeStackFromClassificationType (curSpan.ClassificationType);
				ColoredSegment curColoredSegment = new ColoredSegment (curSpan.TextSpan.Start - offset, curSpan.TextSpan.Length, scopeStack);
				coloredSegments.Add (curColoredSegment);

				lastClassifiedOffsetEnd = curSpan.TextSpan.End;
			}

			if (offset + length > lastClassifiedOffsetEnd) {
				scopeStack = userScope;
				ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - offset, offset + length - lastClassifiedOffsetEnd, scopeStack);
				coloredSegments.Add (whitespaceSegment);
			}

			return new HighlightedLine (line, coloredSegments);
		}

		ScopeStack GetStyleScopeStackFromClassificationType (string classificationType)
		{
			ScopeStack result;
			if (classificationMap.TryGetValue (classificationType, out result))
				return result;
			return defaultScope;
		}

		public Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			return Task.FromResult (defaultScope);
		}

		public void Dispose ()
		{
		}
	}
}
