// 
// IntegrateTemporaryVariable.cs
//  
// Author:
//       Andrea Krüger <andrea@icsharpcode.net>
// 
// Copyright (c) 2009 Andrea Krüger
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.IntegrateTemporaryVariable
{
	public class IntegrateTemporaryVariableRefactoring : RefactoringOperation
	{
		public IntegrateTemporaryVariableRefactoring ()
		{
			Name = "Integrate Temporary Variable";
		}
		
		INode GetMemberBodyNode (MonoDevelop.Refactoring.RefactoringOptions options)
		{
			IMember member = ((LocalVariable) options.SelectedItem).DeclaringMember;
			if (member == null)
				return null;
			int start = options.Document.TextEditor.GetPositionFromLineColumn (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
			int end = options.Document.TextEditor.GetPositionFromLineColumn (member.BodyRegion.End.Line, member.BodyRegion.End.Column);
			string memberBody = options.Document.TextEditor.GetText (start, end);
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (provider == null) {
//				Console.WriteLine("!!!Provider not found!");
				return null;
			}
			return provider.ParseText (memberBody);
		}

		LocalVariableDeclaration GetVariableDeclaration (RefactoringOptions options)
		{
			if (!(options.SelectedItem is LocalVariable)) {
				Console.WriteLine ("!!! Item is not LocalVariable");
				return null;
			}
			LocalVariable var = (LocalVariable)options.SelectedItem;
//			Console.WriteLine ("!!! Item = " + var.ToString ());
//			Console.WriteLine ("!!! VarName = " + var.Name);
//			Console.WriteLine ("!!! Member = " + var.DeclaringMember.ToString ());

			INode result = GetMemberBodyNode (options);
			if (result == null)
				return null;
			Location cursorLocation = new Location (options.Document.TextEditor.CursorColumn, options.Document.TextEditor.CursorLine - var.DeclaringMember.BodyRegion.Start.Line);
			// relativ to the memberBody
			Location selectionStartLocation;
			int l, c;
			if (options.Document.TextEditor.SelectionStartPosition.Equals (options.Document.TextEditor.CursorPosition)) {
				options.Document.TextEditor.GetLineColumnFromPosition (options.Document.TextEditor.SelectionEndPosition, out l, out c);
			} else {
				options.Document.TextEditor.GetLineColumnFromPosition (options.Document.TextEditor.SelectionStartPosition, out l, out c);
			}
			selectionStartLocation = new Location (c, l - var.DeclaringMember.BodyRegion.Start.Line); // relativ to the memberBody
			INode statementAtCursor = null;
//			Console.WriteLine ("!!! Suche Variablendeklaration an Position: " + cursorLocation.ToString ());
			while (result is BlockStatement) {
				foreach (Statement child in result.Children) {
//					Console.WriteLine ("!!! child an: " + child.StartLocation.ToString () + " --- " + child.EndLocation.ToString ());
					if (child.StartLocation <= cursorLocation && child.EndLocation >= cursorLocation) {
						statementAtCursor = child;
//						Console.WriteLine ("!!! Gefunden, Typ: " + statementAtCursor.GetType ().ToString ());
						if (child.StartLocation > selectionStartLocation || child.EndLocation < selectionStartLocation) {
//							Console.WriteLine ("!!!SelectionStart ausserhalb");
							return null;
						}
						break;
					}
				}
				result = statementAtCursor;
			}
			if (statementAtCursor is LocalVariableDeclaration)
				return (LocalVariableDeclaration)statementAtCursor;
			return null;
		}

		
		public override bool IsValid (RefactoringOptions options)
		{
//			Console.WriteLine ("!!! IsValid?");
			if (GetVariableDeclaration (options)  != null) {
//				Console.WriteLine("Yes, is Valid");
				return true;
			}
			return false;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			LocalVariableDeclaration declaration = GetVariableDeclaration (options);
			INode memberNode = GetMemberBodyNode (options);
			List<Change> changes = new List<Change> ();
			try {
				memberNode.AcceptVisitor (new IntegrateTemporaryVariableVisitor (), new IntegrateTemporaryVariableVisitorOptions (changes, options, declaration));
			} catch (IntegrateTemporaryVariableException) {
				return null;
			}
			return null;
		}
		
		public override void Run (RefactoringOptions options)
		{
		}
		
		class IntegrateTemporaryVariableVisitor : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
			{
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (assignmentExpression.Left is IdentifierExpression && ((IdentifierExpression)assignmentExpression.Left).Identifier == options.GetName ())
					throw new IntegrateTemporaryVariableAssignmentException ();
				assignmentExpression.Left.AcceptVisitor(this, data);
				return assignmentExpression.Right.AcceptVisitor(this, data);
			}
			bool MustParantizeForUnary (Expression e)
			{
				if (e is InvocationExpression || e is CastExpression || e is MemberReferenceExpression)
					return true;
				return false;
			}
			bool MustParantizeForBinary (Expression e)
			{
				if (MustParantizeForUnary (e)) {
					return true;
				}
				if (e is CastExpression) {
					return true;
				}
				return false;
			}
			bool IsExpressionToReplace (Expression e, IntegrateTemporaryVariableVisitorOptions o)
			{
				if (!(e is IdentifierExpression))
					return false;
				if (((IdentifierExpression)e).Identifier == o.GetName ())
					return true;
				return false;
			}
			
			Change ReplaceExpression (Expression eToReplace, Expression eReplaceWith, IntegrateTemporaryVariableVisitorOptions options)
			{
				TextReplaceChange change = new TextReplaceChange ();
				change.Description = string.Format (GettextCatalog.GetString ("Substitute variable {0} with the Initializeexpression"), options.GetName());
				change.FileName = options.Options.Document.FileName;

				change.Offset = options.Options.Document.TextEditor.GetPositionFromLineColumn (eToReplace.StartLocation.Line, eToReplace.StartLocation.Column);
				change.RemovedChars = options.GetName ().Length;

				INRefactoryASTProvider provider = options.Options.GetASTProvider ();
				change.InsertedText = options.Options.GetWhitespaces (change.Offset) + provider.OutputNode (options.Options.Dom, eReplaceWith);

				return change;
			}
			
			public override object VisitAddHandlerStatement (AddHandlerStatement statement, object data)
			{
				if (IsExpressionToReplace (((AddHandlerStatement)statement).EventExpression, (IntegrateTemporaryVariableVisitorOptions)data)) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else {
					((AddHandlerStatement)statement).EventExpression.AcceptVisitor (this, data);
				}
				if (IsExpressionToReplace (((AddHandlerStatement)statement).HandlerExpression, (IntegrateTemporaryVariableVisitorOptions)data)) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else {
					return ((AddHandlerStatement)statement).HandlerExpression.AcceptVisitor(this, data);
				}
			}
			
			public override object VisitAddressOfExpression (AddressOfExpression expression, object data)
			{
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (expression, (IntegrateTemporaryVariableVisitorOptions)data)) {
					options.Changes.Add (ReplaceExpression (expression.Expression, options.Declaration.GetVariableDeclaration (options.GetName ()).Initializer, options));
					return null;
				} else {
					return base.VisitAddressOfExpression (expression, data);
				}
			}
			
			public override object VisitIdentifierExpression (IdentifierExpression e, object data)
			{
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (!(e.Identifier == options.GetName ())) {
					return data;
				}
				if (e.Parent is BinaryOperatorExpression) {
					Expression initializer = options.Declaration.GetVariableDeclaration (options.GetName ()).Initializer;
					if (MustParantizeForBinary (initializer)) {

					} else {

					}
				} else if (e.Parent is UnaryOperatorExpression) {
					Expression initializer = options.Declaration.GetVariableDeclaration (options.GetName ()).Initializer;
					if (MustParantizeForUnary (initializer)) {

					} else {

					}
				} else if (e.Parent is AddressOfExpression) {

				} else if (e.Parent is AssignmentExpression) {

				} else if (e.Parent is CaseLabel) {

				} else if (e.Parent is CastExpression) {

				} else if (e.Parent is CatchClause) {

				} else if (e.Parent is CheckedExpression) {

				} else if (e.Parent is CollectionInitializerExpression) {

				} else if (e.Parent is ConditionalExpression) {

				} else if (e.Parent is ConstructorInitializer) {

				} else if (e.Parent is DirectionExpression) {
					if (((DirectionExpression)e.Parent).FieldDirection == FieldDirection.Out || ((DirectionExpression)e.Parent).FieldDirection == FieldDirection.Ref)
						throw new IntegrateTemporaryVariableDirectionException ();

				} else if (e.Parent is DoLoopStatement) {

				} else if (e.Parent is ElseIfSection) {

				} else if (e.Parent is EraseStatement) {

				} else if (e.Parent is ErrorStatement) {

				} else if (e.Parent is EventDeclaration) {

				} else if (e.Parent is ExpressionRangeVariable) {

				} else if (e.Parent is ExpressionStatement) {

				} else if (e.Parent is ForeachStatement) {

				} else if (e.Parent is ForNextStatement) {

				} else if (e.Parent is ForStatement) {

				} else if (e.Parent is GotoCaseStatement) {

				} else if (e.Parent is IfElseStatement) {

				} else if (e.Parent is IndexerExpression) {

				} else if (e.Parent is InvocationExpression) {

				} else if (e.Parent is LambdaExpression) {

				} else if (e.Parent is LockStatement) {

				} else if (e.Parent is MemberReferenceExpression) {

				} else if (e.Parent is NamedArgumentExpression) {

				} else if (e.Parent is ObjectCreateExpression) {

				} else if (e.Parent is ParameterDeclarationExpression) {

				} else if (e.Parent is ParenthesizedExpression) {

				} else if (e.Parent is PointerReferenceExpression) {

				} else if (e.Parent is QueryExpressionFromOrJoinClause) {

				} else if (e.Parent is QueryExpressionGroupClause) {

				} else if (e.Parent is QueryExpressionJoinClause) {

				} else if (e.Parent is QueryExpressionJoinConditionVB) {

				} else if (e.Parent is QueryExpressionLetClause) {

				} else if (e.Parent is QueryExpressionOrdering) {

				} else if (e.Parent is QueryExpressionPartitionVBClause) {

				} else if (e.Parent is QueryExpressionSelectClause) {

				} else if (e.Parent is QueryExpressionWhereClause) {

				} else if (e.Parent is RaiseEventStatement) {

				} else if (e.Parent is RemoveHandlerStatement) {

				} else if (e.Parent is ReturnStatement) {

				} else if (e.Parent is StackAllocExpression) {

				} else if (e.Parent is SwitchStatement) {

				} else if (e.Parent is ThrowStatement) {

				} else if (e.Parent is TypeOfIsExpression) {

				} else if (e.Parent is UncheckedExpression) {

				} else if (e.Parent is VariableDeclaration) {

				} else if (e.Parent is WithStatement) {
					
				} 
				return data;
			}
		}
		class IntegrateTemporaryVariableException : System.Exception
		{}
		class IntegrateTemporaryVariableAssignmentException : IntegrateTemporaryVariableException
		{}
		class IntegrateTemporaryVariableDirectionException : IntegrateTemporaryVariableException
		{}
		class IntegrateTemporaryVariableNotImplementedException : IntegrateTemporaryVariableException
		{}
		class IntegrateTemporaryVariableVisitorOptions
		{
			bool isvalid = true;
			public bool IsValid {
				get { return isvalid; }
			}
			public List<Change> Changes {
				get;
				set;
			}
			public RefactoringOptions Options {
				get;
				set;
			}
			public LocalVariableDeclaration Declaration {
				get;
				set;
			}
			public string GetName ()
			{
				return ((LocalVariable)Options.SelectedItem).Name;
			}
			public IntegrateTemporaryVariableVisitorOptions (List<Change> changes, RefactoringOptions options, LocalVariableDeclaration declaration)
			{
				Changes = changes;
				Options = options;
				Declaration = declaration;
			}
		}
	}
}
