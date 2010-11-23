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
using System.Linq;

namespace MonoDevelop.CSharp.Formatting
{
	public class DomSpacingVisitor : DomVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		List<Change> changes = new List<Change> ();
		
		public List<Change> Changes {
			get { return this.changes; }
		}
		
		public bool AutoAcceptChanges { get; set; }
		
		public DomSpacingVisitor (CSharpFormattingPolicy policy, TextEditorData data)
		{
			this.policy = policy;
			this.data = data;
			AutoAcceptChanges = true;
		}
		
		internal class MyTextReplaceChange : TextReplaceChange
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
			if (AutoAcceptChanges)
				RefactoringService.AcceptChanges (null, null, changes);
			return null;
		}

		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			
			return base.VisitTypeDeclaration (typeDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			ForceSpacesBefore (indexerDeclaration.LBracket, policy.BeforeIndexerDeclarationBracket);
			ForceSpacesAfter (indexerDeclaration.LBracket, policy.WithinIndexerDeclarationBracket);
			ForceSpacesBefore (indexerDeclaration.RBracket, policy.WithinIndexerDeclarationBracket);
			
			FormatCommas (indexerDeclaration, policy.BeforeIndexerDeclarationParameterComma, policy.AfterIndexerDeclarationParameterComma);
			
			return base.VisitIndexerDeclaration (indexerDeclaration, data);
		}
		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			return base.VisitBlockStatement (blockStatement, data);
		}

		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
		{
			ForceSpacesAround (assignmentExpression.Operator, policy.AroundAssignmentParentheses);
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
			if (castExpression.RPar != null) {
				ForceSpacesAfter (castExpression.LPar, policy.WithinCastParentheses);
				ForceSpacesBefore (castExpression.RPar, policy.WithinCastParentheses);
				
				ForceSpacesAfter (castExpression.RPar, policy.SpacesAfterTypecast);
			}
			return base.VisitCastExpression (castExpression, data);
		}
		
		void ForceSpacesAround (DomNode node, bool forceSpaces)
		{
			ForceSpacesBefore (node, forceSpaces);
			ForceSpacesAfter (node, forceSpaces);
		}
		
		bool IsSpacing (char ch)
		{
			return ch == ' ' || ch == '\t';
		}

		void ForceSpacesAfter (DomNode n, bool forceSpaces)
		{
			if (n == null)
				return;
			DomLocation location = n.EndLocation;
			int offset = data.Document.LocationToOffset (location.Line, location.Column);
			int i = offset;
			while (i < data.Document.Length && IsSpacing (data.Document.GetCharAt (i))) {
				i++;
			}
			ForceSpace (offset - 1, i, forceSpaces);
		}
		
		int ForceSpacesBefore (DomNode n, bool forceSpaces)
		{
			if (n == null)
				return 0;
			DomLocation location = n.StartLocation;
			
			int offset = data.Document.LocationToOffset (location.Line, location.Column);
			int i = offset - 1;
			
			while (i >= 0 && IsSpacing (data.Document.GetCharAt (i))) {
				i--;
			}
			ForceSpace (i, offset, forceSpaces);
			return i;
		}
		
		void FormatCommas (DomNode parent, bool before, bool after)
		{
			if (parent.IsNull)
				return;
			foreach (CSharpTokenNode comma in parent.Children.Where (node => node.Role == FieldDeclaration.Roles.Comma)) {
				ForceSpacesAfter (comma, after);
				ForceSpacesBefore (comma, before);
			}
		}
		
		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			FormatCommas (fieldDeclaration, policy.BeforeFieldDeclarationComma, policy.AfterFieldDeclarationComma);
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitComposedType (ComposedType composedType, object data)
		{
			var spec = composedType.Compositions.FirstOrDefault ();
			if (spec != null)
				ForceSpacesBefore (spec.LBracket, policy.SpacesBeforeArrayDeclarationBrackets);
			
			return base.VisitComposedType (composedType, data);
		}
		
		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			ForceSpacesBefore (delegateDeclaration.LPar, policy.BeforeDelegateDeclarationParentheses);
			if (delegateDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (delegateDeclaration.LPar, policy.WithinDelegateDeclarationParentheses);
				ForceSpacesBefore (delegateDeclaration.RPar, policy.WithinDelegateDeclarationParentheses);
			} else {
				ForceSpacesAfter (delegateDeclaration.LPar, policy.BetweenEmptyDelegateDeclarationParentheses);
				ForceSpacesBefore (delegateDeclaration.RPar, policy.BetweenEmptyDelegateDeclarationParentheses);
			}
			FormatCommas (delegateDeclaration, policy.BeforeDelegateDeclarationParameterComma, policy.AfterDelegateDeclarationParameterComma);
			
			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}
		
		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			ForceSpacesBefore (methodDeclaration.LPar, policy.BeforeMethodDeclarationParentheses);
			if (methodDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (methodDeclaration.LPar, policy.WithinMethodDeclarationParentheses);
				ForceSpacesBefore (methodDeclaration.RPar, policy.WithinMethodDeclarationParentheses);
			} else {
				ForceSpacesAfter (methodDeclaration.LPar, policy.BetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore (methodDeclaration.RPar, policy.BetweenEmptyMethodDeclarationParentheses);
			}
			FormatCommas (methodDeclaration, policy.BeforeMethodDeclarationParameterComma, policy.AfterMethodDeclarationParameterComma);

			return base.VisitMethodDeclaration (methodDeclaration, data);
		}
		
		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			ForceSpacesBefore (constructorDeclaration.LPar, policy.BeforeConstructorDeclarationParentheses);
			if (constructorDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (constructorDeclaration.LPar, policy.WithinConstructorDeclarationParentheses);
				ForceSpacesBefore (constructorDeclaration.RPar, policy.WithinConstructorDeclarationParentheses);
			} else {
				ForceSpacesAfter (constructorDeclaration.LPar, policy.BetweenEmptyConstructorDeclarationParentheses);
				ForceSpacesBefore (constructorDeclaration.RPar, policy.BetweenEmptyConstructorDeclarationParentheses);
			}
			FormatCommas (constructorDeclaration, policy.BeforeConstructorDeclarationParameterComma, policy.AfterConstructorDeclarationParameterComma);

			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}
		
		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			CSharpTokenNode lParen = (CSharpTokenNode)destructorDeclaration.GetChildByRole (DestructorDeclaration.Roles.LPar);
			int offset = this.data.Document.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.BeforeConstructorDeclarationParentheses);
			return base.VisitDestructorDeclaration (destructorDeclaration, data);
		}
		
		void AddChange (int offset, int removedChars, string insertedText)
		{
			if (changes.Cast<DomSpacingVisitor.MyTextReplaceChange> ().Any (c => c.Offset == offset && c.RemovedChars == removedChars 
				&& c.InsertedText == insertedText))
				return;
			string currentText = data.Document.GetTextAt (offset, removedChars);
			if (currentText == insertedText)
				return;
			if (currentText.Any (c => !(char.IsWhiteSpace (c) || c == '\r' || c == '\t')))
				throw new InvalidOperationException ("Tried to remove non ws chars: '" + currentText + "'");
			foreach (MyTextReplaceChange change in changes) {
				if (change.Offset == offset) {
					if (removedChars > 0 && insertedText == change.InsertedText) {
						change.RemovedChars = removedChars;
//						change.InsertedText = insertedText;
						return;
					}
				}
			}
//			Console.WriteLine ("offset={0}, removedChars={1}, insertedText={2}", offset, removedChars, insertedText.Replace("\n", "\\n").Replace("\t", "\\t").Replace(" ", "."));
//			Console.WriteLine (Environment.StackTrace);
			changes.Add (new MyTextReplaceChange (data, offset, removedChars, insertedText));
		}
		
		void ForceSpaceBefore (int offset, bool forceSpace)
		{
			bool insertedSpace = false;
			do {
				char ch = data.Document.GetCharAt (offset);
				//Console.WriteLine (ch);
				if (!IsSpacing (ch) && (insertedSpace || !forceSpace))
					break;
				if (ch == ' ' && forceSpace) {
					if (insertedSpace) {
						AddChange (offset, 1, null);
					} else {
						insertedSpace = true;
					}
				} else if (forceSpace) {
					if (!insertedSpace) {
						AddChange (offset, IsSpacing (ch) ? 1 :  0, " ");
						insertedSpace = true;
					} else if (IsSpacing (ch)) {
						AddChange (offset, 1, null);
					}
				}
				
				offset--;
			} while (offset >= 0);
		}

		void ForceSpace (int startOffset, int endOffset, bool forceSpace)
		{
			int lastNonWs = SearchLastNonWsChar (startOffset, endOffset);
			AddChange (lastNonWs + 1, System.Math.Max (0, endOffset - lastNonWs - 1), forceSpace ? " " : "");
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
			startOffset = System.Math.Max (0, startOffset);
			endOffset = System.Math.Max (startOffset, endOffset);
			if (startOffset >= endOffset)
				return startOffset;
			int result = -1;
			bool inComment = false;
			
			for (int i = startOffset; i < endOffset && i < data.Document.Length; i++) {
				char ch = data.Document.GetCharAt (i);
				if (IsSpacing (ch))
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
		
		public override object VisitVariableInitializer (VariableInitializer variableInitializer, object data)
		{
			if (!variableInitializer.Assign.IsNull)
				ForceSpacesAround (variableInitializer.Assign, policy.AroundAssignmentParentheses);
			if (!variableInitializer.Initializer.IsNull)
				variableInitializer.Initializer.AcceptVisitor (this, data);
			return data;
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			foreach (var initializer in variableDeclarationStatement.Variables) {
				initializer.AcceptVisitor (this, data);
			}
			FormatCommas (variableDeclarationStatement, policy.BeforeLocalVariableDeclarationComma, policy.AfterLocalVariableDeclarationComma);
			return data;
		}
		
		public override object VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			ForceSpacesBefore (invocationExpression.LPar, policy.BeforeMethodCallParentheses);
			if (invocationExpression.Arguments.Any ()) {
				ForceSpacesAfter (invocationExpression.LPar, policy.WithinMethodCallParentheses);
				ForceSpacesBefore (invocationExpression.RPar, policy.WithinMethodCallParentheses);
			} else {
				ForceSpacesAfter (invocationExpression.LPar, policy.BetweenEmptyMethodCallParentheses);
				ForceSpacesBefore (invocationExpression.RPar, policy.BetweenEmptyMethodCallParentheses);
			}
			FormatCommas (invocationExpression, policy.BeforeMethodCallParameterComma, policy.AfterMethodCallParameterComma);

			return base.VisitInvocationExpression (invocationExpression, data);
		}
		
		public override object VisitIndexerExpression (IndexerExpression indexerExpression, object data)
		{
			ForceSpacesBefore (indexerExpression.LBracket, policy.SpacesBeforeBrackets);
			ForceSpacesAfter (indexerExpression.LBracket, policy.SpacesWithinBrackets);
			ForceSpacesBefore (indexerExpression.RBracket, policy.SpacesWithinBrackets);
			FormatCommas (indexerExpression, policy.BeforeBracketComma, policy.AfterBracketComma);
			
			return base.VisitIndexerExpression (indexerExpression, data);
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
			foreach (DomNode node in forStatement.Children) {
				if (node.Role == ForStatement.Roles.Semicolon) {
					if (node.NextSibling is CSharpTokenNode || node.NextSibling is EmptyStatement)
						continue;
					ForceSpacesBefore (node, policy.SpacesBeforeForSemicolon);
					ForceSpacesAfter (node, policy.SpacesAfterForSemicolon);
				}
			}
			
			ForceSpacesBefore (forStatement.LPar, policy.ForParentheses);
			
			ForceSpacesAfter (forStatement.LPar, policy.WithinForParentheses);
			ForceSpacesBefore (forStatement.RPar, policy.WithinForParentheses);
			
			if (forStatement.EmbeddedStatement != null)
				forStatement.EmbeddedStatement.AcceptVisitor (this, data);
			
			return null;
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
		
		public override object VisitSwitchStatement (MonoDevelop.CSharp.Dom.SwitchStatement switchStatement, object data)
		{
			ForceSpacesBefore (switchStatement.LPar, policy.SwitchParentheses);
			
			ForceSpacesAfter (switchStatement.LPar, policy.WithinSwitchParentheses);
			ForceSpacesBefore (switchStatement.RPar, policy.WithinSwitchParentheses);
			
			return base.VisitSwitchStatement (switchStatement, data);
		}
		
		public override object VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, object data)
		{
			ForceSpacesAfter (parenthesizedExpression.LPar, policy.WithinParentheses);
			ForceSpacesBefore (parenthesizedExpression.RPar, policy.WithinParentheses);
			return base.VisitParenthesizedExpression (parenthesizedExpression, data);
		}
		
		public override object VisitSizeOfExpression (SizeOfExpression sizeOfExpression, object data)
		{
			ForceSpacesBefore (sizeOfExpression.LPar, policy.BeforeSizeOfParentheses);
			ForceSpacesAfter (sizeOfExpression.LPar, policy.WithinSizeOfParentheses);
			ForceSpacesBefore (sizeOfExpression.RPar, policy.WithinSizeOfParentheses);
			return base.VisitSizeOfExpression (sizeOfExpression, data);
		}
		
		public override object VisitTypeOfExpression (TypeOfExpression typeOfExpression, object data)
		{
			ForceSpacesBefore (typeOfExpression.LPar, policy.BeforeTypeOfParentheses);
			ForceSpacesAfter (typeOfExpression.LPar, policy.WithinTypeOfParentheses);
			ForceSpacesBefore (typeOfExpression.RPar, policy.WithinTypeOfParentheses);
			return base.VisitTypeOfExpression (typeOfExpression, data);
		}
		
		public override object VisitCheckedExpression (CheckedExpression checkedExpression, object data)
		{
			ForceSpacesAfter (checkedExpression.LPar, policy.WithinCheckedExpressionParantheses);
			ForceSpacesBefore (checkedExpression.RPar, policy.WithinCheckedExpressionParantheses);
			return base.VisitCheckedExpression (checkedExpression, data);
		}

		public override object VisitUncheckedExpression (UncheckedExpression uncheckedExpression, object data)
		{
			ForceSpacesAfter (uncheckedExpression.LPar, policy.WithinCheckedExpressionParantheses);
			ForceSpacesBefore (uncheckedExpression.RPar, policy.WithinCheckedExpressionParantheses);
			return base.VisitUncheckedExpression (uncheckedExpression, data);
		}
		
		public override object VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, object data)
		{
			ForceSpacesBefore (objectCreateExpression.LPar, policy.NewParentheses);
			
			return base.VisitObjectCreateExpression (objectCreateExpression, data);
		}
		
		public override object VisitArrayCreateExpression (ArrayCreateExpression arrayObjectCreateExpression, object data)
		{
			FormatCommas (arrayObjectCreateExpression, policy.BeforeMethodCallParameterComma, policy.AfterMethodCallParameterComma);
			return base.VisitArrayCreateExpression (arrayObjectCreateExpression, data);
		}
		
		public override object VisitLambdaExpression (LambdaExpression lambdaExpression, object data)
		{
			ForceSpacesAfter (lambdaExpression.Arrow, true);
			ForceSpacesBefore (lambdaExpression.Arrow, true);
			
			return base.VisitLambdaExpression (lambdaExpression, data);
		}
		

	}
}
