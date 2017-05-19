//
// BaseFileEntry.cs
//
// Author:
//   Todd Berman
//
// Copyright (C) 2004 Todd Berman
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
using System.IO;
using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Components
{
	public abstract class BaseFileEntry : Gtk.HBox
	{
		readonly Entry pathEntry;
		readonly Button browseButton;
		bool loading, displayAsRelativePath;
		readonly FileFilterSet filterSet = new FileFilterSet ();
		
		public event EventHandler PathChanged;
		
		protected BaseFileEntry (string name) : base (false, 6)
		{
			this.BrowserTitle = name;
			pathEntry = new Entry ();
			browseButton = Button.NewWithMnemonic (GettextCatalog.GetString ("_Browse..."));
			browseButton.SetCommonAccessibilityAttributes ("FileEntry.Browse",
			                                               GettextCatalog.GetString ("Browse"),
			                                               GettextCatalog.GetString ("Select a folder for the entry"));
			
			pathEntry.Changed += OnTextChanged;
			browseButton.Clicked += OnButtonClicked;
			
			PackStart (pathEntry, true, true, 0);
			PackEnd (browseButton, false, false, 0);
		}
		
		protected abstract string ShowBrowseDialog (string name, string startIn);
		
		public string BrowserTitle { get; set; }
		
		public string DefaultPath { get; set; }

		public bool DisplayAsRelativePath {
			get {
				return displayAsRelativePath;
			}
			set {
				if (value == displayAsRelativePath)
					return;
				displayAsRelativePath = value;
				//if there's a value, use setter to make it relative/absolute
				var p = Path;
				if (!string.IsNullOrEmpty (p))
					Path = p;
			}
		}
		
		public virtual Gtk.Window TransientFor {
			get;
			set;
		}
		
		protected Gtk.Window GetTransientFor ()
		{
			return TransientFor ?? Toplevel as Gtk.Window;
		}

		public new string Path {
			get {
				try {
					FilePath path = pathEntry.Text;
					if (path.IsAbsolute || path.IsNullOrEmpty || string.IsNullOrEmpty (DefaultPath))
						return path;
					return path.ToAbsolute (DefaultPath);
				} catch {
					// If path conversion fails for some reason, return the plain text
					return pathEntry.Text;
				}
			}
			set {
				loading = true;
				if (!string.IsNullOrEmpty (value) && displayAsRelativePath)
					value = ((FilePath)value).ToRelative (DefaultPath);
				pathEntry.Text = value ?? "";
				loading = false;
			}
		}
		
		public FileFilterSet FileFilters {
			get { return filterSet; }
		}
		
		void OnButtonClicked (object o, EventArgs args)
		{
			FilePath startIn = Path;

			try {
				if (!startIn.IsNullOrEmpty && !startIn.IsDirectory) {
					startIn = startIn.ParentDirectory;
					if (!startIn.IsNullOrEmpty && !startIn.IsDirectory)
						startIn = FilePath.Null;
				}
			} catch (FileNotFoundException) {
				startIn = FilePath.Null;
			}

			if (startIn.IsNull && !string.IsNullOrEmpty (DefaultPath) && Directory.Exists (DefaultPath))
				startIn = DefaultPath;

			string path = ShowBrowseDialog (BrowserTitle, startIn);
			if (path != null) {
				Path = path;
				// Path setter suppresses change events so fire the event ourselves
				OnTextChanged (null, null);
			}
		}
		
		void OnTextChanged (object o, EventArgs args)
		{
			if (!loading && PathChanged != null)
				PathChanged (this, EventArgs.Empty);
		}

		// Accessibility
		public void SetEntryAccessibilityAttributes (string name, string label, string help)
		{
			pathEntry.SetCommonAccessibilityAttributes (name, label, help);
		}

		public Atk.Object EntryAccessible {
			get {
				return pathEntry.Accessible;
			}
		}
	}
}
