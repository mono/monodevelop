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
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Analysis;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Threading;
using ICSharpCode.NRefactory;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

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

		class HighlightingVisitior : SemanticHighlightingVisitor<string>
		{
			readonly TextSpan textSpan;
			internal HighlightingSegmentTree tree = new HighlightingSegmentTree ();

			public HighlightingVisitior (SemanticModel resolver, CancellationToken cancellationToken, TextSpan textSpan) : base (resolver)
			{
				if (resolver == null)
					throw new ArgumentNullException ("resolver");
				this.cancellationToken = cancellationToken;
				this.textSpan = textSpan;
				Setup ();
			}

			void Setup ()
			{
				defaultTextColor = "Plain Text";
				referenceTypeColor = "User Types";
				valueTypeColor = "User Types(Value types)";
				interfaceTypeColor = "User Types(Interfaces)";
				enumerationTypeColor = "User Types(Enums)";
				typeParameterTypeColor = "User Types(Type parameters)";
				delegateTypeColor = "User Types(Delegates)";

				methodCallColor = "User Method Usage";
				methodDeclarationColor = "User Method Declaration";

				eventDeclarationColor = "User Event Declaration";
				eventAccessColor = "User Event Usage";

				fieldDeclarationColor ="User Field Declaration";
				fieldAccessColor = "User Field Usage";

				propertyDeclarationColor = "User Property Declaration";
				propertyAccessColor = "User Property Usage";

				variableDeclarationColor = "User Variable Declaration";
				variableAccessColor = "User Variable Usage";

				parameterDeclarationColor = "User Parameter Declaration";
				parameterAccessColor = "User Parameter Usage";

				valueKeywordColor = "Keyword(Context)";
				externAliasKeywordColor = "Keyword(Namespace)";
				varKeywordTypeColor = "Keyword(Type)";

				parameterModifierColor = "Keyword(Parameter)";
				inactiveCodeColor = "Excluded Code";
				syntaxErrorColor = "Syntax Error";

				stringFormatItemColor = "String Format Items";
			}

			protected override void Colorize (TextSpan span, string color)
			{
				if (this.textSpan.IntersectsWith (span)) {
					tree.AddStyle (span.Start, span.End, color);
				}
			}
		}


		public override void Colorize (int offset, int count, Action<int, int, string> colorizeCallback)
		{
			if (resolver == null)
				return;
			int lineNumber = editor.OffsetToLineNumber (offset);
			var visitor = new HighlightingVisitior (resolver, colorizeCallback, CancellationToken.None, lineNumber, offset, count);
			resolver.RootNode.AcceptVisitor (visitor);
		}
		#endregion
	}

	class HighlightingVisitior : SemanticHighlightingVisitor<string>
	{
		readonly int lineNumber;
		readonly int lineOffset;
		readonly int lineLength;
		Action<int, int, string> colorizeCallback;

		public HighlightingVisitior (CSharpAstResolver resolver, Action<int, int, string> colorizeCallback, CancellationToken cancellationToken, int lineNumber, int lineOffset, int lineLength)
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
			defaultTextColor = "Plain Text";
			referenceTypeColor = "User Types";
			valueTypeColor = "User Types(Value types)";
			interfaceTypeColor = "User Types(Interfaces)";
			enumerationTypeColor = "User Types(Enums)";
			typeParameterTypeColor = "User Types(Type parameters)";
			delegateTypeColor = "User Types(Delegates)";

			methodCallColor = "User Method Usage";
			methodDeclarationColor = "User Method Declaration";

			eventDeclarationColor = "User Event Declaration";
			eventAccessColor = "User Event Usage";

			fieldDeclarationColor = "User Field Declaration";
			fieldAccessColor = "User Field Usage";


			propertyDeclarationColor = "User Property Declaration";
			propertyAccessColor = "User Property Usage";

			variableDeclarationColor = "User Variable Declaration";
			variableAccessColor = "User Variable Usage";

			parameterDeclarationColor = "User Parameter Declaration";
			parameterAccessColor = "User Parameter Usage";

			valueKeywordColor = "Keyword(Context)";
			externAliasKeywordColor = "Keyword(Namespace)";
			varKeywordTypeColor = "Keyword(Type)";

			parameterModifierColor = "Keyword(Parameter)";
			inactiveCodeColor = "Excluded Code";
			syntaxErrorColor = "Syntax Error";

			stringFormatItemColor = "String Format Items";
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
			colorizeCallback (startOffset, endOffset, color);
		}
	}
}
