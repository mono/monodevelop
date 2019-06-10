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
using MonoDevelop.Ide.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Ide.FindInFiles
{
	class OpenFileProvider : FileProvider
	{
		readonly ITextBuffer textBuffer;
		ITextEdit textEdit;

		public OpenFileProvider (ITextBuffer textBuffer, Project project, string fileName) : this(textBuffer, project, fileName, - 1, -1)
		{
		}

		public OpenFileProvider (ITextBuffer textBuffer, Project project, string fileName, int selectionStartPostion, int selectionEndPosition) : base (fileName, project, selectionStartPostion, selectionEndPosition)
		{
			this.textBuffer = textBuffer;
		}

		public override TextReader ReadString (bool readBinaryFiles)
		{
			return new Microsoft.VisualStudio.Platform.NewTextSnapshotToTextReader (textBuffer.CurrentSnapshot);
		}

		public override void BeginReplace (string content, Encoding encoding)
		{
			Gtk.Application.Invoke ((o, args) => {
				textEdit = textBuffer.CreateEdit ();
			});
		}

		public override void Replace (int offset, int offsetWithoutDelta, int length, string replacement)
		{
			Gtk.Application.Invoke ((o, args) => {
				textEdit.Replace (offsetWithoutDelta, length, replacement);
			});
		}

		public override void EndReplace ()
		{
			Gtk.Application.Invoke ((o, args) => {
				textEdit.Apply ();
				textEdit = null;
			});
		}
	}

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

		public Encoding CurrentEncoding { get; private set; }

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

		public virtual TextReader ReadString (bool readBinaryFiles)
		{
			if (buffer != null) {
				return new StringReader (buffer.ToString ());
			} else {
				var doc = SearchDocument ();
				var tb = doc?.GetContent<ITextBuffer> ();
				if (tb != null) {
					return new Microsoft.VisualStudio.Platform.NewTextSnapshotToTextReader (tb.CurrentSnapshot);
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
				var sr =  TextFileUtility.OpenStream (FileName);
				CurrentEncoding = sr.CurrentEncoding;
				return sr;
			} catch (Exception e) {
				LoggingService.LogError ("Error while opening " + FileName, e);
				return null;
			}
		}

		Document SearchDocument ()
		{
			string fullPath = Path.GetFullPath (FileName);
			return IdeServices.DocumentManager.Documents.FirstOrDefault (d => !string.IsNullOrEmpty (d.FileName) && Path.GetFullPath (d.FileName) == fullPath);
		}

		ITextBuffer textBuffer;
		StringBuilder buffer = null;
		bool somethingReplaced;
		ITextEdit textEdit;
		Encoding encoding;

		public virtual void BeginReplace (string content, Encoding encoding)
		{
			somethingReplaced = false;
			buffer = new StringBuilder (content);
			textBuffer = SearchDocument ()?.GetContent<ITextBuffer> ();
			this.encoding = encoding; 
			if (textBuffer != null) {
				Gtk.Application.Invoke ((o, args) => {
					textEdit = textBuffer.CreateEdit ();
				});
				return;
			}
		}
		
		public virtual void Replace (int offset, int offsetWithoutDelta, int length, string replacement)
		{
			somethingReplaced = true;
			buffer.Remove (offset, length);
			buffer.Insert (offset, replacement);
			if (textBuffer != null) {
				Gtk.Application.Invoke ((o, args) => {
					textEdit.Replace (offsetWithoutDelta, length, replacement);
				});
				return;
			}
		}
		
		public virtual void EndReplace ()
		{
			if (textBuffer != null) {
				Gtk.Application.Invoke ((o, args) => {
					textEdit.Apply ();
					textEdit = null;
				});
				return;
			}
			if (buffer != null && somethingReplaced) {
				object attributes = IdeServices.DesktopService.GetFileAttributes (FileName);
				TextFileUtility.WriteText (FileName, buffer.ToString (), encoding ?? Encoding.UTF8);
				IdeServices.DesktopService.SetFileAttributes (FileName, attributes);
			}
			buffer = null;
		}
	}
}
