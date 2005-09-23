using System;
using System.IO;
using Gtk;
using Gdk;

using MonoDevelop.Services;

namespace MonoDevelop.Gui.Widgets {
	public abstract class BaseFileEntry : Gtk.HBox {
		
		string name;
		
		Entry text;
		Button browse;
		
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
		
		
		string default_path;
		public string DefaultPath {
			get { return default_path; }
			set { default_path = value; }
		}
		
		public new string Path {
			get { return System.IO.Path.Combine (default_path, text.Text); }
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
				OnTextChanged (null, null);
			}
		}
		
		void OnTextChanged (object o, EventArgs args)
		{
			if (PathChanged != null)
				PathChanged (this, EventArgs.Empty);
		}
	}
}
