// 
// PlatformCatalog.cs
//  
// Author:
//       David Pugh <dpugh@microsoft.com>
// 
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace Microsoft.VisualStudio.Platform
{
	public sealed class TagBasedSyntaxHighlighting : ISyntaxHighlighting
	{
		internal static ISyntaxHighlighting CreateSyntaxHighlighting(Mono.TextEditor.TextDocument document)
		{
			return new TagBasedSyntaxHighlighting(document);
		}

		private Mono.TextEditor.TextDocument document { get; }
		private IClassifier classifier { get; set; }

		internal TagBasedSyntaxHighlighting(Mono.TextEditor.TextDocument document)
		{
			this.document = document;

		}

		public Task<HighlightedLine> GetHighlightedLineAsync(IDocumentLine line, CancellationToken cancellationToken)
		{
			ITextSnapshotLine snapshotLine = (line as Mono.TextEditor.TextDocument.DocumentLineFromTextSnapshotLine)?.Line;
			if ((this.classifier == null) || (snapshotLine == null))
			{
				return Task.FromResult(new HighlightedLine(new[] { new ColoredSegment(0, line.Length, ScopeStack.Empty) }));
			}

			List<ColoredSegment> coloredSegments = new List<ColoredSegment>();

			SnapshotSpan snapshotSpan = snapshotLine.Extent;
			int lastClassifiedOffsetEnd = snapshotSpan.Start;
			ScopeStack scopeStack;

			IList<ClassificationSpan> classifications = this.classifier.GetClassificationSpans(snapshotSpan);
			foreach (ClassificationSpan curSpan in classifications)
			{
				if (curSpan.Span.Start > lastClassifiedOffsetEnd)
				{
					scopeStack = new ScopeStack(EditorThemeColors.UserTypes);
					ColoredSegment whitespaceSegment = new ColoredSegment(lastClassifiedOffsetEnd - snapshotLine.Start, curSpan.Span.Start - lastClassifiedOffsetEnd, scopeStack);
					coloredSegments.Add(whitespaceSegment);
				}

				string styleName = GetStyleNameFromClassificationType(curSpan.ClassificationType);
				scopeStack = new ScopeStack(styleName);
				ColoredSegment curColoredSegment = new ColoredSegment(curSpan.Span.Start - snapshotLine.Start, curSpan.Span.Length, scopeStack);
				coloredSegments.Add(curColoredSegment);

				lastClassifiedOffsetEnd = curSpan.Span.End;
			}

			if (snapshotLine.End.Position  > lastClassifiedOffsetEnd)
			{
				scopeStack = new ScopeStack(EditorThemeColors.UserTypes);
				ColoredSegment whitespaceSegment = new ColoredSegment(lastClassifiedOffsetEnd - snapshotLine.Start, snapshotLine.End.Position - lastClassifiedOffsetEnd, scopeStack);
				coloredSegments.Add(whitespaceSegment);
			}

			HighlightedLine result = new HighlightedLine(coloredSegments);
			return Task.FromResult(result);
		}

		public Task<ScopeStack> GetScopeStackAsync(int offset, CancellationToken cancellationToken)
		{
			return Task.FromResult(ScopeStack.Empty);
		}

		private EventHandler<LineEventArgs> _highlightingStateChanged;
		public event EventHandler<LineEventArgs> HighlightingStateChanged
		{
			add
			{
				lock(this)
				{
					_highlightingStateChanged += value;
				}

				if (this.classifier == null)
				{
					this.classifier = PlatformCatalog.Instance.ClassifierAggregatorService.GetClassifier(this.document.TextBuffer);
					this.classifier.ClassificationChanged += this.OnClassificationChanged;
				}
			}

			remove
			{
				bool dispose = false;
				lock (this)
				{
					_highlightingStateChanged -= value;
					dispose = _highlightingStateChanged == null;
				}

				if (dispose && (this.classifier != null))
				{
					this.classifier.ClassificationChanged -= this.OnClassificationChanged;
					(this.classifier as IDisposable)?.Dispose();
					this.classifier = null;
				}
			}
		}

		private void OnClassificationChanged(object sender, ClassificationChangedEventArgs args)
		{
			var handler = _highlightingStateChanged;
			if (handler != null)
			{

			}
		}

		private string GetStyleNameFromClassificationType(IClassificationType classificationType)
		{
			string styleName = EditorThemeColors.Foreground;

			// MONO: TODO: get this from the EditorFormat?
			switch (classificationType.Classification)
			{
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
					styleName = "meta.property-value.css";
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
					//styleName = style.HtmlServerSideScript.Name;
					break;
				case "HTML Tag Delimiter":
					styleName = "punctuation.definition.tag.begin.html";
					break;
				case "RazorCode":
					//styleName = style.RazorCode.Name;
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

				default:
					styleName = EditorThemeColors.Foreground;
					break;
			}

			return styleName;
		}
	}
}