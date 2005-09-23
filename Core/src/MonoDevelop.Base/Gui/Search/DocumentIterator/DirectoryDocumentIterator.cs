// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Search
{
	internal class DirectoryDocumentIterator : IDocumentIterator
	{
		string fileMask;
		bool searchSubdirectories;
		
		StringCollection files = new StringCollection ();
		Queue directories = new Queue ();
		int curIndex = -1;
		
		public DirectoryDocumentIterator(string searchDirectory, string fileMask, bool searchSubdirectories)
		{
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
	}
}
