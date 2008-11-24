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

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class DirectoryDocumentIterator : IDocumentIterator
	{
		string searchDirectory;
		string fileMask;
		bool searchSubdirectories;
		
		StringCollection files = new StringCollection ();
		Queue directories = new Queue ();
		int curIndex = -1;
		
		public DirectoryDocumentIterator(string searchDirectory, string fileMask, bool searchSubdirectories)
		{
			this.searchDirectory      = searchDirectory;
			this.fileMask             = fileMask;
			this.searchSubdirectories = searchSubdirectories;
			directories.Enqueue (searchDirectory);
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
				return new FileDocumentInformation(fileName, 0);
			}
		}
		
		public bool MoveForward() 
		{
			curIndex++;
			if (curIndex >= files.Count)
				return FetchDirectories ();
			else
				return true;
		}
		
		bool FetchDirectories ()
		{
			if (directories.Count == 0)
				return false;

			string dir = (string) directories.Dequeue ();
			
			string[] dirFiles = Directory.GetFiles (dir, fileMask);
			for (int n = 0; n < dirFiles.Length; n++) {
				files.Add (dirFiles [n]);
			}
			
			if (searchSubdirectories) {
				string[] dirDirs = Directory.GetDirectories (dir);
				for (int n = 0; n < dirDirs.Length; n++)
					directories.Enqueue (dirDirs [n]);
			}
			
			if (dirFiles.Length == 0)
				return FetchDirectories ();
			else
				return true;
		}
		
		public bool MoveBackward()
		{
			if (curIndex == -1) {
				while (FetchDirectories ());	// Fetch all
				curIndex = files.Count - 1;
				return true;
			}
			return --curIndex >= -1;
		}
		
		
		public void Reset() 
		{
			curIndex = -1;
		}
		
		public string GetSearchDescription (string pattern)
		{
			return GettextCatalog.GetString ("Looking for '{0}' in directory '{1}'", pattern, searchDirectory);
		}
		
		public string GetReplaceDescription (string pattern)
		{
			return GettextCatalog.GetString ("Replacing '{0}' in directory '{1}'", pattern, searchDirectory);
		}
	}
}
