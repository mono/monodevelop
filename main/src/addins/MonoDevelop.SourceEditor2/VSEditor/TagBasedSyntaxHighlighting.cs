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

namespace Microsoft.VisualStudio.Platform
{
    [Export(typeof(ITagBasedSyntaxHighlightingFactory))]
    internal sealed class TagBasedSyntaxHighlightingFactory : ITagBasedSyntaxHighlightingFactory {
        public ISyntaxHighlighting CreateSyntaxHighlighting (ITextView textView) {
            return new TagBasedSyntaxHighlighting(textView);
        }
    }

    internal sealed class TagBasedSyntaxHighlighting : ISyntaxHighlighting
    {
        private static string[] delimiters = new string[] { " - " };
        private ITextBuffer textBuffer { get; }
        private IClassifier classifier { get; set; }
        private MonoDevelop.Ide.Editor.ITextDocument textDocument { get; }

        internal TagBasedSyntaxHighlighting(ITextView textView)
        {
            this.textBuffer = textView.TextBuffer;
            this.textDocument = textView.GetTextEditor();
        }

        public async Task<HighlightedLine> GetHighlightedLineAsync(IDocumentLine line, CancellationToken cancellationToken)
        {
            //TODO verify that the snapshot line from this.textBuffer is equivalent to the document line converted to a snapshotline.
            //Possibly take in a TextDataModel as a parameter and verify the buffers are appropriate.
            //ITextSnapshotLine snapshotLine = (line as Mono.TextEditor.TextDocument.DocumentLineFromTextSnapshotLine)?.Line;
            ITextSnapshotLine snapshotLine = textBuffer.CurrentSnapshot.GetLineFromLineNumber (line.LineNumber - 1);
            if ((this.classifier == null) || (snapshotLine == null))
            {
                return new HighlightedLine(line, new[] { new ColoredSegment(0, line.Length, ScopeStack.Empty) });
            }

            List<ColoredSegment> coloredSegments = new List<ColoredSegment>();

            SnapshotSpan snapshotSpan = snapshotLine.Extent;
            int lastClassifiedOffsetEnd = snapshotSpan.Start;
            ScopeStack scopeStack;

            IList<ClassificationSpan> classifications = await MonoDevelop.Core.Runtime.RunInMainThread (() => this.classifier.GetClassificationSpans (snapshotSpan));

            foreach (ClassificationSpan curSpan in classifications)
            {
                if (curSpan.Span.Start > lastClassifiedOffsetEnd)
                {
                    scopeStack = new ScopeStack(EditorThemeColors.Foreground);
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
                scopeStack = new ScopeStack(EditorThemeColors.Foreground);
                ColoredSegment whitespaceSegment = new ColoredSegment(lastClassifiedOffsetEnd - snapshotLine.Start, snapshotLine.End.Position - lastClassifiedOffsetEnd, scopeStack);
                coloredSegments.Add(whitespaceSegment);
            }

            HighlightedLine result = new HighlightedLine(line, coloredSegments);
            return result;
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
                    this.classifier = PlatformCatalog.Instance.ClassifierAggregatorService.GetClassifier(this.textBuffer);
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
                int startLineIndex = this.textDocument.OffsetToLineNumber (args.ChangeSpan.Start);
                int endLineIndex = this.textDocument.OffsetToLineNumber (args.ChangeSpan.End);

                for (int curLineIndex = startLineIndex; curLineIndex <= endLineIndex; curLineIndex++)
                {
                    IDocumentLine documentLine = this.textDocument.GetLine(curLineIndex);
                    handler(this, new LineEventArgs(documentLine));
                }
            }
        }

        private string GetStyleNameFromClassificationType (IClassificationType classificationType)
        {
            string styleName = null;
            string[] classificationNames = classificationType.Classification.Split (TagBasedSyntaxHighlighting.delimiters, StringSplitOptions.None);

            for (int i = classificationNames.Length - 1; i >= 0 && styleName == null; i--) {
                styleName = GetStyleNameFromClassificationName (classificationNames[i]);
            }

            return styleName ?? EditorThemeColors.Foreground;
        }

        private string GetStyleNameFromClassificationName (string classificationName) {
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

        public void Dispose()
        {
        }
    }
}