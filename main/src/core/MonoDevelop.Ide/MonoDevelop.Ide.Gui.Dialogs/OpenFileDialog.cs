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
using System.Collections.Generic;
using Mono.Addins;

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
		
		public OpenFileDialog (string title) : this (title, Gtk.FileChooserAction.Save)
		{
		}
		
		public OpenFileDialog (string title, Gtk.FileChooserAction action)
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
				return Handler.Run (data);
			else
				return RunDefault ();
		}
		
		bool RunDefault ()
		{	
			var win = new FileSelectorDialog (Title, Action);
			win.Encoding = Encoding;
			win.ShowEncodingSelector = ShowEncodingSelector;
			win.ShowViewerSelector = ShowViewerSelector;
			
			//FIXME: should these be optional? they were hardcoded into the dialog - at least they're factored out now
			AddDefaultFileFilters ();
			LoadDefaultFilter ();
			
			SetDefaultProperties (win);
			
			try {
				var result = MessageService.RunCustomDialog (win, TransientFor ?? MessageService.RootWindow);
				if (result == (int) Gtk.ResponseType.Ok) {
					GetDefaultProperties (win);
					data.Encoding = win.Encoding;
					data.CloseCurrentWorkspace = win.CloseCurrentWorkspace;
					data.SelectedViewer = win.SelectedViewer;
					SaveDefaultFilter ();
					return true;
				} else
					return false;
			} finally {
				win.Destroy ();
			}
		}
		
		/// <summary>
		/// Adds the default file filters registered by MD core and addins. Includes the All Files filter.
		/// </summary>
		void AddDefaultFileFilters ()
		{
			foreach (var f in GetDefaultFilters ())
				data.Filters.Add (f);
			AddAllFilesFilter ();
		}
		
		//loads last default filter from MD prefs
		void LoadDefaultFilter ()
		{
			// Load last used filter pattern
			var lastPattern = PropertyService.Get ("Monodevelop.FileSelector.LastPattern", "*");
			foreach (var filter in Filters) {
				if (filter.Patterns != null && filter.Patterns.Contains (lastPattern)) {
					DefaultFilter = filter;
					break;
				}
			}
		}
		
		//saves last default filter to MD prefs
		void SaveDefaultFilter ()
		{
			// Save active filter
			//it may be null if e.g. SetSelectedFile was used
			if (DefaultFilter != null && DefaultFilter.Patterns != null && DefaultFilter.Patterns.Count > 0)
				PropertyService.Set ("Monodevelop.FileSelector.LastPattern", DefaultFilter.Patterns[0]);
		}
		
		static IEnumerable<SelectFileDialogFilter> GetDefaultFilters ()
		{
			foreach (var f in ParseFilters (AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectFileFilters")))
				yield return f;
			foreach (var f in ParseFilters (AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/FileFilters")))
				yield return f;
		}
		
		static IEnumerable<SelectFileDialogFilter> ParseFilters (System.Collections.IEnumerable filterStrings)
		{
			if (filterStrings == null)
				yield break;
			foreach (string filterStr in filterStrings) {
				var parts = filterStr.Split ('|');
				var f = new SelectFileDialogFilter (parts[0], parts[1].Split (';'));
				yield return f;
			}
		}
	}
}
