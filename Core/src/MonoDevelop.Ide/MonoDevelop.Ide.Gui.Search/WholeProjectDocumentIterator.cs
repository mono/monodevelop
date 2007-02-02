// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class WholeProjectDocumentIterator : IDocumentIterator
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
				
				return files[curIndex].ToString();
			}
		}
				
		public IDocumentInformation Current {
			get {
				if (curIndex < 0 || curIndex >= files.Count) {
					return null;
				}
				if (!File.Exists(files[curIndex].ToString())) {
					++curIndex;
					return Current;
				}

				string fileName = files[curIndex].ToString();
				foreach (Document document in IdeApp.Workbench.Documents) {
					// WINDOWS DEPENDENCY : ToUpper
					if (document.FileName != null && document.FileName.ToUpper() == fileName.ToUpper()) {
						IDocumentInformation doc = document.Window.ViewContent as IDocumentInformation;
						if (doc != null) return doc;
					}
				}
				return new FileDocumentInformation (fileName, 0);
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
		
		
		void AddFiles(Project project)
		{
			foreach (ProjectFile file in project.ProjectFiles) {
				if (file.Subtype == Subtype.Code) {
					files.Add(file.Name);
				}
			}
		}
		
		void AddFiles(Combine combine)
		{
			foreach (CombineEntry entry in combine.Entries) {
				if (entry is Project) {
					AddFiles ((Project)entry);
				} else if (entry is Combine) {
					AddFiles ((Combine)entry);
				}
			}
		}
		
		public void Reset() 
		{
			files.Clear();
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				AddFiles (IdeApp.ProjectOperations.CurrentOpenCombine);
			}
			
			curIndex = -1;
		}
	}
}
