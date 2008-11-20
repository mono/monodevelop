//  AllOpenDocumentIterator.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class AllOpenDocumentIterator : GuiSyncObject, IDocumentIterator
	{
		int  startIndex = -1;
		bool resetted    = true;
		
		public AllOpenDocumentIterator()
		{
			Reset();
		}
		
		public string GetSearchDescription (string pattern)
		{
			return MonoDevelop.Core.GettextCatalog.GetString ("Looking for '{0}' in all open documents.", pattern);
		}
		
		public string GetReplaceDescription (string pattern)
		{
			return MonoDevelop.Core.GettextCatalog.GetString ("Replacing '{0}' in all open documents.", pattern);
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
			do {
				int curIndex =  GetCurIndex();
				if (curIndex < 0) {
					return false;
				}
				
				if (resetted) {
					resetted = false;
					continue;
				}
				
				int nextIndex = (curIndex + 1) % IdeApp.Workbench.Documents.Count;
				if (nextIndex == startIndex) {
					return false;
				}
				IdeApp.Workbench.Documents [nextIndex].Select ();
			} 
			while (Current == null);
			
			return true;
		}
		
		public bool MoveBackward()
		{
			do {
				int curIndex =  GetCurIndex();
				if (curIndex < 0) {
					return false;
				}
				if (resetted) {
					resetted = false;
					continue;
				}
				
				if (curIndex == 0)
					curIndex = IdeApp.Workbench.Documents.Count - 1;
				else
					curIndex--;
				
				if (curIndex == startIndex)
					return false;

				IdeApp.Workbench.Documents [curIndex].Select ();
			}
			while (Current == null);
			
			return true;
		}
		
		public void Reset() 
		{
			startIndex = GetCurIndex();
			resetted = true;
		}
	}
}
