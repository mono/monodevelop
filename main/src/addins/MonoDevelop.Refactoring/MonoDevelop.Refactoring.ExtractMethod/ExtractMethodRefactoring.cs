// 
// ExtractMethod.cs
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
using System.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.Refactoring.ExtractMethod
{
	public class ExtractMethodRefactoring : RefactoringOperation
	{
		public override string AccelKey {
			get {
				var cmdInfo = IdeApp.CommandService.GetCommandInfo (RefactoryCommands.ExtractMethod);
				if (cmdInfo != null && cmdInfo.AccelKey != null)
					return cmdInfo.AccelKey.Replace ("dead_circumflex", "^");
				return null;
			}
		}
		
		public ExtractMethodRefactoring ()
		{
			Name = "Extract Method";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
//			if (options.SelectedItem != null)
//				return false;
			var buffer = options.Document.TextEditor;
			if (buffer.SelectionStartPosition - buffer.SelectionEndPosition != 0) {
				ParsedDocument doc = options.ParseDocument ();
				if (doc != null && doc.CompilationUnit != null) {
					int line, column;
					buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out column);
					if (doc.CompilationUnit.GetMemberAt (line, column) == null)
						return false;
					return true;
				}
			}
			return false;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Extract Method...");
		}
		
		public override void Run (RefactoringOptions options)
		{
			ExtractMethodParameters param = CreateParameters (options);
			if (param == null)
				return;
			if (Analyze (options, param, false) == null || param.VariablesToGenerate == null) {
				MessageService.ShowError (GettextCatalog.GetString ("Invalid selection for method extraction."));
				return;
			}
			MessageService.ShowCustomDialog (new ExtractMethodDialog (options, this, param));
		}
		
		public ExtractMethodParameters CreateParameters (RefactoringOptions options)
		{
			var buffer = options.Document.TextEditor;
			
			if (buffer.SelectionStartPosition - buffer.SelectionEndPosition == 0)
				return null;

			ParsedDocument doc = options.ParseDocument ();
			if (doc == null || doc.CompilationUnit == null)
				return null;

			int line, column;
			buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out column);
			IMember member = doc.CompilationUnit.GetMemberAt (line, column);
			if (member == null)
				return null;
			
			ExtractMethodParameters param = new ExtractMethodParameters () {
				DeclaringMember = member,
				Location = new DomLocation (line, column)
			};
			Analyze (options, param, true);
			return param;
		}
		
		public class ExtractMethodParameters 
		{
			public IMember DeclaringMember {
				get;
				set;
			}
				
			public string Name {
				get;
				set;
			}
			
			public string Text {
				get;
				set;
			}
			
			public bool GenerateComment {
				get;
				set;
			}
			
			public bool ReferencesMember {
				get;
				set;
			}
			
			public DomLocation Location {
				get;
				set;
			}
			
			public ICSharpCode.NRefactory.Ast.Modifiers Modifiers {
				get;
				set;
			}
			
			public HashSet<string> ChangedVariables {
				get;
				set;
			}
			
			public List<VariableDescriptor> Variables {
				get;
				set;
			}
			
			public Dictionary<string, VariableDescriptor> VariablesOutside {
				get;
				set;
			}
			public List<VariableDescriptor> OutsideVariableList {
				get;
				set;
			}
				
			/// <summary>
			/// The type of the expression, if the text is an expression, otherwise null.
			/// </summary>
			public IReturnType ExpressionType {
				get;
				set;
			}
			
			List<VariableDescriptor> parameters = new List<VariableDescriptor> ();
			public List<VariableDescriptor> Parameters {
				get {
					return parameters;
				}
			}
			
			public List<VariableDescriptor> VariablesToDefine { get; set; }
			public List<VariableDescriptor> ChangedVariablesUsedOutside { get; set; }
			public List<VariableDescriptor> VariablesToGenerate { get; set; }
			
			public bool OneChangedVariable { get; set; }
		}
		
		static string GetIndent (string text)
		{
			Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
			doc.Text = text;
			StringBuilder result = null;
			for (int i = 1; i < doc.LineCount; i++) {
				LineSegment line = doc.GetLine (i);
				
				StringBuilder lineIndent = new StringBuilder ();
				foreach (char ch in doc.GetTextAt (line)) {
					if (!char.IsWhiteSpace (ch))
						break;
					lineIndent.Append (ch);
				}
				if (line.EditableLength == lineIndent.Length)
					continue;
				if (result == null || lineIndent.Length < result.Length)
					result = lineIndent;
			}
			if (result == null)
				return "";
			return result.ToString ();
		}
		
		static string RemoveIndent (string text, string indent)
		{
			Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (LineSegment line in doc.Lines) {
				string curLineIndent = line.GetIndentation (doc);
				int offset = Math.Min (curLineIndent.Length, indent.Length);
				result.Append (doc.GetTextBetween (line.Offset + offset, line.EndOffset));
			}
			return result.ToString ();
		}
		
		static string AddIndent (string text, string indent)
		{
			Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (LineSegment line in doc.Lines) {
				if (result.Length > 0)
					result.Append (indent);
				result.Append (doc.GetTextAt (line));
			}
			return result.ToString ();
		}
		
		ICSharpCode.NRefactory.Ast.INode Analyze (RefactoringOptions options, ExtractMethodParameters param, bool fillParameter)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return null;

			string text = options.Document.TextEditor.GetText (options.Document.TextEditor.SelectionStartPosition, options.Document.TextEditor.SelectionEndPosition);
			
			TextEditorData data = options.GetTextEditorData ();
			var cu = provider.ParseFile (data.Document.GetTextAt (0, data.SelectionRange.Offset) + "MethodCall ();" + data.Document.GetTextAt (data.SelectionRange.EndOffset, data.Document.Length - data.SelectionRange.EndOffset));
			
			if (cu == null || provider.LastErrors.Count > 0) 
				cu = provider.ParseFile (data.Document.GetTextAt (0, data.SelectionRange.Offset) + "MethodCall ()" + data.Document.GetTextAt (data.SelectionRange.EndOffset, data.Document.Length - data.SelectionRange.EndOffset));
			
			if (cu == null || provider.LastErrors.Count > 0) 
				return null;
			
			param.Text = RemoveIndent (text, GetIndent (text)).TrimEnd ('\n', '\r');
			
			ICSharpCode.NRefactory.Ast.INode result = provider.ParseText (text);
			if (cu == null || provider.LastErrors.Count > 0) {
				return null;
			}
			
			VariableLookupVisitor visitor = new VariableLookupVisitor (resolver, param.Location);
			visitor.MemberLocation = new Location (param.DeclaringMember.Location.Column, param.DeclaringMember.Location.Line);
			if (fillParameter) {
				if (result != null)
					result.AcceptVisitor (visitor, null);
				if (result is Expression) {
					ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (text), param.Location);
					if (resolveResult != null)
						param.ExpressionType = resolveResult.ResolvedType;
				}
				
				var startLocation = data.Document.OffsetToLocation (data.SelectionRange.Offset);
				var endLocation = data.Document.OffsetToLocation (data.SelectionRange.EndOffset);
//				Console.WriteLine ("startLocation={0}, endLocation={1}", startLocation, endLocation);
				
				foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefined && v.InitialValueUsed)) {
					if (startLocation <= varDescr.Location && varDescr.Location < endLocation)
						continue;
//					Console.WriteLine (varDescr.Location);
//					Console.WriteLine (startLocation <= varDescr.Location);
//					Console.WriteLine (varDescr.Location < endLocation);
					param.Parameters.Add (varDescr);
				}
				param.Variables = new List<VariableDescriptor> (visitor.Variables.Values);
				foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefined && param.Variables.Contains (v))) {
					if (param.Parameters.Contains (varDescr))
						continue;
					if (startLocation <= varDescr.Location && varDescr.Location < endLocation)
						continue;
					param.Parameters.Add (varDescr);
				}
				
				param.ReferencesMember = visitor.ReferencesMember;
				param.ChangedVariables = new HashSet<string> (visitor.Variables.Values.Where (v => v.GetsChanged).Select (v => v.Name));
				
				// analyze the variables outside of the selected text
				IMember member = param.DeclaringMember;
			
				int startOffset = data.Document.LocationToOffset (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
				int endOffset = data.Document.LocationToOffset (member.BodyRegion.End.Line, member.BodyRegion.End.Column);
				if (data.SelectionRange.Offset < startOffset || endOffset < data.SelectionRange.EndOffset)
					return null;
				text = data.Document.GetTextBetween (startOffset, data.SelectionRange.Offset) + data.Document.GetTextBetween (data.SelectionRange.EndOffset, endOffset);
				ICSharpCode.NRefactory.Ast.INode parsedNode = provider.ParseText (text);
				visitor = new VariableLookupVisitor (resolver, param.Location);
				visitor.CutRegion = new DomRegion (data.MainSelection.MinLine, data.MainSelection.MaxLine);
				visitor.MemberLocation = new Location (param.DeclaringMember.Location.Column, param.DeclaringMember.Location.Line);
				if (parsedNode != null)
					parsedNode.AcceptVisitor (visitor, null);
				
				
				param.VariablesOutside = new Dictionary<string, VariableDescriptor> ();
				foreach (var pair in visitor.Variables) {
					if (startLocation < pair.Value.Location || endLocation >= pair.Value.Location) {
						param.VariablesOutside.Add (pair.Key, pair.Value);
					}
				}
				param.OutsideVariableList = new List<VariableDescriptor> ();
				foreach (var v in visitor.VariableList) {
					if (startLocation < v.Location || endLocation >= v.Location)
						param.OutsideVariableList.Add (v);
				}
				
				
				
				param.ChangedVariablesUsedOutside = new List<VariableDescriptor> (param.Variables.Where (v => v.GetsChanged && param.VariablesOutside.ContainsKey (v.Name)));
				param.OneChangedVariable = result is BlockStatement;
				if (param.OneChangedVariable) 
					param.OneChangedVariable = param.ChangedVariablesUsedOutside.Count == 1;
				
				param.VariablesToGenerate = new List<VariableDescriptor> (param.ChangedVariablesUsedOutside.Where (v => v.IsDefined));
				foreach (VariableDescriptor var in param.VariablesToGenerate) {
					param.Parameters.Add (var);
				}
				if (param.OneChangedVariable) {
					param.VariablesToDefine = new List<VariableDescriptor> (param.Parameters.Where (var => !var.InitialValueUsed));
					param.VariablesToDefine.ForEach (var => param.Parameters.Remove (var));
				} else {
					param.VariablesToDefine = new List<VariableDescriptor> ();
				}
			}
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			ExtractMethodParameters param = (ExtractMethodParameters)prop;
			TextEditorData data = options.GetTextEditorData ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			IResolver resolver = options.GetResolver ();
			ICSharpCode.NRefactory.Ast.INode node = Analyze (options, param, false);
			if (param.VariablesToGenerate.Count > 0) {
				TextReplaceChange varGen = new TextReplaceChange ();
				varGen.Description = GettextCatalog.GetString ("Generate some temporary variables");
				varGen.FileName = options.Document.FileName;
				LineSegment line = data.Document.GetLine (Math.Max (0, data.Document.OffsetToLineNumber (data.SelectionRange.Offset) - 1));
				varGen.Offset = line.Offset + line.EditableLength;
				varGen.InsertedText = Environment.NewLine + options.GetWhitespaces (line.Offset);
				foreach (VariableDescriptor var in param.VariablesToGenerate) {
					TypeReference tr = options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ();
					varGen.InsertedText += provider.OutputNode (options.Dom, new LocalVariableDeclaration (new VariableDeclaration (var.Name, null, tr))).Trim ();
				}
				result.Add (varGen);
			}
			InvocationExpression invocation = new InvocationExpression (new IdentifierExpression (param.Name));
			foreach (VariableDescriptor var in param.Parameters) {
				if (!param.OneChangedVariable && param.ChangedVariables.Contains (var.Name)) {
					FieldDirection fieldDirection = FieldDirection.Ref;
					VariableDescriptor outsideVar = null;
					if (param.VariablesOutside.TryGetValue (var.Name, out outsideVar) && (var.GetsAssigned || param.VariablesToGenerate.Where (v => v.Name == var.Name).Any ())) {
						if (!outsideVar.GetsAssigned)
							fieldDirection = FieldDirection.Out;
					}
					invocation.Arguments.Add (new DirectionExpression (fieldDirection, new IdentifierExpression (var.Name)));
				} else {
					invocation.Arguments.Add (new IdentifierExpression (var.Name));
				}
			}
			//	string mimeType = DesktopService.GetMimeTypeForUri (options.Document.FileName);
			TypeReference returnType = new TypeReference ("System.Void", true);
			ICSharpCode.NRefactory.Ast.INode outputNode;
			if (param.OneChangedVariable) {
				string name = param.ChangedVariables.First ();
				returnType = options.ShortenTypeName (param.Variables.Find (v => v.Name == name).ReturnType).ConvertToTypeReference ();
				if (param.OutsideVariableList.Any (v => v.Name == name && !v.IsDefined)) {
					LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
					varDecl.Variables.Add (new VariableDeclaration (name, invocation));
					outputNode = varDecl;
				} else {
					outputNode = new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (name), ICSharpCode.NRefactory.Ast.AssignmentOperatorType.Assign, invocation));
				}
			} else {
				outputNode = node is BlockStatement ? (ICSharpCode.NRefactory.Ast.INode)new ExpressionStatement (invocation) : invocation;
			}
			TextReplaceChange replacement = new TextReplaceChange ();
			replacement.Description = string.Format (GettextCatalog.GetString ("Substitute selected statement(s) with call to {0}"), param.Name);
			replacement.FileName = options.Document.FileName;
			replacement.Offset = options.Document.TextEditor.SelectionStartPosition;
			replacement.RemovedChars = options.Document.TextEditor.SelectionEndPosition - options.Document.TextEditor.SelectionStartPosition;
			replacement.MoveCaretToReplace = true;
			
			LineSegment line1 = data.Document.GetLineByOffset (options.Document.TextEditor.SelectionEndPosition);
			if (options.Document.TextEditor.SelectionEndPosition == line1.Offset) {
				if (line1.Offset > 0) {
					LineSegment line2 = data.Document.GetLineByOffset (line1.Offset - 1);
					replacement.RemovedChars -= line2.DelimiterLength;
				}
			}
			
			replacement.InsertedText = options.GetWhitespaces (options.Document.TextEditor.SelectionStartPosition) + provider.OutputNode (options.Dom, outputNode).Trim ();
			
			result.Add (replacement);
			
			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.FileName = options.Document.FileName;
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0} from selected statement(s)"), param.Name);
			insertNewMethod.Offset = options.Document.TextEditor.GetPositionFromLineColumn (param.DeclaringMember.BodyRegion.End.Line, param.DeclaringMember.BodyRegion.End.Column);
			
			ExtractMethodAstTransformer transformer = new ExtractMethodAstTransformer (param.VariablesToGenerate);
			node.AcceptVisitor (transformer, null);
			if (!param.OneChangedVariable && node is Expression) {
				ResolveResult resolveResult = resolver.Resolve (new ExpressionResult ("(" + provider.OutputNode (options.Dom, node) + ")"), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));
				if (resolveResult.ResolvedType != null)
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
			}
			
			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = param.Name;
			methodDecl.Modifier = param.Modifiers;
			methodDecl.TypeReference = returnType;
			
			if (!param.ReferencesMember)
				methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			if (node is BlockStatement) {
				methodDecl.Body = new BlockStatement ();
				methodDecl.Body.AddChild (new EmptyStatement ());
				if (param.OneChangedVariable)
					methodDecl.Body.AddChild (new ReturnStatement (new IdentifierExpression (param.ChangedVariables.First ())));
			} else if (node is Expression) {
				methodDecl.Body = new BlockStatement ();
				methodDecl.Body.AddChild (new ReturnStatement (node as Expression));
			}
			
			foreach (VariableDescriptor var in param.VariablesToDefine) {
				BlockStatement block = methodDecl.Body;
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ());
				varDecl.Variables.Add (new VariableDeclaration (var.Name));
				block.Children.Insert (0, varDecl);
			}
			
			foreach (VariableDescriptor var in param.Parameters) {
				TypeReference typeReference = options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ();
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Name);
				if (!param.OneChangedVariable) {
					if (param.ChangedVariables.Contains (var.Name))
						pde.ParamModifier = ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref;
					if (param.VariablesToGenerate.Where (v => v.Name == var.Name).Any ()) {
						pde.ParamModifier = ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
					}
					VariableDescriptor outsideVar = null;
					if (var.GetsAssigned && param.VariablesOutside.TryGetValue (var.Name, out outsideVar)) {
						if (!outsideVar.GetsAssigned)
							pde.ParamModifier = ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
					}
				}
				
				methodDecl.Parameters.Add (pde);
			}
			
			string indent = options.GetIndent (param.DeclaringMember);
			StringBuilder methodText = new StringBuilder ();
			methodText.AppendLine ();
			methodText.Append (indent);
			methodText.AppendLine ();
			if (param.GenerateComment) {
				methodText.Append (indent);
				methodText.AppendLine ("/// <summary>");
				methodText.Append (indent);
				methodText.AppendLine ("/// TODO: write a comment.");
				methodText.Append (indent);
				methodText.AppendLine ("/// </summary>");
				Ambience ambience = AmbienceService.GetAmbienceForFile (options.Document.FileName);
				foreach (ParameterDeclarationExpression pde in methodDecl.Parameters) {
					methodText.Append (indent);
					methodText.Append ("/// <param name=\"");
					methodText.Append (pde.ParameterName);
					methodText.Append ("\"> A ");
					methodText.Append (ambience.GetString (pde.TypeReference.ConvertToReturnType (), OutputFlags.IncludeGenerics | OutputFlags.UseFullName));
					methodText.Append (" </param>");
					methodText.AppendLine ();
				}
				if (methodDecl.TypeReference.Type != "System.Void") {
					methodText.Append (indent);
					methodText.AppendLine ("/// <returns>");
					methodText.Append (indent);
					methodText.Append ("/// A ");
					methodText.AppendLine (ambience.GetString (methodDecl.TypeReference.ConvertToReturnType (), OutputFlags.IncludeGenerics | OutputFlags.UseFullName));
					methodText.Append (indent);
					methodText.AppendLine ("/// </returns>");
				}
			}
			/*		foreach (VariableDescriptor var in variablesToOutput) {
				TypeReference typeReference = options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ();
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Name);
				if (!param.OneChangedVariable && param.ChangedVariables.Contains (var.Name))
					pde.ParamModifier |= ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
				methodDecl.Parameters.Add (pde);
			}*/			
			
			methodText.Append (indent);
			if (node is BlockStatement) {
				string text = provider.OutputNode (options.Dom, methodDecl, indent).Trim ();
				int emptyStatementMarker = text.LastIndexOf (';');
				if (param.OneChangedVariable)
					emptyStatementMarker = text.LastIndexOf (';', emptyStatementMarker - 1);
				StringBuilder sb = new StringBuilder ();
				sb.Append (text.Substring (0, emptyStatementMarker));
				sb.Append (AddIndent (param.Text, indent + "\t"));
				sb.Append (text.Substring (emptyStatementMarker + 1));
				
				methodText.Append (sb.ToString ());
			} else {
				methodText.Append (provider.OutputNode (options.Dom, methodDecl, options.GetIndent (param.DeclaringMember)).Trim ());
			}
			
			insertNewMethod.InsertedText = methodText.ToString ();
			result.Add (insertNewMethod);

			return result;
		}
	}
}
