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
using MonoDevelop.CodeGeneration;

namespace MonoDevelop.Refactoring
{
	public static class RefactoringService
	{
		static List<RefactoringOperation> refactorings = new List<RefactoringOperation>();
		static List<INRefactoryASTProvider> astProviders = new List<INRefactoryASTProvider>();
		static List<ICodeGenerator> codeGenerators = new List<ICodeGenerator>();
		
		static RefactoringService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/Refactorings", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					refactorings.Add ((RefactoringOperation)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					refactorings.Remove ((RefactoringOperation)args.ExtensionObject);
					break;
				}
			});

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/ASTProvider", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					astProviders.Add ((INRefactoryASTProvider)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					astProviders.Remove ((INRefactoryASTProvider)args.ExtensionObject);
					break;
				}
			});

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeGenerators", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					codeGenerators.Add ((ICodeGenerator)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					codeGenerators.Remove ((ICodeGenerator)args.ExtensionObject);
					break;
				}
			});
		}
		
		public static IEnumerable<RefactoringOperation> Refactorings {
			get {
				return refactorings;
			}
		}
		
		public static IEnumerable<ICodeGenerator> CodeGenerators {
			get {
				return codeGenerators;
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
					TextReplaceChange replaceChange = change as TextReplaceChange;
					if (replaceChange == null)
						continue;
					if (args.SourceFile == replaceChange.FileName)
						replaceChange.FileName = args.TargetFile;
				}
			}
		}
		
		public static void AcceptChanges (IProgressMonitor monitor, ProjectDom dom, List<Change> changes)
		{
			AcceptChanges (monitor, dom, changes, MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance);
		}
		
		public static void AcceptChanges (IProgressMonitor monitor, ProjectDom dom, List<Change> changes, MonoDevelop.Projects.Text.ITextFileProvider fileProvider)
		{
			RefactorerContext rctx = new RefactorerContext (dom, fileProvider, null);
			RenameHandler handler = new RenameHandler (changes);
			FileService.FileRenamed += handler.FileRename;
			for (int i = 0; i < changes.Count; i++) {
				changes[i].PerformChange (monitor, rctx);
				TextReplaceChange replaceChange = changes[i] as TextReplaceChange;
				if (replaceChange == null)
					continue;
				for (int j = i + 1; j < changes.Count; j++) {
					TextReplaceChange change = changes[j] as TextReplaceChange;
					if (change == null)
						continue;
					if (replaceChange.Offset >= 0 && change.Offset >= 0 && replaceChange.FileName == change.FileName && replaceChange.Offset < change.Offset) {
						change.Offset -= replaceChange.RemovedChars;
						if (!string.IsNullOrEmpty (replaceChange.InsertedText))
							change.Offset += replaceChange.InsertedText.Length;
					}
				}
			}
			FileService.FileRenamed -= handler.FileRename;
		}
		
		public static INRefactoryASTProvider GetASTProvider (string mimeType)
		{
			foreach (INRefactoryASTProvider provider in astProviders) {
				if (provider.CanGenerateASTFrom (mimeType)) {
					return provider;
				}
			}
			return null;
		}
	}
}
