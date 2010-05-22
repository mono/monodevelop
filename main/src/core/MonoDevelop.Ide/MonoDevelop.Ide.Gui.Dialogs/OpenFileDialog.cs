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
using MonoDevelop.Components.Extensions;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	/// <summary>
	/// Dialog which allows selecting a file to be opened or saved
	/// </summary>
	public class OpenFileDialog: SelectFileDialog<IOpenFileDialogHandler, OpenFileDialogData>
	{
		public OpenFileDialog ()
		{
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
		public string Encoding {
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
		
		/// <summary>
		/// Shows the dialog
		/// </summary>
		/// <returns>
		/// True if the user clicked OK.
		/// </returns>
		public bool Run ()
		{
			if (Handler != null)
				Handler.Run (data);
			
			FileSelectorDialog win = new FileSelectorDialog (data.Title, data.Action);
			win.Encoding = data.Encoding;
			win.ShowEncodingSelector = data.ShowEncodingSelector;
			win.ShowViewerSelector = data.ShowViewerSelector;
			
			foreach (var filter in GetGtkFileFilters ())
				win.AddFilter (filter);
			
			if (win.Run () == (int) Gtk.ResponseType.Ok) {
				data.Encoding = win.Encoding;
				data.CloseCurrentWorkspace = win.CloseCurrentWorkspace;
				data.SelectedFiles = win.Filenames.ToFilePathArray ();
				data.SelectedViewer = win.SelectedViewer;
				return true;
			} else
				return false;
		}
	}
}
