// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.TextEditor;

namespace MonoDevelop.TextEditor.Document
{
	public class WholeProjectDocumentIterator : IDocumentIterator
	{
		ArrayList files    = new ArrayList();
		int       curIndex = -1;
		
		public WholeProjectDocumentIterator()
		{
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
						document = (((ITextEditorControlProvider)content).TextEditorControl).Document;
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
		
		
		void AddFiles(IProject project)
		{
			foreach (ProjectFile file in project.ProjectFiles) {
				if (file.BuildAction == BuildAction.Compile &&
				    file.Subtype     == Subtype.Code) {
					files.Add(file.Name);
				}
			}
		}
		
		void AddFiles(Combine combine)
		{
			foreach (CombineEntry entry in combine.Entries) {
				if (entry is ProjectCombineEntry) {
					AddFiles(((ProjectCombineEntry)entry).Project);
				} else {
					AddFiles(((CombineCombineEntry)entry).Combine);
				}
			}
		}
		
		public void Reset() 
		{
			files.Clear();
			IProjectService projectService = (IProjectService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			if (projectService.CurrentOpenCombine != null) {
				AddFiles(projectService.CurrentOpenCombine);
			}
			
			curIndex = -1;
		}
	}
}
