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
			if (type == null || File.Exists (GetCorrectFileName (type)))
				return false;
			return Path.GetFileNameWithoutExtension (type.CompilationUnit.FileName) != type.Name;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			IType type = options.SelectedItem as IType;
			if (type.CompilationUnit.Types.Count == 1)
				return String.Format (GettextCatalog.GetString ("_Rename file to '{0}'"), type.Name + Path.GetExtension (type.CompilationUnit.FileName));
			return String.Format (GettextCatalog.GetString ("_Move type to file '{0}'"), type.Name + Path.GetExtension (type.CompilationUnit.FileName));
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
					content.Append (StandardHeaderService.GetHeader (options.Dom.Project, ((DotNetProject)options.Dom.Project).LanguageName, type.CompilationUnit.FileName, true) + Environment.NewLine);
				INRefactoryASTProvider provider = options.GetASTProvider ();
				Mono.TextEditor.TextEditorData data = options.GetTextEditorData ();
				ICSharpCode.NRefactory.Ast.CompilationUnit unit = provider.ParseFile (options.Document.TextEditor.Text);
				TypeFilterTransformer typeFilterTransformer = new TypeFilterTransformer (type.Name);
				unit.AcceptVisitor (typeFilterTransformer, null);

				content.Append (provider.OutputNode (options.Dom, unit));
				result.Add (new CreateFileChange (newName, content.ToString ()));
				TextReplaceChange removeDeclaration = new TextReplaceChange ();
				removeDeclaration.Description = "Remove type declaration";
				removeDeclaration.FileName = type.CompilationUnit.FileName;
				removeDeclaration.Offset = data.Document.LocationToOffset (typeFilterTransformer.TypeDeclaration.StartLocation.Line - 1, typeFilterTransformer.TypeDeclaration.StartLocation.Column - 1);
				removeDeclaration.RemovedChars = data.Document.LocationToOffset (typeFilterTransformer.TypeDeclaration.EndLocation.Line - 1, typeFilterTransformer.TypeDeclaration.EndLocation.Column) - removeDeclaration.Offset;
				result.Add (removeDeclaration);

			}
			
			return result;
		}

		static string GetCorrectFileName (MonoDevelop.Projects.Dom.IType type)
		{
			return Path.Combine (Path.GetDirectoryName (type.CompilationUnit.FileName), type.Name + Path.GetExtension (type.CompilationUnit.FileName));
		}
	}
}
