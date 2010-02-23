// 
// MoveTypeToFileRefactoring.cs
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
using System.IO;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System.Text;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects;
using MonoDevelop.Ide.StandardHeader;


namespace MonoDevelop.Refactoring.MoveTypeToFile
{
	public class MoveTypeToFileRefactoring : RefactoringOperation
	{

		public override bool IsValid (RefactoringOptions options)
		{
			IType type = options.SelectedItem as IType;
			string fileName = GetCorrectFileName (type);
			if (type == null || string.IsNullOrEmpty (fileName) || File.Exists (fileName) || type.DeclaringType != null)
				return false;
			return Path.GetFileNameWithoutExtension (type.CompilationUnit.FileName) != type.Name;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			IType type = options.SelectedItem as IType;
			if (type.CompilationUnit.Types.Count == 1)
				return String.Format (GettextCatalog.GetString ("_Rename file to '{0}'"), Path.GetFileName (GetCorrectFileName (type)));
			return String.Format (GettextCatalog.GetString ("_Move type to file '{0}'"), Path.GetFileName (GetCorrectFileName (type)));
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			List<Change> result = new List<Change> ();
			IType type = options.SelectedItem as IType;
			if (type == null)
				return result;
			string newName = GetCorrectFileName (type);
			if (type.CompilationUnit.Types.Count == 1) {
				result.Add (new RenameFileChange (type.CompilationUnit.FileName, newName));
			} else {
				StringBuilder content = new StringBuilder ();
				
				if (options.Dom.Project is DotNetProject)
					content.Append (StandardHeaderService.GetHeader (options.Dom.Project, type.CompilationUnit.FileName, true) + Environment.NewLine);
				
				INRefactoryASTProvider provider = options.GetASTProvider ();
				Mono.TextEditor.TextEditorData data = options.GetTextEditorData ();
				ICSharpCode.NRefactory.Ast.CompilationUnit unit = provider.ParseFile (options.Document.TextEditor.Text);
				
				TypeFilterTransformer typeFilterTransformer = new TypeFilterTransformer ((type is InstantiatedType) ? ((InstantiatedType)type).UninstantiatedType.DecoratedFullName : type.DecoratedFullName);
				unit.AcceptVisitor (typeFilterTransformer, null);
				if (typeFilterTransformer.TypeDeclaration == null)
					return result;
				Mono.TextEditor.Document generatedDocument = new Mono.TextEditor.Document ();
				generatedDocument.Text = provider.OutputNode (options.Dom, unit);
				
				int startLine = -1;
				for (int i = typeFilterTransformer.TypeDeclaration.StartLocation.Line - 2; i >= 0; i--) {
					string lineText = data.Document.GetTextAt (data.Document.GetLine (i)).Trim ();
					if (string.IsNullOrEmpty (lineText))
						continue;
					if (lineText.StartsWith ("///")) {
						startLine = i;
					} else {
						break;
					}
				}
				
				int start;
				if (startLine >= 0) {
					start = data.Document.GetLine (startLine).Offset;
				} else {
					start = data.Document.LocationToOffset (typeFilterTransformer.TypeDeclaration.StartLocation.Line - 1, typeFilterTransformer.TypeDeclaration.StartLocation.Column - 1);
				}
				int length = data.Document.LocationToOffset (typeFilterTransformer.TypeDeclaration.EndLocation.Line - 1, typeFilterTransformer.TypeDeclaration.EndLocation.Column) - start;
				
				ICSharpCode.NRefactory.Ast.CompilationUnit generatedCompilationUnit = provider.ParseFile (generatedDocument.Text);
				TypeSearchVisitor typeSearchVisitor = new TypeSearchVisitor ();
				generatedCompilationUnit.AcceptVisitor (typeSearchVisitor, null);
					
				int genStart = generatedDocument.LocationToOffset (typeSearchVisitor.Types[0].StartLocation.Line - 1, 0);
				int genEnd   = generatedDocument.LocationToOffset (typeSearchVisitor.Types[0].EndLocation.Line - 1, typeSearchVisitor.Types[0].EndLocation.Column - 1);
				((Mono.TextEditor.IBuffer)generatedDocument).Replace (genStart, genEnd - genStart, data.Document.GetTextAt (start, length));
				content.Append (generatedDocument.Text);
				
				result.Add (new CreateFileChange (newName, content.ToString ()));
				
				TextReplaceChange removeDeclaration = new TextReplaceChange ();
				removeDeclaration.Description = "Remove type declaration";
				removeDeclaration.FileName = type.CompilationUnit.FileName;
				removeDeclaration.Offset = start;
				removeDeclaration.RemovedChars = length;
				result.Add (removeDeclaration);
			}
			result.Add (new SaveProjectChange (options.Document.Project));
			
			return result;
		}

		static string GetCorrectFileName (MonoDevelop.Projects.Dom.IType type)
		{
			if (type == null || type.CompilationUnit == null || type.SourceProject == null || string.IsNullOrEmpty (type.CompilationUnit.FileName))
				return null;
			if (type is InstantiatedType)
				type = ((InstantiatedType)type).UninstantiatedType;
			return Path.Combine (Path.GetDirectoryName (type.CompilationUnit.FileName), type.Name + Path.GetExtension (type.CompilationUnit.FileName));
		}
	}
}
