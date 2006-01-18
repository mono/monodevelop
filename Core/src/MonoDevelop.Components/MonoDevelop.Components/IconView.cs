using System;
using System.Collections;

using Gtk;
using Gnome;

namespace MonoDevelop.Components {
	public class IconView : ScrolledWindow {
		IconList iconList;
		
		Hashtable userData = new Hashtable ();
	
		object currentlySelected;
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
		
		public object CurrentlySelected {
			get {
				return currentlySelected;
			}
			set {
				foreach (DictionaryEntry de in userData) {
					if (de.Value == value) {
						iconList.SelectIcon ((int)de.Key);
						return;
					}
				}
			}
		}
		
		void HandleKeyPressed (object o, KeyPressEventArgs args)
		{
			if (currentlySelected == null)
				return;
			
			if (args.Event.Key == Gdk.Key.Return && IconDoubleClicked != null)
				IconDoubleClicked (this, EventArgs.Empty);
		}
		
		void HandleIconSelected (object o, IconSelectedArgs args)
		{
			currentlySelected = userData [args.Num];
			
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
