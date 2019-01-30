//
// Workbench.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components.DockNotebook;
using System.Text;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Ide.Gui
{
	public class DocumentOpenInformation
	{
		public OpenDocumentOptions Options { get; set; } = OpenDocumentOptions.Default;

		public WorkspaceObject Owner { get; set; }

		/// <summary>
		/// Is true when the file is already open and reload is requested.
		/// </summary>
		public bool IsReloadOperation { get; set; }

		internal IShellNotebook DockNotebook { get; set; }

		internal DocumentController DocumentController { get; set; }
		internal DocumentControllerDescription DocumentControllerDescription { get; set; }
	}

	public class FileOpenInformation: DocumentOpenInformation
	{
		FilePath fileName;
		public FilePath FileName {
			get {
				return fileName;
			}
			set {
				fileName = value.CanonicalPath.ResolveLinks ();
				if (fileName.IsNullOrEmpty)
					LoggingService.LogError ("FileName == null\n" + Environment.StackTrace);
			}
		}

		internal FilePath OriginalFileName { get; set; }

		int offset = -1;
		public int Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		public int Line { get; set; }
		public int Column { get; set; }
		public Encoding Encoding { get; set; }

		[Obsolete ("Use FileOpenInformation (FilePath filePath, Project project, int line, int column, OpenDocumentOptions options)")]
		public FileOpenInformation (string fileName, int line, int column, OpenDocumentOptions options)
		{
			this.OriginalFileName = fileName;
			this.FileName = fileName;
			this.Line = line;
			this.Column = column;
			this.Options = options;

		}

		public FileOpenInformation (FilePath filePath, WorkspaceObject project = null)
		{
			this.OriginalFileName = filePath;
			this.FileName = filePath;
			this.Owner = project;
		}

		public FileOpenInformation (FilePath filePath, Project project, int line, int column, OpenDocumentOptions options)
		{
			this.OriginalFileName = filePath;
			this.FileName = filePath;
			this.Owner = project;
			this.Line = line;
			this.Column = column;
			this.Options = options;
		}

		public FileOpenInformation (FilePath filePath, Project project, bool bringToFront)
		{
			this.OriginalFileName = filePath;
			this.FileName = filePath;
			this.Owner = project;
			this.Options = OpenDocumentOptions.Default;
			if (bringToFront) {
				this.Options |= OpenDocumentOptions.BringToFront;
			} else {
				this.Options &= ~OpenDocumentOptions.BringToFront;
			}
		}

		static FilePath ResolveSymbolicLink (FilePath fileName)
		{
			if (fileName.IsEmpty)
				return fileName;
			try {
				var alreadyVisted = new HashSet<FilePath> ();
				while (true) {
					if (alreadyVisted.Contains (fileName)) {
						LoggingService.LogError ("Cyclic links detected: " + fileName);
						return FilePath.Empty;
					}
					alreadyVisted.Add (fileName);
					var linkInfo = new Mono.Unix.UnixSymbolicLinkInfo (fileName);
					if (linkInfo.IsSymbolicLink && linkInfo.HasContents) {
						FilePath contentsPath = linkInfo.ContentsPath;
						if (contentsPath.IsAbsolute) {
							fileName = linkInfo.ContentsPath;
						} else {
							fileName = fileName.ParentDirectory.Combine (contentsPath);
						}
						fileName = fileName.CanonicalPath;
						continue;
					}
					return ResolveSymbolicLink (fileName.ParentDirectory).Combine (fileName.FileName).CanonicalPath;
				}
			} catch (Exception) {
				return fileName;
			}
		}
	}
}
