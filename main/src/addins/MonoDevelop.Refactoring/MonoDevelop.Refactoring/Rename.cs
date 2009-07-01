// 
// Rename.cs
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
using MonoDevelop.Projects.Dom.Refactoring;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Refactoring
{
	public class Rename : MonoDevelop.Projects.Dom.Refactoring.Refactoring
	{
		public Rename ()
		{
			Name = "Rename";
		}
		
		public override bool IsValid (ProjectDom dom, IDomVisitable item)
		{
			if (item is LocalVariable || item is IParameter)
				return true;

			if (item is IType)
				return ((IType)item).SourceProject != null;

			if (item is IMember) {
				IType cls = ((IMember)item).DeclaringType;
				return cls != null && cls.SourceProject != null;
			}
			return false;
		}
		
		public override string GetMenuDescription (ProjectDom dom, IDomVisitable item)
		{
			return GettextCatalog.GetString ("_Rename");
		}
		
		public override void Run (ProjectDom dom, IDomVisitable item)
		{
			RenameItemDialog dialog = new RenameItemDialog (dom, item, this);
			dialog.Show ();
		}
		
		public class RenameProperties
		{
			public string NewName {
				get;
				set;
			}
			
			public bool RenameFile {
				get;
				set;
			}
		}
		
		public override List<Change> PerformChanges (ProjectDom dom, IDomVisitable item, object prop)
		{
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Name, null);
			MemberReferenceCollection col = null;

			RenameProperties properties = (RenameProperties)prop;
			List<Change> result = new List<Change> ();
			if (item is IType) {
				IType cls = (IType)item;

				if (properties.RenameFile) {
					Console.WriteLine ("RENAME FILE !!");
					if (cls.IsPublic) {
						foreach (IType part in cls.Parts) {
							Console.WriteLine ("part: " + part);
							Console.WriteLine (System.IO.Path.GetFileNameWithoutExtension (part.CompilationUnit.FileName)+ "/" + cls.Name);
							if (System.IO.Path.GetFileNameWithoutExtension (part.CompilationUnit.FileName) == cls.Name) {
								string newFileName = System.IO.Path.HasExtension (part.CompilationUnit.FileName) ? properties.NewName + System.IO.Path.GetExtension (part.CompilationUnit.FileName) : properties.NewName;
								newFileName = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (part.CompilationUnit.FileName), newFileName);
								result.Add (new RenameFileChange (part.CompilationUnit.FileName, newFileName));
							}
						}
					}
				}

				col = refactorer.FindClassReferences (monitor, cls, RefactoryScope.Solution);
			} else if (item is IMember) {
				IMember member = (IMember)item;
				col = refactorer.FindMemberReferences (monitor, member.DeclaringType, member, RefactoryScope.Solution);
			} else if (item is LocalVariable) {
				col = refactorer.FindVariableReferences (monitor, (LocalVariable)item);
			} else if (item is IParameter) {
				col = refactorer.FindParameterReferences (monitor, (IParameter)item);
			} else {
				return null;
			}

			foreach (MemberReference memberRef in col) {
				Change change = new Change ();
				change.FileName = memberRef.FileName;
				change.Location = new DomLocation (memberRef.Line, memberRef.Column);
				change.RemovedChars = memberRef.Name.Length;
				change.InsertedText = properties.NewName;
				change.Description = string.Format ("Replace '{0}' with '{1}'", memberRef.Name, properties.NewName);
				result.Add (change);
			}
			return result;
		}
	}
}
