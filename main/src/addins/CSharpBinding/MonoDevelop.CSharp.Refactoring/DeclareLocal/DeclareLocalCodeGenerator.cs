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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Refactoring;
using System.IO;

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
		
		string expressionText;
		
		public override bool IsValid (RefactoringOptions options)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			TextEditorData data = options.GetTextEditorData ();
			
			if (resolver == null || provider == null || data == null || !data.IsSomethingSelected)
				return false;
			expressionText = data.SelectedText.Trim ();
			ExpressionResult expressionResult = new ExpressionResult (expressionText);
			if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
				expressionResult.Expression = "(" + expressionResult.Expression + ")";
			
			var endPoint = data.MainSelection.Anchor < data.MainSelection.Lead ? data.MainSelection.Lead : data.MainSelection.Anchor; 
			options.ResolveResult = resolver.Resolve (expressionResult, new DomLocation (endPoint.Line, endPoint.Column));
			
			if (options.ResolveResult == null)
				return false;
			
			if (options.ResolveResult.CallingMember == null || !options.ResolveResult.CallingMember.BodyRegion.Contains (endPoint.Line, endPoint.Column))
				return false;
		
			return options.ResolveResult.ResolvedType != null && !string.IsNullOrEmpty (options.ResolveResult.ResolvedType.FullName) && 
				(options.ResolveResult.ResolvedType.FullName != DomReturnType.Void.FullName || 
				(options.ResolveResult is MethodResolveResult && !((MethodResolveResult)options.ResolveResult).GetsInvoked));
		}
		
		List<AstNode> matches = new List<AstNode> ();
		bool replaceAll = false;

		void SearchMatchingExpressions (RefactoringOptions options)
		{
			var parser = new CSharpParser ();
			var astNode = parser.ParseExpression (new StringReader (expressionText));
			var data = options.GetTextEditorData ();
			var unit = parser.Parse (data);
			if (unit != null) {
				var node = unit.GetNodeAt (data.Caret.Line, data.Caret.Column);
				while (node != null && !(node is BlockStatement)) {
					node = node.Parent;
				}
				if (node != null) {
					var nodeStack = new Stack<AstNode> ();
					nodeStack.Push (node);
					var minLoc = new AstLocation (data.Caret.Line, data.Caret.Column);
					while (nodeStack.Count > 0) {
						var curNode = nodeStack.Pop ();
						
						if (curNode.StartLocation > minLoc && curNode.IsMatch (astNode)) {
							matches.Add (curNode);
						}
						foreach (var child in curNode.Children)
							nodeStack.Push (child);
					}
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
						link.AddLink (new Segment (offset - selectionStart, varName.Length));
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
		
		public IReturnType GetResolvedType (RefactoringOptions options, ResolveResult resolveResult)
		{
			var result = resolveResult.ResolvedType;
			if (result == null || string.IsNullOrEmpty (result.Name))
				result = DomReturnType.Object;
			if (resolveResult is MethodResolveResult) {
				var mrr = (MethodResolveResult)resolveResult;
				if (!mrr.GetsInvoked) {
					var method = options.SelectedItem as IMethod ?? mrr.MostLikelyMethod;
					
					DomReturnType type;
					if (method.ReturnType == null || method.ReturnType.FullName == DomReturnType.Void.FullName) {
						type = new DomReturnType ("System.Action");
					} else {
						type = new DomReturnType ("System.Func");
					}
						
					foreach (var param in method.Parameters)
						type.AddTypeParameter (param.ReturnType);
					
					if (method.ReturnType != null && method.ReturnType.FullName != DomReturnType.Void.FullName)
						type.AddTypeParameter (method.ReturnType);
					result = type;
				}
			}
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			varCount = 0;
			selectionStart = selectionEnd = -1;
			
			List<Change > result = new List<Change> ();
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			TextEditorData data = options.GetTextEditorData ();
			if (resolver == null || provider == null || data == null)
				return result;
			
			DocumentLocation endPoint;
			if (data.IsSomethingSelected) {
				endPoint = data.MainSelection.Anchor < data.MainSelection.Lead ? data.MainSelection.Lead : data.MainSelection.Anchor; 
			} else {
				endPoint = data.Caret.Location;
			}
			ResolveResult resolveResult;
			LineSegment lineSegment;
			var unit = provider.ParseFile (data.Document.Text);
			if (unit == null) {
				LoggingService.LogError ("Declare local error: parese file == null");
				return result;
			}
			var visitor = new VariableLookupVisitor (resolver, new DomLocation (endPoint.Line, endPoint.Column));
			if (options.ResolveResult == null) {
				LoggingService.LogError ("Declare local error: resolve result == null");
				return result;
			}
			IMember callingMember = options.ResolveResult.CallingMember;
			if (callingMember != null)
				visitor.MemberLocation = new AstLocation (callingMember.Location.Column, callingMember.Location.Line);
			unit.AcceptVisitor (visitor, null);
			
			ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
			if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
				expressionResult.Expression = "(" + expressionResult.Expression + ")";
			resolveResult = resolver.Resolve (expressionResult, new DomLocation (endPoint.Line, endPoint.Column));
			if (resolveResult == null)
				return result;
			IReturnType resolvedType = GetResolvedType (options, resolveResult); 
			
			AstType returnType;
			if (resolveResult.ResolvedType == null || string.IsNullOrEmpty (resolveResult.ResolvedType.Name)) {
				returnType = new SimpleType ("var");
			} else {
				returnType = options.ShortenTypeName (resolvedType).ConvertToTypeReference ();
			}
			
			varName = CreateVariableName (resolvedType, visitor);
			options.ParseMember (resolveResult.CallingMember);
			
			// insert local variable declaration
			TextReplaceChange insert = new TextReplaceChange ();
			insert.FileName = options.Document.FileName;
			insert.Description = GettextCatalog.GetString ("Insert variable declaration");
			
			var varDecl = new VariableDeclarationStatement (returnType, varName, provider.ParseExpression (data.SelectedText));
			
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
			insert.InsertedText = options.GetWhitespaces (lineSegment.Offset) + provider.OutputNode (options.Dom, varDecl);
			var insertOffset = insert.Offset + options.GetWhitespaces (lineSegment.Offset).Length + provider.OutputNode (options.Dom, varDecl.Type).Length + " ".Length;
			offsets.Add (insertOffset);
			result.Add (insert);
			varCount++;

			// replace main selection
			TextReplaceChange replace = new TextReplaceChange ();
			replace.FileName = options.Document.FileName;
			replace.Offset = data.SelectionRange.Offset;
			replace.RemovedChars = data.SelectionRange.Length;
			replace.InsertedText = varName;
			result.Add (replace);
			int delta = insert.InsertedText.Length - insert.RemovedChars;
			offsets.Add (replace.Offset + delta);
			Console.WriteLine (replace.Offset);
			delta += varName.Length - replace.RemovedChars;
			varCount++;
			selectionStart = insert.Offset;
			
			if (replaceAll) {
				matches.Sort ((x, y) => x.StartLocation.CompareTo (y.StartLocation));
				foreach (var match in matches) {
					replace = new TextReplaceChange ();
					replace.FileName = options.Document.FileName;
					int start = data.LocationToOffset (match.StartLocation.Line, match.StartLocation.Column);
					int end = data.LocationToOffset (match.EndLocation.Line, match.EndLocation.Column);
					
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

		static bool SearchSubExpression (string expression, string subexpression, int startOffset, out int offset, out int length)
		{
			length = -1;
			for (offset = startOffset; offset < expression.Length; offset++) {
				if (Char.IsWhiteSpace (expression[offset])) 
					continue;
				
				bool mismatch = false;
				int i = offset, j = 0;
				while (i < expression.Length && j < subexpression.Length) {
					if (Char.IsWhiteSpace (expression[i])) {
						i++;
						continue;
					}
					if (Char.IsWhiteSpace (subexpression[j])) {
						j++;
						continue;
					}
					if (expression[i] != subexpression[j]) {
						mismatch = true;
						break;
					}
					i++;
					j++;
				}
				if (!mismatch && j > 0) {
					length = j;
					return true;
				}
			}
			return false;
		}
		
		static string[] GetPossibleName (MonoDevelop.Projects.Dom.IReturnType returnType)
		{
			switch (returnType.FullName) {
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
		
		static string CreateVariableName (MonoDevelop.Projects.Dom.IReturnType returnType, VariableLookupVisitor visitor)
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
		
		public IReturnType ReturnType {
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
		List<KeyValuePair <string, IReturnType>> unknownVariables = new List<KeyValuePair <string, IReturnType>> ();
		Dictionary<string, VariableDescriptor> variables = new Dictionary<string, VariableDescriptor> ();
		
		public bool ReferencesMember {
			get;
			set;
		}
		
		public List<KeyValuePair <string, IReturnType>> UnknownVariables {
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
		
		public AstLocation MemberLocation {
			get;
			set;
		}
		
		IResolver resolver;
		DomLocation position;
		public DomRegion CutRegion {
			get;
			set;
		}
		
		public VariableLookupVisitor (IResolver resolver, DomLocation position)
		{
			this.resolver = resolver;
			this.position = position;
			this.MemberLocation = AstLocation.Empty;
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement localVariableDeclaration, object data)
		{
			if (!CutRegion.Contains (localVariableDeclaration.StartLocation.Line, localVariableDeclaration.StartLocation.Column)) {
				foreach (var varDecl in localVariableDeclaration.Variables) {
					variables[varDecl.Name] = new VariableDescriptor (varDecl.Name) {
						IsDefined = true, 
						ReturnType = MonoDevelop.Refactoring.HelperMethods.ConvertToReturnType  (localVariableDeclaration.Type),
						Location = new DocumentLocation (MemberLocation.Line + localVariableDeclaration.StartLocation.Line, localVariableDeclaration.StartLocation.Column)
					};
				}
			}
			return base.VisitVariableDeclarationStatement(localVariableDeclaration, data);
		}
		
		public override object VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
		{
			if (!variables.ContainsKey (identifierExpression.Identifier)) {

				ExpressionResult expressionResult = new ExpressionResult (identifierExpression.Identifier);

				ResolveResult result = resolver.Resolve (expressionResult, position);
				
				MemberResolveResult mrr = result as MemberResolveResult;
				ReferencesMember |= mrr != null && mrr.ResolvedMember != null && !mrr.ResolvedMember.IsStatic;
				
				if (!(result is LocalVariableResolveResult || result is ParameterResolveResult))
					return base.VisitIdentifierExpression (identifierExpression, data);
				
				// result.ResolvedType == null may be true for namespace names or undeclared variables
				if (!result.StaticResolve && !variables.ContainsKey (identifierExpression.Identifier)) {
					variables [identifierExpression.Identifier] = new VariableDescriptor (identifierExpression.Identifier) {
						InitialValueUsed = !valueGetsChanged,
						Location = new DocumentLocation (MemberLocation.Line + identifierExpression.StartLocation.Line, identifierExpression.StartLocation.Column)

					};
					variables [identifierExpression.Identifier].ReturnType = result.ResolvedType;
				}
				if (result != null && !result.StaticResolve && result.ResolvedType != null && !(result is MethodResolveResult) && !(result is NamespaceResolveResult) && !(result is MemberResolveResult))
					unknownVariables.Add (new KeyValuePair <string, IReturnType> (identifierExpression.Identifier, result.ResolvedType));
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
