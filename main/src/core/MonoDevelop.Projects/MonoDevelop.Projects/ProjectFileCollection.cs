// ProjectFileCollection.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	[Serializable()]
	public class ProjectFileCollection : ProjectItemCollection<ProjectFile> {
	
		public ProjectFileCollection ()
		{
		}
		
		public ProjectFile GetFile (string fileName)
		{
			if (fileName == null) return null;
			fileName = FileService.GetFullPath (fileName);
			
			foreach (ProjectFile file in this) {
				if (file.Name == fileName)
					return file;
			}
			return null;
		}
		
		public ProjectFile[] GetFilesInPath (string path)
		{
			path = path + Path.DirectorySeparatorChar;
			
			List<ProjectFile> list = new List<ProjectFile> ();
			foreach (ProjectFile file in Items) {
				if ((file.Name + Path.DirectorySeparatorChar).StartsWith (path))
					list.Add (file);
			}
			return list.ToArray ();
		}
		
		public void Remove (string fileName)
		{
			fileName = FileService.GetFullPath (fileName);
			for (int n=0; n<Count; n++) {
				if (Items [n].Name == fileName)
					RemoveAt (n);
			}
		}
	}
}
