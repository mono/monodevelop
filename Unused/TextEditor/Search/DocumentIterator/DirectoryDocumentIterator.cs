// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.TextEditor;

namespace MonoDevelop.TextEditor.Document
{
	public class DirectoryDocumentIterator : IDocumentIterator
	{
		string searchDirectory;
		string fileMask;
		bool   searchSubdirectories;
		
		StringCollection files    = null;
		int              curIndex = -1;
		
		public DirectoryDocumentIterator(string searchDirectory, string fileMask, bool searchSubdirectories)
		{
			this.searchDirectory      = searchDirectory;
			this.fileMask             = fileMask;
			this.searchSubdirectories = searchSubdirectories;
			
			Reset();
		}
		
		public string CurrentFileName {
			get {
				if (curIndex < 0 || curIndex >= files.Count) {
					return null;
				}
				
				return files[curIndex].ToString();;
			}
		}
				
		public ProvidedDocumentInformation Current {
			get {
				if (curIndex < 0 || curIndex >= files.Count) {
					return null;
				}
				if (!File.Exists(files[curIndex].ToString())) {
					++curIndex;
					return Current;
				}
				IDocument document;
				string fileName = files[curIndex].ToString();
				foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
					// WINDOWS DEPENDENCY : ToUpper
					if (content.ContentName != null &&
						content.ContentName.ToUpper() == fileName.ToUpper()) {
						document = ((ITextEditorControlProvider)content).TextEditorControl.Document;
						return new ProvidedDocumentInformation(document,
						                                       fileName);
					}
				}
				ITextBufferStrategy strategy = null;
				try {
					strategy = StringTextBufferStrategy.CreateTextBufferFromFile(fileName);
				} catch (Exception) {
					TaskService taskService = (TaskService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(TaskService));
					taskService.Tasks.Add(new Task(String.Empty, "can't access " + fileName, -1, -1));
					return null;
				}
				return new ProvidedDocumentInformation(strategy, 
				                                       fileName, 
				                                       0);
			}
		}
		
		public bool MoveForward() 
		{
			if (curIndex == -1) {
				FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
				files = fileUtilityService.SearchDirectory(this.searchDirectory, this.fileMask, this.searchSubdirectories);
			}
			return ++curIndex < files.Count;
		}
		
		public bool MoveBackward()
		{
			if (curIndex == -1) {
				curIndex = files.Count - 1;
				return true;
			}
			return --curIndex >= -1;
		}
		
		
		public void Reset() 
		{
			curIndex = -1;
		}
	}
}
