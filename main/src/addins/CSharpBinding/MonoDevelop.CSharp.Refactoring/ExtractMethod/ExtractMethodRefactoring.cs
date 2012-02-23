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
using System.Linq;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Text;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Parser;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Text;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Formatting;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.CSharp.Refactoring.ExtractMethod
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
			var buffer = options.Document.Editor;
			if (buffer.Document.MimeType != CSharpFormatter.MimeType)
				return false;
			if (buffer.IsSomethingSelected) {
				var doc = options.Document.ParsedDocument;
				if (doc != null) {
					var member = doc.GetMember (buffer.Caret.Location);
					if (member == null)
						return false;
					if (!member.BodyRegion.IsInside (buffer.Caret.Line, buffer.Caret.Column))
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
			if (!Analyze (options, param, false)) {
				MessageService.ShowError (GettextCatalog.GetString ("Invalid selection for method extraction."));
				return;
			}
			MessageService.ShowCustomDialog (new ExtractMethodDialog (options, this, param));
		}
		
		public ExtractMethodParameters CreateParameters (RefactoringOptions options)
		{
			var buffer = options.Document.Editor;
			
			if (!buffer.IsSomethingSelected)
				return null;

			ParsedDocument doc = options.Document.ParsedDocument;
			if (doc == null)
				return null;

			var member = doc.GetMember (buffer.Caret.Location);
			if (member == null)
				return null;
			
			var param = new ExtractMethodParameters () {
// TODO: Type system conversion.
//				DeclaringMember = member,
				Location = new TextLocation (buffer.Caret.Line, buffer.Caret.Column)
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
			
			public InsertionPoint InsertionPoint {
				get;
				set;
			}
			
			public TextLocation Location {
				get;
				set;
			}
			
			public Accessibility Modifiers {
				get;
				set;
			}
			
			public List<VariableDescriptor> Variables {
				get;
				set;
			}
			
			public List<AstNode> Nodes {
				get;
				set;
			}
			
			public int StartOffset {
				get;
				set;
			}
			
			public int EndOffset {
				get;
				set;
			}
			
	
			/// <summary>
			/// The type of the expression, if the text is an expression, otherwise null.
			/// </summary>
			public IType ExpressionType {
				get;
				set;
			}
			
			public bool OneChangedVariable {
				get;
				set;
			}
			
			List<VariableDescriptor> parameters = new List<VariableDescriptor> ();
			public List<VariableDescriptor> Parameters {
				get {
					return parameters;
				}
			}
		}
		
		static string GetIndent (string text)
		{
			Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
			doc.Text = text;
			string result = null;
			for (int i = 1; i < doc.LineCount; i++) {
				string lineIndent = doc.GetLineIndent (i);
				if (doc.GetLine (i).EditableLength == lineIndent.Length)
					continue;
				if (result == null || lineIndent.Length < result.Length)
					result = lineIndent;
			}
			return result ?? "";
		}
		
		static string RemoveIndent (string text, string indent)
		{
			Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			bool firstLine = true;
			foreach (LineSegment line in doc.Lines) {
				string curLineIndent = line.GetIndentation (doc);
				if (firstLine && curLineIndent.Length == line.EditableLength)
					continue;
				firstLine = false;
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
		
		bool Analyze (RefactoringOptions options, ExtractMethodParameters param, bool fillParameter)
		{
			var data = options.GetTextEditorData ();
			var parser = new CSharpParser ();
			var unit = parser.Parse (data);
			if (unit == null)
				return false;
			var selectionRange = data.SelectionRange;
			var startOffset = selectionRange.Offset;
			while (startOffset + 1 < data.Length && char.IsWhiteSpace (data.GetCharAt (startOffset + 1)))
				startOffset++;
			var endOffset = selectionRange.EndOffset;
			while (startOffset < endOffset && endOffset - 1 > 0 && char.IsWhiteSpace (data.GetCharAt (endOffset - 1)))
				endOffset--;
			if (startOffset >= endOffset)
				return false;
			
			var endLocation = data.OffsetToLocation (endOffset);
			var startLocation = data.OffsetToLocation (startOffset);
			param.StartOffset = startOffset;
			param.EndOffset = endOffset;
			param.Nodes = new List<AstNode> (unit.GetNodesBetween (startLocation.Line, startLocation.Column, endLocation.Line, endLocation.Column));
			string text = options.Document.Editor.GetTextBetween (startLocation, endLocation);
			
			param.Text = RemoveIndent (text, GetIndent (data.GetTextBetween (data.GetLine (startLocation.Line).Offset, data.GetLine (endLocation.Line).EndOffset))).TrimEnd ('\n', '\r');
			var visitor = new VariableLookupVisitor (options, param.Location);
			visitor.MemberLocation = param.DeclaringMember.Region.Begin;
			visitor.CutRegion = new DomRegion (startLocation.Line, startLocation.Column, endLocation.Line, endLocation.Column);
			if (fillParameter) {
				unit.AcceptVisitor (visitor, null);
				if (param.Nodes != null && (param.Nodes.Count == 1 && param.Nodes [0].NodeType == NodeType.Expression)) {
					var resolveResult = options.Resolve (param.Nodes[0]);
					if (resolveResult != null)
						param.ExpressionType = resolveResult.Type;
				}
				
				foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefinedInsideCutRegion && (v.UsedInCutRegion || v.IsChangedInsideCutRegion || v.UsedAfterCutRegion && v.IsDefinedInsideCutRegion))) {
					param.Parameters.Add (varDescr);
				}
			
				param.Variables = new List<VariableDescriptor> (visitor.Variables.Values);
				param.ReferencesMember = visitor.ReferencesMember;
				
				param.OneChangedVariable = param.Variables.Count (p => p.IsDefinedInsideCutRegion && p.UsedAfterCutRegion) == 1;
				if (param.OneChangedVariable)
					param.ExpressionType = param.Variables.First (p => p.IsDefinedInsideCutRegion && p.UsedAfterCutRegion).ReturnType;
				/*
					foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefined && param.Variables.Contains (v))) {
					if (param.Parameters.Contains (varDescr))
						continue;
					if (startLocation <= varDescr.Location && varDescr.Location < endLocation)
						continue;
					param.Parameters.Add (varDescr);
				}
				
				
				param.ChangedVariables = new HashSet<string> (visitor.Variables.Values.Where (v => v.GetsChanged).Select (v => v.Name));
				*/
				// analyze the variables outside of the selected text
				IMember member = param.DeclaringMember;
			
				int bodyStartOffset = data.Document.LocationToOffset (member.BodyRegion.BeginLine, member.BodyRegion.BeginColumn);
				int bodyEndOffset = data.Document.LocationToOffset (member.BodyRegion.EndLine, member.BodyRegion.EndColumn);
				if (startOffset < bodyStartOffset || bodyEndOffset < endOffset)
					return false;
				text = data.Document.GetTextBetween (bodyStartOffset, startOffset) + data.Document.GetTextBetween (endOffset, bodyEndOffset);
				//				ICSharpCode.OldNRefactory.Ast.INode parsedNode = provider.ParseText (text);
				//				visitor = new VariableLookupVisitor (resolver, param.Location);
				//				visitor.CutRegion = new DomRegion (data.MainSelection.MinLine, data.MainSelection.MaxLine);
				//				visitor.MemberLocation = new Location (param.DeclaringMember.Location.Column, param.DeclaringMember.Location.Line);
				//				if (parsedNode != null)
				//					parsedNode.AcceptVisitor (visitor, null);
				
				
				/*	
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
				param.OneChangedVariable = param.Nodes.Count == 1 && param.Nodes[0] is BlockStatement;
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
				}*/
			}
			
			return true;
		}
		
		static string GenerateMethodCall (RefactoringOptions options, ExtractMethodParameters param)
		{
//			var data = options.GetTextEditorData ();
			StringBuilder sb = new StringBuilder ();
			
	/*		LineSegment line = data.Document.GetLine (Math.Max (0, data.Document.OffsetToLineNumber (data.SelectionRange.Offset) - 1));
			if (param.VariablesToGenerate != null && param.VariablesToGenerate.Count > 0) {
				string indent = options.GetWhitespaces (line.Offset);
				sb.Append (Environment.NewLine + indent);
				foreach (VariableDescriptor var in param.VariablesToGenerate) {
					var returnType = options.ShortenTypeName (var.ReturnType);
					sb.Append (returnType.ToInvariantString ());
					sb.Append (" ");
					sb.Append (var.Name);
					sb.AppendLine (";");
					sb.Append (indent);
				}
			}*/
			if (param.OneChangedVariable) {
				var resultVariable = param.Variables.First (p => p.IsDefinedInsideCutRegion && p.UsedAfterCutRegion);
				if (resultVariable.IsDefinedInsideCutRegion) {
					var s = resultVariable.Declaration.Type.StartLocation;
					var e = resultVariable.Declaration.Type.EndLocation;
					sb.Append (options.Document.Editor.GetTextBetween (s.Line, s.Column, e.Line, e.Column) + " ");
				}
				
				sb.Append (resultVariable.Name);
				sb.Append (" = ");
			}
			sb.Append (param.Name);
			sb.Append (" "); // TODO: respect formatting
			sb.Append ("(");
			bool first = true;
			foreach (VariableDescriptor var in param.Parameters) {
				if (param.OneChangedVariable && var.UsedAfterCutRegion && !var.UsedInCutRegion)
					continue;
				if (first) {
					first = false;
				} else {
					sb.Append (", "); // TODO: respect formatting
				}
				if (!param.OneChangedVariable) {
					if (!var.IsDefinedInsideCutRegion && var.IsChangedInsideCutRegion) {
						sb.Append (var.UsedBeforeCutRegion ? "ref " : "out ");
					}
				}
				sb.Append (var.Name);
			}
			sb.Append (")");
			if (param.Nodes != null && (param.Nodes.Count > 1 || param.Nodes.Count == 1 && param.Nodes[0].NodeType != NodeType.Expression)) 
				sb.Append (";");
			return sb.ToString ();
		}
		
		static IUnresolvedMethod GenerateMethodStub (RefactoringOptions options, IUnresolvedTypeDefinition callingType, ExtractMethodParameters param)
		{
			var result = new DefaultUnresolvedMethod (callingType, param.Name);
			result.ReturnType = param.ExpressionType.ToTypeReference ();
			result.Accessibility = param.Modifiers;
//			if (!param.ReferencesMember)
//				result.Modifiers |= MonoDevelop.Projects.Dom.Modifiers.Static;
			
			if (param.Parameters == null)
				return result;
			foreach (var p in param.Parameters) {
				if (param.OneChangedVariable && p.UsedAfterCutRegion && !p.UsedInCutRegion)
					continue;
				var newParameter = new DefaultUnresolvedParameter (p.ReturnType.ToTypeReference (), p.Name);
				if (!param.OneChangedVariable) {
					if (!p.IsDefinedInsideCutRegion && p.IsChangedInsideCutRegion) {
						if (p.UsedBeforeCutRegion) {
							newParameter.IsRef = true;
						} else {
							newParameter.IsOut = true;
						}
					}
				}
				result.Parameters.Add (newParameter);
			}
			return result;
		}
		
		static string GenerateMethodDeclaration (RefactoringOptions options, ExtractMethodParameters param)
		{
			StringBuilder methodText = new StringBuilder ();
			string indent = options.GetIndent (param.DeclaringMember);
			if (param.InsertionPoint != null) {
				switch (param.InsertionPoint.LineBefore) {
				case NewLineInsertion.Eol:
					methodText.AppendLine ();
					break;
				case NewLineInsertion.BlankLine:
					methodText.Append (indent);
					methodText.AppendLine ();
					break;
				}
			} else {
				methodText.AppendLine ();
				methodText.Append (indent);
				methodText.AppendLine ();
			}
			var codeGenerator = new CSharpCodeGenerator () {
				UseSpaceIndent = options.Document.Editor.Options.TabsToSpaces,
				EolMarker = options.Document.Editor.EolMarker,
				TabSize = options.Document.Editor.Options.TabSize
			};
			IUnresolvedTypeDefinition callingType = null;
			var cu = options.Document.ParsedDocument;
			if (cu != null)
				callingType = cu.GetInnermostTypeDefinition (options.Document.Editor.Caret.Location);
			var newMethod = GenerateMethodStub (options, callingType, param);
			var type = callingType.Resolve (options.Document.ParsedDocument.GetTypeResolveContext (options.Document.Compilation, options.Location)).GetDefinition ();

			var createdMethod = codeGenerator.CreateMemberImplementation (type, callingType, newMethod, false);
			
			if (param.GenerateComment && DocGenerator.Instance != null) {
				var ctx = (cu.ParsedFile as CSharpParsedFile).GetTypeResolveContext (options.Document.Compilation, callingType.Region.Begin);
				methodText.AppendLine (DocGenerator.Instance.GenerateDocumentation (newMethod.CreateResolved (ctx), indent + "/// "));
			}
			string code = createdMethod.Code;
			int idx1 = code.LastIndexOf ("throw");
			int idx2 = code.LastIndexOf (";");
			methodText.Append (code.Substring (0, idx1));

			if (param.Nodes != null && (param.Nodes.Count == 1 && param.Nodes[0].NodeType == NodeType.Expression)) {
				methodText.Append ("return ");
				methodText.Append (param.Text.Trim ());
				methodText.Append (";");
			} else {
				StringBuilder text = new StringBuilder ();
				if (param.OneChangedVariable) {
					var par = param.Variables.First (p => p.IsDefinedInsideCutRegion && p.UsedAfterCutRegion);
					if (!par.UsedInCutRegion) {
						text.Append (codeGenerator.GetShortTypeString (options.Document, par.ReturnType));
						text.Append (" ");
						text.Append (par.Name);
						text.AppendLine (";");
					}
				}
				text.Append (param.Text);
				if (param.OneChangedVariable) {
					text.AppendLine ();
					text.Append ("return ");
					text.Append (param.Variables.First (p => p.IsDefinedInsideCutRegion && p.UsedAfterCutRegion).Name);
					text.Append (";");
				}
				methodText.Append (AddIndent (text.ToString (), indent + "\t"));
			}

			methodText.Append (code.Substring (idx2 + 1));
			if (param.InsertionPoint != null) {
				switch (param.InsertionPoint.LineAfter) {
				case NewLineInsertion.Eol:
					methodText.AppendLine ();
					break;
				case NewLineInsertion.BlankLine:
					methodText.AppendLine ();
					methodText.Append (indent);
					methodText.AppendLine ();
					break;
				case NewLineInsertion.None:
					methodText.AppendLine ();
					break;
				}
			} else {
				methodText.AppendLine ();
				methodText.Append (indent);
				methodText.AppendLine ();
			}
			return methodText.ToString ();
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			ExtractMethodParameters param = (ExtractMethodParameters)prop;
			var data = options.GetTextEditorData ();
		//	IResolver resolver = options.GetResolver ();
			
			TextReplaceChange replacement = new TextReplaceChange ();
			replacement.Description = string.Format (GettextCatalog.GetString ("Substitute selected statement(s) with call to {0}"), param.Name);
			replacement.FileName = options.Document.FileName;
			replacement.Offset = param.StartOffset;
			replacement.RemovedChars = param.EndOffset - param.StartOffset;
			replacement.MoveCaretToReplace = true;
			replacement.InsertedText = GenerateMethodCall (options, param);
			result.Add (replacement);
			
			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.FileName = options.Document.FileName;
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0} from selected statement(s)"), param.Name);
			var insertionPoint = param.InsertionPoint;
			if (insertionPoint == null) {
				var points = CodeGenerationService.GetInsertionPoints (options.Document, param.DeclaringMember.DeclaringTypeDefinition.Parts.First ());
				insertionPoint = points.LastOrDefault (p => p.Location.Line < param.DeclaringMember.Region.BeginLine);
				if (insertionPoint == null)
					insertionPoint = points.FirstOrDefault ();
			}
			
			insertNewMethod.RemovedChars = 0; //insertionPoint.LineBefore == NewLineInsertion.Eol ? 0 : insertionPoint.Location.Column - 1;
			insertNewMethod.Offset = data.Document.LocationToOffset (insertionPoint.Location) - insertNewMethod.RemovedChars;
			insertNewMethod.InsertedText = GenerateMethodDeclaration (options, param);
			result.Add (insertNewMethod);
			/*
			
			ExtractMethodAstTransformer transformer = new ExtractMethodAstTransformer (param.VariablesToGenerate);
			node.AcceptVisitor (transformer, null);
			if (!param.OneChangedVariable && node is Expression) {
				ResolveResult resolveResult = resolver.Resolve (new ExpressionResult ("(" + provider.OutputNode (options.Dom, node) + ")"), new TextLocation (options.Document.Editor.Caret.Line, options.Document.Editor.Caret.Column));
				if (resolveResult.ResolvedType != null)
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
			}
			
			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = param.Name;
			methodDecl.Modifier = param.Modifiers;
			methodDecl.TypeReference = returnType;
			
			
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
			
			
			
			string indent = options.GetIndent (param.DeclaringMember);
			StringBuilder methodText = new StringBuilder ();
			switch (param.InsertionPoint.LineBefore) {
			case NewLineInsertion.Eol:
				methodText.AppendLine ();
				break;
			case NewLineInsertion.BlankLine:
				methodText.Append (indent);
				methodText.AppendLine ();
				break;
			}
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
			
			 */
			return result;
		}
	}
}
