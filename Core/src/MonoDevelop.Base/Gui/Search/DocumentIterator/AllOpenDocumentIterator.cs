// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class AllOpenDocumentIterator : IDocumentIterator
	{
		int  startIndex = -1;
		bool resetted    = true;
		
		public AllOpenDocumentIterator()
		{
			Reset();
		}
		
		public string CurrentFileName {
			get {
				if (!SearchReplaceUtilities.IsTextAreaSelected) {
					return null;
				}
				
				if (IdeApp.Workbench.ActiveDocument.FileName == null) {
					return IdeApp.Workbench.ActiveDocument.Window.ViewContent.UntitledName;
				}
				
				return IdeApp.Workbench.ActiveDocument.FileName;
			}
		}
		
		public IDocumentInformation Current {
			get {
				if (!SearchReplaceUtilities.IsTextAreaSelected) {
					return null;
				}
				return IdeApp.Workbench.ActiveDocument.Window.ViewContent as IDocumentInformation;
			}
		}
		
		int GetCurIndex()
		{
			for (int i = 0; i < IdeApp.Workbench.Documents.Count; ++i) {
				if (IdeApp.Workbench.ActiveDocument == IdeApp.Workbench.Documents [i]) {
					return i;
				}
			}
			return -1;
		}
		
		public bool MoveForward() 
		{
			int curIndex =  GetCurIndex();
			if (curIndex < 0) {
				return false;
			}
			
			if (resetted) {
				resetted = false;
				return true;
			}
			
			int nextIndex = (curIndex + 1) % IdeApp.Workbench.Documents.Count;
			if (nextIndex == startIndex) {
				return false;
			}
			IdeApp.Workbench.Documents [nextIndex].Select ();
			return true;
		}
		
		public bool MoveBackward()
		{
			int curIndex =  GetCurIndex();
			if (curIndex < 0) {
				return false;
			}
			if (resetted) {
				resetted = false;
				return true;
			}
			
			if (curIndex == 0)
				curIndex = IdeApp.Workbench.Documents.Count - 1;
			else
				curIndex--;
			
			if (curIndex == startIndex)
				return false;

			IdeApp.Workbench.Documents [curIndex].Select ();
			return true;
		}
		
		public void Reset() 
		{
			startIndex = GetCurIndex();
			resetted = true;
		}
	}
}
