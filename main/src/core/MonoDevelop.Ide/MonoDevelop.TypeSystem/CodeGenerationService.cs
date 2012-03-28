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
using Mono.TextEditor;
using MonoDevelop.Core;
using System.CodeDom;
using MonoDevelop.Projects;
using System.CodeDom.Compiler;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;
using MonoDevelop.TypeSystem;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.TypeSystem
{
	public static class CodeGenerationService
	{
		public static IUnresolvedMember AddCodeDomMember (Project project, IUnresolvedTypeDefinition type, CodeTypeMember newMember)
		{
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (type.Region.FileName, out isOpen);
			var parsedDocument = TypeSystemService.ParseFile (data.Document.FileName, data.Document.MimeType, data.Text);
			
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
			
			suitableInsertionPoint.Insert (data, sw.ToString ());
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
		
		public static void AddNewMember (ITypeDefinition type, IUnresolvedTypeDefinition part, IUnresolvedMember newMember, bool implementExplicit = false)
		{
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (part.Region.FileName, out isOpen);
			var parsedDocument = TypeSystemService.ParseFile (data.FileName, data.MimeType, data.Text);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, part);
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, part, newMember);
			
			var generator = CreateCodeGenerator (data, type.Compilation);

			generator.IndentLevel = CalculateBodyIndentLevel (parsedDocument.GetInnermostTypeDefinition (type.Region.Begin));
			var generatedCode = generator.CreateMemberImplementation (type, part, newMember, implementExplicit);
			suitableInsertionPoint.Insert (data, generatedCode.Code);
			if (!isOpen) {
				try {
					File.WriteAllText (type.Region.FileName, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.Region.FileName), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.Region.FileName));
				}
			}
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
			var file = declaringType.ParsedFile as CSharpParsedFile;
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
		
		public static void AddNewMembers (Project project, ITypeDefinition type, IUnresolvedTypeDefinition part, IEnumerable<IUnresolvedMember> newMembers, string regionName = null, Func<IUnresolvedMember, bool> implementExplicit = null)
		{
			IUnresolvedMember firstNewMember = newMembers.FirstOrDefault ();
			if (firstNewMember == null)
				return;
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (part.Region.FileName, out isOpen);
			var parsedDocument = TypeSystemService.ParseFile (project, data);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, part);
			
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, part, firstNewMember);
			
			var generator = CreateCodeGenerator (data, type.Compilation);
			generator.IndentLevel = CalculateBodyIndentLevel (parsedDocument.GetInnermostTypeDefinition (part.Region.Begin));
			StringBuilder sb = new StringBuilder ();
			foreach (var newMember in newMembers) {
				if (sb.Length > 0) {
					sb.AppendLine ();
					sb.AppendLine ();
				}
				sb.Append (generator.CreateMemberImplementation (type, part, newMember, implementExplicit != null ? implementExplicit (newMember) : false).Code);
			}
			suitableInsertionPoint.Insert (data, string.IsNullOrEmpty (regionName) ? sb.ToString () : generator.WrapInRegions (regionName, sb.ToString ()));
			if (!isOpen) {
				try {
					File.WriteAllText (type.Region.FileName, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.Region.FileName), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.Region.FileName));
				}
			}
		}
		
		public static CodeGenerator CreateCodeGenerator (this Ide.Gui.Document doc)
		{
			return CodeGenerator.CreateGenerator (doc);
		}
		
		public static CodeGenerator CreateCodeGenerator (this TextEditorData data, ICompilation compilation)
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
		
		public static List<InsertionPoint> GetInsertionPoints (TextEditorData data, ParsedDocument parsedDocument, IUnresolvedTypeDefinition type)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			if (parsedDocument == null)
				throw new ArgumentNullException ("parsedDocument");
			if (type == null)
				throw new ArgumentNullException ("type");
			
			// update type from parsed document, since this is always newer.
			//type = parsedDocument.GetInnermostTypeDefinition (type.GetLocation ()) ?? type;
			
			List<InsertionPoint > result = new List<InsertionPoint> ();
			int offset = data.LocationToOffset (type.Region.Begin);
			if (offset < 0)
				return result;
			while (offset < data.Length && data.GetCharAt (offset) != '{') {
				offset++;
			}
			
			var realStartLocation = data.OffsetToLocation (offset);
			result.Add (GetInsertionPosition (data.Document, realStartLocation.Line, realStartLocation.Column));
			result [0].LineBefore = NewLineInsertion.None;
			
			foreach (var member in type.Members) {
				TextLocation domLocation = member.BodyRegion.End;
				if (domLocation.Line <= 0) {
					LineSegment lineSegment = data.GetLine (member.Region.BeginLine);
					if (lineSegment == null)
						continue;
					domLocation = new TextLocation (member.Region.BeginLine, lineSegment.EditableLength + 1);
				}
				result.Add (GetInsertionPosition (data.Document, domLocation.Line, domLocation.Column));
			}
			result [result.Count - 1].LineAfter = NewLineInsertion.None;
			CheckStartPoint (data.Document, result [0], result.Count == 1);
			if (result.Count > 1) {
				result.RemoveAt (result.Count - 1); 
				NewLineInsertion insertLine;
				var lineBefore = data.GetLine (type.BodyRegion.EndLine - 1);
				if (lineBefore != null && lineBefore.EditableLength == lineBefore.GetIndentation (data.Document).Length) {
					insertLine = NewLineInsertion.None;
				} else {
					insertLine = NewLineInsertion.Eol;
				}
				// search for line start
				var line = data.GetLine (type.BodyRegion.EndLine);
				int col = type.BodyRegion.EndColumn - 1;
				while (col > 1 && char.IsWhiteSpace (data.GetCharAt (line.Offset + col - 2)))
					col--;
				result.Add (new InsertionPoint (new DocumentLocation (type.BodyRegion.EndLine, col), insertLine, NewLineInsertion.Eol));
				CheckEndPoint (data.Document, result [result.Count - 1], result.Count == 1);
			}
			
			foreach (var region in parsedDocument.UserRegions.Where (r => type.BodyRegion.IsInside (r.Region.Begin))) {
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.BeginLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.EndLine + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
			}
			result.Sort ((left, right) => left.Location.CompareTo (right.Location));
			return result;
		}

		static void CheckEndPoint (TextDocument doc, InsertionPoint point, bool isStartPoint)
		{
			LineSegment line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;
			
			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.BlankLine;
			if (point.Location.Column < line.EditableLength + 1)
				point.LineAfter = NewLineInsertion.Eol;
		}
		
		static void CheckStartPoint (TextDocument doc, InsertionPoint point, bool isEndPoint)
		{
			LineSegment line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;
			if (doc.GetLineIndent (line).Length + 1 == point.Location.Column) {
				int lineNr = point.Location.Line;
				while (lineNr > 1 && doc.GetLineIndent (lineNr - 1).Length == doc.GetLine (lineNr - 1).EditableLength) {
					lineNr--;
				}
				line = doc.GetLine (lineNr);
				point.Location = new DocumentLocation (lineNr, doc.GetLineIndent (line).Length + 1);
			}
			
			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.Eol;
			if (point.Location.Column < line.EditableLength + 1)
				point.LineAfter = isEndPoint ? NewLineInsertion.Eol : NewLineInsertion.BlankLine;
		}
		
		static InsertionPoint GetInsertionPosition (TextDocument doc, int line, int column)
		{
			int bodyEndOffset = doc.LocationToOffset (line, column) + 1;
			LineSegment curLine = doc.GetLine (line);
			if (curLine != null) {
				if (bodyEndOffset < curLine.Offset + curLine.EditableLength) {
					// case1: positition is somewhere inside the start line
					return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.Eol, NewLineInsertion.BlankLine);
				}
			}
			
			// -> if position is at line end check next line
			LineSegment nextLine = doc.GetLine (line + 1);
			if (nextLine == null) // check for 1 line case.
				return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.BlankLine, NewLineInsertion.BlankLine);
			
			for (int i = nextLine.Offset; i < nextLine.Offset + nextLine.EditableLength; i++) {
				char ch = doc.GetCharAt (i);
				if (!char.IsWhiteSpace (ch)) {
					// case2: next line contains non ws chars.
					return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.BlankLine);
				}
			}
			// case3: whitespace line
			return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.None);
		}
		
		static InsertionPoint GetSuitableInsertionPoint (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls, IUnresolvedMember member)
		{
			var mainPart = cls;
			switch (member.EntityType) {
			case EntityType.Field:
				return GetNewFieldPosition (points, mainPart);
			case EntityType.Method:
			case EntityType.Constructor:
			case EntityType.Destructor:
			case EntityType.Operator:
				return GetNewMethodPosition (points, mainPart);
			case EntityType.Event:
				return GetNewEventPosition (points, mainPart);
			case EntityType.Property:
				return GetNewPropertyPosition (points, mainPart);
			}
			throw new InvalidOperationException ("Invalid member type: " + member.EntityType);
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
			throw new NotImplementedException ();
//			if (cls.GetDefinition ().Fields.Count  == 0) 
//				return points.FirstOrDefault ();
//			var lastField = cls.Fields.Last ();
//			return points.FirstOrDefault (p => p.Location.Convert () > lastField.Location);
		}
		
		static InsertionPoint GetNewMethodPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			throw new NotImplementedException ();
//			if (cls.MethodCount + cls.ConstructorCount == 0) 
//				return GetNewPropertyPosition (points, cls);
//			var lastMember = cls.Members.Last ();
//			return points.FirstOrDefault (p => p.Location.Convert () > lastMember.Location);
		}
		
		static InsertionPoint GetNewPropertyPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			throw new NotImplementedException ();
//			if (cls.PropertyCount == 0)
//			IProperty lastProperty = cls.Properties.Last ();
//				return GetNewFieldPosition (points, cls);
//			return points.FirstOrDefault (p => p.Location.Convert () > lastProperty.Location);
		}
		
		static InsertionPoint GetNewEventPosition (IEnumerable<InsertionPoint> points, IUnresolvedTypeDefinition cls)
		{
			throw new NotImplementedException ();
//			if (cls.EventCount == 0)
//				return GetNewMethodPosition (points, cls);
//			IEvent lastEvent = cls.Events.Last ();
//			return points.FirstOrDefault (p => p.Location.Convert () > lastEvent.Location);
		}
		#endregion
		
		public static void AddAttribute (ITypeDefinition cls, string name, params object[] parameters)
		{
			bool isOpen;
			string fileName = cls.Region.FileName;
			var buffer = TextFileProvider.Instance.GetTextEditorData (fileName, out isOpen);
			
			var attr = new CodeAttributeDeclaration (name);
			foreach (var parameter in parameters) {
				attr.Arguments.Add (new CodeAttributeArgument (new CodePrimitiveExpression (parameter)));
			}
			
			var type = new CodeTypeDeclaration ("temp");
			type.CustomAttributes.Add (attr);
			
			var provider = ((DotNetProject)cls.GetSourceProject ()).LanguageBinding.GetCodeDomProvider ();
			var sw = new StringWriter ();
			provider.GenerateCodeFromType (type, sw, new CodeGeneratorOptions ());
			string code = sw.ToString ();
			int start = code.IndexOf ('[');
			int end = code.LastIndexOf (']');
			code = code.Substring (start, end - start + 1) + Environment.NewLine;

			int pos = buffer.LocationToOffset (cls.Region.BeginLine, cls.Region.BeginColumn);

			code = buffer.GetLineIndent (cls.Region.BeginLine) + code;
			buffer.Insert (pos, code);
			if (!isOpen) {
				File.WriteAllText (fileName, buffer.Text);
				buffer.Dispose ();
			}
			
		}
		
		public static IUnresolvedTypeDefinition AddType (DotNetProject project, string folder, string namspace, CodeTypeDeclaration type)
		{
			
			var unit = new CodeCompileUnit ();
			var ns = new CodeNamespace (namspace);
			ns.Types.Add (type);
			unit.Namespaces.Add (ns);
			
			string fileName = project.LanguageBinding.GetFileName (Path.Combine (folder, type.Name));
			using (var sw = new StreamWriter (fileName)) {
				var provider = project.LanguageBinding.GetCodeDomProvider ();
				var options = new CodeGeneratorOptions ();
				options.IndentString = "\t";
				options.BracingStyle = "C";
		
				provider.GenerateCodeFromCompileUnit (unit, sw, options);
			}
			return TypeSystemService.ParseFile (project, fileName).TopLevelTypeDefinitions.FirstOrDefault ();
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
