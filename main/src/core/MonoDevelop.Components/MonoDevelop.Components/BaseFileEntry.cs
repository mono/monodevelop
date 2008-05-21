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

namespace MonoDevelop.Components {
	public abstract class BaseFileEntry : Gtk.HBox {
		
		string name;
		
		Entry text;
		Button browse;
		bool loading;
		
		public event EventHandler PathChanged;
		
		protected BaseFileEntry (string name) : base (false, 6)
		{
			this.name = name;
			text = new Entry ();
			browse = Button.NewWithMnemonic (GettextCatalog.GetString ("_Browse..."));
			
			text.Changed += new EventHandler (OnTextChanged);
			browse.Clicked += new EventHandler (OnButtonClicked);
			
			PackStart (text, true, true, 0);
			PackEnd (browse, false, false, 0);
		}
		
		protected abstract string ShowBrowseDialog (string name, string start_in);
		
		public string BrowserTitle {
			get { return name; }
			set { name = value; }
		}
		
		string default_path;
		public string DefaultPath {
			get { return default_path; }
			set { default_path = value; }
		}
		
		public new string Path {
			get {
				return default_path != null && text.Text.Length > 0 ? System.IO.Path.Combine (default_path, text.Text) : text.Text;
			}
			set {
				loading = true; 
				text.Text = value;
				loading = false;
			}
		}
		
		void OnButtonClicked (object o, EventArgs args)
		{
			string start_in;
			
			if (Directory.Exists (Path))
				start_in = Path;
			else
				start_in = default_path;
			
			string path = ShowBrowseDialog (name, start_in + System.IO.Path.DirectorySeparatorChar);
			if (path != null) {
				text.Text = path;
			}
		}
		
		void OnTextChanged (object o, EventArgs args)
		{
			if (!loading && PathChanged != null)
				PathChanged (this, EventArgs.Empty);
		}
	}
}
