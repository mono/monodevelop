// 
// SyntaxMode.cs
//  
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CSharp.Highlighting
{
	static class StringHelper
	{
		public static bool IsAt (this string str, int idx, string pattern)
		{
			if (idx + pattern.Length > str.Length)
				return false;

			for (int i = 0; i < pattern.Length; i++)
				if (pattern [i] != str [idx + i])
					return false;
			return true;
		}
	}

	class CSharpSyntaxMode : SemanticHighlighting
	{
		CSharpAstResolver resolver;
		CancellationTokenSource src;

		public CSharpSyntaxMode (TextEditor editor, DocumentContext documentContext) : base (editor, documentContext)
		{
		}

		#region implemented abstract members of SemanticHighlighting

		protected override void DocumentParsed ()
		{
			if (src != null)
				src.Cancel ();
			resolver = null;
			if (documentContext.IsProjectContextInUpdate)
				return;
			var parsedDocument = documentContext.ParsedDocument;
			if (parsedDocument != null) {
				if (documentContext.Project != null && documentContext.IsCompileableInProject) {
					src = new CancellationTokenSource ();
					var newResolverTask = documentContext.GetSharedResolver ();
					var cancellationToken = src.Token;
					System.Threading.Tasks.Task.Factory.StartNew (delegate {
						if (newResolverTask == null)
							return;
						var newResolver = newResolverTask.Result;
						if (newResolver == null)
							return;
						if (!cancellationToken.IsCancellationRequested) {
							Gtk.Application.Invoke (delegate {
								if (cancellationToken.IsCancellationRequested)
									return;
								if (!parsedDocument.HasErrors) {
									resolver = newResolver;
									UpdateSemanticHighlighting ();
								}
							});
						}
					}, cancellationToken);
				}
			}
		}

		public override void Colorize (ISegment segment, Action<ISegment, string> colorizeCallback)
		{
			if (resolver == null)
				return;
			int lineNumber = editor.OffsetToLineNumber (segment.Offset);
			var visitor = new HighlightingVisitior (resolver, colorizeCallback, CancellationToken.None, lineNumber, segment.Offset, segment.Length);
			resolver.RootNode.AcceptVisitor (visitor);
		}
		#endregion
	}

	class HighlightingVisitior : SemanticHighlightingVisitor<string>
	{
		readonly int lineNumber;
		readonly int lineOffset;
		readonly int lineLength;
		Action<ISegment, string> colorizeCallback;

		public HighlightingVisitior (CSharpAstResolver resolver, Action<ISegment, string> colorizeCallback, CancellationToken cancellationToken, int lineNumber, int lineOffset, int lineLength)
		{
			if (resolver == null)
				throw new ArgumentNullException ("resolver");
			this.resolver = resolver;
			this.cancellationToken = cancellationToken;
			this.lineNumber = lineNumber;
			this.lineOffset = lineOffset;
			this.lineLength = lineLength;
			this.colorizeCallback = colorizeCallback;
			regionStart = new TextLocation (lineNumber, 1);
			regionEnd = new TextLocation (lineNumber, lineLength);

			Setup ();
		}

		void Setup ()
		{
			
			defaultTextColor = Mono.TextEditor.Highlighting.ColorScheme.PlainTextKey;
			referenceTypeColor = Mono.TextEditor.Highlighting.ColorScheme.UserTypesKey;
			valueTypeColor = Mono.TextEditor.Highlighting.ColorScheme.UserTypesValueTypesKey;
			interfaceTypeColor = Mono.TextEditor.Highlighting.ColorScheme.UserTypesInterfacesKey;
			enumerationTypeColor = Mono.TextEditor.Highlighting.ColorScheme.UserTypesEnumsKey;
			typeParameterTypeColor = Mono.TextEditor.Highlighting.ColorScheme.UserTypesTypeParametersKey;
			delegateTypeColor = Mono.TextEditor.Highlighting.ColorScheme.UserTypesDelegatesKey;

			methodCallColor = Mono.TextEditor.Highlighting.ColorScheme.UserMethodUsageKey;
			methodDeclarationColor = Mono.TextEditor.Highlighting.ColorScheme.UserMethodDeclarationKey;

			eventDeclarationColor = Mono.TextEditor.Highlighting.ColorScheme.UserEventDeclarationKey;
			eventAccessColor = Mono.TextEditor.Highlighting.ColorScheme.UserEventUsageKey;

			fieldDeclarationColor = Mono.TextEditor.Highlighting.ColorScheme.UserFieldDeclarationKey;
			fieldAccessColor = Mono.TextEditor.Highlighting.ColorScheme.UserFieldUsageKey;

			propertyDeclarationColor = Mono.TextEditor.Highlighting.ColorScheme.UserPropertyDeclarationKey;
			propertyAccessColor = Mono.TextEditor.Highlighting.ColorScheme.UserPropertyUsageKey;

			variableDeclarationColor = Mono.TextEditor.Highlighting.ColorScheme.UserVariableDeclarationKey;
			variableAccessColor = Mono.TextEditor.Highlighting.ColorScheme.UserVariableUsageKey;

			parameterDeclarationColor = Mono.TextEditor.Highlighting.ColorScheme.UserParameterDeclarationKey;
			parameterAccessColor = Mono.TextEditor.Highlighting.ColorScheme.UserParameterUsageKey;

			valueKeywordColor = Mono.TextEditor.Highlighting.ColorScheme.KeywordContextKey;
			externAliasKeywordColor = Mono.TextEditor.Highlighting.ColorScheme.KeywordNamespaceKey;
			varKeywordTypeColor = Mono.TextEditor.Highlighting.ColorScheme.KeywordTypesKey;

			parameterModifierColor = Mono.TextEditor.Highlighting.ColorScheme.KeywordParameterKey;
			inactiveCodeColor = Mono.TextEditor.Highlighting.ColorScheme.ExcludedCodeKey;
			syntaxErrorColor = Mono.TextEditor.Highlighting.ColorScheme.SyntaxErrorKey;

			stringFormatItemColor = Mono.TextEditor.Highlighting.ColorScheme.StringFormatItemsKey;
		}

		protected override void Colorize (TextLocation start, TextLocation end, string color)
		{
			int startOffset;
			if (start.Line == lineNumber) {
				startOffset = lineOffset + start.Column - 1;
			} else {
				if (start.Line > lineNumber)
					return;
				startOffset = lineOffset;
			}
			int endOffset;
			if (end.Line == lineNumber) {
				endOffset = lineOffset + end.Column - 1;
			} else {
				if (end.Line < lineNumber)
					return;
				endOffset = lineOffset + lineLength;
			}
			colorizeCallback (TextSegment.FromBounds (startOffset, endOffset), color);
		}
	}
}