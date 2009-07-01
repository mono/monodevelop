// 
// RefactoringService.cs
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
using Mono.Addins;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.Gui;


namespace MonoDevelop.Refactoring
{
	public static class RefactoringService
	{
		static List<Refactoring> refactorings = new List<Refactoring>();
		
		static RefactoringService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/Refactorings", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					refactorings.Add ((Refactoring)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					refactorings.Remove ((Refactoring)args.ExtensionObject);
					break;
				}
			});
		}
		
		public static IEnumerable<Refactoring> Refactorings {
			get {
				return refactorings;
			}
		}
		
		class RenameHandler 
		{
			IEnumerable<Change> changes;
			public RenameHandler (IEnumerable<Change> changes)
			{
				this.changes = changes;
			}
			public void FileRename (object sender, FileCopyEventArgs args)
			{
				foreach (Change change in changes) {
					if (change is RenameFileChange)
						continue;
					if (args.SourceFile == change.FileName)
						change.FileName = args.TargetFile;
				}
			}
		}
		
		public static void AcceptChanges (IProgressMonitor monitor, ProjectDom dom, IEnumerable<Change> changes)
		{
			RefactorerContext rctx = new RefactorerContext (dom, MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance, null);
			RenameHandler handler = new RenameHandler (changes);
			FileService.FileRenamed += handler.FileRename;
			foreach (Change change in changes) {
				change.PerformChange (monitor, rctx);
			}
			FileService.FileRenamed -= handler.FileRename;
		}
	}
}
