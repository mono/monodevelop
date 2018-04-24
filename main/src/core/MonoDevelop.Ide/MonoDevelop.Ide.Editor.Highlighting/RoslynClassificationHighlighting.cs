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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.TypeSystem;
using Roslyn.Utilities;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public class RoslynClassificationHighlighting : ISyntaxHighlighting
	{
		readonly DocumentId documentId;
		readonly MonoDevelopWorkspace workspace;
		readonly ScopeStack defaultScope;
		readonly ScopeStack userScope;
		readonly ISemanticChangeNotificationService semanticChangeNotificationService;
		readonly Dictionary<string, ScopeStack> classificationMap;

		public DocumentId DocumentId => documentId;
		VersionStamp lastSemanticVersion;
		public RoslynClassificationHighlighting (MonoDevelopWorkspace workspace, DocumentId documentId, string defaultScope)
		{
			this.workspace = workspace;
			this.documentId = documentId;
			this.defaultScope = new ScopeStack (defaultScope);
			this.userScope = this.defaultScope.Push (EditorThemeColors.UserTypes);

			classificationMap = GetClassificationMap (defaultScope);

			semanticChangeNotificationService = Composition.CompositionManager.GetExportedValue<ISemanticChangeNotificationService> ();
			if (semanticChangeNotificationService != null) {
				semanticChangeNotificationService.OpenedDocumentSemanticChanged += SemanticChangeNotificationService_OpenedDocumentSemanticChanged;
			}
			workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
		}

		async void Workspace_WorkspaceChanged (object sender, WorkspaceChangeEventArgs e)
		{
			await CheckForSemanticChange ();
		}

		async void SemanticChangeNotificationService_OpenedDocumentSemanticChanged (object sender, Document e)
		{
			await CheckForSemanticChange ();
		}

		async Task CheckForSemanticChange ()
		{
			var document = workspace.GetDocument (DocumentId);
			if (document == null)
				return;
			var projectVersion = await document.Project.GetDependentSemanticVersionAsync (CancellationToken.None);
			if (lastSemanticVersion != projectVersion) {
				lastSemanticVersion = projectVersion;
				await Runtime.RunInMainThread (delegate {
					HighlightingStateChanged?.Invoke (null, LineEventArgs.AllLines);
				});
			}
		}

		static ImmutableDictionary<string, Dictionary<string, ScopeStack>> classificationMapCache = ImmutableDictionary<string, Dictionary<string, ScopeStack>>.Empty;

		public static Dictionary<string, ScopeStack> GetClassificationMap (string scope)
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

		static ScopeStack MakeScope (ScopeStack defaultScope, string scope)
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
				var start = Math.Max (offset, curSpan.TextSpan.Start);
				if (start < lastClassifiedOffsetEnd) { // Work around for : https://github.com/dotnet/roslyn/issues/25648
					continue;
				}
				if (start > lastClassifiedOffsetEnd) {
					scopeStack = defaultScope;
					ColoredSegment whitespaceSegment = new ColoredSegment (lastClassifiedOffsetEnd - offset, start - lastClassifiedOffsetEnd, scopeStack);
					coloredSegments.Add (whitespaceSegment);
				}
				scopeStack = GetStyleScopeStackFromClassificationType (curSpan.ClassificationType);
				ColoredSegment curColoredSegment = new ColoredSegment (curSpan.TextSpan.Start - offset, curSpan.TextSpan.Length, scopeStack);
				coloredSegments.Add (curColoredSegment);

				lastClassifiedOffsetEnd = curSpan.TextSpan.End;
			}

			if (offset + length > lastClassifiedOffsetEnd) {
				scopeStack = defaultScope;
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
			workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
			if (semanticChangeNotificationService != null) 
				semanticChangeNotificationService.OpenedDocumentSemanticChanged -= SemanticChangeNotificationService_OpenedDocumentSemanticChanged;
		}
	}
}
