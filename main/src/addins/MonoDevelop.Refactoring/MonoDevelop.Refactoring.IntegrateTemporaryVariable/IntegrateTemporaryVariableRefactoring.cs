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
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
 
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring.IntegrateTemporaryVariable
{
	public class IntegrateTemporaryVariableRefactoring : RefactoringOperation
	{
		public override string AccelKey {
			get {
				var cmdInfo = IdeApp.CommandService.GetCommandInfo (RefactoryCommands.IntegrateTemporaryVariable, null);
				if (cmdInfo != null && cmdInfo.AccelKey != null)
					return cmdInfo.AccelKey.Replace ("dead_circumflex", "^");
				return null;
			}
		}
		
		public IntegrateTemporaryVariableRefactoring ()
		{
			Name = "Integrate Temporary Variable";
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Integrate Temporary Variable");
		}
		
		ICSharpCode.NRefactory.Ast.INode GetMemberBodyNode (MonoDevelop.Refactoring.RefactoringOptions options)
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

		public override bool IsValid (RefactoringOptions options)
		{
			if (!(options.SelectedItem is LocalVariable)) {
				//				Console.WriteLine ("!!! Item is not LocalVariable");
				return false;
			}
			ICSharpCode.NRefactory.Ast.INode result = GetMemberBodyNode (options);
			if (result == null)
				return false;
			return true;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			ICSharpCode.NRefactory.Ast.INode memberNode = GetMemberBodyNode (options);
			List<Change> changes = new List<Change> ();
			if (memberNode == null)
				return null;
			try {
				//				Console.WriteLine ("AcceptVisitor");
				//				Console.WriteLine ("Start: " + memberNode.StartLocation.ToString () + " - End: " + memberNode.EndLocation.ToString ());
				memberNode.AcceptVisitor (new IntegrateTemporaryVariableVisitor (), new IntegrateTemporaryVariableVisitorOptions (changes, options));
				//				Console.WriteLine ("AcceptVisitor done");
			} catch (IntegrateTemporaryVariableException e) {
				//				Console.WriteLine ("Exception catched");
				MessageService.ShowError ("Could not perform integration : ", e.Message);
				return new List<Change>();
			}
//			Console.WriteLine ("Changes calculated");
			return changes;
		}
		
		public override void Run (RefactoringOptions options)
		{
			List<Change> changes = PerformChanges (options, null);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Name, null);
			RefactoringService.AcceptChanges (monitor, options.Dom, changes);
//			Console.WriteLine ("Changes accepted");
		}
		
		class IntegrateTemporaryVariableVisitor : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			bool IsPrimary (Expression e)
			{
				if (e is IdentifierExpression || e is ParenthesizedExpression || e is PrimitiveExpression) {
//					if (e is PrimitiveExpression)
//						Console.WriteLine ("PrimitiveExpression: " + ((PrimitiveExpression)e).StringValue);
					return true;
				}
				if (e is InvocationExpression || e is CastExpression || e is MemberReferenceExpression)
					return true;
				return false;
			}
			bool IsUnary (Expression e)
			{
				if (IsPrimary (e)) {
					return true;
				}
				if (e is CastExpression) {
					return true;
				}
				return false;
			}
			bool IsExpressionToReplace (Expression e, IntegrateTemporaryVariableVisitorOptions o)
			{
//				Console.WriteLine ("Checking Replace: " + e.ToString() + " - " + o.GetName());
				return (e is IdentifierExpression) && (((IdentifierExpression)e).Identifier == o.GetName ());
			}
			
			Change ReplaceExpression (Expression toReplace, Expression replaceWith, IntegrateTemporaryVariableVisitorOptions options)
			{
//				Console.WriteLine ("Replace");
				TextReplaceChange change = new TextReplaceChange ();
				change.Description = string.Format (GettextCatalog.GetString ("Substitute variable {0} with the Initializeexpression"), options.GetName ());
				change.FileName = options.Options.Document.FileName;

				change.Offset = options.Options.Document.TextEditor.GetPositionFromLineColumn (toReplace.StartLocation.Line + ((LocalVariable)options.Options.SelectedItem).DeclaringMember.BodyRegion.Start.Line, toReplace.StartLocation.Column);
				change.RemovedChars = options.GetName ().Length;

				INRefactoryASTProvider provider = options.Options.GetASTProvider ();
				change.InsertedText = provider.OutputNode (options.Options.Dom, replaceWith);

//				Console.WriteLine ("Replace done");
				return change;
			}
			
			public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
			{
				//				Console.WriteLine ("LocalVariableDeclaration: " + localVariableDeclaration.StartLocation.ToString () + " - " + localVariableDeclaration.EndLocation.ToString ());
				localVariableDeclaration.TypeReference.AcceptVisitor (this, data);
				foreach (VariableDeclaration o in localVariableDeclaration.Variables) {
					if (o.Name == ((IntegrateTemporaryVariableVisitorOptions)data).GetName ()) {
						IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
						options.Initializer = localVariableDeclaration.GetVariableDeclaration(((LocalVariable)options.Options.SelectedItem).Name).Initializer;
						if (localVariableDeclaration.Variables.Count == 1) {
							TextReplaceChange change = new TextReplaceChange ();
							change.Description = string.Format (GettextCatalog.GetString ("Deleting local variable declaration {0}"), options.GetName ());
							change.FileName = options.Options.Document.FileName;

							change.Offset = options.Options.Document.TextEditor.GetPositionFromLineColumn (localVariableDeclaration.StartLocation.Line + ((LocalVariable)options.Options.SelectedItem).DeclaringMember.BodyRegion.Start.Line, localVariableDeclaration.StartLocation.Column);
							int end = options.Options.Document.TextEditor.GetPositionFromLineColumn (localVariableDeclaration.EndLocation.Line + ((LocalVariable)options.Options.SelectedItem).DeclaringMember.BodyRegion.Start.Line, localVariableDeclaration.EndLocation.Column);

							change.RemovedChars = end - change.Offset;
							change.InsertedText = "";
							((IntegrateTemporaryVariableVisitorOptions)data).Changes.Add (change);
						} else {
							TextReplaceChange change = new TextReplaceChange ();
							change.Description = string.Format (GettextCatalog.GetString ("Deleting local variable declaration {0}"), options.GetName ());
							change.FileName = options.Options.Document.FileName;

							change.Offset = options.Options.Document.TextEditor.GetPositionFromLineColumn (localVariableDeclaration.StartLocation.Line + ((LocalVariable)options.Options.SelectedItem).DeclaringMember.BodyRegion.Start.Line, localVariableDeclaration.StartLocation.Column);
							int end = options.Options.Document.TextEditor.GetPositionFromLineColumn (localVariableDeclaration.EndLocation.Line + ((LocalVariable)options.Options.SelectedItem).DeclaringMember.BodyRegion.Start.Line, localVariableDeclaration.EndLocation.Column);

							change.RemovedChars = end - change.Offset;
							localVariableDeclaration.Variables.Remove (localVariableDeclaration.GetVariableDeclaration (options.GetName ()));
							INRefactoryASTProvider provider = options.Options.GetASTProvider ();
							change.InsertedText = options.Options.GetWhitespaces (change.Offset) + provider.OutputNode (options.Options.Dom, localVariableDeclaration);
							((IntegrateTemporaryVariableVisitorOptions)data).Changes.Add (change);
						}
					} else {
						o.AcceptVisitor (this, data);
					}
				}
				return null;
			}
			public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
			{
//				Console.WriteLine ("AssignmentExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
//				Console.WriteLine (assignmentExpression.ToString ());
				if (IsExpressionToReplace (assignmentExpression.Left, (IntegrateTemporaryVariableVisitorOptions)data)) {
//					Console.WriteLine ("Assignment Left");
					throw new IntegrateTemporaryVariableAssignmentException ();
				} else {
//					Console.WriteLine ("Assignment not to replace");
					assignmentExpression.Left.AcceptVisitor (this, data);
				}
				if (IsExpressionToReplace (assignmentExpression.Right, (IntegrateTemporaryVariableVisitorOptions)data)) {
					options.Changes.Add (ReplaceExpression (assignmentExpression.Right, options.Initializer, options));
					return null;
				} else
					return assignmentExpression.Right.AcceptVisitor (this, data);
			}
			
			// ExpressionStatement, LockStatement can not be the parent of an IdentifierExpression
			
			public override object VisitAddressOfExpression (AddressOfExpression expression, object data)
			{
//				Console.WriteLine ("AddressOfExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (expression.Expression, options)) {
					// in this Case the integration is not valid
					throw new IntegrateTemporaryVariableAddressOfException ();
				} else {
					return base.VisitAddressOfExpression (expression, data);
				}
			}
			public override object VisitCastExpression (CastExpression expression, object data)
			{
				//				Console.WriteLine ("CastExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (expression.Expression, (IntegrateTemporaryVariableVisitorOptions)data)) {
					if (IsPrimary (options.Initializer))
						options.Changes.Add (ReplaceExpression (expression.Expression, options.Initializer, options));
					else
						options.Changes.Add (ReplaceExpression (expression.Expression, new ParenthesizedExpression (options.Initializer), options));
					return null;
				} else {
					return base.VisitCastExpression (expression, data);
				}
			}
			bool IsMultiplicativ (BinaryOperatorType t) 
			{
				return t == BinaryOperatorType.Divide || t == BinaryOperatorType.Multiply || t == BinaryOperatorType.Modulus;
			}
			bool IsAdditivOrHigher (BinaryOperatorType t)
			{
				return IsMultiplicativ(t) || t == BinaryOperatorType.Add || t == BinaryOperatorType.Subtract;
			}
			bool IsShiftOrHigher (BinaryOperatorType t)
			{
				return IsAdditivOrHigher(t) || t == BinaryOperatorType.ShiftLeft || t == BinaryOperatorType.ShiftRight;
			}
			public override object VisitTypeOfIsExpression (TypeOfIsExpression stmt, object data)
			{
				Console.WriteLine ("TypeOfIsExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (stmt.Expression, options)) {
					if (IsUnary (options.Initializer) || (options.Initializer is BinaryOperatorExpression && IsShiftOrHigher (((BinaryOperatorExpression)options.Initializer).Op)))
						options.Changes.Add (ReplaceExpression (stmt.Expression, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (stmt.Expression, new ParenthesizedExpression (options.Initializer), options));
					return null;
				} else {
					return base.VisitTypeOfIsExpression (stmt, data);
				}
			}
			public override object VisitUnaryOperatorExpression (UnaryOperatorExpression expression, object data)
			{
				Console.WriteLine ("UnaryOperatorExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (expression.Expression, options)) {
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (expression.Expression, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (expression.Expression, new ParenthesizedExpression (options.Initializer), options));
					return null;
				} else {
					return base.VisitUnaryOperatorExpression (expression, data);
				}
			}
			public override object VisitBinaryOperatorExpression (BinaryOperatorExpression expression, object data) // there are too much Parenthisiz
			{
				Console.WriteLine ("BinaryOperatorExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (expression.Left, (IntegrateTemporaryVariableVisitorOptions)data))
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (expression.Left, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (expression.Left, new ParenthesizedExpression (options.Initializer), options));
				else
					expression.Left.AcceptVisitor(this, data);
				Console.WriteLine("LeftSide done");
				if (IsExpressionToReplace (expression.Right, (IntegrateTemporaryVariableVisitorOptions)data)) {
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (expression.Right, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (expression.Right, new ParenthesizedExpression (options.Initializer), options));
					return null;
				} else
					return expression.Right.AcceptVisitor(this, data);
			}
			public override object VisitConditionalExpression (ConditionalExpression expression, object data)
			{
				Console.WriteLine ("ConditionalExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (expression.Condition, (IntegrateTemporaryVariableVisitorOptions)data))
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (expression.Condition, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (expression.Condition, new ParenthesizedExpression (options.Initializer), options)); else
					expression.Condition.AcceptVisitor (this, data);
				if (IsExpressionToReplace (expression.TrueExpression, (IntegrateTemporaryVariableVisitorOptions)data))
					if (!(options.Initializer is AssignmentExpression || options.Initializer is ConditionalExpression))
						options.Changes.Add (ReplaceExpression (expression.TrueExpression, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (expression.TrueExpression, new ParenthesizedExpression (options.Initializer), options)); else
					expression.TrueExpression.AcceptVisitor (this, data);
				if (IsExpressionToReplace (expression.FalseExpression, (IntegrateTemporaryVariableVisitorOptions)data)) {
					if (!(options.Initializer is AssignmentExpression || options.Initializer is ConditionalExpression))
						options.Changes.Add (ReplaceExpression (expression.FalseExpression, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (expression.FalseExpression, new ParenthesizedExpression (options.Initializer), options));
					return null;
				} else
					return expression.FalseExpression.AcceptVisitor(this, data);
			}
			
			public override object VisitIndexerExpression (IndexerExpression e, object data)
			{
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (e.TargetObject, options)) {
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (e.TargetObject, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (e.TargetObject, new ParenthesizedExpression (options.Initializer), options));
				} else
					e.TargetObject.AcceptVisitor (this, data);
				
				foreach (Expression o in e.Indexes) {
					o.AcceptVisitor(this, data);
				}
				return null;
			}
			
			public override object VisitInvocationExpression (InvocationExpression e, object data)
			{
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (e.TargetObject, options)) {
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (e.TargetObject, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (e.TargetObject, new ParenthesizedExpression (options.Initializer), options));
				} else
					e.TargetObject.AcceptVisitor (this, data);

				foreach (Expression o in e.Arguments) {
					o.AcceptVisitor (this, data);
				}
				return null;
			}
			public override object VisitMemberReferenceExpression (MemberReferenceExpression e, object data)
			{
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (IsExpressionToReplace (e.TargetObject, options)) {
					if (IsUnary (options.Initializer))
						options.Changes.Add (ReplaceExpression (e.TargetObject, options.Initializer, options)); else
						options.Changes.Add (ReplaceExpression (e.TargetObject, new ParenthesizedExpression (options.Initializer), options));
					return null;
				} else
					return e.TargetObject.AcceptVisitor (this, data);
			}
			
			public override object VisitIdentifierExpression (IdentifierExpression e, object data)
			{
				Console.WriteLine ("IdentifierExpression");
				IntegrateTemporaryVariableVisitorOptions options = (IntegrateTemporaryVariableVisitorOptions)data;
				if (!(e.Identifier == options.GetName ())) {
					return data;
				}
				// if sometimes Paranthesis are needed, there is an own Visitor for the Parent
				// here are all Cases left, where no Parent Visitor ist written
				// somtimes because the Syntax ist not clear (add them if you know the Syntax)
				// or because no Paranthesis are needet (because different Chars are used to separate the expression)
				// or because the integration is not valid in this case
				if (e.Parent is CaseLabel) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is AddHandlerStatement) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is RemoveHandlerStatement) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is CatchClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is DirectionExpression) {
					if (((DirectionExpression)e.Parent).FieldDirection == FieldDirection.Out || ((DirectionExpression)e.Parent).FieldDirection == FieldDirection.Ref)
						throw new IntegrateTemporaryVariableDirectionException ();
				} else if (e.Parent is DoLoopStatement) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is EraseStatement) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is ErrorStatement) {
					// VB
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is EventDeclaration) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is ExpressionRangeVariable) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is ForNextStatement) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is LambdaExpression) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is PointerReferenceExpression) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionFromOrJoinClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionGroupClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionJoinClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionJoinConditionVB) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionLetClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionOrdering) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionPartitionVBClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionSelectClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is QueryExpressionWhereClause) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is RaiseEventStatement) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				} else if (e.Parent is StackAllocExpression) {
					throw new IntegrateTemporaryVariableNotImplementedException ();
				}
				// ConstructorInitializer, WithStatement, ParameterDeclarationExpression, CollectionInitializerExpression, ParenthesizedExpression, 
				// ElseIfSection, IfElseStatement, ForeachStatement, ForStatement, Indexes of the IndexerExpression, Arguments of the InvocationExpression,
				// NamedArgumentExpression, ObjectCreateExpression, VariableDeclaration (if the Initializer is the IdentifierExpression, this is handled here)
				// ThrowStatement, SwitchStatement, ReturnStatement, GotoCaseStatement, CheckedExpression, UncheckedExpression
				options.Changes.Add (ReplaceExpression (e, options.Initializer, options));
				return data;
			}
		}
		class IntegrateTemporaryVariableException : System.Exception
		{}
		class IntegrateTemporaryVariableAddressOfException : IntegrateTemporaryVariableException
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
			public Expression Initializer
			{
				get;
				set;
			}
			public string GetName ()
			{
				return ((LocalVariable)Options.SelectedItem).Name;
			}
			public IntegrateTemporaryVariableVisitorOptions (List<Change> changes, RefactoringOptions options)
			{
				Changes = changes;
				Options = options;
			}
		}
	}
}
