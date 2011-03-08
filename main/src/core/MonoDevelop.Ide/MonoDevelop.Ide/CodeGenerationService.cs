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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using System.CodeDom;
using MonoDevelop.Projects;
using System.CodeDom.Compiler;

namespace MonoDevelop.Ide
{
	public class CodeGenerationService
	{
		public static IMember AddCodeDomMember (IType type, CodeTypeMember newMember)
		{
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (type.CompilationUnit.FileName, out isOpen);
			var parsedDocument = ProjectDomService.GetParsedDocument (type.SourceProjectDom, type.CompilationUnit.FileName);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, type);
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, type, newMember);
			
			var dotNetProject = type.SourceProject as DotNetProject;
			if (dotNetProject == null) {
				LoggingService.LogError ("Only .NET projects are supported.");
				return null;
			}
			
			var generator = dotNetProject.LanguageBinding.GetCodeDomProvider ();
			StringWriter sw = new StringWriter ();
			
			var options = new CodeGeneratorOptions ();
			options.IndentString = "\t";
			if (newMember is CodeMemberMethod)
				options.BracingStyle = "C";
			generator.GenerateCodeFromMember (newMember, sw, options);
			
			suitableInsertionPoint.Insert (data, sw.ToString ());
			if (!isOpen) {
				try {
					File.WriteAllText (type.CompilationUnit.FileName, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (string.Format ("Failed to write file '{0}'.", type.CompilationUnit.FileName), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.CompilationUnit.FileName));
				}
			}
			var newDocument = ProjectDomService.Parse (type.SourceProject as Project, type.CompilationUnit.FileName, data.Text);
			return newDocument.CompilationUnit.GetMemberAt (suitableInsertionPoint.Location.Line, int.MaxValue);
		}
		
		public static void AddNewMember (IType type, IMember newMember, bool implementExplicit = false)
		{
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (type.CompilationUnit.FileName, out isOpen);
			var parsedDocument = ProjectDomService.GetParsedDocument (type.SourceProjectDom, type.CompilationUnit.FileName);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, type);
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, type, newMember);
			
			var generator = CreateCodeGenerator (data);
			var generatedCode = generator.CreateMemberImplementation (type, newMember, implementExplicit);
			suitableInsertionPoint.Insert (data, generatedCode.Code);
			if (!isOpen) {
				try {
					File.WriteAllText (type.CompilationUnit.FileName, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.CompilationUnit.FileName), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.CompilationUnit.FileName));
				}
			}
		}
		
		public static void AddNewMembers (IType type, IEnumerable<IMember> newMembers, string regionName = null, Func<IMember, bool> implementExplicit = null)
		{
			bool isOpen;
			var data = TextFileProvider.Instance.GetTextEditorData (type.CompilationUnit.FileName, out isOpen);
			var parsedDocument = ProjectDomService.GetParsedDocument (type.SourceProjectDom, type.CompilationUnit.FileName);
			
			var insertionPoints = GetInsertionPoints (data, parsedDocument, type);
			
			var suitableInsertionPoint = GetSuitableInsertionPoint (insertionPoints, type, newMembers.First ());
			
			var generator = CreateCodeGenerator (data);
			
			StringBuilder sb = new StringBuilder ();
			foreach (IMember newMember in newMembers) {
				if (sb.Length > 0) {
					sb.AppendLine ();
					sb.AppendLine ();
				}
				sb.Append (generator.CreateMemberImplementation (type, newMember, implementExplicit != null ? implementExplicit (newMember) : false).Code);
			}
			suitableInsertionPoint.Insert (data, string.IsNullOrEmpty (regionName) ? sb.ToString () : generator.WrapInRegions (regionName, sb.ToString ()));
			if (!isOpen) {
				try {
					File.WriteAllText (type.CompilationUnit.FileName, data.Text);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.CompilationUnit.FileName), e);
					MessageService.ShowError (GettextCatalog.GetString ("Failed to write file '{0}'.", type.CompilationUnit.FileName));
				}
			}
		}
		
		static MonoDevelop.Projects.CodeGeneration.CodeGenerator CreateCodeGenerator (TextEditorData data)
		{
			return MonoDevelop.Projects.CodeGeneration.CodeGenerator.CreateGenerator (data.Document.MimeType, 
				data.Options.TabsToSpaces, data.Options.TabSize, data.EolMarker);
		}
		
		protected static IType GetMainPart (IType t)
		{
			return t.HasParts ? t.Parts.First () : t;
		}
		
		#region Insertion Points
		public static List<InsertionPoint> GetInsertionPoints (MonoDevelop.Ide.Gui.Document document, IType type)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return GetInsertionPoints (document.Editor, document.ParsedDocument, type);
		}
		
		public static List<InsertionPoint> GetInsertionPoints (TextEditorData data, ParsedDocument parsedDocument, IType type)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			if (parsedDocument == null)
				throw new ArgumentNullException ("parsedDocument");
			if (type == null)
				throw new ArgumentNullException ("type");
			List<InsertionPoint> result = new List<InsertionPoint> ();
			int offset = data.LocationToOffset (type.BodyRegion.Start.Line, type.BodyRegion.Start.Column);
			if (offset < 0)
				return result;
			
			while (offset < data.Length && data.GetCharAt (offset) != '{') {
				offset++;
			}
			
			var realStartLocation = data.OffsetToLocation (offset);
			result.Add (GetInsertionPosition (data.Document, realStartLocation.Line, realStartLocation.Column));
			result[0].LineBefore = NewLineInsertion.None;
			foreach (IMember member in type.Members) {
				DomLocation domLocation = member.BodyRegion.End;
				if (domLocation.Line <= 0) {
					LineSegment lineSegment = data.GetLine (member.Location.Line);
					if (lineSegment == null)
						continue;
					domLocation = new DomLocation (member.Location.Line, lineSegment.EditableLength + 1);
				}
				result.Add (GetInsertionPosition (data.Document, domLocation.Line, domLocation.Column));
			}
			result[result.Count - 1].LineAfter = NewLineInsertion.None;
			CheckStartPoint (data.Document, result[0], result.Count == 1);
			if (result.Count > 1) {
				result.RemoveAt (result.Count - 1); 
				NewLineInsertion insertLine;
				var lineBefore = data.GetLine (type.BodyRegion.End.Line - 1);
				if (lineBefore != null && lineBefore.EditableLength == lineBefore.GetIndentation (data.Document).Length) {
					insertLine = NewLineInsertion.None;
				} else {
					insertLine = NewLineInsertion.Eol;
				}
				// search for line start
				var line = data.GetLine (type.BodyRegion.End.Line);
				int col = type.BodyRegion.End.Column - 1;
				while (col > 1 && char.IsWhiteSpace (data.GetCharAt (line.Offset + col - 2)))
					col--;
				result.Add (new InsertionPoint (new DocumentLocation (type.BodyRegion.End.Line, col), insertLine, NewLineInsertion.Eol));
				CheckEndPoint (data.Document, result[result.Count - 1], result.Count == 1);
			}
			
			foreach (var region in parsedDocument.UserRegions.Where (r => type.BodyRegion.Contains (r.Region))) {
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.Start.Line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.End.Line, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
				result.Add (new InsertionPoint (new DocumentLocation (region.Region.End.Line + 1, 1), NewLineInsertion.Eol, NewLineInsertion.Eol));
			}
			result.Sort ((left, right) => left.Location.CompareTo (right.Location));
			return result;
		}

		static void CheckEndPoint (Document doc, InsertionPoint point, bool isStartPoint)
		{
			LineSegment line = doc.GetLine (point.Location.Line);
			if (line == null)
				return;
			
			if (doc.GetLineIndent (line).Length + 1 < point.Location.Column)
				point.LineBefore = NewLineInsertion.BlankLine;
			if (point.Location.Column < line.EditableLength + 1)
				point.LineAfter = NewLineInsertion.Eol;
		}
		
		static void CheckStartPoint (Document doc, InsertionPoint point, bool isEndPoint)
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
				point.LineBefore = isEndPoint ? NewLineInsertion.Eol : NewLineInsertion.BlankLine;
			if (point.Location.Column < line.EditableLength + 1)
				point.LineAfter = isEndPoint ? NewLineInsertion.Eol : NewLineInsertion.BlankLine;
		}
		
		static InsertionPoint GetInsertionPosition (Document doc, int line, int column)
		{
			int bodyEndOffset = doc.LocationToOffset (line, column) + 1;
			LineSegment curLine = doc.GetLine (line);
			if (curLine != null) {
				if (bodyEndOffset < curLine.Offset + curLine.EditableLength) {
					System.Console.WriteLine (1);
					// case1: positition is somewhere inside the start line
					return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.BlankLine, NewLineInsertion.BlankLine);
				}
			}
			
			// -> if position is at line end check next line
			LineSegment nextLine = doc.GetLine (line + 1);
			if (nextLine == null) // check for 1 line case.
				return new InsertionPoint (new DocumentLocation (line, column + 1), NewLineInsertion.BlankLine, NewLineInsertion.BlankLine);
			
			for (int i = nextLine.Offset; i < nextLine.EndOffset; i++) {
				char ch = doc.GetCharAt (i);
				if (!char.IsWhiteSpace (ch)) {
					// case2: next line contains non ws chars.
					System.Console.WriteLine (2);
					return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.BlankLine, NewLineInsertion.BlankLine);
				}
			}
			// case3: whitespace line
			return new InsertionPoint (new DocumentLocation (line + 1, 1), NewLineInsertion.BlankLine, NewLineInsertion.None);
		}
		
		static InsertionPoint GetSuitableInsertionPoint (IEnumerable<InsertionPoint> points, IType cls, IMember member)
		{
			var mainPart = GetMainPart (cls);
			switch (member.MemberType) {
			case MemberType.Field:
				return GetNewFieldPosition (points, mainPart);
			case MemberType.Method:
				return GetNewMethodPosition (points, mainPart);
			case MemberType.Event:
				return GetNewEventPosition (points, mainPart);
			case MemberType.Property:
				return GetNewPropertyPosition (points, mainPart);
			}
			throw new InvalidOperationException ("Invalid member type: " + member.MemberType);
		}
		
		static InsertionPoint GetSuitableInsertionPoint (IEnumerable<InsertionPoint> points, IType cls, CodeTypeMember mem)
		{
			var mainPart = GetMainPart (cls);
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
		
		static InsertionPoint GetNewFieldPosition (IEnumerable<InsertionPoint> points, IType cls)
		{
			if (cls.FieldCount == 0) 
				return points.FirstOrDefault ();
			var lastField = cls.Fields.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastField.Location);
		}
		
		static InsertionPoint GetNewMethodPosition (IEnumerable<InsertionPoint> points, IType cls)
		{
			if (cls.MethodCount + cls.ConstructorCount == 0) 
				return GetNewPropertyPosition (points, cls);
			var lastMember = cls.Members.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastMember.Location);
		}
		
		static InsertionPoint GetNewPropertyPosition (IEnumerable<InsertionPoint> points, IType cls)
		{
			if (cls.PropertyCount == 0)
				return GetNewFieldPosition (points, cls);
			IProperty lastProperty = cls.Properties.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastProperty.Location);
		}
		
		static InsertionPoint GetNewEventPosition (IEnumerable<InsertionPoint> points, IType cls)
		{
			if (cls.EventCount == 0)
				return GetNewMethodPosition (points, cls);
			IEvent lastEvent = cls.Events.Last ();
			return points.FirstOrDefault (p => p.Location.Convert () > lastEvent.Location);
		}
		#endregion
	}
	
	public static class HelperMethods
	{
		public static DomLocation Convert (this DocumentLocation location)
		{
			return new DomLocation (location.Line, location.Column);
		}
	}
}
