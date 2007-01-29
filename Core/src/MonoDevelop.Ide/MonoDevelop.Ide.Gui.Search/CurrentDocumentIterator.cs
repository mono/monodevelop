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
				return IdeApp.Workbench.ActiveDocument.GetContent <IDocumentInformation> ();
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
