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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;

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
		List<SelectFileDialogFilter> filters = new List<SelectFileDialogFilter> ();
		public Gtk.FileChooserAction Action { get; set; }
		public List<SelectFileDialogFilter> Filters { get { return filters; } }
		public FilePath CurrentFolder { get; set; }
		public bool SelectMultiple { get; set; }
		public FilePath[] SelectedFiles { get; set; }
		public FilePath InitialFileName { get; set; }
	}
	
	/// <summary>
	/// Filter option to be displayed in file selector dialogs.
	/// </summary>
	public class SelectFileDialogFilter
	{
		public SelectFileDialogFilter (string name, params string[] patterns)
		{
			this.Name = name;
			this.Patterns = patterns;
		}
		
		/// <summary>Label for the filter</summary>
		public string Name { get; private set; }
		
		/// <summary>Filename glob patterns permitted by this filter</summary>
		public IList<string> Patterns { get; private set; }
		
		/// <summary>MIME types permitted by this filter</summary>
		public IList<string> MimeTypes { get; private set; }
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
		public FilePath CurrentFolder {
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
		public FilePath[] SelectedFiles {
			get { return data.SelectedFiles; }
		}
		
		/// <summary>
		/// Selected file (or folder) when using single selection mode.
		/// </summary>
		public FilePath SelectedFile {
			get { return data.SelectedFiles.Length > 0 ? data.SelectedFiles [0] : null; }
		}
		
		/// <summary>
		/// File name to show by default.
		/// </summary>
		public string InitialFileName {
			get { return data.InitialFileName; }
			set { data.InitialFileName = value; }
		}
		
		public void AddFilter (string label, params string[] patterns)
		{
			AddFilter (new SelectFileDialogFilter (label, patterns));
		}
		
		public void AddFilter (SelectFileDialogFilter filter)
		{
			data.Filters.Add (filter);
		}
		
		public void AddAllFilesFilter ()
		{
			AddFilter (GettextCatalog.GetString ("All Files"), "*.*");
		}
		
		/// <summary>
		/// Runs the default implementation of the dialog.
		/// </summary>
		protected bool RunDefault ()
		{
			FileSelector fdiag  = new FileSelector (data.Title, data.Action) {
				SelectMultiple = data.SelectMultiple,
				TransientFor = data.TransientFor,
			};
			if (!data.CurrentFolder.IsNullOrEmpty)
				fdiag.SetCurrentFolder (data.CurrentFolder);
			if (!data.InitialFileName.IsNullOrEmpty)
				fdiag.SetFilename (data.InitialFileName);
			
			foreach (var filter in GetGtkFileFilters ())
				fdiag.AddFilter (filter);
			
			try {
				int result = MessageService.RunCustomDialog (fdiag, data.TransientFor ?? MessageService.RootWindow);
				data.SelectedFiles = fdiag.Filenames.ToFilePathArray ();
				return result == (int) Gtk.ResponseType.Ok;
			} finally {
				fdiag.Destroy ();
			}
		}
		
		protected IEnumerable<Gtk.FileFilter> GetGtkFileFilters ()
		{
			foreach (var filter in data.Filters) {
				var gf = new Gtk.FileFilter ();
				if (!string.IsNullOrEmpty (filter.Name))
					gf.Name = filter.Name;
				if (filter.Patterns != null)
					foreach (var pattern in filter.Patterns)
						gf.AddPattern (pattern);
				if (filter.MimeTypes != null)
					foreach (var mimetype in filter.MimeTypes)
						gf.AddMimeType (mimetype);
				yield return gf;
			}
		}
	}
}
