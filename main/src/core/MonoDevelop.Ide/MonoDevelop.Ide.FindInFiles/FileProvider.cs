// 
// FileProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using System.Text;


namespace MonoDevelop.Ide.FindInFiles
{
	public class FileProvider
	{
		public string FileName {
			get;
			set;
		}
		
		public Project Project {
			get;
			set;
		}
		
		public FileProvider (string fileName) : this (fileName, null)
		{
		}
		
		public FileProvider (string fileName, Project project)
		{
			this.FileName = fileName;
			this.Project  = project;
		}
		
		string content = null;
		public virtual TextReader Open ()
		{
			if (buffer != null)
				return new StringReader (buffer.ToString ());
			Document doc = SearchDocument ();
			if (doc != null) 
				return new StringReader (doc.TextEditor.Text);
			return new StreamReader (FileName);
		}
		
		Document SearchDocument ()
		{
			foreach (Document document in IdeApp.Workbench.Documents) {
				if (string.IsNullOrEmpty (document.FileName))
					continue;
				if (Path.GetFullPath (document.FileName) == Path.GetFullPath (FileName)) 
					return document;
			}
			return null;
		}
		
		Document document;
		StringBuilder buffer = null;
		public void BeginReplace ()
		{
			TextReader reader = Open ();
			buffer = new StringBuilder (reader.ReadToEnd ());
			reader.Close ();
			this.document = SearchDocument ();
			if (this.document != null) {
				Gtk.Application.Invoke (delegate {
					document.TextEditor.BeginAtomicUndo ();
				});
				return;
			}
		}
		
		public void Replace (int offset, int length, string replacement)
		{
			buffer.Remove (offset, length);
			buffer.Insert (offset, replacement);
			if (this.document != null) {
				Gtk.Application.Invoke (delegate {
					document.TextEditor.DeleteText (offset, length);
					document.TextEditor.InsertText (offset, replacement);
				});
				return;
			}
		}
		
		public void EndReplace ()
		{
			if (this.document != null) {
				Gtk.Application.Invoke (delegate {
					document.TextEditor.EndAtomicUndo ();
					document = null;
					buffer = null;
				});
				return;
			}
			object attributes = IdeApp.Services.PlatformService.GetFileAttributes (FileName);
			File.WriteAllText (FileName, buffer.ToString ());
			IdeApp.Services.PlatformService.SetFileAttributes (FileName, attributes);
			
			buffer = null;
		}
	}
}
