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

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.Visitors;

namespace MonoDevelop.Refactoring.DeclareLocal
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
		
		public override bool IsValid (RefactoringOptions options)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return false;
			TextEditorData data = options.GetTextEditorData ();
			if (data == null)
				return false;
			if (data.IsSomethingSelected) {
				ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
				if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
					expressionResult.Expression = "(" + expressionResult.Expression + ")";
				var endPoint = data.MainSelection.Anchor < data.MainSelection.Lead ? data.MainSelection.Lead : data.MainSelection.Anchor; 
				options.ResolveResult = resolver.Resolve (expressionResult, new DomLocation (endPoint.Line, endPoint.Column));
				if (options.ResolveResult == null)
					return false;
				if (options.ResolveResult.CallingMember == null || !options.ResolveResult.CallingMember.BodyRegion.Contains (endPoint.Line, endPoint.Column))
					return false;
				return true;
			}
			LineSegment lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);
			Expression expression = provider.ParseExpression (line);
			BlockStatement block = provider.ParseText (line) as BlockStatement;
			if (expression == null || (block != null && block.Children [0] is LocalVariableDeclaration))
				return false;
			
			options.ResolveResult = resolver.Resolve (new ExpressionResult (line), new DomLocation (options.Document.Editor.Caret.Line, options.Document.Editor.Caret.Column));
			return options.ResolveResult.ResolvedType != null && !string.IsNullOrEmpty (options.ResolveResult.ResolvedType.FullName) && options.ResolveResult.ResolvedType.FullName != DomReturnType.Void.FullName;
		}
		
		public override void Run (RefactoringOptions options)
		{
			base.Run (options);
			if (selectionEnd >= 0) {
				options.Document.Editor.Caret.Offset = selectionEnd;
				options.Document.Editor.SetSelection (selectionStart, selectionEnd);
			} else {
				TextEditorData data = options.GetTextEditorData ();
				Mono.TextEditor.TextEditor editor = data.Parent;
				TextLink link = new TextLink ("name");
				if (varName != null) {
					if (insertOffset >= 0) {
						link.AddLink (new Segment (insertOffset - selectionStart, varName.Length));
					} else {
						LoggingService.LogWarning ("insert offset not found.");
					}
					if (replaceOffset >= 0)
						link.AddLink (new Segment (replaceOffset - selectionStart, varName.Length));
				}
				List<TextLink> links = new List<TextLink> ();
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
		int replaceOffset = -1;
		int insertOffset = -1;
		
		string varName;
		int varCount;
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			varCount = 0;
			selectionStart = selectionEnd = -1;
			List<Change> result = new List<Change> ();
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return result;
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
			ICSharpCode.NRefactory.Ast.CompilationUnit unit = provider.ParseFile (data.Document.Text);
			var visitor = new VariableLookupVisitor (resolver, new DomLocation (endPoint.Line, endPoint.Column));
			if (options.ResolveResult == null) {
				LoggingService.LogError ("Declare local error: resolve result == null");
				return result;
			}
			IMember callingMember = options.ResolveResult.CallingMember;
			if (callingMember != null)
				visitor.MemberLocation = new Location (callingMember.Location.Column, callingMember.Location.Line);
			unit.AcceptVisitor (visitor, null);
			
			if (data.IsSomethingSelected) {
				ExpressionResult expressionResult = new ExpressionResult (data.SelectedText.Trim ());
				if (expressionResult.Expression.Contains (" ") || expressionResult.Expression.Contains ("\t"))
					expressionResult.Expression = "(" + expressionResult.Expression + ")";
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (endPoint.Line, endPoint.Column));
				if (resolveResult == null)
					return result;
				IReturnType resolvedType = resolveResult.ResolvedType;
				if (resolvedType == null || string.IsNullOrEmpty (resolvedType.Name))
					resolvedType = DomReturnType.Object;
				varName = CreateVariableName (resolvedType, visitor);
				TypeReference returnType;
				if (resolveResult.ResolvedType == null || string.IsNullOrEmpty (resolveResult.ResolvedType.Name)) {
					returnType = new TypeReference ("var");
					returnType.IsKeyword = true;
				} else {
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
				}
				options.ParseMember (resolveResult.CallingMember);
				
				TextReplaceChange insert = new TextReplaceChange ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
				varDecl.Variables.Add (new VariableDeclaration (varName, provider.ParseExpression (data.SelectedText)));
				
				GetContainingEmbeddedStatementVisitor blockVisitor = new GetContainingEmbeddedStatementVisitor ();
				blockVisitor.LookupLocation = new Location (endPoint.Column, endPoint.Line);
			
				unit.AcceptVisitor (blockVisitor, null);
				
				StatementWithEmbeddedStatement containing = blockVisitor.ContainingStatement as StatementWithEmbeddedStatement;
				
				if (containing != null && !(containing.EmbeddedStatement is BlockStatement)) {
					insert.Offset = data.Document.LocationToOffset (containing.StartLocation.Line, containing.StartLocation.Column);
					lineSegment = data.Document.GetLineByOffset (insert.Offset);
					insert.RemovedChars = data.Document.LocationToOffset (containing.EndLocation.Line, containing.EndLocation.Column) - insert.Offset;
					BlockStatement insertedBlock = new BlockStatement ();
					insertedBlock.AddChild (varDecl);
					insertedBlock.AddChild (containing.EmbeddedStatement);
					
					containing.EmbeddedStatement = insertedBlock;
					insert.InsertedText = provider.OutputNode (options.Dom, containing, options.GetWhitespaces (lineSegment.Offset)).TrimStart ();
					int offset, length;
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, 0, out offset, out length)) 
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, offset + 1, out offset, out length)) {
						insert.InsertedText = insert.InsertedText.Substring (0, offset) + varName + insert.InsertedText.Substring (offset + length);
						insertOffset = insert.Offset + offset;
					}
					
				} else if (blockVisitor.ContainingStatement is IfElseStatement) {
					IfElseStatement ifElse = blockVisitor.ContainingStatement as IfElseStatement;
					
					insert.Offset = data.Document.LocationToOffset (blockVisitor.ContainingStatement.StartLocation.Line, blockVisitor.ContainingStatement.StartLocation.Column);
					lineSegment = data.Document.GetLineByOffset (insert.Offset);
					insert.RemovedChars = data.Document.LocationToOffset (blockVisitor.ContainingStatement.EndLocation.Line, blockVisitor.ContainingStatement.EndLocation.Column) - insert.Offset;
					BlockStatement insertedBlock = new BlockStatement ();
					insertedBlock.AddChild (varDecl);
					if (blockVisitor.ContainsLocation (ifElse.TrueStatement [0])) {
						insertedBlock.AddChild (ifElse.TrueStatement [0]);
						ifElse.TrueStatement [0] = insertedBlock;
					} else {
						insertedBlock.AddChild (ifElse.FalseStatement [0]);
						ifElse.FalseStatement [0] = insertedBlock;
					}
					
					insert.InsertedText = provider.OutputNode (options.Dom, blockVisitor.ContainingStatement, options.GetWhitespaces (lineSegment.Offset));
					int offset, length;
					
					if (SearchSubExpression (insert.InsertedText, provider.OutputNode (options.Dom, insertedBlock), 0, out offset, out length)) 
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, offset + 1, out offset, out length)) 
					if (SearchSubExpression (insert.InsertedText, data.SelectedText, offset + 1, out offset, out length)) {
						insert.InsertedText = insert.InsertedText.Substring (0, offset) + varName + insert.InsertedText.Substring (offset + length);
						insertOffset = insert.Offset + offset;
					}
				} else {
					lineSegment = data.Document.GetLine (data.Caret.Line);
					insert.Offset = lineSegment.Offset;
					insert.InsertedText = options.GetWhitespaces (lineSegment.Offset) + provider.OutputNode (options.Dom, varDecl) + Environment.NewLine;
					insertOffset = insert.Offset + options.GetWhitespaces (lineSegment.Offset).Length + provider.OutputNode (options.Dom, varDecl.TypeReference).Length + " ".Length;

					TextReplaceChange replace = new TextReplaceChange ();
					replace.FileName = options.Document.FileName;
					replace.Offset = data.SelectionRange.Offset;
					replace.RemovedChars = data.SelectionRange.Length;
					replace.InsertedText = varName;
					result.Add (replace);
					replaceOffset = replace.Offset;
					if (insert.Offset < replaceOffset)
						replaceOffset += insert.InsertedText.Length - insert.RemovedChars;
					varCount++;
				}
				result.Add (insert);
				varCount++;
				selectionStart = insert.Offset;
				return result;
			}
			
			lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);

			Expression expression = provider.ParseExpression (line);

			if (expression == null)
				return result;

			resolveResult = resolver.Resolve (new ExpressionResult (line), new DomLocation (options.Document.Editor.Caret.Line, options.Document.Editor.Caret.Column));

			if (resolveResult.ResolvedType != null && !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName)) {
				TextReplaceChange insert = new TextReplaceChange ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				insert.Offset = lineSegment.Offset + options.GetWhitespaces (lineSegment.Offset).Length;
				varName = CreateVariableName (resolveResult.ResolvedType, visitor);
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ());
				varDecl.Variables.Add (new VariableDeclaration (varName, expression));
				insert.RemovedChars = expression.EndLocation.Column - 1;
				insert.InsertedText = provider.OutputNode (options.Dom, varDecl);
				insertOffset = insert.Offset + provider.OutputNode (options.Dom, varDecl.TypeReference).Length + " ".Length;

				result.Add (insert);
				varCount++;
				
				int idx = 0;
				while (idx < insert.InsertedText.Length - varName.Length) {
					if (insert.InsertedText.Substring (idx, varName.Length) == varName && (idx == 0 || insert.InsertedText [idx - 1] == ' ') && (idx == insert.InsertedText.Length - varName.Length - 1 || insert.InsertedText [idx + varName.Length] == ' ')) {
						selectionStart = insert.Offset + idx;
						selectionEnd = selectionStart + varName.Length;
						break;
					}
					idx++;
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
				
			case "System.Bool":
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
	
	public class VariableLookupVisitor : AbstractAstVisitor
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
		
		public Location MemberLocation {
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
			this.MemberLocation = Location.Empty;
		}
		
		static IReturnType ConvertTypeReference (TypeReference typeRef)
		{
			if (typeRef == null)
				return null;
			DomReturnType result = new DomReturnType (typeRef.Type);
			foreach (TypeReference genericArgument in typeRef.GenericTypes) {
				result.AddTypeParameter (ConvertTypeReference (genericArgument));
			}
			result.PointerNestingLevel = typeRef.PointerNestingLevel;
			if (typeRef.IsArrayType) {
				result.ArrayDimensions = typeRef.RankSpecifier.Length;
				for (int i = 0; i < typeRef.RankSpecifier.Length; i++) {
					result.SetDimension (i, typeRef.RankSpecifier[i]);
				}
			}
			return result;
		}
		
		public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
		{
			if (!CutRegion.Contains (localVariableDeclaration.StartLocation.Line, localVariableDeclaration.StartLocation.Column)) {
				foreach (VariableDeclaration varDecl in localVariableDeclaration.Variables) {
					variables[varDecl.Name] = new VariableDescriptor (varDecl.Name) {
						IsDefined = true, 
						ReturnType = ConvertTypeReference (localVariableDeclaration.TypeReference),
						Location = new DocumentLocation (MemberLocation.Line + localVariableDeclaration.StartLocation.Line, localVariableDeclaration.StartLocation.Column)
					};
				}
			}
			return base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
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
					variables[identifierExpression.Identifier] = new VariableDescriptor (identifierExpression.Identifier) {
						InitialValueUsed = !valueGetsChanged,
						Location = new DocumentLocation (MemberLocation.Line + identifierExpression.StartLocation.Line, identifierExpression.StartLocation.Column)

					};
					variables[identifierExpression.Identifier].ReturnType = result.ResolvedType;
				}
				if (result != null && !result.StaticResolve && result.ResolvedType != null && !(result is MethodResolveResult) && !(result is NamespaceResolveResult) && !(result is MemberResolveResult))
					unknownVariables.Add (new KeyValuePair <string, IReturnType> (identifierExpression.Identifier, result.ResolvedType));
			}
			return base.VisitIdentifierExpression (identifierExpression, data);
		}
		bool valueGetsChanged = false;
		public override object VisitAssignmentExpression (ICSharpCode.NRefactory.Ast.AssignmentExpression assignmentExpression, object data)
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
		
		public override object VisitUnaryOperatorExpression (ICSharpCode.NRefactory.Ast.UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Op) {
			case UnaryOperatorType.Increment:
			case UnaryOperatorType.Decrement:
			case UnaryOperatorType.PostIncrement:
			case UnaryOperatorType.PostDecrement:
				valueGetsChanged = true;
				break;
			}
			object result = base.VisitUnaryOperatorExpression (unaryOperatorExpression, data);
			valueGetsChanged = false;
			switch (unaryOperatorExpression.Op) {
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

		public override object VisitDirectionExpression (ICSharpCode.NRefactory.Ast.DirectionExpression directionExpression, object data)
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
