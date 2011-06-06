// 
// ConvertForeachToFor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Converts a foreach loop to for.
	/// </summary>
	public class ConvertForeachToFor : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			return GetForeachStatement (context) != null;
		}

		public void Run (RefactoringContext context)
		{ // TODO: Missing resolver!

//			var foreachStatement = GetForeachStatement (context);
//			
//			var resolver = context.Resolver;
//			
//			var result = resolver.Resolve (foreachStatement.InExpression.ToString (), new DomLocation (foreachStatement.InExpression.StartLocation.Line, foreachStatement.InExpression.StartLocation.Column));
//			string itemNumberProperty = "Count";
//			
//			if (result != null && result.ResolvedType != null && result.ResolvedType.ArrayDimensions > 0)
//				itemNumberProperty = "Length";
//			
//			ForStatement forStatement = new ForStatement () {
//				Initializers = {
//					new VariableDeclarationStatement (new PrimitiveType ("int"), "i", new PrimitiveExpression (0))
//				},
//				Condition = new BinaryOperatorExpression (new IdentifierExpression ("i"), BinaryOperatorType.LessThan, new MemberReferenceExpression (foreachStatement.InExpression.Clone (), itemNumberProperty)),
//				Iterators = {
//					new ExpressionStatement (new UnaryOperatorExpression (UnaryOperatorType.PostIncrement, new IdentifierExpression ("i")))
//				},
//				EmbeddedStatement = new BlockStatement {
//					new VariableDeclarationStatement (foreachStatement.VariableType.Clone (), foreachStatement.VariableName, new IndexerExpression (foreachStatement.InExpression.Clone (), new IdentifierExpression ("i")))
//				}
//			};
//			
//			var editor = context.Document.Editor;
//			var offset = editor.LocationToOffset (foreachStatement.StartLocation.Line, foreachStatement.StartLocation.Column);
//			var endOffset = editor.LocationToOffset (foreachStatement.EndLocation.Line, foreachStatement.EndLocation.Column);
//			var offsets = new List<int> ();
//			string lineIndent = editor.GetLineIndent (foreachStatement.Parent.StartLocation.Line);
//			string text = context.OutputNode (forStatement, context.Document.CalcIndentLevel (lineIndent) + 1, delegate(int nodeOffset, AstNode astNode) {
//				if (astNode is VariableInitializer && ((VariableInitializer)astNode).Name == "i")
//					offsets.Add (nodeOffset);
//				if (astNode is IdentifierExpression && ((IdentifierExpression)astNode).Identifier == "i") {
//					offsets.Add (nodeOffset);
//				}
//			});
//			string foreachBlockText;
//			
//			if (foreachStatement.EmbeddedStatement is BlockStatement) {
//				foreachBlockText = editor.GetTextBetween (foreachStatement.EmbeddedStatement.StartLocation.Line, foreachStatement.EmbeddedStatement.StartLocation.Column + 1,
//						foreachStatement.EmbeddedStatement.EndLocation.Line, foreachStatement.EmbeddedStatement.EndLocation.Column - 1);
//			} else {
//				foreachBlockText = editor.GetTextBetween (foreachStatement. EmbeddedStatement.StartLocation.Line, foreachStatement.EmbeddedStatement.StartLocation.Column,
//					foreachStatement.EmbeddedStatement.EndLocation.Line, foreachStatement.EmbeddedStatement.EndLocation.Column);
//			}
//			string singeleIndent = GetSingleIndent (editor);
//			string indent = lineIndent + singeleIndent;
//			foreachBlockText = indent + foreachBlockText.TrimEnd () + editor.EolMarker;
//			int i = text.LastIndexOf ('}');
//			while (i > 1 && text[i - 1] == ' ' || text[i - 1] == '\t')
//				i--;
//			
//			text = text.Insert (i, foreachBlockText).TrimEnd ();
//			string trimmedText = text.TrimStart ();
//			editor.Replace (offset, endOffset - offset + 1, trimmedText);
//			context.StartTextLinkMode (offset, "i".Length, offsets.Select (o => o - (text.Length - trimmedText.Length)));
		}
		
		static ForeachStatement GetForeachStatement (RefactoringContext context)
		{
			var astNode = context.GetNode ();
			if (astNode == null)
				return null;
			return (astNode as ForeachStatement) ?? astNode.Parent as ForeachStatement;
		}
	}
}
