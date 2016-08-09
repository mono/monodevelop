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
using System.Linq;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Analysis;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

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
		HighlightingSegmentTree highlightTree;
		CancellationTokenSource src = new CancellationTokenSource ();

		public CSharpSyntaxMode (TextEditor editor, DocumentContext documentContext) : base (editor, documentContext)
		{
			DocumentParsed ();
		}

		#region implemented abstract members of SemanticHighlighting

		protected override void DocumentParsed ()
		{
			var parsedDocument = documentContext.ParsedDocument;
			if (parsedDocument == null)
				return;
			var resolver = parsedDocument.GetAst<SemanticModel> ();
			if (resolver == null)
				return;
			CancelHighlightingTask ();
			var token = src.Token;

			Task.Run (async delegate {
				try {
					var root = await resolver.SyntaxTree.GetRootAsync (token);
					var newTree = new HighlightingSegmentTree ();

					var visitor = new HighlightingVisitior (resolver, newTree.Add, token, TextSegment.FromBounds(0, root.FullSpan.Length));
					visitor.Visit (root);

					if (!token.IsCancellationRequested) {
						Gtk.Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							if (highlightTree != null) {
								highlightTree.RemoveListener ();
							}
							highlightTree = newTree;
							highlightTree.InstallListener (editor);
							NotifySemanticHighlightingUpdate ();
						});
					}
				} catch (OperationCanceledException) {
				} catch (AggregateException ae) {
					ae.Flatten ().Handle (x => x is OperationCanceledException); 
				}
			}, token);
		}

		void CancelHighlightingTask ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		public override IEnumerable<ColoredSegment> GetColoredSegments (ISegment segment)
		{
			var result = new List<ColoredSegment> ();
			if (highlightTree == null)
				return result;
			return highlightTree.GetSegmentsOverlapping (segment).Select (seg => seg.GetColoredSegment () );
		}

		public override void Dispose ()
		{
			CancelHighlightingTask ();
			if (highlightTree != null)
				highlightTree.RemoveListener ();
			highlightTree = null;
			base.Dispose ();
		}

		#endregion
	}

	class StyledTreeSegment : TreeSegment
	{
		string style;

		public StyledTreeSegment (int offset, int length, string colorStyleKey) : base (offset, length)
		{
			this.style = colorStyleKey;
		}

		public ColoredSegment GetColoredSegment ()
		{
			return new ColoredSegment (Offset, Length, ImmutableStack<string>.Empty.Push (style));
		}
	}

	class HighlightingSegmentTree : SegmentTree<StyledTreeSegment>
	{
	}

	class HighlightingVisitior : SemanticHighlightingVisitor<string>
	{
		readonly Action<StyledTreeSegment> colorizeCallback;

		public HighlightingVisitior (SemanticModel resolver, Action<StyledTreeSegment> colorizeCallback, CancellationToken cancellationToken, ISegment textSpan) : base (resolver)
		{
			if (resolver == null)
				throw new ArgumentNullException (nameof (resolver));
			this.cancellationToken = cancellationToken;
			this.colorizeCallback = colorizeCallback;
			this.region = new TextSpan (textSpan.Offset, textSpan.Length);
			Setup ();
		}

		void Setup ()
		{
			defaultTextColor = EditorThemeColors.Foreground;
			referenceTypeColor = EditorThemeColors.UserTypes;
			valueTypeColor = EditorThemeColors.UserTypesValueTypes;
			interfaceTypeColor = EditorThemeColors.UserTypesInterfaces;
			enumerationTypeColor = EditorThemeColors.UserTypesEnums;
			typeParameterTypeColor = EditorThemeColors.UserTypesTypeParameters;
			delegateTypeColor = EditorThemeColors.UserTypesDelegates;

			methodCallColor = EditorThemeColors.UserMethodUsage;
			methodDeclarationColor = EditorThemeColors.UserMethodDeclaration;

			eventDeclarationColor = EditorThemeColors.UserEventDeclaration;
			eventAccessColor = EditorThemeColors.UserEventUsage;

			fieldDeclarationColor = EditorThemeColors.UserFieldDeclaration;
			fieldAccessColor = EditorThemeColors.UserFieldUsage;

			propertyDeclarationColor = EditorThemeColors.UserPropertyDeclaration;
			propertyAccessColor = EditorThemeColors.UserPropertyUsage;

			variableDeclarationColor = EditorThemeColors.UserVariableDeclaration;
			variableAccessColor = EditorThemeColors.UserVariableUsage;

			parameterDeclarationColor = EditorThemeColors.UserParameterDeclaration;
			parameterAccessColor = EditorThemeColors.UserParameterUsage;

			valueKeywordColor = "keyword.other.source.cs";
			externAliasKeywordColor = "keyword.other.source.cs";
			varKeywordTypeColor = "keyword.other.source.cs";

			parameterModifierColor = "keyword.other.source.cs";
			inactiveCodeColor = "punctuation.definition.comment.source.cs";

			stringFormatItemColor = "constant.character.escape.source.cs";
			nameofKeywordColor = "keyword.other.source.cs";
			whenKeywordColor = "keyword.other.source.cs";

			stringRegexCharacterClass = "constant.character.regex.characterclass.source.cs";
			stringRegexGroupingConstructs = "constant.character.regex.grouping.source.cs";
			stringRegexSetConstructs = "constant.character.regex.set.source.cs";
			stringRegexErrors = "constant.character.regex.errors.source.cs";
			stringRegexComments = "constant.character.regex.comments.source.cs";
			stringRegexEscapeCharacter = "constant.character.regex.escape.source.cs";
			stringRegexAltEscapeCharacter = "constant.character.regex.altescape.source.cs";
		}

		protected override void Colorize (TextSpan span, string color)
		{
			colorizeCallback (new StyledTreeSegment (span.Start, span.Length, color));
		}

		public override void VisitIdentifierName (Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax node)
		{
			switch (node.Identifier.Text) {
			case "nfloat":
			case "nint":
			case "nuint":
				var symbol = base.semanticModel.GetSymbolInfo (node).Symbol as INamedTypeSymbol;
				if (symbol != null && symbol.ContainingNamespace.ToDisplayString () == "System") {
					Colorize (node.Span, "Keyword(Type)");
					return;
				}
				break;
			}
			base.VisitIdentifierName (node);
		}
	}
}
