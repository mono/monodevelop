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

using System.IO;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using System.Text;
using MonoDevelop.Core;
using System;
using MonoDevelop.Core.Text;
using System.Threading.Tasks;

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

		public int SelectionStartPosition
		{
			get;
			set;
		}

		public int SelectionEndPosition
		{
			get;
			set;
		}

		public FileProvider(string fileName) : this(fileName, null)
		{
		}

		public FileProvider(string fileName, Project project) : this(fileName, project, -1, -1)
		{
		}

		public FileProvider(string fileName, Project project, int selectionStartPostion, int selectionEndPosition)
		{
			FileName = fileName;
			Project = project;
			SelectionStartPosition = selectionStartPostion;
			SelectionEndPosition = selectionEndPosition;
		}
		
		public TextReader ReadString ()
		{
			return ReadString (false);
		}

		public TextReader ReadString (bool readBinaryFiles)
		{
			if (buffer != null) {
				return new StringReader (buffer.ToString ());
			} else {
				Document doc = null;

				var task = SearchDocument ();
				if (task.Wait (1000)) 
					doc = task.Result;

				if (doc != null && doc.Editor != null) {
					return doc.Editor.CreateReader ();
				} else {
					return GetReaderForFileName (readBinaryFiles);
				}
			}
		}

		internal TextReader GetReaderForFileName (bool readBinaryFiles = false)
		{
			try {
				if (!File.Exists (FileName))
					return null;
				if (!readBinaryFiles && TextFileUtility.IsBinary (FileName))
					return null;
				return TextFileUtility.OpenStream (FileName, out hadBom);
			} catch (Exception e) {
				LoggingService.LogError ("Error while opening " + FileName, e);
				return null;
			}
		}

		async Task<Document> SearchDocument ()
		{
			string fullPath = Path.GetFullPath (FileName);
			return await Runtime.RunInMainThread (() => IdeApp.Workbench.Documents.FirstOrDefault (d => !string.IsNullOrEmpty (d.FileName) && Path.GetFullPath (d.FileName) == fullPath));
		}

		Document document;
		StringBuilder buffer = null;
		bool somethingReplaced;
		IDisposable undoGroup;
		bool hadBom;
		Encoding encoding;

		public async void BeginReplace (string content)
		{
			somethingReplaced = false;
			buffer = new StringBuilder (content);
			document = await SearchDocument ();
			if (document != null) {
				Gtk.Application.Invoke (delegate {
					undoGroup = document.Editor.OpenUndoGroup ();
				});
				return;
			}
		}
		
		public void Replace (int offset, int length, string replacement)
		{
			somethingReplaced = true;
			buffer.Remove (offset, length);
			buffer.Insert (offset, replacement);
			if (document != null) {
				Gtk.Application.Invoke (delegate {
					document.Editor.ReplaceText (offset, length, replacement);
				});
				return;
			}
		}
		
		public void EndReplace ()
		{
			if (document != null) {
				Gtk.Application.Invoke (delegate { 
					if (undoGroup != null) {
						undoGroup.Dispose ();
						undoGroup = null;
					}
					/*document.Editor.Document.CommitUpdateAll (); */
				});
				return;
			}
			if (buffer != null && somethingReplaced) {
				object attributes = DesktopService.GetFileAttributes (FileName);
				TextFileUtility.WriteText (FileName, buffer.ToString (), encoding ?? Encoding.UTF8, hadBom);
				DesktopService.SetFileAttributes (FileName, attributes);
			}
			FileService.NotifyFileChanged (FileName);
			buffer = null;
		}
	}
}
