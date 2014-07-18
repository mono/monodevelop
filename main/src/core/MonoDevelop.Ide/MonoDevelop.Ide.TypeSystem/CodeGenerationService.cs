// 
// CodeGenerationService.cs
//  
// Author:
//       mkrueger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Linq;
using System.Text;
using MonoDevelop.Core;
using System.CodeDom;
using MonoDevelop.Projects;
using System.CodeDom.Compiler;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.TypeSystem
{
	public static class CodeGenerationService
	{
		public static IUnresolvedMember AddCodeDomMember (MonoDevelop.Projects.Project project, IUnresolvedTypeDefinition type, CodeTypeMember newMember)
		{
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (type.Region.FileName, out isOpen);
			var parsedDocument = TypeSystemService.ParseFile (data.FileName, data.MimeType, data.Text);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, type);
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, type, newMember);
			
			var dotNetProject = project as DotNetProject;
			if (dotNetProject == null) {
				LoggingService.LogError ("Only .NET projects are supported.");
				return null;
			}
			
			var generator = dotNetProject.LanguageBinding.GetCodeDomProvider ();
			StringWriter sw = new StringWriter ();
			var options = new CodeGeneratorOptions ();
			options.IndentString = data.GetLineIndent (type.Region.BeginLine) + "\t";
			if (newMember is CodeMemberMethod)
				options.BracingStyle = "C";
			generator.GenerateCodeFromMember (newMember, sw, options);

			var code = sw.ToString ();
			if (!string.IsNullOrEmpty (code))
				suitableInsertionPoint.Insert (data, code);
			if (!isOpen) {
				try {
					File.WriteAllText (type.Region.FileName, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (string.Format ("Failed to write file '{0}'.", type.Region.FileName), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.Region.FileName));
				}
			}
			var newDocument = TypeSystemService.ParseFile (data.FileName, data.MimeType, data.Text);
			return newDocument.ParsedFile.GetMember (suitableInsertionPoint.Location.Line, int.MaxValue);
		}
		
		public static void AddNewMember (ITypeSymbol type, Location part, SyntaxNode newMember, bool implementExplicit = false)
		{
			bool isOpen;
			var filePath = part.SourceTree.FilePath;
			var data = TextFileProvider.Instance.GetTextEditorData (filePath, out isOpen);
			var parsedDocument = TypeSystemService.ParseFile (data.FileName, data.MimeType, data.Text);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, type, part);
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (data, insertionPoints, type, part, newMember);
			
			/*
			var generator = CreateCodeGenerator (data, type.Compilation);

			generator.IndentLevel = CalculateBodyIndentLevel (parsedDocument.GetInnermostTypeDefinition (type.Region.Begin));
			var generatedCode = generator.CreateMemberImplementation (type, part, newMember, implementExplicit);
			*/
			suitableInsertionPoint.Insert (data, newMember.ToString ());
			if (!isOpen) {
				try {
					File.WriteAllText (filePath, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Failed to write file '{0}'.", filePath), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", filePath));
				}
			}
		}

		public static Task<bool> InsertMemberWithCursor (
			string operation, ITypeDefinition parentType, IUnresolvedTypeDefinition part,
			IUnresolvedMember newMember, bool implementExplicit = false)
		{
			var tcs = new TaskCompletionSource<bool>();
			if (parentType == null)
				return tcs.Task;
			part = part ?? parentType.Parts.FirstOrDefault ();
			if (part == null)
				return tcs.Task;
			var loadedDocument = IdeApp.Workbench.OpenDocument (part.Region.FileName);
			loadedDocument.RunWhenLoaded (delegate {
				var editor = loadedDocument.Editor;
				var loc = part.Region.Begin;
				var parsedDocument = loadedDocument.UpdateParseDocument ();
				var declaringType = parsedDocument.GetInnermostTypeDefinition (loc);
				var insertionPoints = CodeGenerationService.GetInsertionPoints (loadedDocument, declaringType);
				if (insertionPoints.Count == 0) {
					MessageService.ShowError (
						GettextCatalog.GetString ("No valid insertion point can be found in type '{0}'.", declaringType.Name)
						);
					return;
				}
				editor.StartInsertionMode (new InsertionModeOptions (operation, insertionPoints, delegate(InsertionCursorEventArgs iCArgs) {
					if (!iCArgs.Success) {
						tcs.SetResult (false);
						return;
					}
					var generator = CreateCodeGenerator (editor, parentType.Compilation);
					generator.IndentLevel = CalculateBodyIndentLevel (declaringType);
// TODO: Roslyn port.					
//					var generatedCode = generator.CreateMemberImplementation (parentType, part, newMember, implementExplicit);
//					iCArgs.InsertionPoint.Insert (editor, generatedCode.Code);
//					tcs.SetResult (true);
				}));
//				var mode = new InsertionCursorEditMode (
//					editor.Parent,
//					insertionPoints);
//
//				var suitableInsertionPoint = GetSuitableInsertionPoint (mode.InsertionPoints, part, newMember);
//				if (suitableInsertionPoint != null)
//					mode.CurIndex = mode.InsertionPoints.IndexOf (suitableInsertionPoint);
//				else
//					mode.CurIndex = 0;
//
//				var helpWindow = new Mono.TextEditor.PopupWindow.InsertionCursorLayoutModeHelpWindow () {
//					TitleText = operation
//				};
//				mode.HelpWindow = helpWindow;
//
//				mode.StartMode ();
//				mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
//					if (!iCArgs.Success) {
//						tcs.SetResult (false);
//						return;
//					}
//					var generator = CreateCodeGenerator (editor, parentType.Compilation);
//					generator.IndentLevel = CalculateBodyIndentLevel (declaringType);
//					var generatedCode = generator.CreateMemberImplementation (parentType, part, newMember, implementExplicit);
//					mode.InsertionPoints[mode.CurIndex].Insert (editor, generatedCode.Code);
//					tcs.SetResult (true);
//				};
			});

			return tcs.Task;
		}
		
		public static Task<bool> InsertMember (
			ITypeDefinition parentType, IUnresolvedTypeDefinition part,
			IUnresolvedMember newMember, bool implementExplicit = false)
		{
			var tcs = new TaskCompletionSource<bool>();
			if (parentType == null)
				return tcs.Task;
			part = part ?? parentType.Parts.FirstOrDefault ();
			if (part == null)
				return tcs.Task;

			var loadedDocument = IdeApp.Workbench.OpenDocument (part.Region.FileName);
			loadedDocument.RunWhenLoaded (delegate {
				var editor = loadedDocument.Editor;
				var loc = part.Region.Begin;
				var parsedDocument = loadedDocument.UpdateParseDocument ();
				var declaringType = parsedDocument.GetInnermostTypeDefinition (loc);
				var insertionPoints = CodeGenerationService.GetInsertionPoints (loadedDocument, declaringType);
				if (insertionPoints.Count == 0) {
					MessageService.ShowError (
						GettextCatalog.GetString ("No valid insertion point can be found in type '{0}'.", declaringType.Name)
						);
					return;
				}
				var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, part, newMember) ?? insertionPoints.First ();
// TODO: Roslyn port.
//				var generator = CreateCodeGenerator (editor, parentType.Compilation);
//				generator.IndentLevel = CalculateBodyIndentLevel (declaringType);
//				var generatedCode = generator.CreateMemberImplementation (parentType, part, newMember, implementExplicit);
//				suitableInsertionPoint.Insert (editor, generatedCode.Code);
			});

			return tcs.Task;
		}

		public static int CalculateBodyIndentLevel (IUnresolvedTypeDefinition declaringType)
		{
			if (declaringType == null)
				return 0;
			int indentLevel = 1;
			while (declaringType.DeclaringTypeDefinition != null) {
				indentLevel++;
				declaringType = declaringType.DeclaringTypeDefinition;
			}
			var file = declaringType.UnresolvedFile as CSharpUnresolvedFile;
			if (file == null)
				return indentLevel;
			var scope = file.GetUsingScope (declaringType.Region.Begin);
			while (scope != null && !string.IsNullOrEmpty (scope.NamespaceName)) {
				indentLevel++;
				// skip virtual scopes.
				while (scope.Parent != null && scope.Parent.Region == scope.Region)
					scope = scope.Parent;
				scope = scope.Parent;
			}
			return indentLevel;
		}
		public static CodeGenerator CreateCodeGenerator (this Ide.Gui.Document doc)
		{
			return CodeGenerator.CreateGenerator (doc);
		}

		public static CodeGenerator CreateCodeGenerator (this DocumentContext documentContext, TextEditor editor)
		{
			return CodeGenerator.CreateGenerator (editor, documentContext);
		}

		public static CodeGenerator CreateCodeGenerator (this ITextDocument data, ICompilation compilation)
		{
			return CodeGenerator.CreateGenerator (data, compilation);
		}
		
		static IUnresolvedTypeDefinition GetMainPart (IType t)
		{
			return t.GetDefinition ().Parts.First ();
		}
		
		#region Insertion Points
		public static List<InsertionPoint> GetInsertionPoints (MonoDevelop.Ide.Gui.Document document, IUnresolvedTypeDefinition type)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (document.ParsedDocument == null)
				return new List<InsertionPoint> ();
			return GetInsertionPoints (document.Editor, document.ParsedDocument, type);
		}
		
		public static List<InsertionPoint> GetInsertionPoints (IReadonlyTextDocument data, ParsedDocument parsedDocument, IUnresolvedTypeDefinition type)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			if (parsedDocument == null)
				throw new ArgumentNullException ("parsedDocument");
			if (type == null)
				throw new ArgumentNullException ("type");
			
			// update type from parsed document, since this is always newer.
			//type = parsedDocument.GetInnermostTypeDefinition (type.GetLocation ()) ?? type;
			var result = new List<InsertionPoint> ();
			int offset = data.LocationToOffset (type.Region.Begin.Line, type.Region.Begin.Column);
			if (offset < 0 || type.BodyRegion.IsEmpty)
				return result;
			while (offset < data.Length && data.GetCharAt (offset) != '{') {
				offset++;
			}
			var realStartLocation = data.OffsetToLocation (offset);
			result.Add (GetInsertionPosition (data, realStartLocation.Line, realStartLocation.Column));
			result [0].LineBefore = NewLineInsertion.None;
			
			foreach (var member in type.Members) {
				var domLocation = member.BodyRegion.End;
				if (domLocation.Line <= 0) {
					var lineSegment = data.GetLine (member.Region.BeginLine);
					if (lineSegment == null)
						continue;
					domLocation = new TextLocation (member.Region.BeginLine, lineSegment.Length + 1);
				}
				result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
			}

			foreach (var nestedType in type.NestedTypes) {
				var domLocation = nestedType.BodyRegion.End;
				if (domLocation.Line <= 0) {
					var lineSegment = data.GetLine (nestedType.Region.BeginLine);
					if (lineSegment == null)
						continue;
					domLocation = new TextLocation (nestedType.Region.BeginLine, lineSegment.Length + 1);
				}
				result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
			}

			result [result.Count - 1].LineAfter = NewLineInsertion.None;
			CheckStartPoint (data, result [0], result.Count == 1);
			if (result.Count > 1) {
				result.RemoveAt (result.Count - 1); 
				NewLineInsertion insertLine;
				var lineBefore = data.GetLine (type.BodyRegion.EndLine - 1);
				if (lineBefore != null && lineBefore.Length == lineBefore.GetIndentation (data).Length) {
					insertLine = NewLineInsertion.None;
				} else {
					insertLine = NewLineInsertion.Eol;
				}
				// search for line start
				int col = type.BodyRegion.EndColumn - 1;
				var line = data.GetLine (type.BodyRegion.EndLine);
				if (line != null) {
					var lineOffset = line.Offset;
					col = Math.Min (line.Length, col);
					while (lineOffset + col - 2 >= 0 && col > 1 && char.IsWhiteSpace (data.GetCharAt (lineOffset + col - 2)))
						col--;
				}
				result.Add (new InsertionPoint (new DocumentLocation (type.BodyRegion.EndLine, col), insertLine, NewLineInsertion.Eol));
				CheckEndPoint (data, result [result.Count - 1], result.Count == 1);
			}
			
			foreach (var region in parsedDocument.UserRegions.Where (r => type.BodyRegion.IsInside (r.Region.Begin))) {
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.BeginLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
			}
			result.Sort ((left, right) => left.Location.CompareTo (right.Location));
//			foreach (var res in result)
//				Console.WriteLine (res);
			return result;
		}

		public static List<InsertionPoint> GetInsertionPoints (IReadonlyTextDocument data, ParsedDocument parsedDocument, ITypeSymbol type, Location part)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			if (parsedDocument == null)
				throw new ArgumentNullException ("parsedDocument");
			if (type == null)
				throw new ArgumentNullException ("type");
			
			// update type from parsed document, since this is always newer.
			//type = parsedDocument.GetInnermostTypeDefinition (type.GetLocation ()) ?? type;
			List<InsertionPoint> result = new List<InsertionPoint> ();
			int offset = part.SourceSpan.Start;
			if (offset < 0)
				return result;
			while (offset < data.Length && data.GetCharAt (offset) != '{') {
				offset++;
			}
			var realStartLocation = data.OffsetToLocation (offset);
			result.Add (GetInsertionPosition (data, realStartLocation.Line, realStartLocation.Column));
			result [0].LineBefore = NewLineInsertion.None;
			
			foreach (var member in type.GetMembers ()) {
				Location loc = member.Locations.FirstOrDefault (l => l.SourceTree.FilePath == part.SourceTree.FilePath);
				if (loc == null)
					continue;
				
				TextLocation domLocation = data.OffsetToLocation (loc.SourceSpan.End);
				if (domLocation.Line <= 0) {
					var lineSegment = data.GetLineByOffset (loc.SourceSpan.Start);
					if (lineSegment == null)
						continue;
					domLocation = new TextLocation (lineSegment.LineNumber, lineSegment.Length + 1);
				}
				result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
			}

			foreach (var member in type.GetMembers ()) {
				Location loc = member.Locations.FirstOrDefault (l => l.SourceTree.FilePath == part.SourceTree.FilePath);
				if (loc == null)
					continue;

				var lineSegment = data.GetLineByOffset (loc.SourceSpan.Start);
				if (lineSegment == null)
					continue;
				var domLocation = new TextLocation (lineSegment.LineNumber, lineSegment.Length + 1);
				result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
			}

			foreach (var nestedType in type.GetTypeMembers ()) {
				Location loc = nestedType.Locations.FirstOrDefault (l => l.SourceTree.FilePath == part.SourceTree.FilePath);
				if (loc == null)
					continue;
				
				var lineSegment = data.GetLineByOffset (loc.SourceSpan.Start);
				if (lineSegment == null)
					continue;
				var domLocation = new TextLocation (lineSegment.LineNumber, lineSegment.Length + 1);
				
				result.Add (GetInsertionPosition (data, domLocation.Line, domLocation.Column));
			}

			result [result.Count - 1].LineAfter = NewLineInsertion.None;
			CheckStartPoint (data, result [0], result.Count == 1);
			if (result.Count > 1) {
				result.RemoveAt (result.Count - 1); 
				NewLineInsertion insertLine;
				var endLine = data.GetLineByOffset (part.SourceSpan.End);
				var lineBefore = endLine.PreviousLine;
				if (lineBefore != null && lineBefore.Length == lineBefore.GetIndentation (data).Length) {
					insertLine = NewLineInsertion.None;
				} else {
					insertLine = NewLineInsertion.Eol;
				}
				// search for line start
				int col = part.SourceSpan.End - endLine.Offset;
				var line = endLine;
				if (line != null) {
					var lineOffset = line.Offset;
					col = Math.Min (line.Length, col);
					while (lineOffset + col - 2 >= 0 && col > 1 && char.IsWhiteSpace (data.GetCharAt (lineOffset + col - 2)))
						col--;
				}
				result.Add (new InsertionPoint (new DocumentLocation (endLine.LineNumber, col), insertLine, NewLineInsertion.Eol));
				CheckEndPoint (data, result [result.Count - 1], result.Count == 1);
			}
			var bodyRegion = new DomRegion (data.OffsetToLocation (part.SourceSpan.Start), data.OffsetToLocation (part.SourceSpan.End));
			foreach (var region in parsedDocument.UserRegions.Where (r => bodyRegion.IsInside (r.Region.Begin))) {
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.BeginLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
			}
			result.Sort ((left, right) => left.Location.CompareTo (right.Location));
//			foreach (var res in result)
//				Console.WriteLine (res);
			return result;
		}


		static void CheckEndPoint (IReadonlyTextDocument doc, InsertionPoint point, bool isStartPoint)
		{
			var line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;
			
			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.BlankLine;
			if (point.Location.Column < line.Length + 1)
				point.LineAfter = NewLineInsertion.Eol;
		}
		
		static void CheckStartPoint (IReadonlyTextDocument doc, InsertionPoint point, bool isEndPoint)
		{
			var line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;
			if (doc.GetLineIndent (line).Length + 1 == point.Location.Column) {
				int lineNr = point.Location.Line;
				while (lineNr > 1 && doc.GetLineIndent (lineNr - 1).Length == doc.GetLine (lineNr - 1).Length) {
					lineNr--;
				}
				line = doc.GetLine (lineNr);
				point.Location = new DocumentLocation (lineNr, doc.GetLineIndent (line).Length + 1);
			}
			
			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.Eol;
			if (point.Location.Column < line.Length + 1)
				point.LineAfter = isEndPoint ? NewLineInsertion.Eol : NewLineInsertion.BlankLine;
		}
		
		static InsertionPoint GetInsertionPosition (IReadonlyTextDocument doc, int line, int column)
		{
			int bodyEndOffset = doc.LocationToOffset (line, column) + 1;
			var curLine = doc.GetLine (line);
			if (curLine != null) {
				if (bodyEndOffset < curLine.Offset + curLine.Length) {
					// case1: positition is somewhere inside the start line
					return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.Eol, NewLineInsertion.BlankLine);
				}
			}
			
			// -> if position is at line end check next line
			var nextLine = doc.GetLine (line + 1);
			if (nextLine == null) // check for 1 line case.
				return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.BlankLine, NewLineInsertion.BlankLine);
			
			for (int i = nextLine.Offset; i < nextLine.EndOffset; i++) {
				char ch = doc.GetCharAt (i);
				if (!char.IsWhiteSpace (ch)) {
					// case2: next line contains non ws chars.
					return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.BlankLine);
				}
			}

			var nextLine2 = doc.GetLine (line + 2);
			if (nextLine2 != null) {
				for (int i = nextLine2.Offset; i < nextLine2.EndOffset; i++) {
					char ch = doc.GetCharAt (i);
					if (!char.IsWhiteSpace (ch)) {
						// case3: one blank line
						return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol);
					}
				}
			}
			// case4: more than 1 blank line
			return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.None);
		}
		
		static InsertionPoint GetSuitableInsertionPoint (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls, IUnresolvedMember member)
		{
			var mainPart = cls;
			switch (member.SymbolKind) {
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Field:
				return GetNewFieldPosition (points, mainPart);
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Method:
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Constructor:
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Destructor:
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Operator:
				return GetNewMethodPosition (points, mainPart);
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Event:
				return GetNewEventPosition (points, mainPart);
			case ICSharpCode.NRefactory.TypeSystem.SymbolKind.Property:
				return GetNewPropertyPosition (points, mainPart);
			}
			throw new InvalidOperationException ("Invalid member type: " + member.SymbolKind);
		}
		
		static InsertionPoint GetSuitableInsertionPoint (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part, SyntaxNode member)
		{
			switch (member.CSharpKind ()) {
			case SyntaxKind.FieldDeclaration:
				return GetNewFieldPosition (data, points, cls, part);
			case SyntaxKind.MethodDeclaration:
			case SyntaxKind.ConstructorDeclaration:
			case SyntaxKind.DestructorDeclaration:
			case SyntaxKind.OperatorDeclaration:
				return GetNewMethodPosition (data, points, cls, part);
			case SyntaxKind.EventDeclaration:
				return GetNewEventPosition (data, points, cls, part);
			case SyntaxKind.PropertyDeclaration:
				return GetNewPropertyPosition (data, points, cls, part);
			}
			throw new InvalidOperationException ("Invalid member type: " + member.CSharpKind ());
		}

		static InsertionPoint GetSuitableInsertionPoint (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls, CodeTypeMember mem)
		{
			var mainPart = cls;
			if (mem is System.CodeDom.CodeMemberEvent)
				return GetNewEventPosition (points, mainPart);
			if (mem is System.CodeDom.CodeMemberProperty)
				return GetNewPropertyPosition (points, mainPart);
			if (mem is System.CodeDom.CodeMemberField)
				return GetNewFieldPosition (points, mainPart);
			if (mem is System.CodeDom.CodeMemberMethod)
				return GetNewMethodPosition (points, mainPart);
			return GetNewFieldPosition (points, mainPart);
		}
		
		static InsertionPoint GetNewFieldPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			if (!cls.Fields.Any ()) 
				return points.FirstOrDefault ();
			var lastField = cls.Fields.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastField.Region.Begin);
		}
		
		static InsertionPoint GetNewMethodPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			if (!cls.Methods.Any ()) 
				return GetNewPropertyPosition (points, cls);
			var lastMember = cls.Members.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastMember.Region.Begin);
		}
		
		static InsertionPoint GetNewPropertyPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			if (!cls.Properties.Any ())
				return GetNewFieldPosition (points, cls);
			var lastProperty = cls.Properties.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastProperty.Region.Begin);
		}
		
		static InsertionPoint GetNewEventPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			if (!cls.Events.Any ())
				return GetNewMethodPosition (points, cls);
			var lastEvent = cls.Events.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastEvent.Region.Begin);
		}
		
		
		
		static InsertionPoint GetNewFieldPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IFieldSymbol> ().Any ()) 
				return points.FirstOrDefault ();
			var lastField = cls.GetMembers ().OfType<IFieldSymbol> ().Last ();
			TextLocation begin = data.OffsetToLocation (lastField.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location.Convert () > begin);
		}
		
		static InsertionPoint GetNewMethodPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IMethodSymbol> ().Any ()) 
				return GetNewPropertyPosition (data, points, cls, part);
			var lastMethod = cls.GetMembers ().OfType<IMethodSymbol> ().Last ();
			TextLocation begin = data.OffsetToLocation (lastMethod.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location.Convert () > begin);
		}
		
		static InsertionPoint GetNewPropertyPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IPropertySymbol> ().Any ()) 
				return GetNewFieldPosition (data, points, cls, part);
			var lastProperty = cls.GetMembers ().OfType<IPropertySymbol> ().Last ();
			TextLocation begin = data.OffsetToLocation (lastProperty.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location.Convert () > begin);
		}
		
		static InsertionPoint GetNewEventPosition (IReadonlyTextDocument data, IEnumerable<InsertionPoint> points, ITypeSymbol cls, Location part)
		{
			if (!cls.GetMembers ().OfType<IEventSymbol> ().Any ()) 
				return GetNewMethodPosition (data, points, cls, part);
			var lastEvent = cls.GetMembers ().OfType<IEventSymbol> ().Last ();
			TextLocation begin = data.OffsetToLocation (lastEvent.Locations.First ().SourceSpan.Start);
			return points.FirstOrDefault (p => p.Location.Convert () > begin);
		}
		
		#endregion
		
		public static void AddAttribute (INamedTypeSymbol cls, string name, params object[] parameters)
		{
			bool isOpen;
			string fileName = cls.Locations.First ().SourceTree.FilePath;
			var buffer = TextFileProvider.Instance.GetTextEditorData (fileName, out isOpen);
			
			
			var code = new StringBuilder ();
			int pos = cls.Locations.First ().SourceSpan.Start;
			var line = buffer.GetLineByOffset (pos);
			code.Append (buffer.GetLineIndent (line));
			code.Append ("[");
			code.Append (name);
			if (parameters != null && parameters.Length > 0) {
				code.Append ("(");
				for (int i = 0; i < parameters.Length; i++) {
					if (i > 0)
						code.Append (", ");
					code.Append (parameters [i]);
				}
				code.Append (")");
			}
 			code.Append ("]");
			code.AppendLine ();
			
			buffer.InsertText (line.Offset, code.ToString ());

			if (!isOpen) {
				File.WriteAllText (fileName, buffer.Text);
			}
		}
		
		public static ITypeSymbol AddType (DotNetProject project, string folder, string namspace, ClassDeclarationSyntax type)
		{
			var ns = SyntaxFactory.NamespaceDeclaration (SyntaxFactory.ParseName (namspace)).WithMembers (new SyntaxList<MemberDeclarationSyntax> () { type });
			
			string fileName = project.LanguageBinding.GetFileName (Path.Combine (folder, type.Identifier.ToString ()));
			using (var sw = new StreamWriter (fileName)) {
				sw.WriteLine (ns.ToString ());
			}
			FileService.NotifyFileChanged (fileName); 
			var roslynProject = RoslynTypeSystemService.GetProject (project); 
			var id = RoslynTypeSystemService.Workspace.GetDocumentId (roslynProject.Id, fileName); 
			if (id == null)
				return null;
			var model = roslynProject.GetDocument (id).GetSemanticModelAsync ().Result;
			var typeSyntax = model.SyntaxTree.GetCompilationUnitRoot ().ChildNodes ().First ().ChildNodes ().First () as ClassDeclarationSyntax;
			return model.GetDeclaredSymbol (typeSyntax);
		}
	}
	
	public static class HelperMethods
	{
		public static TextLocation Convert (this DocumentLocation location)
		{
			return new TextLocation (location.Line, location.Column);
		}
	}
}
