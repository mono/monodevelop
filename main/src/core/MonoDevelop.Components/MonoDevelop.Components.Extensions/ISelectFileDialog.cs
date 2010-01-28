// 
// ISelectFileDialog.cs
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

namespace MonoDevelop.Components.Extensions
{
	/// <summary>
	/// This interface can be implemented to provide a custom implementation
	/// for the SelectFileDialog dialog.
	/// </summary>
	public interface ISelectFileDialogHandler
	{
		bool Run (SelectFileDialogData data);
	}
	
	/// <summary>
	/// Data for the ISelectFileDialogHandler implementation
	/// </summary>
	public class SelectFileDialogData: PlatformDialogData
	{
		public Gtk.FileChooserAction Action { get; set; }
		public string CurrentFolder { get; set; }
		public bool SelectMultiple { get; set; }
		public string[] SelectedFiles { get; set; }
		public string InitialFileName { get; set; }
	}
	
	/// <summary>
	/// Generic class to be used to implement file selectors.
	/// The T type argument is the type of the handler.
	/// The U type is the type of the data parameter (must subclass SelectFileDialogData)
	/// </summary>
	public class SelectFileDialog<T,U>: PlatformDialog<T,U> where U:SelectFileDialogData, new()
	{
		/// <summary>
		/// Action to perform with the file dialog.
		/// </summary>
		public Gtk.FileChooserAction Action {
			get { return data.Action; }
			set { data.Action = value; }
		}
		
		/// <summary>
		/// Folder to show by default.
		/// </summary>
		public string CurrentFolder {
			get { return data.CurrentFolder; }
			set { data.CurrentFolder = value; }
		}
		
		/// <summary>
		/// Set to True to allow multiple selection.
		/// </summary>
		public bool SelectMultiple {
			get { return data.SelectMultiple; }
			set { data.SelectMultiple = value; }
		}
		
		/// <summary>
		/// List of selected files (or folders).
		/// </summary>
		public string[] SelectedFiles {
			get { return data.SelectedFiles; }
		}
		
		/// <summary>
		/// Selected file (or folder) when using single selection mode.
		/// </summary>
		public string SelectedFile {
			get { return data.SelectedFiles.Length > 0 ? data.SelectedFiles [0] : null; }
		}
		
		/// <summary>
		/// File name to show by default.
		/// </summary>
		public string InitialFileName {
			get { return data.InitialFileName; }
			set { data.InitialFileName = value; }
		}
		
		/// <summary>
		/// Runs the default implementation of the dialog.
		/// </summary>
		protected bool RunDefault ()
		{
			FileSelector fdiag  = new FileSelector (data.Title, data.Action);
			fdiag.SelectMultiple = data.SelectMultiple;
			fdiag.TransientFor = data.TransientFor;
            if (!string.IsNullOrEmpty(data.CurrentFolder))
                fdiag.SetCurrentFolder(data.CurrentFolder);
            if (!string.IsNullOrEmpty(data.InitialFileName))
				fdiag.SetFilename (data.InitialFileName);
			
			try {
				int result = fdiag.Run ();
				data.SelectedFiles = fdiag.Filenames;
				return result == (int) Gtk.ResponseType.Ok;
			} finally {
				fdiag.Destroy ();
			}
		}
	}
}
