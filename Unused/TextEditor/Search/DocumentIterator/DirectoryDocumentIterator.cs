//  DirectoryDocumentIterator.cs
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
using System.Collections.Specialized;
using System.IO;

using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.Core;
using MonoDevelop.Core;
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
				foreach (Document doc in IdeApp.Workbench.Documents) {
					// WINDOWS DEPENDENCY : ToUpper
					if (doc.FileName != null &&
						doc.FileName.ContentName.ToUpper() == fileName.ToUpper()) {
						document = ((ITextEditorControlProvider)content).TextEditorControl.Document;
						return new ProvidedDocumentInformation(document,
						                                       fileName);
					}
				}
				ITextBufferStrategy strategy = null;
				try {
					strategy = StringTextBufferStrategy.CreateTextBufferFromFile(fileName);
				} catch (Exception) {
					TaskService taskService = (TaskService)MonoDevelop.Core.ServiceManager.Services.GetService(typeof(TaskService));
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
