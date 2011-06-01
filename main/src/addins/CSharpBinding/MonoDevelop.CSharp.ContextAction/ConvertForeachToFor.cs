// 
// ConvertForeachToFor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;

namespace MonoDevelop.CSharp.ContextAction
{
	public class ConvertForeachToFor : CSharpContextAction
	{
		public override string GetMenuText (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			return GettextCatalog.GetString ("Convert 'foreach' loop to 'for'");
		}
		
		ForeachStatement GetForeachStatement (ParsedDocument doc, DomLocation loc)
		{
			var unit = doc.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			AstNode astNode = unit.GetNodeAt (loc.Line, loc.Column);
			return (astNode as ForeachStatement) ?? astNode.Parent as ForeachStatement;
		}
		
		public override bool IsValid (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var foreachStatement = GetForeachStatement (document.ParsedDocument, loc);
			return foreachStatement != null;
		}

		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var foreachStatement = GetForeachStatement (document.ParsedDocument, loc);
			if (foreachStatement == null)
				return;
			var resolver = GetResolver (document);
			
			var result = resolver.Resolve (foreachStatement.InExpression.ToString (), new DomLocation (foreachStatement.InExpression.StartLocation.Line, foreachStatement.InExpression.StartLocation.Column));
			string itemNumberProperty = "Count";
			
			if (result != null && result.ResolvedType != null && result.ResolvedType.ArrayDimensions > 0)
				itemNumberProperty = "Length";
			
			ForStatement forStatement = new ForStatement () {
				Initializers = {
					new VariableDeclarationStatement (new PrimitiveType ("int"), "i", new PrimitiveExpression (0))
				},
				Condition = new BinaryOperatorExpression (new IdentifierExpression ("i"), BinaryOperatorType.LessThan, new MemberReferenceExpression (foreachStatement.InExpression.Clone (), itemNumberProperty)),
				Iterators = {
					new ExpressionStatement (new UnaryOperatorExpression (UnaryOperatorType.PostIncrement, new IdentifierExpression ("i")))
				},
				EmbeddedStatement = new BlockStatement {
					new VariableDeclarationStatement (foreachStatement.VariableType.Clone (), foreachStatement.VariableName, new IndexerExpression (foreachStatement.InExpression.Clone (), new IdentifierExpression ("i")))
				}
			};
			
			var editor = document.Editor;
			var offset = editor.LocationToOffset (foreachStatement.StartLocation.Line, foreachStatement.StartLocation.Column);
			var endOffset = editor.LocationToOffset (foreachStatement.EndLocation.Line, foreachStatement.EndLocation.Column);
			var offsets = new List<int> ();
			string lineIndent = editor.GetLineIndent (foreachStatement.Parent.StartLocation.Line);
			string text = OutputNode (document, forStatement, lineIndent, delegate(int nodeOffset, AstNode astNode) {
				if (astNode is VariableDeclarationStatement && ((VariableDeclarationStatement)astNode).Variables.First ().Name == "i")
					offsets.Add (nodeOffset + "int ".Length);
				if (astNode is IdentifierExpression && ((IdentifierExpression)astNode).Identifier == "i") {
					offsets.Add (nodeOffset);
				}
			});
			string foreachBlockText;
			
			if (foreachStatement.EmbeddedStatement is BlockStatement) {
				foreachBlockText = editor.GetTextBetween (foreachStatement.EmbeddedStatement.StartLocation.Line, foreachStatement.EmbeddedStatement.StartLocation.Column + 1,
						foreachStatement.EmbeddedStatement.EndLocation.Line, foreachStatement.EmbeddedStatement.EndLocation.Column - 1);
			} else {
				foreachBlockText = editor.GetTextBetween (foreachStatement. EmbeddedStatement.StartLocation.Line, foreachStatement.EmbeddedStatement.StartLocation.Column,
					foreachStatement.EmbeddedStatement.EndLocation.Line, foreachStatement.EmbeddedStatement.EndLocation.Column);
			}
			string singeleIndent = GetSingleIndent (editor);
			string indent = lineIndent + singeleIndent;
			foreachBlockText = indent + foreachBlockText.TrimEnd () + editor.EolMarker;
			int i = text.LastIndexOf ('}');
			while (i > 1 && text[i - 1] == ' ' || text[i - 1] == '\t')
				i--;
			
			text = text.Insert (i, foreachBlockText).TrimEnd ();
			string trimmedText = text.TrimStart ();
			editor.Replace (offset, endOffset - offset + 1, trimmedText);
			
			// start text link edit mode
			TextLink link = new TextLink ("name");
			foreach (var o in offsets) {
				link.AddLink (new Segment (o - (text.Length - trimmedText.Length), "i".Length));
			}
			List<TextLink > links = new List<TextLink> ();
			links.Add (link);
			TextLinkEditMode tle = new TextLinkEditMode (editor.Parent, offset, links);
			tle.SetCaretPosition = false;
			if (tle.ShouldStartTextLinkMode) {
				tle.OldMode = editor.CurrentMode;
				tle.StartMode ();
				editor.CurrentMode = tle;
			}
			
		}
	}
}

