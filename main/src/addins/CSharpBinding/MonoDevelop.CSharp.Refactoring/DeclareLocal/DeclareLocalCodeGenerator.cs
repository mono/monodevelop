// 
// DeclareLocalCodeGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Refactoring;
using System.IO;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp.Refactoring.DeclareLocal
{
	public class DeclareLocalCodeGenerator : RefactoringOperation
	{
		public override string AccelKey {
			get {
				var cmdInfo = IdeApp.CommandService.GetCommandInfo (RefactoryCommands.DeclareLocal);
				if (cmdInfo != null && cmdInfo.AccelKey != null)
					return cmdInfo.AccelKey.Replace ("dead_circumflex", "^");
				return null;
			}
		}
		
		public DeclareLocalCodeGenerator ()
		{
			Name = "Declare Local";
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Declare Local");
		}
		
		Expression selectedExpression;
		
		public override bool IsValid (RefactoringOptions options)
		{
			TextEditorData data = options.GetTextEditorData ();
			
			if (data == null || !data.IsSomethingSelected)
				return false;
			
			if (options.Document.ParsedDocument == null)
				return false;
			var unit = options.Document.ParsedDocument.GetAst<CompilationUnit> ();
			if (unit == null)
				return false;
			
			var startPoint = data.MainSelection.Anchor < data.MainSelection.Lead ? data.MainSelection.Anchor : data.MainSelection.Lead; 
			var endPoint = data.MainSelection.Anchor < data.MainSelection.Lead ? data.MainSelection.Lead : data.MainSelection.Anchor; 
			
			var startExpression = unit.GetNodeAt<Expression> (startPoint);
			if (startExpression == null)
				return false;
			
			selectedExpression = null;
			foreach (var expr in GetExpressionTree (startExpression)) {
				if (expr.EndLocation <= (TextLocation)endPoint)
					selectedExpression = expr;
			}
			
			return selectedExpression != null;
		}
		
		IEnumerable<Expression> GetExpressionTree (Expression start)
		{
			var node = start;
			while (node != null) {
				yield return node;
				node = node.Parent as Expression;
			}
		}
		
		List<AstNode> matches = new List<AstNode> ();
		bool replaceAll = false;
		
		class SearchNodeVisitior : DepthFirstAstVisitor
		{
			readonly AstNode searchForNode;
			public readonly List<AstNode> Matches = new List<AstNode> ();
			
			public SearchNodeVisitior (AstNode searchForNode)
			{
				this.searchForNode = searchForNode;
				Matches.Add (searchForNode);
			}
			
			protected override void VisitChildren (AstNode node)
			{
				if (node.StartLocation > searchForNode.StartLocation && node.IsMatch (searchForNode))
					Matches.Add (node);
				base.VisitChildren (node);
			}
		}
		
		void SearchMatchingExpressions (RefactoringOptions options)
		{
			var data = options.GetTextEditorData ();
			var unit = options.Document.ParsedDocument.GetAst<CompilationUnit> ();
			if (unit != null) {
				var node = unit.GetNodeAt<BlockStatement> (data.Caret.Location);
				if (node != null) {
					var visitor = new SearchNodeVisitior (selectedExpression);
					node.AcceptVisitor (visitor);
					matches = visitor.Matches;
				}
			}
			
			if (matches.Count > 1) {
				var result = MessageService.AskQuestion (string.Format (GettextCatalog.GetString ("Replace all {0} occurences ?"), matches.Count), AlertButton.Yes, AlertButton.No);
				replaceAll = result == AlertButton.Yes;
			}
		}
		
		public override void Run (RefactoringOptions options)
		{
			var data = options.GetTextEditorData ();
			
			SearchMatchingExpressions (options);
			
			base.Run (options);
			if (selectionEnd >= 0) {
				options.Document.Editor.Caret.Offset = selectionEnd;
				options.Document.Editor.SetSelection (selectionStart, selectionEnd);
			} else {
				Mono.TextEditor.TextEditor editor = data.Parent;
				TextLink link = new TextLink ("name");
				if (varName != null) {
					foreach (var offset in offsets) {
						link.AddLink (new TextSegment (offset - selectionStart, varName.Length));
					}
				}
				List<TextLink > links = new List<TextLink> ();
				links.Add (link);
				TextLinkEditMode tle = new TextLinkEditMode (editor, selectionStart, links);
				tle.SetCaretPosition = false;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = data.CurrentMode;
					tle.StartMode ();
					data.CurrentMode = tle;
				}
			}
		}

		static bool IsIdentifierPart (Mono.TextEditor.TextEditorData data, int offset)
		{
			if (offset < 0 || offset >= data.Document.Length)
				return false;
			char ch = data.Document.GetCharAt (offset);
			return char.IsLetterOrDigit (ch) || ch == '_' || ch == '.';
		}

		int selectionStart;
		int selectionEnd;
		List<int> offsets = new List<int> ();
		
		string varName;
		int varCount;
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			varCount = 0;
			selectionStart = selectionEnd = -1;
			
			List<Change > result = new List<Change> ();
			TextEditorData data = options.GetTextEditorData ();
			if (data == null)
				return result;
			
			DocumentLocation endPoint;
			if (data.IsSomethingSelected) {
				endPoint = data.MainSelection.Anchor < data.MainSelection.Lead ? data.MainSelection.Lead : data.MainSelection.Anchor; 
			} else {
				endPoint = data.Caret.Location;
			}
			ResolveResult resolveResult;
			LineSegment lineSegment;
			var unit = options.Document.ParsedDocument.GetAst<CompilationUnit> ();
			var resolver = options.CreateResolver (unit);
			var visitor = new VariableLookupVisitor (resolver, endPoint);
			
			var callingMember = options.Document.ParsedDocument.GetMember (options.Location);
			if (callingMember != null)
				visitor.MemberLocation = callingMember.Region.Begin;
			unit.AcceptVisitor (visitor, null);
			resolveResult = resolver.Resolve (selectedExpression);
			
			AstType returnType;
			if (resolveResult == null || resolveResult.Type.Kind == TypeKind.Unknown) {
				returnType = new SimpleType ("var");
				varName = "newVar";
			
			} else {
				returnType = options.CreateShortType (resolveResult.Type);
				varName = CreateVariableName (resolveResult.Type, visitor);
			}
			
			// insert local variable declaration
			var insert = new TextReplaceChange ();
			insert.FileName = options.Document.FileName;
			insert.Description = GettextCatalog.GetString ("Insert variable declaration");
			
			var varDecl = new VariableDeclarationStatement (returnType, varName, selectedExpression.Clone ());
			
			var node = unit.GetNodeAt (endPoint.Line, endPoint.Column);
			
			var containing = node.Parent;
			while (!(containing.Parent is BlockStatement)) {
				containing = containing.Parent;
			}
			
			if (containing is BlockStatement) {
				lineSegment = data.Document.GetLine (data.Caret.Line);
			} else {
				lineSegment = data.Document.GetLine (containing.StartLocation.Line);
			}
			insert.Offset = lineSegment.Offset;
			insert.InsertedText = options.GetWhitespaces (lineSegment.Offset) + options.OutputNode (varDecl);
			var insertOffset = insert.Offset + options.GetWhitespaces (lineSegment.Offset).Length + options.OutputNode (varDecl.Type).Length + " ".Length;
			offsets.Add (insertOffset);
			result.Add (insert);
			varCount++;

			// replace main selection
			TextReplaceChange replace = new TextReplaceChange ();
			replace.FileName = options.Document.FileName;
			int startOffset = options.Document.Editor.LocationToOffset (selectedExpression.StartLocation); 
			int endOffset   = options.Document.Editor.LocationToOffset (selectedExpression.EndLocation); 
			replace.Offset = startOffset;
			replace.RemovedChars = endOffset - startOffset;
			replace.InsertedText = varName;
			result.Add (replace);
			int delta = insert.InsertedText.Length - insert.RemovedChars;
			offsets.Add (replace.Offset + delta);
			delta += varName.Length - replace.RemovedChars;
			varCount++;
			selectionStart = insert.Offset;
			
			if (replaceAll) {
				matches.Sort ((x, y) => x.StartLocation.CompareTo (y.StartLocation));
				foreach (var match in matches) {
					if (match == selectedExpression)
						continue;
					replace = new TextReplaceChange ();
					replace.FileName = options.Document.FileName;
					int start = data.LocationToOffset (match.StartLocation);
					int end = data.LocationToOffset (match.EndLocation);
					
					replace.Offset = start;
					replace.RemovedChars = end - start;
					replace.InsertedText = varName;
					result.Add (replace);
					offsets.Add (start + delta);
					delta += varName.Length - replace.RemovedChars;
				}
			}
			return result;
		}
		
		static string[] GetPossibleName (IType returnType)
		{
			switch (returnType.ReflectionName) {
			case "System.Byte":
			case "System.SByte":
				return new [] { "b" };
				
			case "System.Int16":
			case "System.UInt16":
			case "System.Int32":
			case "System.UInt32":
			case "System.Int64":
			case "System.UInt64":
				return new [] { "i", "j", "k", "l" };
				
			case "System.Boolean":
				return new [] {"b"};
				
			case "System.DateTime":
				return new [] { "date", "d" };
				
			case "System.Char":
				return new [] {"ch", "c"};
			case "System.String":
				return new [] {"str", "s"};
				
			case "System.Exception":
				return new [] {"e"};
			case "System.Object":
				return new [] {"obj", "o"};
			case "System.Func":
				return new [] {"func", "f"};
			case "System.Action":
				return new [] {"action", "act"};
			}
			if (Char.IsLower (returnType.Name [0]))
				return new [] { "a" + Char.ToUpper (returnType.Name[0]) + returnType.Name.Substring (1) };
			
			return new [] { Char.ToLower (returnType.Name[0]) + returnType.Name.Substring (1) };
		}
		
		static string CreateVariableName (IType returnType, VariableLookupVisitor visitor)
		{
			string[] possibleNames = GetPossibleName (returnType);
			foreach (string name in possibleNames) {
				if (!VariableExists (visitor, name))
					return name;
			}
			foreach (string name in possibleNames) {
				for (int i = 1; i < 99; i++) {
					if (!VariableExists (visitor, name + i.ToString ()))
						return name + i.ToString ();
				}
			}
			return "a" + returnType.Name;
		}

		static bool VariableExists (VariableLookupVisitor visitor, string name)
		{
			foreach (var descriptor in visitor.Variables.Values) {
				if (descriptor.Name == name)
					return true;
			}
			return false;
		}
	}
	

	public class VariableDescriptor
	{
		public string Name {
			get;
			set;
		}
		
		public bool GetsChanged {
			get;
			set;
		}
		
		public bool InitialValueUsed {
			get;
			set;
		}
		
		public bool GetsAssigned {
			get;
			set;
		}
		
		public bool IsDefined {
			get;
			set;
		}
		
		public IType ReturnType {
			get;
			set;
		}
		
		public DocumentLocation Location {
			get;
			set;
		}
		
		public VariableDescriptor (string name)
		{
			this.Name = name;
			this.GetsChanged = this.IsDefined = this.InitialValueUsed = false;
		}
		
		public override string ToString ()
		{
			return string.Format("[VariableDescriptor: Name={0}, GetsChanged={1}, InitialValueUsed={2}, GetsAssigned={3}, IsDefined={4}, ReturnType={5}, Location={6}]", Name, GetsChanged, InitialValueUsed, GetsAssigned, IsDefined, ReturnType, Location);
		}
	}
	
	public class VariableLookupVisitor : DepthFirstAstVisitor<object, object>
	{
		List<KeyValuePair <string, IType>> unknownVariables = new List<KeyValuePair <string, IType>> ();
		Dictionary<string, VariableDescriptor> variables = new Dictionary<string, VariableDescriptor> ();
		
		public bool ReferencesMember {
			get;
			set;
		}
		
		public List<KeyValuePair <string, IType>> UnknownVariables {
			get {
				return unknownVariables;
			}
		}

		public Dictionary<string, VariableDescriptor> Variables {
			get {
				return variables;
			}
		}
		
		public List<VariableDescriptor> VariableList {
			get {
				return new List<VariableDescriptor> (variables.Values);
			}
		}
		
		public TextLocation MemberLocation {
			get;
			set;
		}
		
		CSharpAstResolver resolver;
		TextLocation position;
		public DomRegion CutRegion {
			get;
			set;
		}
		
		public VariableLookupVisitor (CSharpAstResolver resolver, TextLocation position)
		{
			this.resolver = resolver;
			this.position = position;
			this.MemberLocation = TextLocation.Empty;
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement localVariableDeclaration, object data)
		{
			if (!CutRegion.IsInside (localVariableDeclaration.StartLocation)) {
				foreach (var varDecl in localVariableDeclaration.Variables) {
					variables[varDecl.Name] = new VariableDescriptor (varDecl.Name) {
						IsDefined = true, 
						ReturnType = resolver.Resolve (localVariableDeclaration.Type).Type,
						Location = new DocumentLocation (MemberLocation.Line + localVariableDeclaration.StartLocation.Line, localVariableDeclaration.StartLocation.Column)
					};
				}
			}
			return base.VisitVariableDeclarationStatement(localVariableDeclaration, data);
		}
		
		public override object VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
		{
			if (!variables.ContainsKey (identifierExpression.Identifier)) {
				var result = resolver.Resolve (identifierExpression);
				
				var mrr = result as MemberResolveResult;
				ReferencesMember |= mrr != null && mrr.Member != null && !mrr.Member.IsStatic;
				
				if (!(result is LocalResolveResult))
					return base.VisitIdentifierExpression (identifierExpression, data);
				
				// result.ResolvedType == null may be true for namespace names or undeclared variables
				if (!(result is TypeResolveResult) && !variables.ContainsKey (identifierExpression.Identifier)) {
					variables [identifierExpression.Identifier] = new VariableDescriptor (identifierExpression.Identifier) {
						InitialValueUsed = !valueGetsChanged,
						Location = new DocumentLocation (MemberLocation.Line + identifierExpression.StartLocation.Line, identifierExpression.StartLocation.Column)

					};
					variables [identifierExpression.Identifier].ReturnType = result.Type;
				}
				if (result != null && !(result is TypeResolveResult) && result.Type != null && !(result is MethodGroupResolveResult) && !(result is NamespaceResolveResult) && !(result is MemberResolveResult))
					unknownVariables.Add (new KeyValuePair <string, IType> (identifierExpression.Identifier, result.Type));
			}
			return base.VisitIdentifierExpression (identifierExpression, data);
		}
		
		bool valueGetsChanged = false;
		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
		{
			assignmentExpression.Right.AcceptVisitor(this, data);
				
			valueGetsChanged = true;
			IdentifierExpression left = assignmentExpression.Left as IdentifierExpression;
			bool isInitialUse = left != null && !variables.ContainsKey (left.Identifier);
			assignmentExpression.Left.AcceptVisitor(this, data);
			valueGetsChanged = false;
			
			if (left != null && variables.ContainsKey (left.Identifier)) {
				variables[left.Identifier].GetsChanged = true;
				if (isInitialUse)
					variables[left.Identifier].GetsAssigned = true;
			}
			return null;
		}
		
		public override object VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Operator) {
			case UnaryOperatorType.Increment:
			case UnaryOperatorType.Decrement:
			case UnaryOperatorType.PostIncrement:
			case UnaryOperatorType.PostDecrement:
				valueGetsChanged = true;
				break;
			}
			object result = base.VisitUnaryOperatorExpression (unaryOperatorExpression, data);
			valueGetsChanged = false;
			switch (unaryOperatorExpression.Operator) {
			case UnaryOperatorType.Increment:
			case UnaryOperatorType.Decrement:
			case UnaryOperatorType.PostIncrement:
			case UnaryOperatorType.PostDecrement:
				IdentifierExpression left = unaryOperatorExpression.Expression as IdentifierExpression;
				if (left != null && variables.ContainsKey (left.Identifier))
					variables[left.Identifier].GetsChanged = true;
				break;
			}
			return result;
		}

		public override object VisitDirectionExpression (DirectionExpression directionExpression, object data)
		{
			valueGetsChanged = true;
			IdentifierExpression left = directionExpression.Expression as IdentifierExpression;
			bool isInitialUse = left != null && !variables.ContainsKey (left.Identifier);
			object result = base.VisitDirectionExpression (directionExpression, data);
			valueGetsChanged = false;
			if (left != null && variables.ContainsKey (left.Identifier)) {
				variables[left.Identifier].GetsChanged = true;
				if (isInitialUse && directionExpression.FieldDirection == FieldDirection.Out)
					variables[left.Identifier].GetsAssigned = true;
			}
			return result;
		}
		
		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && methodDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && propertyDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && eventDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			return base.VisitEventDeclaration (eventDeclaration, data);
		}
	}
}
