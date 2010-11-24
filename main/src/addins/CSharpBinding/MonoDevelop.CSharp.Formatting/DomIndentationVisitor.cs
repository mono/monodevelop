// 
// DomIndentationVisitor.cs
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
	public class DomIndentationVisitor : DomVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		List<Change> changes = new List<Change> ();
		Indent curIndent = new Indent ();
		
		public int IndentLevel {
			get {
				return curIndent.Level;
			}
			set {
				curIndent.Level = value;
			}
		}
		
		public int CurrentSpaceIndents {
			get;
			set;
		}
		
		public List<Change> Changes {
			get { return this.changes; }
		}
		
		public bool AutoAcceptChanges {
			get;
			set;
		}
		
		public bool CorrectBlankLines {
			get;
			set;
		}
		
		public DomIndentationVisitor (CSharpFormattingPolicy policy, TextEditorData data)
		{
			this.policy = policy;
			this.data = data;
			this.curIndent.TabsToSpaces = this.data.Options.TabsToSpaces;
			this.curIndent.TabSize = this.data.Options.TabSize;
			AutoAcceptChanges = true;
			CorrectBlankLines = true;
		}
		
		public override object VisitCompilationUnit (MonoDevelop.CSharp.Dom.CompilationUnit unit,     object data)
		{
			base.VisitCompilationUnit (unit, data);
			if (AutoAcceptChanges)
				RefactoringService.AcceptChanges (null, null, changes);
			return null;
		}
		
		public void EnsureBlankLinesAfter (DomNode node, int blankLines)
		{
			if (!CorrectBlankLines)
				return;
			var loc = node.EndLocation;
			int line = loc.Line;
			LineSegment lineSegment;
			do {
				line++;
				lineSegment = data.Document.GetLine (line);
			} while (lineSegment != null && lineSegment.EditableLength == lineSegment.GetIndentation (data.Document).Length);
			var startLine = data.Document.GetLine (loc.Line);
			int start = startLine.EndOffset;
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < blankLines; i++)
				sb.Append (data.EolMarker);
			int removedChars = lineSegment != null ? lineSegment.Offset - startLine.EndOffset : 0;
			AddChange (start, removedChars, sb.ToString ());
		}
		
		public void EnsureBlankLinesBefore (DomNode node, int blankLines)
		{
			if (!CorrectBlankLines)
				return;
			var loc = node.StartLocation;
			int line = loc.Line;
			LineSegment lineSegment;
			do {
				line--;
				lineSegment = data.GetLine (line);
				if (lineSegment == null)
					break;
			} while (lineSegment.EditableLength == lineSegment.GetIndentation (data.Document).Length);
			int end = data.GetLine (loc.Line).Offset;
			int start = lineSegment != null ? lineSegment.EndOffset : 0;
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < blankLines; i++)
				sb.Append (data.EolMarker);
			AddChange (start, end - start, sb.ToString ());
		}

		public override object VisitUsingDeclaration (UsingDeclaration usingDeclaration, object data)
		{
			if (!(usingDeclaration.NextSibling is UsingDeclaration || usingDeclaration.NextSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesAfter (usingDeclaration, policy.BlankLinesAfterUsings);
			if (!(usingDeclaration.PrevSibling is UsingDeclaration || usingDeclaration.PrevSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesBefore (usingDeclaration, policy.BlankLinesBeforeUsings);

			return null;
		}
		
		public override object VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration, object data)
		{
			if (!(usingDeclaration.NextSibling is UsingDeclaration || usingDeclaration.NextSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesAfter (usingDeclaration, policy.BlankLinesAfterUsings);
			if (!(usingDeclaration.PrevSibling is UsingDeclaration || usingDeclaration.PrevSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesBefore (usingDeclaration, policy.BlankLinesBeforeUsings);
			return null;
		}
		
		public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
		{
			var firstNsMember = namespaceDeclaration.GetChildByRole (NamespaceDeclaration.Roles.Member);
			if (firstNsMember != null)
				EnsureBlankLinesBefore (firstNsMember, policy.BlankLinesBeforeFirstDeclaration);
			FixIndentationForceNewLine (namespaceDeclaration.StartLocation);
			EnforceBraceStyle (policy.NamespaceBraceStyle, namespaceDeclaration.LBrace, namespaceDeclaration.RBrace);
			if (policy.IndentNamespaceBody)
				IndentLevel++;
			object result = base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			if (policy.IndentNamespaceBody)
				IndentLevel--;
			FixIndentation (namespaceDeclaration.EndLocation);
			return result;
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			FixIndentationForceNewLine (typeDeclaration.StartLocation);
			BraceStyle braceStyle;
			bool indentBody = false;
			switch (typeDeclaration.ClassType) {
			case ClassType.Class:
				braceStyle = policy.ClassBraceStyle;
				indentBody = policy.IndentClassBody;
				break;
			case ClassType.Struct:
				braceStyle = policy.StructBraceStyle;
				indentBody = policy.IndentStructBody;
				break;
			case ClassType.Interface:
				braceStyle = policy.InterfaceBraceStyle;
				indentBody = policy.IndentInterfaceBody;
				break;
			case ClassType.Enum:
				braceStyle = policy.EnumBraceStyle;
				indentBody = policy.IndentEnumBody;
				break;
			default:
				throw new InvalidOperationException ("unsupported class type : " + typeDeclaration.ClassType);
			}
			EnforceBraceStyle (braceStyle, typeDeclaration.LBrace, typeDeclaration.RBrace);
			
			if (indentBody)
				IndentLevel++;
			object result = base.VisitTypeDeclaration (typeDeclaration, data);
			if (indentBody)
				IndentLevel--;
			
			if (typeDeclaration.NextSibling is TypeDeclaration || typeDeclaration.NextSibling is DelegateDeclaration)
				EnsureBlankLinesAfter (typeDeclaration, policy.BlankLinesBetweenTypes);
			return result;
		}
		
		bool IsSimpleAccessor (Accessor accessor)
		{
			if (accessor.IsNull || accessor.Body.IsNull || accessor.Body.FirstChild == null)
				return true;
			if (accessor.Body.Children.Count () != 1)
				return false;
			return !(accessor.Body.FirstChild is BlockStatement);
			
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			FixIndentationForceNewLine (propertyDeclaration.StartLocation);
			bool oneLine = false;
			switch (policy.PropertyFormatting) {
			case PropertyFormatting.AllowOneLine:
				bool isSimple = IsSimpleAccessor (propertyDeclaration.GetAccessor) && IsSimpleAccessor (propertyDeclaration.SetAccessor);
				if (!isSimple || propertyDeclaration.LBrace.StartLocation.Line != propertyDeclaration.RBrace.StartLocation.Line) {
					EnforceBraceStyle (policy.PropertyBraceStyle, propertyDeclaration.LBrace, propertyDeclaration.RBrace);
				} else {
					oneLine = true;
				}
				break;
			case PropertyFormatting.ForceNewLine:
				EnforceBraceStyle (policy.PropertyBraceStyle, propertyDeclaration.LBrace, propertyDeclaration.RBrace);
				break;
			case PropertyFormatting.ForceOneLine:
				isSimple = IsSimpleAccessor (propertyDeclaration.GetAccessor) && IsSimpleAccessor (propertyDeclaration.SetAccessor);
				if (isSimple) {
					int offset = this.data.Document.LocationToOffset (propertyDeclaration.LBrace.StartLocation.Line, propertyDeclaration.LBrace.StartLocation.Column);
					
					int start = SearchWhitespaceStart (offset);
					int end = SearchWhitespaceEnd (offset);
					AddChange (start, offset - start, " ");
					AddChange (offset + 1, end - offset - 2, " ");
					
					offset = this.data.Document.LocationToOffset (propertyDeclaration.RBrace.StartLocation.Line, propertyDeclaration.RBrace.StartLocation.Column);
					start = SearchWhitespaceStart (offset);
					AddChange (start, offset - start, " ");
					oneLine = true;

				} else {
					EnforceBraceStyle (policy.PropertyBraceStyle, propertyDeclaration.LBrace, propertyDeclaration.RBrace);
				}
				break;
			}
			if (policy.IndentPropertyBody)
				IndentLevel++;
			
			if (!propertyDeclaration.GetAccessor.IsNull) {
				if (!oneLine) {
					if (!IsLineIsEmptyUpToEol (propertyDeclaration.GetAccessor.StartLocation)) {
						int offset = this.data.Document.LocationToOffset (propertyDeclaration.GetAccessor.StartLocation.Line, propertyDeclaration.GetAccessor.StartLocation.Column);
						int start = SearchWhitespaceStart (offset);
						string indentString = this.curIndent.IndentString;
						AddChange (start, offset - start, this.data.EolMarker + indentString);
					} else {
						FixIndentation (propertyDeclaration.GetAccessor.StartLocation);
					}
				} else {
					if (!propertyDeclaration.SetAccessor.IsNull && propertyDeclaration.SetAccessor.StartLocation < propertyDeclaration.GetAccessor.StartLocation) {
						int offset = this.data.Document.LocationToOffset (propertyDeclaration.GetAccessor.StartLocation.Line, propertyDeclaration.GetAccessor.StartLocation.Column);
						int start = SearchWhitespaceStart (offset);
						AddChange (start, offset - start, " ");
					}
				}
				if (!propertyDeclaration.GetAccessor.Body.IsNull) {
					if (!policy.AllowPropertyGetBlockInline || propertyDeclaration.GetAccessor.Body.LBrace.StartLocation.Line != propertyDeclaration.GetAccessor.Body.RBrace.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertyGetBraceStyle, propertyDeclaration.GetAccessor.Body.LBrace, propertyDeclaration.GetAccessor.Body.RBrace);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (propertyDeclaration.GetAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (!propertyDeclaration.SetAccessor.IsNull) {
				if (!oneLine) {
					if (!IsLineIsEmptyUpToEol (propertyDeclaration.SetAccessor.StartLocation)) {
						int offset = this.data.Document.LocationToOffset (propertyDeclaration.SetAccessor.StartLocation.Line, propertyDeclaration.SetAccessor.StartLocation.Column);
						int start = SearchWhitespaceStart (offset);
						string indentString = this.curIndent.IndentString;
						AddChange (start, offset - start, this.data.EolMarker + indentString);
					} else {
						FixIndentation (propertyDeclaration.SetAccessor.StartLocation);
					}
				} else {
					if (!propertyDeclaration.GetAccessor.IsNull && propertyDeclaration.SetAccessor.StartLocation > propertyDeclaration.GetAccessor.StartLocation) {
						int offset = this.data.Document.LocationToOffset (propertyDeclaration.SetAccessor.StartLocation.Line, propertyDeclaration.SetAccessor.StartLocation.Column);
						int start = SearchWhitespaceStart (offset);
						AddChange (start, offset - start, " ");
					}
				}
				if (!propertyDeclaration.SetAccessor.Body.IsNull) {
					if (!policy.AllowPropertySetBlockInline || propertyDeclaration.SetAccessor.Body.LBrace.StartLocation.Line != propertyDeclaration.SetAccessor.Body.RBrace.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertySetBraceStyle, propertyDeclaration.SetAccessor.Body.LBrace, propertyDeclaration.SetAccessor.Body.RBrace);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (propertyDeclaration.SetAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (policy.IndentPropertyBody)
				IndentLevel--;
			if (IsMember (propertyDeclaration.NextSibling))
				EnsureBlankLinesAfter (propertyDeclaration, policy.BlankLinesBetweenMembers);
			return null;
		}
		
		public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			FixIndentationForceNewLine (indexerDeclaration.StartLocation);
			EnforceBraceStyle (policy.PropertyBraceStyle, indexerDeclaration.LBrace, indexerDeclaration.RBrace);
			if (policy.IndentPropertyBody)
				IndentLevel++;
			
			if (!indexerDeclaration.GetAccessor.IsNull) {
				FixIndentation (indexerDeclaration.GetAccessor.StartLocation);
				if (!indexerDeclaration.GetAccessor.Body.IsNull) {
					if (!policy.AllowPropertyGetBlockInline || indexerDeclaration.GetAccessor.Body.LBrace.StartLocation.Line != indexerDeclaration.GetAccessor.Body.RBrace.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertyGetBraceStyle, indexerDeclaration.GetAccessor.Body.LBrace, indexerDeclaration.GetAccessor.Body.RBrace);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (indexerDeclaration.GetAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (!indexerDeclaration.SetAccessor.IsNull) {
				FixIndentation (indexerDeclaration.SetAccessor.StartLocation);
				if (!indexerDeclaration.SetAccessor.Body.IsNull) {
					if (!policy.AllowPropertySetBlockInline || indexerDeclaration.SetAccessor.Body.LBrace.StartLocation.Line != indexerDeclaration.SetAccessor.Body.RBrace.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertySetBraceStyle, indexerDeclaration.SetAccessor.Body.LBrace, indexerDeclaration.SetAccessor.Body.RBrace);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (indexerDeclaration.SetAccessor.Body, policy.IndentBlocks, data);
				}
			}
			if (policy.IndentPropertyBody)
				IndentLevel--;
			if (IsMember (indexerDeclaration.NextSibling))
				EnsureBlankLinesAfter (indexerDeclaration, policy.BlankLinesBetweenMembers);
			return null;
		}
		
		static bool IsSimpleEvent (DomNode node)
		{
			var evt = (EventDeclaration)node;
			return evt.AddAccessor.IsNull;
		}
		
		public override object VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			FixIndentationForceNewLine (eventDeclaration.StartLocation);
			EnforceBraceStyle (policy.EventBraceStyle, eventDeclaration.LBrace, eventDeclaration.RBrace);
			if (policy.IndentEventBody)
				IndentLevel++;
			
			if (!eventDeclaration.AddAccessor.IsNull) {
				FixIndentation (eventDeclaration.AddAccessor.StartLocation);
				if (!eventDeclaration.AddAccessor.Body.IsNull) {
					if (!policy.AllowEventAddBlockInline || eventDeclaration.AddAccessor.Body.LBrace.StartLocation.Line != eventDeclaration.AddAccessor.Body.RBrace.StartLocation.Line) {
						EnforceBraceStyle (policy.EventAddBraceStyle, eventDeclaration.AddAccessor.Body.LBrace, eventDeclaration.AddAccessor.Body.RBrace);
					} else {
						nextStatementIndent = " ";
					}
					
					VisitBlockWithoutFixIndentation (eventDeclaration.AddAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (!eventDeclaration.RemoveAccessor.IsNull) {
				FixIndentation (eventDeclaration.RemoveAccessor.StartLocation);
				if (!eventDeclaration.RemoveAccessor.Body.IsNull) {
					if (!policy.AllowEventRemoveBlockInline || eventDeclaration.RemoveAccessor.Body.LBrace.StartLocation.Line != eventDeclaration.RemoveAccessor.Body.RBrace.StartLocation.Line) {
						EnforceBraceStyle (policy.EventRemoveBraceStyle, eventDeclaration.RemoveAccessor.Body.LBrace, eventDeclaration.RemoveAccessor.Body.RBrace);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (eventDeclaration.RemoveAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (policy.IndentEventBody)
				IndentLevel--;
			
			if (eventDeclaration.NextSibling is EventDeclaration && IsSimpleEvent (eventDeclaration) && IsSimpleEvent (eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (eventDeclaration, policy.BlankLinesBetweenEventFields);
			} else if (IsMember (eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (eventDeclaration, policy.BlankLinesBetweenMembers);
			}
			return null;
		}
		
		
		public override object VisitAccessorDeclaration (Accessor accessorDeclaration, object data)
		{
			FixIndentationForceNewLine (accessorDeclaration.StartLocation);
			object result = base.VisitAccessorDeclaration (accessorDeclaration, data);
			return result;
		}
		
		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			FixIndentationForceNewLine (fieldDeclaration.StartLocation);
			
			if (fieldDeclaration.NextSibling is FieldDeclaration) {
				EnsureBlankLinesAfter (fieldDeclaration, policy.BlankLinesBetweenFields);
			} else if (IsMember (fieldDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (fieldDeclaration, policy.BlankLinesBetweenMembers);
			}
			
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			FixIndentationForceNewLine (enumMemberDeclaration.StartLocation);
			return base.VisitEnumMemberDeclaration (enumMemberDeclaration, data);
		}
		
		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			FixIndentation (delegateDeclaration.StartLocation);
			if (delegateDeclaration.NextSibling is TypeDeclaration || delegateDeclaration.NextSibling is DelegateDeclaration) {
				EnsureBlankLinesAfter (delegateDeclaration, policy.BlankLinesBetweenTypes);
			} else if (IsMember (delegateDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (delegateDeclaration, policy.BlankLinesBetweenMembers);
			}

			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}
		
		static bool IsMember (DomNode nextSibling)
		{
			return nextSibling != null && nextSibling.Role == DomNode.Roles.Member;
		}

		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			FixIndentationForceNewLine (methodDeclaration.StartLocation);
			if (!methodDeclaration.Body.IsNull) {
				EnforceBraceStyle (policy.MethodBraceStyle, methodDeclaration.Body.LBrace, methodDeclaration.Body.RBrace);
				if (policy.IndentMethodBody)
					IndentLevel++;
				base.VisitBlockStatement (methodDeclaration.Body, data);
				if (policy.IndentMethodBody)
					IndentLevel--;
			}
			if (IsMember (methodDeclaration.NextSibling))
				EnsureBlankLinesAfter (methodDeclaration, policy.BlankLinesBetweenMembers);

			return null;
		}
		
		public override object VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
		{
			FixIndentationForceNewLine (operatorDeclaration.StartLocation);
			if (!operatorDeclaration.Body.IsNull) {
				EnforceBraceStyle (policy.MethodBraceStyle, operatorDeclaration.Body.LBrace, operatorDeclaration.Body.RBrace);
				if (policy.IndentMethodBody)
					IndentLevel++;
				base.VisitBlockStatement (operatorDeclaration.Body, data);
				if (policy.IndentMethodBody)
					IndentLevel--;
			}
			if (IsMember (operatorDeclaration.NextSibling))
				EnsureBlankLinesAfter (operatorDeclaration, policy.BlankLinesBetweenMembers);
			
			return null;
		}
		
		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			FixIndentationForceNewLine (constructorDeclaration.StartLocation);
			if (!constructorDeclaration.Body.IsNull)
				EnforceBraceStyle (policy.ConstructorBraceStyle, constructorDeclaration.Body.LBrace, constructorDeclaration.Body.RBrace);
			object result = base.VisitConstructorDeclaration (constructorDeclaration, data);
			if (IsMember (constructorDeclaration.NextSibling))
				EnsureBlankLinesAfter (constructorDeclaration, policy.BlankLinesBetweenMembers);
			return result;
		}
		
		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			FixIndentationForceNewLine (destructorDeclaration.StartLocation);
			if (!destructorDeclaration.Body.IsNull)
				EnforceBraceStyle (policy.DestructorBraceStyle, destructorDeclaration.Body.LBrace, destructorDeclaration.Body.RBrace);
			object result = base.VisitDestructorDeclaration (destructorDeclaration, data);
			if (IsMember (destructorDeclaration.NextSibling))
				EnsureBlankLinesAfter (destructorDeclaration, policy.BlankLinesBetweenMembers);
			return result;
		}
		
		#region Statements
		public override object VisitExpressionStatement (ExpressionStatement expressionStatement, object data)
		{
			FixStatementIndentation (expressionStatement.StartLocation);
			return null;
		}
		
		object VisitBlockWithoutFixIndentation (BlockStatement blockStatement, bool indent, object data)
		{
			if (indent)
				IndentLevel++;
			object result = base.VisitBlockStatement (blockStatement, data);
			if (indent)
				IndentLevel--;
			return result;
		}
		
		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			FixIndentation (blockStatement.StartLocation);
			object result = VisitBlockWithoutFixIndentation (blockStatement, policy.IndentBlocks, data);
			FixIndentation (blockStatement.EndLocation, -1);
			return result;
		}
		
		public override object VisitComment (MonoDevelop.CSharp.Dom.Comment comment, object data)
		{
			System.Console.WriteLine (comment.StartLocation);
			System.Console.WriteLine (comment.StartsLine);
			if (comment.StartsLine)
				FixIndentation (comment.StartLocation);
			return null;
		}
		
		public override object VisitBreakStatement (BreakStatement breakStatement, object data)
		{
			FixStatementIndentation (breakStatement.StartLocation);
			return null;
		}
		
		public override object VisitCheckedStatement (CheckedStatement checkedStatement, object data)
		{
			FixStatementIndentation (checkedStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, checkedStatement.Block);
		}
		
		public override object VisitContinueStatement (ContinueStatement continueStatement, object data)
		{
			FixStatementIndentation (continueStatement.StartLocation);
			return null;
		}
		
		public override object VisitEmptyStatement (EmptyStatement emptyStatement, object data)
		{
			FixStatementIndentation (emptyStatement.StartLocation);
			return null;
		}
		
		public override object VisitFixedStatement (FixedStatement fixedStatement, object data)
		{
			FixStatementIndentation (fixedStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, fixedStatement.EmbeddedStatement);
		}
		
		public override object VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			FixStatementIndentation (foreachStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.ForEachBraceForcement, foreachStatement.EmbeddedStatement);
		}

		object FixEmbeddedStatment (MonoDevelop.CSharp.Formatting.BraceStyle braceStyle, MonoDevelop.CSharp.Formatting.BraceForcement braceForcement, DomNode node)
		{
			return FixEmbeddedStatment (braceStyle, braceForcement, null, false, node);
		}
		
		object FixEmbeddedStatment (MonoDevelop.CSharp.Formatting.BraceStyle braceStyle, MonoDevelop.CSharp.Formatting.BraceForcement braceForcement, CSharpTokenNode token, bool allowInLine, DomNode node)
		{
			if (node == null)
				return null;
			bool isBlock = node is BlockStatement;
			switch (braceForcement) {
			case BraceForcement.DoNotChange:
				//nothing
				break;
			case BraceForcement.AddBraces:
				if (!isBlock) {
					int offset = data.Document.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
					int start = SearchWhitespaceStart (offset);
					string startBrace = "";
					switch (braceStyle) {
					case BraceStyle.EndOfLineWithoutSpace:
						startBrace = "{";
						break;
					case BraceStyle.EndOfLine:
						startBrace = " {";
						break;
					case BraceStyle.NextLine:
						startBrace = data.EolMarker + curIndent.IndentString + "{";
						break;
					case BraceStyle.NextLineShifted2:
					case BraceStyle.NextLineShifted:
						startBrace = data.EolMarker + curIndent.IndentString + curIndent.SingleIndent + "{";
						break;
					}
					if (IsLineIsEmptyUpToEol (data.Document.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column)))
						startBrace += data.EolMarker;
					AddChange (start, offset - start, startBrace);
				}
				break;
			case BraceForcement.RemoveBraces:
				if (isBlock) {
					BlockStatement block = node as BlockStatement;
					if (block.Statements.Count () == 1) {
						int offset1 = data.Document.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
						int start = SearchWhitespaceStart (offset1);
						
						int offset2 = data.Document.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
						int end = SearchWhitespaceStart (offset2 - 1);
						
						AddChange (start, offset1 - start + 1, null);
						AddChange (end + 1, offset2 - end, null);
						node = block.FirstChild;
						isBlock = false;
					}
				}
				break;
			}
			int originalLevel = curIndent.Level;
			if (isBlock) {
				BlockStatement block = node as BlockStatement;
				if (allowInLine && block.StartLocation.Line == block.EndLocation.Line && block.Statements.Count () <= 1) {
					if (block.Statements.Count () == 1)
						nextStatementIndent = " ";
				} else {
					EnforceBraceStyle (braceStyle, block.LBrace, block.RBrace);
				}
				if (braceStyle == BraceStyle.NextLineShifted2)
					curIndent.Level++;
			} else {
				if (allowInLine && token.StartLocation.Line == node.EndLocation.Line) {
					nextStatementIndent = " ";
				}
			}
			if (!(policy.AlignEmbeddedIfStatements && node is IfElseStatement && node.Parent is IfElseStatement || 
				policy.AlignEmbeddedUsingStatements && node is UsingStatement && node.Parent is UsingStatement)) 
				curIndent.Level++;
			object result = isBlock ? base.VisitBlockStatement ((BlockStatement)node, null) : node.AcceptVisitor (this, null);
			curIndent.Level = originalLevel;
			switch (braceForcement) {
			case BraceForcement.DoNotChange:
				break;
			case BraceForcement.AddBraces:
				if (!isBlock) {
					int offset = data.Document.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column) + 1;
					string startBrace = "";
					switch (braceStyle) {
					case BraceStyle.DoNotChange:
						startBrace = null;
						break;
					case BraceStyle.EndOfLineWithoutSpace:
						startBrace = data.EolMarker + curIndent.IndentString + "}";
						break;
					case BraceStyle.EndOfLine:
						startBrace = data.EolMarker + curIndent.IndentString + "}";
						break;
					case BraceStyle.NextLine:
						startBrace = data.EolMarker + curIndent.IndentString + "}";
						break;
					case BraceStyle.NextLineShifted2:
					case BraceStyle.NextLineShifted:
						startBrace = data.EolMarker + curIndent.IndentString + curIndent.SingleIndent + "}";
						break;
					}
					if (startBrace != null)
						AddChange (offset, 0, startBrace);
				}
				break;
			}
			return result;
		}
		
		void EnforceBraceStyle (MonoDevelop.CSharp.Formatting.BraceStyle braceStyle, DomNode lbrace, DomNode rbrace)
		{
			if (lbrace.IsNull || rbrace.IsNull)
				return;
			
//			LineSegment lbraceLineSegment = data.Document.GetLine (lbrace.StartLocation.Line);
			int lbraceOffset = data.Document.LocationToOffset (lbrace.StartLocation.Line, lbrace.StartLocation.Column);
			
//			LineSegment rbraceLineSegment = data.Document.GetLine (rbrace.StartLocation.Line);
			int rbraceOffset = data.Document.LocationToOffset (rbrace.StartLocation.Line, rbrace.StartLocation.Column);
			int whitespaceStart = SearchWhitespaceStart (lbraceOffset);
			int whitespaceEnd = SearchWhitespaceLineStart (rbraceOffset);
			string startIndent = "";
			string endIndent = "";
			switch (braceStyle) {
			case BraceStyle.DoNotChange:
				startIndent = endIndent = null;
				break;
			case BraceStyle.EndOfLineWithoutSpace:
				startIndent = "";
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString : data.EolMarker + curIndent.IndentString;
				break;
			case BraceStyle.EndOfLine:
				startIndent = " ";
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString : data.EolMarker + curIndent.IndentString;
				break;
			case BraceStyle.NextLine:
				startIndent = data.EolMarker + curIndent.IndentString;
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString : data.EolMarker + curIndent.IndentString;
				break;
			case BraceStyle.NextLineShifted2:
			case BraceStyle.NextLineShifted:
				startIndent = data.EolMarker + curIndent.IndentString + curIndent.SingleIndent;
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString + curIndent.SingleIndent : data.EolMarker + curIndent.IndentString + curIndent.SingleIndent;
				break;
			}
			
			if (lbraceOffset > 0 && startIndent != null)
				AddChange (whitespaceStart, lbraceOffset - whitespaceStart, startIndent);
			if (rbraceOffset > 0 && endIndent != null)
				AddChange (whitespaceEnd, rbraceOffset - whitespaceEnd, endIndent);
		}
		
		void AddChange (int offset, int removedChars, string insertedText)
		{
			if (changes.Cast<DomSpacingVisitor.MyTextReplaceChange> ().Any (c => c.Offset == offset && c.RemovedChars == removedChars 
				&& c.InsertedText == insertedText))
				return;
			string currentText = data.Document.GetTextAt (offset, removedChars);
			if (currentText == insertedText)
				return;
			if (currentText.Any (c => !(char.IsWhiteSpace (c) || c == '\r' || c == '\t' || c == '{' || c == '}')))
				throw new InvalidOperationException ("Tried to remove non ws chars: '" + currentText + "'");
			foreach (DomSpacingVisitor.MyTextReplaceChange change in changes) {
				if (change.Offset == offset) {
					if (removedChars > 0 && insertedText == change.InsertedText) {
						change.RemovedChars = removedChars;
//						change.InsertedText = insertedText;
						return;
					}
					if (!string.IsNullOrEmpty (change.InsertedText)) {
						change.InsertedText += insertedText;
					} else {
						change.InsertedText = insertedText;
					}
					change.RemovedChars = System.Math.Max (removedChars, change.RemovedChars);
					return;
				}
			}
//			Console.WriteLine ("offset={0}, removedChars={1}, insertedText={2}", offset, removedChars, insertedText.Replace("\n", "\\n").Replace("\t", "\\t").Replace(" ", "."));
//			Console.WriteLine (Environment.StackTrace);
			changes.Add (new DomSpacingVisitor.MyTextReplaceChange (data, offset, removedChars, insertedText));
		}
		
		public bool IsLineIsEmptyUpToEol (DomLocation startLocation)
		{
			return IsLineIsEmptyUpToEol (data.Document.LocationToOffset (startLocation.Line, startLocation.Column) - 1);
		}

		bool IsLineIsEmptyUpToEol (int startOffset)
		{
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = data.Document.GetCharAt (offset);
				if (ch != ' ' && ch != '\t')
					return ch == '\n' || ch == '\r';
			}
			return true;
		}
		
		int SearchWhitespaceStart (int startOffset)
		{
			if (startOffset < 0)
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = data.Document.GetCharAt (offset);
				if (!Char.IsWhiteSpace (ch)) {
					return offset + 1;
				}
			}
			return 0;
		}
		
		int SearchWhitespaceEnd (int startOffset)
		{
			if (startOffset > data.Document.Length)
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			for (int offset = startOffset + 1; offset < data.Document.Length; offset++) {
				char ch = data.Document.GetCharAt (offset);
				if (!Char.IsWhiteSpace (ch)) {
					return offset + 1;
				}
			}
			return data.Document.Length - 1;
		}
		
		int SearchWhitespaceLineStart (int startOffset)
		{
			if (startOffset < 0)
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = data.Document.GetCharAt (offset);
				if (ch != ' ' && ch != '\t') {
					return offset + 1;
				}
			}
			return 0;
		}

		public override object VisitForStatement (ForStatement forStatement, object data)
		{
			FixStatementIndentation (forStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.ForBraceForcement, forStatement.EmbeddedStatement);
		}

		public override object VisitGotoStatement (GotoStatement gotoStatement, object data)
		{
			FixStatementIndentation (gotoStatement.StartLocation);
			return VisitChildren (gotoStatement, data);
		}
		
		public override object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
		{
			if (!(ifElseStatement.Parent is IfElseStatement && ((IfElseStatement)ifElseStatement.Parent).FalseEmbeddedStatement == ifElseStatement))
				FixStatementIndentation (ifElseStatement.StartLocation);
			
			if (!ifElseStatement.Condition.IsNull)
				ifElseStatement.Condition.AcceptVisitor (this, data);
			
			if (!ifElseStatement.TrueEmbeddedStatement.IsNull)
				FixEmbeddedStatment (policy.StatementBraceStyle, policy.IfElseBraceForcement, ifElseStatement.IfKeyword, policy.AllowIfBlockInline, ifElseStatement.TrueEmbeddedStatement);
			
			if (!ifElseStatement.FalseEmbeddedStatement.IsNull) {
				PlaceOnNewLine (policy.PlaceElseOnNewLine, ifElseStatement.ElseKeyword);
				if (ifElseStatement.FalseEmbeddedStatement is IfElseStatement) {
					PlaceOnNewLine (policy.PlaceElseIfOnNewLine, ((IfElseStatement)ifElseStatement.FalseEmbeddedStatement).IfKeyword);
				}
				FixEmbeddedStatment (policy.StatementBraceStyle, policy.IfElseBraceForcement, ifElseStatement.ElseKeyword, policy.AllowIfBlockInline, ifElseStatement.FalseEmbeddedStatement);
			}
			
			return null;
		}

		
		public override object VisitLabelStatement (LabelStatement labelStatement, object data)
		{
			// TODO
			return VisitChildren (labelStatement, data);
		}
		
		public override object VisitLockStatement (LockStatement lockStatement, object data)
		{
			FixStatementIndentation (lockStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, lockStatement.EmbeddedStatement);
		}
		
		public override object VisitReturnStatement (ReturnStatement returnStatement, object data)
		{
			FixStatementIndentation (returnStatement.StartLocation);
			return VisitChildren (returnStatement, data);
		}
		
		public override object VisitSwitchStatement (SwitchStatement switchStatement, object data)
		{
			FixStatementIndentation (switchStatement.StartLocation);
			EnforceBraceStyle (policy.StatementBraceStyle, switchStatement.LBrace, switchStatement.RBrace);
			object result = VisitChildren (switchStatement, data);
			return result;
		}
		
		public override object VisitSwitchSection (SwitchSection switchSection, object data)
		{
			if (policy.IndentSwitchBody)
				curIndent.Level++;
			
			foreach (CaseLabel label in switchSection.CaseLabels) {
				FixStatementIndentation (label.StartLocation);
			}
			if (policy.IndentCaseBody)
				curIndent.Level++;
			
			foreach (var stmt in switchSection.Statements) {
				stmt.AcceptVisitor (this, null);
			}
			if (policy.IndentCaseBody)
				curIndent.Level--;
				
			if (policy.IndentSwitchBody)
				curIndent.Level--;
			return null;
		}
		
		public override object VisitCaseLabel (CaseLabel caseLabel, object data)
		{
			// handled in switchsection
			return null;
		}
		
		public override object VisitThrowStatement (ThrowStatement throwStatement, object data)
		{
			FixStatementIndentation (throwStatement.StartLocation);
			return VisitChildren (throwStatement, data);
		}
		
		public override object VisitTryCatchStatement (TryCatchStatement tryCatchStatement, object data)
		{
			FixStatementIndentation (tryCatchStatement.StartLocation);
			
			if (!tryCatchStatement.TryBlock.IsNull)
				FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, tryCatchStatement.TryBlock);
			
			foreach (CatchClause clause in tryCatchStatement.CatchClauses) {
				PlaceOnNewLine (policy.PlaceCatchOnNewLine, clause.CatchKeyword);
				
				FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, clause.Block);
			}
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				PlaceOnNewLine (policy.PlaceFinallyOnNewLine, tryCatchStatement.FinallyKeyword);
				
				FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, tryCatchStatement.FinallyBlock);
			}
			
			return VisitChildren (tryCatchStatement, data);
		}
		
		public override object VisitCatchClause (CatchClause catchClause, object data)
		{
			// Handled in TryCatchStatement
			return null;
		}
		
		public override object VisitUncheckedStatement (UncheckedStatement uncheckedStatement, object data)
		{
			FixStatementIndentation (uncheckedStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, uncheckedStatement.Block);
		}
		
		public override object VisitUnsafeStatement (UnsafeStatement unsafeStatement, object data)
		{
			FixStatementIndentation (unsafeStatement.StartLocation);
			Console.WriteLine (unsafeStatement.Block);
			return FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, unsafeStatement.Block);
		}
		
		public override object VisitUsingStatement (UsingStatement usingStatement, object data)
		{
			FixStatementIndentation (usingStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.UsingBraceForcement, usingStatement.EmbeddedStatement);
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			if (!variableDeclarationStatement.Semicolon.IsNull)
				FixStatementIndentation (variableDeclarationStatement.StartLocation);
			return null;
		}
		
		public override object VisitWhileStatement (WhileStatement whileStatement, object data)
		{
			FixStatementIndentation (whileStatement.StartLocation);
			if (whileStatement.WhilePosition == WhilePosition.End) {
				PlaceOnNewLine (policy.PlaceWhileOnNewLine, whileStatement.WhileKeyword);
			}
				
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.WhileBraceForcement, whileStatement.EmbeddedStatement);
		}
		
		public override object VisitYieldStatement (YieldStatement yieldStatement, object data)
		{
			FixStatementIndentation (yieldStatement.StartLocation);
			return null;
		}
		
		#endregion
		
		void PlaceOnNewLine (bool newLine, DomNode keywordNode)
		{
			if (keywordNode == null)
				return;
			int offset = data.Document.LocationToOffset (keywordNode.StartLocation.Line, keywordNode.StartLocation.Column);
			
			int whitespaceStart = SearchWhitespaceStart (offset);
			string indentString = newLine ? data.EolMarker + this.curIndent.IndentString : " ";
			AddChange (whitespaceStart, offset - whitespaceStart, indentString);
		}
		
		string nextStatementIndent = null;
		void FixStatementIndentation (MonoDevelop.Projects.Dom.DomLocation location)
		{
			int offset = data.Document.LocationToOffset (location.Line, location.Column);
			if (offset <= 0) {
				Console.WriteLine ("possible wrong offset");
				Console.WriteLine (Environment.StackTrace);
				return;
			}
			bool isEmpty = IsLineIsEmptyUpToEol (offset);
			int lineStart = SearchWhitespaceLineStart (offset);
			string indentString = nextStatementIndent == null ? (isEmpty ? "" : data.EolMarker) + this.curIndent.IndentString : nextStatementIndent;
			nextStatementIndent = null;
			AddChange (lineStart, offset - lineStart, indentString);
		}
		
		void FixIndentation (MonoDevelop.Projects.Dom.DomLocation location)
		{
			FixIndentation (location, 0);
		}
		
		void FixIndentation (MonoDevelop.Projects.Dom.DomLocation location, int relOffset)
		{
			LineSegment lineSegment = data.Document.GetLine (location.Line);
			string lineIndent = lineSegment.GetIndentation (data.Document);
			string indentString = this.curIndent.IndentString;
			if (indentString != lineIndent && location.Column - 1 + relOffset == lineIndent.Length) {
				AddChange (lineSegment.Offset, lineIndent.Length, indentString);
			}
		}
		
		void FixIndentationForceNewLine (MonoDevelop.Projects.Dom.DomLocation location)
		{
			LineSegment lineSegment = data.Document.GetLine (location.Line);
			string lineIndent = lineSegment.GetIndentation (data.Document);
			string indentString = this.curIndent.IndentString;
			if (indentString != lineIndent && location.Column - 1 == lineIndent.Length) {
				AddChange (lineSegment.Offset, lineIndent.Length, indentString);
			} else {
				int offset = data.Document.LocationToOffset (location.Line, location.Column);
				int start = SearchWhitespaceLineStart (offset);
				if (start > 0) { 
					char ch = data.Document.GetCharAt (start - 1);
					if (ch == '\n') {
						start--;
						if (start > 1 && data.Document.GetCharAt (start - 1) == '\r')
							start--;
					} else if (ch == '\r') {
						start--;
					}
					AddChange (start, offset - start, data.EolMarker + indentString);
				}
			}
		}
	}
}

