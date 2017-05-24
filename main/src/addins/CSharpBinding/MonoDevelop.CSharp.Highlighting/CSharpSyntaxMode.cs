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
using MonoDevelop.Components;

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
			var theme = editor.Options.GetEditorTheme ();
			Task.Run (async delegate {
				try {
					var root = await resolver.SyntaxTree.GetRootAsync (token);
					var newTree = new HighlightingSegmentTree ();

					var visitor = new HighlightingVisitior (theme, resolver, newTree.Add, token, TextSegment.FromBounds(0, root.FullSpan.Length));
					visitor.Visit (root);
					var doNotify = !AreEqual (highlightTree, newTree, token);

					if (!token.IsCancellationRequested) {
						Gtk.Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							if (highlightTree != null) {
								highlightTree.RemoveListener ();
							}
							highlightTree = newTree;
							highlightTree.InstallListener (editor);
							if (doNotify) {
								NotifySemanticHighlightingUpdate ();
							}
						});
					}
				} catch (OperationCanceledException) {
				} catch (AggregateException ae) {
					ae.Flatten ().Handle (x => x is OperationCanceledException); 
				}
			}, token);
		}

		bool AreEqual (HighlightingSegmentTree highlightTree, HighlightingSegmentTree newTree, CancellationToken token)
		{
			if (newTree == null || highlightTree == null ||  highlightTree.Count != newTree.Count)
				return false;
			var e1 = highlightTree.GetEnumerator ();
			var e2 = newTree.GetEnumerator ();
			int i = 0;
			while (e1.MoveNext () && e2.MoveNext ()) {
				var i1 = e1.Current;
				var i2 = e2.Current;
				if (i++ % 1000 == 0) {
					if (token.IsCancellationRequested)
						return false;
				}
				if (i1.Offset != i2.Offset ||
					i1.Length != i2.Length ||
					i1.Style != i2.Style)
					return false;
			}

			return true;
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
		readonly string style;
		
		public string Style {
			get {
				return style;
			}
		}

		public StyledTreeSegment (int offset, int length, string colorStyleKey) : base (offset, length)
		{
			this.style = colorStyleKey;
		}

		public ColoredSegment GetColoredSegment ()
		{
			return new ColoredSegment (Offset, Length,  csScope.Push(style));
		}

		static readonly ScopeStack csScope = new ScopeStack ("source.cs");
		
	}

	class HighlightingSegmentTree : SegmentTree<StyledTreeSegment>
	{
	}

	class HighlightingVisitior : SemanticHighlightingVisitor<string>
	{
		readonly Action<StyledTreeSegment> colorizeCallback;
		readonly EditorTheme theme;
		HslColor defaultColor;

		public HighlightingVisitior (EditorTheme theme, SemanticModel resolver, Action<StyledTreeSegment> colorizeCallback, CancellationToken cancellationToken, ISegment textSpan) : base (resolver)
		{
			if (resolver == null)
				throw new ArgumentNullException (nameof (resolver));
			this.theme = theme;
			this.cancellationToken = cancellationToken;
			this.colorizeCallback = colorizeCallback;
			this.region = new TextSpan (textSpan.Offset, textSpan.Length);
			theme.TryGetColor (EditorThemeColors.Foreground, out defaultColor);
			Setup ();
		}

		void Setup ()
		{
			defaultTextColor = CheckScopeExists ("");
			referenceTypeColor = CheckScopeExists (EditorThemeColors.UserTypes);
			valueTypeColor = CheckScopeExists (EditorThemeColors.UserTypesValueTypes);
			interfaceTypeColor = CheckScopeExists (EditorThemeColors.UserTypesInterfaces);
			enumerationTypeColor = CheckScopeExists (EditorThemeColors.UserTypesEnums);
			typeParameterTypeColor = CheckScopeExists (EditorThemeColors.UserTypesTypeParameters);
			delegateTypeColor = CheckScopeExists (EditorThemeColors.UserTypesDelegates);

			methodCallColor = CheckScopeExists (EditorThemeColors.UserMethodUsage);
			methodDeclarationColor = CheckScopeExists (EditorThemeColors.UserMethodDeclaration);

			eventDeclarationColor = CheckScopeExists (EditorThemeColors.UserEventDeclaration);
			eventAccessColor = CheckScopeExists (EditorThemeColors.UserEventUsage);

			fieldDeclarationColor = CheckScopeExists (EditorThemeColors.UserFieldDeclaration);
			fieldAccessColor = CheckScopeExists (EditorThemeColors.UserFieldUsage);

			propertyDeclarationColor = CheckScopeExists (EditorThemeColors.UserPropertyDeclaration);
			propertyAccessColor = CheckScopeExists (EditorThemeColors.UserPropertyUsage);

			variableDeclarationColor = CheckScopeExists (EditorThemeColors.UserVariableDeclaration);
			variableAccessColor = CheckScopeExists(EditorThemeColors.UserVariableUsage);

			parameterDeclarationColor = CheckScopeExists(EditorThemeColors.UserParameterDeclaration);
			parameterAccessColor = CheckScopeExists(EditorThemeColors.UserParameterUsage);

			valueKeywordColor = CheckScopeExists("keyword.other.source.cs");
			externAliasKeywordColor = CheckScopeExists("keyword.other.source.cs");
			varKeywordTypeColor = CheckScopeExists("keyword.other.source.cs");

			parameterModifierColor = CheckScopeExists("keyword.other.source.cs");
			inactiveCodeColor = CheckScopeExists("comment.inactivecode.source.cs");

			stringFormatItemColor = CheckScopeExists("constant.character.escape.source.cs");
			nameofKeywordColor = CheckScopeExists("keyword.other.source.cs");
			whenKeywordColor = CheckScopeExists("keyword.other.source.cs");

			stringRegexCharacterClass = CheckScopeExists("constant.character.regex.characterclass.source.cs");
			stringRegexGroupingConstructs = CheckScopeExists("constant.character.regex.grouping.source.cs");
			stringRegexSetConstructs = CheckScopeExists("constant.character.regex.set.source.cs");
			stringRegexErrors = CheckScopeExists("constant.character.regex.errors.source.cs");
			stringRegexComments = CheckScopeExists("constant.character.regex.comments.source.cs");
			stringRegexEscapeCharacter = CheckScopeExists("constant.character.regex.escape.source.cs");
			stringRegexAltEscapeCharacter = CheckScopeExists("constant.character.regex.altescape.source.cs");
		}

		string CheckScopeExists (string color)
		{
			HslColor c;
			if (!theme.TryGetColor (color, EditorThemeColors.Foreground, out c) || c.Equals (defaultColor))
				return null;
			return color;
		}

		protected override void Colorize (TextSpan span, string color)
		{
			if (color == null)
				return;
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
					Colorize (node.Span, "keyword.source.cs");
					return;
				}
				break;
			}
			base.VisitIdentifierName (node);
		}
	}
}
