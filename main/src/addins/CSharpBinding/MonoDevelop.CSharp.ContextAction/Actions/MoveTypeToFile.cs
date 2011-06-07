// 
// MoveTypeToFile.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Refactoring;
using System.IO;
using System.Text;
using MonoDevelop.Ide.StandardHeader;

namespace MonoDevelop.CSharp.ContextAction
{
	public class MoveTypeToFile : MDRefactoringContextAction
	{
		protected override string GetMenuText (MDRefactoringContext context)
		{
			var type = GetTypeDeclaration (context);
			if (IsSingleType (context))
				return String.Format (GettextCatalog.GetString ("_Rename file to '{0}'"), Path.GetFileName (GetCorrectFileName (context, type)));
			return String.Format (GettextCatalog.GetString ("_Move type to file '{0}'"), Path.GetFileName (GetCorrectFileName (context, type)));
		}
		
		protected override bool IsValid (MDRefactoringContext context)
		{
			var type = GetTypeDeclaration (context);
			if (type == null)
				return false;
			return Path.GetFileNameWithoutExtension (context.Document.FileName) != type.Name;
		}
		
		protected override void Run (MDRefactoringContext context)
		{
			var type = GetTypeDeclaration (context);
			string correctFileName = GetCorrectFileName (context, type);
			if (IsSingleType (context)) {
				FileService.RenameFile (context.Document.FileName, correctFileName);
//				context.Do (new RenameFileChange ());
				return;
			}
			
			CreateNewFile (context, type, correctFileName);
			using (var script = context.StartScript ()) {
				script.Remove (type);
			}
		}
		
		void CreateNewFile (MDRefactoringContext context, TypeDeclaration type, string correctFileName)
		{
			var content = context.Document.Editor.Text;
			
			var types = new List<TypeDeclaration> (context.Unit.GetTypes ().Where (t => t != type));
			types.Sort ((x, y) => y.StartLocation.CompareTo (x.StartLocation));
			
			foreach (var removeType in types) {
				var start = context.GetOffset (removeType.StartLocation);
				var end = context.GetOffset (removeType.EndLocation);
				content = content.Remove (start, end - start);
			}
			
			if (context.Document.Project is MonoDevelop.Projects.DotNetProject) {
				string header = StandardHeaderService.GetHeader (context.Document.Project, correctFileName, true);
				if (!string.IsNullOrEmpty (header))
					content = header + context.Document.Editor.EolMarker + StripHeader (content);
			}
			content = StripDoubleBlankLines (content);
			
			File.WriteAllText (correctFileName, content);
			context.Document.Project.AddFile (correctFileName);
			MonoDevelop.Ide.IdeApp.ProjectOperations.Save (context.Document.Project);
		}

		static bool IsBlankLine (Document doc, int i)
		{
			var line = doc.GetLine (i);
			return line.EditableLength == line.GetIndentation (doc).Length;
		}

		static string StripDoubleBlankLines (string content)
		{
			var doc = new Mono.TextEditor.Document (content);
			for (int i = 1; i + 1 <= doc.LineCount; i++) {
				if (IsBlankLine (doc, i) && IsBlankLine (doc, i + 1)) {
					((IBuffer)doc).Remove (doc.GetLine (i));
					i--;
					continue;
				}
			}
			return doc.Text;
		}

		static string StripHeader (string content)
		{
			var doc = new Mono.TextEditor.Document (content);
			while (true) {
				string lineText = doc.GetLineText (1);
				if (lineText == null)
					break;
				if (lineText.StartsWith ("//")) {
					((IBuffer)doc).Remove (doc.GetLine (1));
					continue;
				}
				break;
			}
			return doc.Text;
		}
		
		bool IsSingleType (MDRefactoringContext context)
		{
			return context.Unit.GetTypes ().Count () == 1;
		}
		
		TypeDeclaration GetTypeDeclaration (MDRefactoringContext context)
		{
			var result = context.GetNode<TypeDeclaration> ();
			if (result == null || result.Parent is TypeDeclaration)
				return null;
			if (result != null && result.NameToken.Contains (context.Location.Line, context.Location.Column))
				return result;
			return null;
		}
		
		string GetCorrectFileName (MDRefactoringContext context, TypeDeclaration type)
		{
			return Path.Combine (Path.GetDirectoryName (context.Document.FileName), type.Name + Path.GetExtension (context.Document.FileName));
		}
	}
}

