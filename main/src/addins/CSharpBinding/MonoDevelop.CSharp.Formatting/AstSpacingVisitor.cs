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
using MonoDevelop.CSharp.Ast;
using System.Text;
using MonoDevelop.Projects.Dom;
using Mono.TextEditor;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Formatting
{
	public class AstSpacingVisitor : DepthFirstAstVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		List<Change> changes = new List<Change> ();

		public List<Change> Changes {
			get { return this.changes; }
		}

		public bool AutoAcceptChanges { get; set; }

		public AstSpacingVisitor (CSharpFormattingPolicy policy,TextEditorData data)
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

			public void SetTextEditorData (TextEditorData data)
			{
				this.data = data;
			}

			public MyTextReplaceChange (TextEditorData data,int offset,int count, string replaceWith)
			{
				this.data = data;
				this.FileName = data.Document.FileName;
				this.Offset = offset;
				this.RemovedChars = count;
				this.InsertedText = replaceWith;
			}
		}

		public override object VisitCompilationUnit (MonoDevelop.CSharp.Ast.CompilationUnit unit, object data)
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
			ForceSpacesBefore (indexerDeclaration.LBracketToken, policy.SpaceBeforeIndexerDeclarationBracket);
			ForceSpacesAfter (indexerDeclaration.LBracketToken, policy.SpacesWithinIndexerDeclarationBracket);
			ForceSpacesBefore (indexerDeclaration.RBracketToken, policy.SpacesWithinIndexerDeclarationBracket);

			FormatCommas (indexerDeclaration, policy.SpaceBeforeIndexerDeclarationParameterComma, policy.SpaceAfterIndexerDeclarationParameterComma);

			return base.VisitIndexerDeclaration (indexerDeclaration, data);
		}

		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			return base.VisitBlockStatement (blockStatement, data);
		}

		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
		{
			ForceSpacesAround (assignmentExpression.OperatorToken, policy.SpaceAroundAssignment);
			return base.VisitAssignmentExpression (assignmentExpression, data);
		}

		public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			bool forceSpaces = false;
			switch (binaryOperatorExpression.Operator) {
			case BinaryOperatorType.Equality:
			case BinaryOperatorType.InEquality:
				forceSpaces = policy.SpaceAroundEqualityOperator;
				break;
			case BinaryOperatorType.GreaterThan:
			case BinaryOperatorType.GreaterThanOrEqual:
			case BinaryOperatorType.LessThan:
			case BinaryOperatorType.LessThanOrEqual:
				forceSpaces = policy.SpaceAroundRelationalOperator;
				break;
			case BinaryOperatorType.ConditionalAnd:
			case BinaryOperatorType.ConditionalOr:
				forceSpaces = policy.SpaceAroundLogicalOperator;
				break;
			case BinaryOperatorType.BitwiseAnd:
			case BinaryOperatorType.BitwiseOr:
			case BinaryOperatorType.ExclusiveOr:
				forceSpaces = policy.SpaceAroundBitwiseOperator;
				break;
			case BinaryOperatorType.Add:
			case BinaryOperatorType.Subtract:
				forceSpaces = policy.SpaceAroundAdditiveOperator;
				break;
			case BinaryOperatorType.Multiply:
			case BinaryOperatorType.Divide:
			case BinaryOperatorType.Modulus:
				forceSpaces = policy.SpaceAroundMultiplicativeOperator;
				break;
			case BinaryOperatorType.ShiftLeft:
			case BinaryOperatorType.ShiftRight:
				forceSpaces = policy.SpaceAroundShiftOperator;
				break;
			case BinaryOperatorType.NullCoalescing:
				forceSpaces = policy.SpaceAroundNullCoalescingOperator;
				break;
			}
			ForceSpacesAround (binaryOperatorExpression.OperatorToken, forceSpaces);

			return base.VisitBinaryOperatorExpression (binaryOperatorExpression, data);
		}

		public override object VisitConditionalExpression (ConditionalExpression conditionalExpression, object data)
		{
			ForceSpacesBefore (conditionalExpression.QuestionMarkToken, policy.SpaceBeforeConditionalOperatorCondition);
			ForceSpacesAfter (conditionalExpression.QuestionMarkToken, policy.SpaceAfterConditionalOperatorCondition);
			ForceSpacesBefore (conditionalExpression.ColonToken, policy.SpaceBeforeConditionalOperatorSeparator);
			ForceSpacesAfter (conditionalExpression.ColonToken, policy.SpaceAfterConditionalOperatorSeparator);
			return base.VisitConditionalExpression (conditionalExpression, data);
		}

		public override object VisitCastExpression (CastExpression castExpression, object data)
		{
			if (castExpression.RParToken != null) {
				ForceSpacesAfter (castExpression.LParToken, policy.SpacesWithinCastParentheses);
				ForceSpacesBefore (castExpression.RParToken, policy.SpacesWithinCastParentheses);

				ForceSpacesAfter (castExpression.RParToken, policy.SpaceAfterTypecast);
			}
			return base.VisitCastExpression (castExpression, data);
		}

		void ForceSpacesAround (AstNode node, bool forceSpaces)
		{
			if (node.IsNull) {
				LoggingService.LogWarning ("Encountered null node.");
				return;
			}
			ForceSpacesBefore (node, forceSpaces);
			ForceSpacesAfter (node, forceSpaces);
		}

		bool IsSpacing (char ch)
		{
			return ch == ' ' || ch == '\t';
		}

		void ForceSpacesAfter (AstNode n, bool forceSpaces)
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

		int ForceSpacesBefore (AstNode n, bool forceSpaces)
		{
			if (n == null || n.IsNull)
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

		void FormatCommas (AstNode parent, bool before, bool after)
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
			FormatCommas (fieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}

		public override object VisitComposedType (ComposedType composedType, object data)
		{
			var spec = composedType.ArraySpecifiers.FirstOrDefault ();
			if (spec != null)
				ForceSpacesBefore (spec.LBracketToken, policy.SpaceBeforeArrayDeclarationBrackets);

			return base.VisitComposedType (composedType, data);
		}

		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			ForceSpacesBefore (delegateDeclaration.LParToken, policy.SpaceBeforeDelegateDeclarationParentheses);
			if (delegateDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (delegateDeclaration.LParToken, policy.SpacesWithinDelegateDeclarationParentheses);
				ForceSpacesBefore (delegateDeclaration.RParToken, policy.SpacesWithinDelegateDeclarationParentheses);
			} else {
				ForceSpacesAfter (delegateDeclaration.LParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
				ForceSpacesBefore (delegateDeclaration.RParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
			}
			FormatCommas (delegateDeclaration, policy.SpaceBeforeDelegateDeclarationParameterComma, policy.SpaceAfterDelegateDeclarationParameterComma);

			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}

		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			ForceSpacesBefore (methodDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (methodDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (methodDeclaration.LParToken, policy.SpacesWithinMethodDeclarationParentheses);
				ForceSpacesBefore (methodDeclaration.RParToken, policy.SpacesWithinMethodDeclarationParentheses);
			} else {
				ForceSpacesAfter (methodDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore (methodDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}
			FormatCommas (methodDeclaration, policy.SpaceBeforeMethodDeclarationParameterComma, policy.SpaceAfterMethodDeclarationParameterComma);

			return base.VisitMethodDeclaration (methodDeclaration, data);
		}

		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			ForceSpacesBefore (constructorDeclaration.LParToken, policy.SpaceBeforeConstructorDeclarationParentheses);
			if (constructorDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (constructorDeclaration.LParToken, policy.SpacesWithinConstructorDeclarationParentheses);
				ForceSpacesBefore (constructorDeclaration.RParToken, policy.SpacesWithinConstructorDeclarationParentheses);
			} else {
				ForceSpacesAfter (constructorDeclaration.LParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
				ForceSpacesBefore (constructorDeclaration.RParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
			}
			FormatCommas (constructorDeclaration, policy.SpaceBeforeConstructorDeclarationParameterComma, policy.SpaceAfterConstructorDeclarationParameterComma);

			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}

		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			CSharpTokenNode lParen = destructorDeclaration.LParToken;
			int offset = this.data.Document.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.SpaceBeforeConstructorDeclarationParentheses);
			return base.VisitDestructorDeclaration (destructorDeclaration, data);
		}

		void AddChange (int offset, int removedChars, string insertedText)
		{
			if (changes.Cast<AstSpacingVisitor.MyTextReplaceChange> ().Any (c => c.Offset == offset && c.RemovedChars == removedChars 
 && c.InsertedText == insertedText))
				return;
			string currentText = data.Document.GetTextAt (offset, removedChars);
			if (currentText == insertedText)
				return;
			if (currentText.Any (c =>
				!(char.IsWhiteSpace (c) || c == '\r' || c == '\t')))
				throw new InvalidOperationException ("Tried to remove non ws chars: '" + currentText + "' at:" + data.Document.OffsetToLocation (offset));
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
						AddChange (offset, IsSpacing (ch) ? 1 : 0, " ");
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

		public void FixSemicolon (CSharpTokenNode semicolon)
		{
			if (semicolon.IsNull)
				return;
			int endOffset = data.Document.LocationToOffset (semicolon.StartLocation.Line, semicolon.StartLocation.Column);
			int offset = endOffset;
			while (offset - 1 > 0 && char.IsWhiteSpace (data.Document.GetCharAt (offset - 1))) {
				offset--;
			}
			if (offset < endOffset) {
				AddChange (offset, endOffset - offset, null);
			}
		}	

		public override object VisitVariableInitializer (VariableInitializer variableInitializer, object data)
		{
			if (!variableInitializer.AssignToken.IsNull)
				ForceSpacesAround (variableInitializer.AssignToken, policy.SpaceAroundAssignment);
			if (!variableInitializer.Initializer.IsNull)
				variableInitializer.Initializer.AcceptVisitor (this, data);
			return data;
		}

		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			foreach (var initializer in variableDeclarationStatement.Variables) {
				initializer.AcceptVisitor (this, data);
			}
			FormatCommas (variableDeclarationStatement, policy.SpaceBeforeLocalVariableDeclarationComma, policy.SpaceAfterLocalVariableDeclarationComma);
			return data;
		}

		public override object VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			ForceSpacesBefore (invocationExpression.LParToken, policy.SpaceBeforeMethodCallParentheses);
			if (invocationExpression.Arguments.Any ()) {
				ForceSpacesAfter (invocationExpression.LParToken, policy.SpacesWithinMethodCallParentheses);
				ForceSpacesBefore (invocationExpression.RParToken, policy.SpacesWithinMethodCallParentheses);
			} else {
				ForceSpacesAfter (invocationExpression.LParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
				ForceSpacesBefore (invocationExpression.RParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
			}
			FormatCommas (invocationExpression, policy.SpaceBeforeMethodCallParameterComma, policy.SpaceAfterMethodCallParameterComma);

			return base.VisitInvocationExpression (invocationExpression, data);
		}

		public override object VisitIndexerExpression (IndexerExpression indexerExpression, object data)
		{
			ForceSpacesBefore (indexerExpression.LBracketToken, policy.SpaceBeforeBrackets);
			ForceSpacesAfter (indexerExpression.LBracketToken, policy.SpacesWithinBrackets);
			ForceSpacesBefore (indexerExpression.RBracketToken, policy.SpacesWithinBrackets);
			FormatCommas (indexerExpression, policy.SpaceBeforeBracketComma, policy.SpaceAfterBracketComma);

			return base.VisitIndexerExpression (indexerExpression, data);
		}

		public override object VisitExpressionStatement (ExpressionStatement expressionStatement, object data)
		{
			FixSemicolon (expressionStatement.SemicolonToken);
			return base.VisitExpressionStatement (expressionStatement, data);
		}

		public override object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
		{
			ForceSpacesBefore (ifElseStatement.LParToken, policy.SpaceBeforeIfParentheses);

			ForceSpacesAfter (ifElseStatement.LParToken, policy.SpacesWithinIfParentheses);
			ForceSpacesBefore (ifElseStatement.RParToken, policy.SpacesWithinIfParentheses);


			return base.VisitIfElseStatement (ifElseStatement, data);
		}

		public override object VisitWhileStatement (WhileStatement whileStatement, object data)
		{
			ForceSpacesBefore (whileStatement.LParToken, policy.SpaceBeforeWhileParentheses);

			ForceSpacesAfter (whileStatement.LParToken, policy.SpacesWithinWhileParentheses);
			ForceSpacesBefore (whileStatement.RParToken, policy.SpacesWithinWhileParentheses);

			return base.VisitWhileStatement (whileStatement, data);
		}

		public override object VisitForStatement (ForStatement forStatement, object data)
		{
			foreach (AstNode node in forStatement.Children) {
				if (node.Role == ForStatement.Roles.Semicolon) {
					if (node.NextSibling is CSharpTokenNode || node.NextSibling is EmptyStatement)
						continue;
					ForceSpacesBefore (node, policy.SpaceBeforeForSemicolon);
					ForceSpacesAfter (node, policy.SpaceAfterForSemicolon);
				}
			}

			ForceSpacesBefore (forStatement.LParToken, policy.SpaceBeforeForParentheses);

			ForceSpacesAfter (forStatement.LParToken, policy.SpacesWithinForParentheses);
			ForceSpacesBefore (forStatement.RParToken, policy.SpacesWithinForParentheses);

			if (forStatement.EmbeddedStatement != null)
				forStatement.EmbeddedStatement.AcceptVisitor (this, data);

			return null;
		}

		public override object VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			ForceSpacesBefore (foreachStatement.LParToken, policy.SpaceBeforeForeachParentheses);

			ForceSpacesAfter (foreachStatement.LParToken, policy.SpacesWithinForEachParentheses);
			ForceSpacesBefore (foreachStatement.RParToken, policy.SpacesWithinForEachParentheses);

			return base.VisitForeachStatement (foreachStatement, data);
		}

		public override object VisitCatchClause (CatchClause catchClause, object data)
		{
			if (!catchClause.LParToken.IsNull) {
				ForceSpacesBefore (catchClause.LParToken, policy.SpaceBeforeCatchParentheses);

				ForceSpacesAfter (catchClause.LParToken, policy.SpacesWithinCatchParentheses);
				ForceSpacesBefore (catchClause.RParToken, policy.SpacesWithinCatchParentheses);
			}

			return base.VisitCatchClause (catchClause, data);
		}

		public override object VisitLockStatement (LockStatement lockStatement, object data)
		{
			ForceSpacesBefore (lockStatement.LParToken, policy.SpaceBeforeLockParentheses);

			ForceSpacesAfter (lockStatement.LParToken, policy.SpacesWithinLockParentheses);
			ForceSpacesBefore (lockStatement.RParToken, policy.SpacesWithinLockParentheses);


			return base.VisitLockStatement (lockStatement, data);
		}

		public override object VisitUsingStatement (UsingStatement usingStatement, object data)
		{
			ForceSpacesBefore (usingStatement.LParToken, policy.SpaceBeforeUsingParentheses);

			ForceSpacesAfter (usingStatement.LParToken, policy.SpacesWithinUsingParentheses);
			ForceSpacesBefore (usingStatement.RParToken, policy.SpacesWithinUsingParentheses);

			return base.VisitUsingStatement (usingStatement, data);
		}

		public override object VisitSwitchStatement (MonoDevelop.CSharp.Ast.SwitchStatement switchStatement, object data)
		{
			ForceSpacesBefore (switchStatement.LParToken, policy.SpaceBeforeSwitchParentheses);

			ForceSpacesAfter (switchStatement.LParToken, policy.SpacesWithinSwitchParentheses);
			ForceSpacesBefore (switchStatement.RParToken, policy.SpacesWithinSwitchParentheses);

			return base.VisitSwitchStatement (switchStatement, data);
		}

		public override object VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, object data)
		{
			ForceSpacesAfter (parenthesizedExpression.LParToken, policy.SpacesWithinParentheses);
			ForceSpacesBefore (parenthesizedExpression.RParToken, policy.SpacesWithinParentheses);
			return base.VisitParenthesizedExpression (parenthesizedExpression, data);
		}

		public override object VisitSizeOfExpression (SizeOfExpression sizeOfExpression, object data)
		{
			ForceSpacesBefore (sizeOfExpression.LParToken, policy.SpaceBeforeSizeOfParentheses);
			ForceSpacesAfter (sizeOfExpression.LParToken, policy.SpacesWithinSizeOfParentheses);
			ForceSpacesBefore (sizeOfExpression.RParToken, policy.SpacesWithinSizeOfParentheses);
			return base.VisitSizeOfExpression (sizeOfExpression, data);
		}

		public override object VisitTypeOfExpression (TypeOfExpression typeOfExpression, object data)
		{
			ForceSpacesBefore (typeOfExpression.LParToken, policy.SpaceBeforeTypeOfParentheses);
			ForceSpacesAfter (typeOfExpression.LParToken, policy.SpacesWithinTypeOfParentheses);
			ForceSpacesBefore (typeOfExpression.RParToken, policy.SpacesWithinTypeOfParentheses);
			return base.VisitTypeOfExpression (typeOfExpression, data);
		}

		public override object VisitCheckedExpression (CheckedExpression checkedExpression, object data)
		{
			ForceSpacesAfter (checkedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBefore (checkedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			return base.VisitCheckedExpression (checkedExpression, data);
		}

		public override object VisitUncheckedExpression (UncheckedExpression uncheckedExpression, object data)
		{
			ForceSpacesAfter (uncheckedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBefore (uncheckedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			return base.VisitUncheckedExpression (uncheckedExpression, data);
		}

		public override object VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, object data)
		{
			ForceSpacesBefore (objectCreateExpression.LParToken, policy.SpaceBeforeNewParentheses);

			return base.VisitObjectCreateExpression (objectCreateExpression, data);
		}

		public override object VisitArrayCreateExpression (ArrayCreateExpression arrayObjectCreateExpression, object data)
		{
			FormatCommas (arrayObjectCreateExpression, policy.SpaceBeforeMethodCallParameterComma, policy.SpaceAfterMethodCallParameterComma);
			return base.VisitArrayCreateExpression (arrayObjectCreateExpression, data);
		}

		public override object VisitLambdaExpression (LambdaExpression lambdaExpression, object data)
		{
			ForceSpacesAfter (lambdaExpression.ArrowToken, true);
			ForceSpacesBefore (lambdaExpression.ArrowToken, true);

			return base.VisitLambdaExpression (lambdaExpression, data);
		}


	}
}
