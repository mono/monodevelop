// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Gui;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor;

namespace MonoDevelop.TextEditor.Document
{
	public class AllOpenDocumentIterator : IDocumentIterator
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
				
				if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName == null) {
					return WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.UntitledName;
				}
				
				return WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName;
			}
		}
		
		public ProvidedDocumentInformation Current {
			get {
				if (!SearchReplaceUtilities.IsTextAreaSelected) {
					return null;
				}
				IDocument document = (((ITextEditorControlProvider)WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent).TextEditorControl).Document;
				return new ProvidedDocumentInformation(document,
				                                       CurrentFileName);
			}
		}
		
		int GetCurIndex()
		{
			for (int i = 0; i < WorkbenchSingleton.Workbench.ViewContentCollection.Count; ++i) {
				if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent == WorkbenchSingleton.Workbench.ViewContentCollection[i]) {
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
			
			int nextIndex = (curIndex + 1) % WorkbenchSingleton.Workbench.ViewContentCollection.Count;
			if (nextIndex == startIndex) {
				return false;
			}
			WorkbenchSingleton.Workbench.ViewContentCollection[nextIndex].WorkbenchWindow.SelectWindow();
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
			
			if (curIndex == 0) {
				curIndex = WorkbenchSingleton.Workbench.ViewContentCollection.Count - 1;
			}
			
			if (curIndex > 0) {
				--curIndex;
				WorkbenchSingleton.Workbench.ViewContentCollection[curIndex].WorkbenchWindow.SelectWindow();
				return true;
			}
			return false;
		}
		
		public void Reset() 
		{
			startIndex = GetCurIndex();
			resetted = true;
		}
	}
}
