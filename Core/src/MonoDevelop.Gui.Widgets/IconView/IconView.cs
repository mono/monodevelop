using System;
using System.Collections;

using Gtk;
using Gnome;

namespace MonoDevelop.Gui.Widgets {
	public class IconView : ScrolledWindow {
		IconList iconList;
		
		Hashtable userData = new Hashtable ();
	
		public object CurrentlySelected;
		public event EventHandler IconSelected;
		public event EventHandler IconDoubleClicked;
				
		public IconView ()
		{
			iconList = new IconList (100, null, 0);
			iconList.IconSelected += new IconSelectedHandler (HandleIconSelected);
			iconList.KeyPressEvent += new KeyPressEventHandler (HandleKeyPressed);
			
			this.Add (iconList);
			this.WidthRequest = 350;
			this.HeightRequest = 200;
			this.ShadowType = Gtk.ShadowType.In;
		}

		public void AddIcon (Image icon, string name, object obj)
		{
			int itm = iconList.AppendPixbuf (icon.Pixbuf, "/dev/null", name);
			userData.Add (itm, obj);
		}
		
		public void AddIcon (string stock, Gtk.IconSize sz, string name, object obj)
		{
			int itm = iconList.AppendPixbuf (iconList.RenderIcon (stock, sz, ""), "/dev/null", name);
			userData.Add (itm, obj);
		}
		
		void HandleKeyPressed (object o, KeyPressEventArgs args)
		{
			if (CurrentlySelected == null)
				return;
			
			if (args.Event.Key == Gdk.Key.Return && IconDoubleClicked != null)
				IconDoubleClicked (this, EventArgs.Empty);
		}
		
		void HandleIconSelected (object o, IconSelectedArgs args)
		{
			CurrentlySelected = userData [args.Num];
			
			if (IconSelected != null)
				IconSelected (this, EventArgs.Empty);

			if (args.Event != null && args.Event.Type == Gdk.EventType.TwoButtonPress)
				if (IconDoubleClicked != null)
					IconDoubleClicked (this, EventArgs.Empty);
		}

		public void Clear ()
		{
			iconList.Clear ();
			userData.Clear ();
		}
	}
}
