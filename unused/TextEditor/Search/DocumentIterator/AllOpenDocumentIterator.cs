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

using MonoDevelop.Core.Gui;
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
				
				if (IdeApp.Workbench.ActiveDocument.FileName == null) {
					return IdeApp.Workbench.ActiveDocument.Window.ViewContent.UntitledName;
				}
				
				return IdeApp.Workbench.ActiveDocument.FileName;
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
			
			if (curIndex == 0) {
				curIndex = IdeApp.Workbench.Documents.Count - 1;
			}
			
			if (curIndex > 0) {
				--curIndex;
				IdeApp.Workbench.Documents [curIndex].Select ();
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
