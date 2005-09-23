// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Gui.Search
{
	internal class CurrentDocumentIterator : IDocumentIterator
	{
		bool didRead = false;
		
		public CurrentDocumentIterator() 
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
		
		public IDocumentInformation Current {
			get {
				if (!SearchReplaceUtilities.IsTextAreaSelected) {
					return null;
				}
				return WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent as IDocumentInformation;
			}
		}
			
		public bool MoveForward() 
		{
			if (!SearchReplaceUtilities.IsTextAreaSelected) {
				return false;
			}
			if (didRead) {
				return false;
			}
			didRead = true;
			
			return true;
		}
		
		public bool MoveBackward()
		{
			return MoveForward();
		}
		
		public void Reset() 
		{
			didRead = false;
		}
	}
}
