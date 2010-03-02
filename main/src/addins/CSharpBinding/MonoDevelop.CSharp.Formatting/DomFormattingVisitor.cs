// 
// DomFormattingVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.CSharp.Dom;
using System.Text;
using MonoDevelop.Projects.Dom;
using Mono.TextEditor;
using MonoDevelop.Refactoring;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Formatting
{
	public class DomFormattingVisitor : AbtractCSharpDomVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		List<Change> changes = new List<Change> ();
		
		public DomFormattingVisitor (CSharpFormattingPolicy policy, TextEditorData data)
		{
			this.policy = policy;
			this.data = data;
		}
		
		class MyTextReplaceChange : TextReplaceChange
		{
			TextEditorData data;
			protected override TextEditorData TextEditorData {
				get {
					return data;
				}
			}
			
			public MyTextReplaceChange (TextEditorData data, int offset, int count, string replaceWith)
			{
				this.data = data;
				this.FileName = data.Document.FileName;
				this.Offset = offset;
				this.RemovedChars = count;
				this.InsertedText = replaceWith;
			}
		}
		
		public override object VisitCompilationUnit (MonoDevelop.CSharp.Dom.CompilationUnit unit, object data)
		{
			base.VisitCompilationUnit (unit, data);
			RefactoringService.AcceptChanges (null, null, changes);
			return null;
		}

		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			BraceStyle braceStyle;
			switch (typeDeclaration.ClassType) {
			case ClassType.Class:
				braceStyle = policy.ClassBraceStyle;
				break;
			case ClassType.Struct:
				braceStyle = policy.StructBraceStyle;
				break;
			case ClassType.Interface:
				braceStyle = policy.InterfaceBraceStyle;
				break;
			case ClassType.Enum:
				braceStyle = policy.EnumBraceStyle;
				break;
			default:
				throw new InvalidOperationException ("unsupported class type : " + typeDeclaration.ClassType);
			}
			EnforceBraceStyle (braceStyle, 
			                   typeDeclaration.GetChildByRole (TypeDeclaration.TypeKeyword),
			                   typeDeclaration.GetChildByRole (AbstractNode.Roles.LBrace), 
			                   typeDeclaration.GetChildByRole (AbstractNode.Roles.RBrace));
			
			return base.VisitTypeDeclaration (typeDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			EnforceBraceStyle (policy.PropertyBraceStyle, 
			                   propertyDeclaration.GetChildByRole (PropertyDeclaration.Roles.ReturnType), 
			                   propertyDeclaration.GetChildByRole (PropertyDeclaration.Roles.LBrace), 
			                   propertyDeclaration.GetChildByRole (PropertyDeclaration.Roles.RBrace));
		
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			EnforceBraceStyle (policy.PropertyBraceStyle, 
			                   indexerDeclaration.GetChildByRole (IndexerDeclaration.Roles.ReturnType), 
			                   indexerDeclaration.GetChildByRole (IndexerDeclaration.Roles.LBrace), 
			                   indexerDeclaration.GetChildByRole (IndexerDeclaration.Roles.RBrace));
			return base.VisitIndexerDeclaration (indexerDeclaration, data);
		}
		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			return base.VisitBlockStatement (blockStatement, data);
		}

		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
		{
			return base.VisitAssignmentExpression (assignmentExpression, data);
		}

		public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			bool forceSpaces = false;
			switch (binaryOperatorExpression.BinaryOperatorType) {
			case BinaryOperatorType.Equality:
			case BinaryOperatorType.InEquality:
				forceSpaces = policy.AroundEqualityOperatorParentheses;
				break;
			case BinaryOperatorType.GreaterThan:
			case BinaryOperatorType.GreaterThanOrEqual:
			case BinaryOperatorType.LessThan:
			case BinaryOperatorType.LessThanOrEqual:
				forceSpaces = policy.AroundRelationalOperatorParentheses;
				break;
			case BinaryOperatorType.LogicalAnd:
			case BinaryOperatorType.LogicalOr:
				forceSpaces = policy.AroundLogicalOperatorParentheses;
				break;
			case BinaryOperatorType.BitwiseAnd:
			case BinaryOperatorType.BitwiseOr:
			case BinaryOperatorType.ExclusiveOr:
				forceSpaces = policy.AroundBitwiseOperatorParentheses;
				break;
			case BinaryOperatorType.Add:
			case BinaryOperatorType.Subtract:
				forceSpaces = policy.AroundAdditiveOperatorParentheses;
				break;
			case BinaryOperatorType.Multiply:
			case BinaryOperatorType.Divide:
			case BinaryOperatorType.Modulus:
				forceSpaces = policy.AroundMultiplicativeOperatorParentheses;
				break;
			case BinaryOperatorType.ShiftLeft:
			case BinaryOperatorType.ShiftRight:
				forceSpaces = policy.AroundShiftOperatorParentheses;
				break;
			}
			ForceSpacesAround (binaryOperatorExpression.Operator, forceSpaces);
			
			return base.VisitBinaryOperatorExpression (binaryOperatorExpression, data);
		}

		public override object VisitConditionalExpression (ConditionalExpression conditionalExpression, object data)
		{
			ForceSpacesBefore (conditionalExpression.QuestionMark, policy.ConditionalOperatorBeforeConditionSpace);
			ForceSpacesAfter (conditionalExpression.QuestionMark, policy.ConditionalOperatorAfterConditionSpace);
			ForceSpacesBefore (conditionalExpression.Colon, policy.ConditionalOperatorBeforeSeparatorSpace);
			ForceSpacesAfter (conditionalExpression.Colon, policy.ConditionalOperatorAfterSeparatorSpace);
			return base.VisitConditionalExpression (conditionalExpression, data);
		}
		
		public override object VisitCastExpression (CastExpression castExpression, object data)
		{
			if (castExpression.RPar != null)
				ForceSpacesAfter (castExpression.RPar, 1, policy.SpacesAfterTypecast);
			return base.VisitCastExpression (castExpression, data);
		}
		
		void ForceSpacesAround (INode node, bool forceSpaces)
		{
			ForceSpacesBefore (node, forceSpaces);
			ForceSpacesAfter (node, forceSpaces);
		}
		
		void ForceSpacesAfter (INode node, bool forceSpaces)
		{
			DomLocation location = ((ICSharpNode)node).EndLocation;
			Console.WriteLine (((ICSharpNode)node).StartLocation + "/" + ((ICSharpNode)node).EndLocation);
			Console.WriteLine (node + ":" + location);
			int offset = data.Document.LocationToOffset (location.Line, location.Column) - 1;
			int i = offset + 1;
			while (i < data.Document.Length && Char.IsWhiteSpace (data.Document.GetCharAt (i))) {
				i++;
			}
			ForceSpace (offset, i, forceSpaces);
		}
		
		void ForceSpacesAfter (INode node, int tokenLength, bool forceSpaces)
		{
			DomLocation location = ((ICSharpNode)node).StartLocation;
			int offset = data.Document.LocationToOffset (location.Line, location.Column) + 1;
			int i = offset;
			while (i < data.Document.Length && Char.IsWhiteSpace (data.Document.GetCharAt (i))) {
				i++;
			}
			ForceSpace (offset - 1, i, forceSpaces);
		}
		
		int ForceSpacesBefore (INode node, bool forceSpaces)
		{
			DomLocation location = ((ICSharpNode)node).StartLocation;
			int offset = data.Document.LocationToOffset (location.Line, location.Column);
			int i = offset - 1;
			
			while (i >= 0 && Char.IsWhiteSpace (data.Document.GetCharAt (i))) {
				i--;
			}
			ForceSpace (i, offset, forceSpaces);
			return i;
		}
		
		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			foreach (INode node in fieldDeclaration.Children) {
				if (node is VariableInitializer && node.NextSibling != null && node.NextSibling.Role == FieldDeclaration.Roles.Comma) {
					VariableInitializer initializer = node as VariableInitializer;
					CSharpTokenNode commaToken = (CSharpTokenNode)node.NextSibling;
					int offset      = this.data.Document.LocationToOffset (initializer.NameIdentifier.StartLocation.Line, initializer.NameIdentifier.StartLocation.Column);
					int commaOffset = this.data.Document.LocationToOffset (commaToken.StartLocation.Line, commaToken.StartLocation.Column);
					ForceSpace (offset, commaOffset, policy.SpacesAfterComma);
					//Console.WriteLine (initializer.Name +"/" + initializer.NameIdentifier + "/" + commaToken);
					
					if (node.NextSibling.NextSibling is VariableInitializer) {
						DomLocation location = ((VariableInitializer)node.NextSibling.NextSibling).NameIdentifier.StartLocation;
						int nextOffset = this.data.Document.LocationToOffset (location.Line, location.Column);
						ForceSpace (commaOffset, nextOffset, policy.SpacesAfterComma);
					}
				}
			}
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)delegateDeclaration.GetChildByRole (DelegateDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.BeforeDelegateDeclarationParentheses);
			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}
		
		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)methodDeclaration.GetChildByRole (MethodDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.BeforeMethodDeclarationParentheses);
			
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}
		
		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)constructorDeclaration.GetChildByRole (ConstructorDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.BeforeConstructorDeclarationParentheses);
			
			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}
		
		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)destructorDeclaration.GetChildByRole (DestructorDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.BeforeConstructorDeclarationParentheses);
			return base.VisitDestructorDeclaration (destructorDeclaration, data);
		}
		
		void ForceSpaceBefore (int offset, bool forceSpace)
		{
			bool insertedSpace = false;
			do {
				char ch = data.Document.GetCharAt (offset);
				//Console.WriteLine (ch);
				if (!Char.IsWhiteSpace (ch) && (insertedSpace || !forceSpace))
					break;
				if (ch == ' ' && forceSpace) {
					if (insertedSpace) {
						changes.Add (new MyTextReplaceChange (data, offset, 1, null));
					} else {
						insertedSpace = true;
					}
				} else if (forceSpace) {
					if (!insertedSpace) {
						changes.Add (new MyTextReplaceChange (data, offset, Char.IsWhiteSpace (ch) ? 1 :  0, " "));
						insertedSpace = true;
					} else if (Char.IsWhiteSpace (ch)) {
						changes.Add (new MyTextReplaceChange (data, offset, 1, null));
					}
				}
				
				offset--;
			} while (offset >= 0);
		}

		void ForceSpace (int startOffset, int endOffset, bool forceSpace)
		{
			int lastNonWs = SearchLastNonWsChar (startOffset, endOffset);
			changes.Add (new MyTextReplaceChange (data, lastNonWs + 1, System.Math.Max (0, endOffset - lastNonWs - 1), forceSpace ? " " : ""));
		}
		/*
		int GetLastNonWsChar (LineSegment line, int lastColumn)
		{
			int result = -1;
			bool inComment = false;
			for (int i = 0; i < lastColumn; i++) {
				char ch = data.Document.GetCharAt (line.Offset + i);
				if (Char.IsWhiteSpace (ch))
					continue;
				if (ch == '/' && i + 1 < line.EditableLength && data.Document.GetCharAt (line.Offset + i + 1) == '/')
					return result;
				if (ch == '/' && i + 1 < line.EditableLength && data.Document.GetCharAt (line.Offset + i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < line.EditableLength && data.Document.GetCharAt (line.Offset + i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment)
					result = i;
			}
			return result;
		}
		*/
		int SearchLastNonWsChar (int startOffset, int endOffset)
		{
			if (startOffset >= endOffset)
				return startOffset;
			int result = -1;
			bool inComment = false;
			//Console.WriteLine ("----");
			for (int i = startOffset; i < endOffset; i++) {
				char ch = data.Document.GetCharAt (i);
				//Console.WriteLine (ch);
				if (Char.IsWhiteSpace (ch))
					continue;
				if (ch == '/' && i + 1 < data.Document.Length && data.Document.GetCharAt (i + 1) == '/')
					return result;
				if (ch == '/' && i + 1 < data.Document.Length && data.Document.GetCharAt (i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < data.Document.Length && data.Document.GetCharAt (i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment)
					result = i;
			}
			return result;
		}
		
		string SingleIndent {
			get {
				return "\t";
			}
		}
		void EnforceBraceStyle (MonoDevelop.CSharp.Formatting.BraceStyle braceStyle, INode startNode, INode lbraceNode, INode rbraceNode)
		{
			ICSharpNode start = (ICSharpNode)startNode;
			ICSharpNode lbrace = (ICSharpNode)lbraceNode;
			ICSharpNode rbrace = (ICSharpNode)rbraceNode;
				
			LineSegment lbraceLineSegment = data.Document.GetLine (lbrace.StartLocation.Line);
			string indent = lbraceLineSegment.GetIndentation (data.Document);
			
			LineSegment rbraceLineSegment = data.Document.GetLine (rbrace.StartLocation.Line);
			
			int lbraceOffset = data.Document.LocationToOffset (lbrace.StartLocation.Line, lbrace.StartLocation.Column);
			
			int lastNonWsChar = SearchLastNonWsChar (data.Document.LocationToOffset (start.StartLocation.Line, start.StartLocation.Column), lbraceOffset);
			
			switch (braceStyle) {
			case BraceStyle.EndOfLineWithoutSpace:
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, null));
				break;
			case BraceStyle.EndOfLine:
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, " "));
				break;
			case BraceStyle.NextLine:
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, data.EolMarker));
				break;
			case BraceStyle.NextLineShifted:
				indent += SingleIndent;
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, data.EolMarker + SingleIndent));
				break;
			case BraceStyle.NextLineShifted2:
				indent += SingleIndent;
				changes.Add (new MyTextReplaceChange (data, lastNonWsChar + 1, lbraceOffset - lastNonWsChar - 1, data.EolMarker + SingleIndent));
				break;
			}
			
			int firstNonWsChar = rbrace.StartLocation.Column - 1;
			
			for (; firstNonWsChar >= 0; firstNonWsChar--) {
				char ch = data.Document.GetCharAt (rbraceLineSegment.Offset + firstNonWsChar);
				if (!Char.IsWhiteSpace (ch))
					break;
			}
			
			changes.Add (new MyTextReplaceChange (data, rbraceLineSegment.Offset + firstNonWsChar + 1, rbrace.StartLocation.Column - firstNonWsChar - 1, firstNonWsChar > 0 ? data.EolMarker + indent : indent));
		}
		
		public override object VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			ForceSpacesBefore (invocationExpression.LPar, policy.BeforeMethodCallParentheses);
			
			ForceSpacesAfter (invocationExpression.LPar, policy.WithinMethodCallParentheses);
			ForceSpacesBefore (invocationExpression.RPar, policy.WithinMethodCallParentheses);
			
			return base.VisitInvocationExpression (invocationExpression, data);
		}
		
		public override object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
		{
			ForceSpacesBefore (ifElseStatement.LPar, policy.IfParentheses);
			
			ForceSpacesAfter (ifElseStatement.LPar, policy.WithinIfParentheses);
			ForceSpacesBefore (ifElseStatement.RPar, policy.WithinIfParentheses);
			
			
			return base.VisitIfElseStatement (ifElseStatement, data);
		}
		
		public override object VisitWhileStatement (WhileStatement whileStatement, object data)
		{
			ForceSpacesBefore (whileStatement.LPar, policy.WhileParentheses);
			
			ForceSpacesAfter (whileStatement.LPar, policy.WithinWhileParentheses);
			ForceSpacesBefore (whileStatement.RPar, policy.WithinWhileParentheses);
			
			return base.VisitWhileStatement (whileStatement, data);
		}
		
		public override object VisitForStatement (ForStatement forStatement, object data)
		{
			foreach (INode node in forStatement.Children) {
				if (node.Role == ForStatement.Roles.Semicolon) {
					ForceSpacesAfter (node, policy.SpacesAfterSemicolon);
				}
			}
			
			ForceSpacesBefore (forStatement.LPar, policy.ForParentheses);
			
			ForceSpacesAfter (forStatement.LPar, policy.WithinForParentheses);
			ForceSpacesBefore (forStatement.RPar, policy.WithinForParentheses);
			
			return base.VisitForStatement (forStatement, data);
		}
		
		public override object VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			ForceSpacesBefore (foreachStatement.LPar, policy.ForeachParentheses);
			
			ForceSpacesAfter (foreachStatement.LPar, policy.WithinForEachParentheses);
			ForceSpacesBefore (foreachStatement.RPar, policy.WithinForEachParentheses);
			
			return base.VisitForeachStatement (foreachStatement, data);
		}
		
		public override object VisitCatchClause (CatchClause catchClause, object data)
		{
			if (catchClause.LPar != null) {
				ForceSpacesBefore (catchClause.LPar, policy.CatchParentheses);
				
				ForceSpacesAfter (catchClause.LPar, policy.WithinCatchParentheses);
				ForceSpacesBefore (catchClause.RPar, policy.WithinCatchParentheses);
			}
			
			return base.VisitCatchClause (catchClause, data);
		}
		
		public override object VisitLockStatement (LockStatement lockStatement, object data)
		{
			ForceSpacesBefore (lockStatement.LPar, policy.LockParentheses);
			
			ForceSpacesAfter (lockStatement.LPar, policy.WithinLockParentheses);
			ForceSpacesBefore (lockStatement.RPar, policy.WithinLockParentheses);
			

			return base.VisitLockStatement (lockStatement, data);
		}
		
		public override object VisitUsingStatement (UsingStatement usingStatement, object data)
		{
			ForceSpacesBefore (usingStatement.LPar, policy.UsingParentheses);
			
			ForceSpacesAfter (usingStatement.LPar, policy.WithinUsingParentheses);
			ForceSpacesBefore (usingStatement.RPar, policy.WithinUsingParentheses);
			
			return base.VisitUsingStatement (usingStatement, data);
		}
	}
}
