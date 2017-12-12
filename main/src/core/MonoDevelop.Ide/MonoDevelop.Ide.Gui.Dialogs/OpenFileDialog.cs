// 
// FileOpenDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Components;
using MonoDevelop.Components.Extensions;
using System.Collections.Generic;
using Mono.Addins;
using System.Text;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	/// <summary>
	/// Dialog which allows selecting a file to be opened or saved
	/// </summary>
	public class OpenFileDialog: SelectFileDialog<OpenFileDialogData>
	{
		public OpenFileDialog ()
		{
		}
		
		public OpenFileDialog (string title) : this (title, FileChooserAction.Save)
		{
		}
		
		public OpenFileDialog (string title, FileChooserAction action)
		{
			Title = title;
			Action = action;
		}
		
		/// <summary>
		/// Set to true if the encoding selector has to be shown
		/// </summary>
		public bool ShowEncodingSelector {
			get { return data.ShowEncodingSelector; }
			set { data.ShowEncodingSelector = value; }
		}
		
		/// <summary>
		/// Set to true if the viewer selector has to be shown
		/// </summary>
		public bool ShowViewerSelector {
			get { return data.ShowViewerSelector; }
			set { data.ShowViewerSelector = value; }
		}
		
		/// <summary>
		/// Selected encoding.
		/// </summary>
		public Encoding Encoding {
			get { return data.Encoding; }
			set { data.Encoding = value; }
		}
		
		/// <summary>
		/// Set to true if the workspace has to be closed before opening a solution.
		/// </summary>
		public bool CloseCurrentWorkspace {
			get { return data.CloseCurrentWorkspace; }
		}
		
		/// <summary>
		/// Selected viewer.
		/// </summary>
		public FileViewer SelectedViewer {
			get { return data.SelectedViewer; }
		}
		
		protected override bool RunDefault ()
		{
			var win = new FileSelectorDialog (Title, Action.ToGtkAction ());
			win.SelectedEncoding = Encoding != null ? Encoding.CodePage : 0;
			win.ShowEncodingSelector = ShowEncodingSelector;
			win.ShowViewerSelector = ShowViewerSelector;
			bool pathAlreadySet = false;
			win.CurrentFolderChanged += (s, e) => {
				var selectedPath = data.OnDirectoryChanged (this, win.CurrentFolder);
				if (selectedPath.IsNull)
					return;
				data.SelectedFiles = new FilePath [] { selectedPath };
				pathAlreadySet = true;
				win.Respond (Gtk.ResponseType.Cancel);
			};
			
			SetDefaultProperties (win);
			
			try {
				var result = MessageService.RunCustomDialog (win, TransientFor ?? MessageService.RootWindow);
				if (result == (int)Gtk.ResponseType.Ok) {
					GetDefaultProperties (win);
					data.Encoding = win.SelectedEncoding > 0 ? Encoding.GetEncoding (win.SelectedEncoding) : null;
					data.CloseCurrentWorkspace = win.CloseCurrentWorkspace;
					data.SelectedViewer = win.SelectedViewer;
					return true;
				} else
					return pathAlreadySet;
			} finally {
				win.Destroy ();
				win.Dispose ();
			}
		}
	}
}
