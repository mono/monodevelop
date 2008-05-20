using System;

namespace Stetic.Editor {

	public class StockItem: BaseImageCell
	{
		string label;
		
		protected override void Initialize ()
		{
			base.Initialize ();
			string name = (string)Value;
			if (name != null) {
				Stetic.ObjectWrapper w = Stetic.ObjectWrapper.Lookup (Instance);
				Stetic.IProject project = w.Project;
				Gdk.Pixbuf px = project.IconFactory.RenderIcon (project, name, ImageSize);
				if (px != null) {
					Image = px;
					label = name;
					return;
				}
				
				Gtk.StockItem item = Gtk.Stock.Lookup (name);
				label = item.Label != null && item.Label.Length > 0 ? item.Label : name;
				label = label.Replace ("_", "");
				
				Gtk.IconSet iset = Gtk.IconFactory.LookupDefault (name);
				if (iset == null)
					Image = WidgetUtils.MissingIcon;
				else
					Image = iset.RenderIcon (new Gtk.Style (), Gtk.TextDirection.Ltr, Gtk.StateType.Normal, Gtk.IconSize.Menu, null, "");
			} else {
				Image = null;
				label = "";
			}
		}
		
		protected override string GetValueText ()
		{
			return label;
		}

		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new StockItemEditor ();
		}
	}
	
/*	[PropertyEditor ("StockId", "Changed")]
	public class StockItemEditor : Image {

		public StockItemEditor () : base (true, false) { }
		
		public override object Value {
			get { return StockId; }
			set { StockId = (string) value; }
		}
	}
	*/
	
	public class StockItemEditor: Gtk.HBox, IPropertyEditor
	{
		Gtk.Image image;
		Gtk.Entry entry;
		Gtk.Button button;
		string icon;
		IProject project;
		Gtk.Frame imageFrame;
		
		public StockItemEditor()
		{
			Spacing = 3;
			imageFrame = new Gtk.Frame ();
			imageFrame.Shadow = Gtk.ShadowType.In;
			imageFrame.BorderWidth = 2;
			PackStart (imageFrame, false, false, 0);

			image = new Gtk.Image (Gnome.Stock.Blank, Gtk.IconSize.Button);
			imageFrame.Add (image);
			
			entry = new Gtk.Entry ();
			entry.Changed += OnTextChanged;
			entry.HasFrame = false;
			PackStart (entry, true, true, 0);

			button = new Gtk.Button ();
			button.Add (new Gtk.Arrow (Gtk.ArrowType.Down, Gtk.ShadowType.Out));
			PackStart (button, false, false, 0);
			button.Clicked += button_Clicked;
			ShowAll ();
		}

		void button_Clicked (object obj, EventArgs args)
		{
			IconSelectorMenu menu = new IconSelectorMenu (project);
			menu.IconSelected += OnStockSelected;
			menu.ShowAll ();
			menu.Popup (null, null, new Gtk.MenuPositionFunc (OnDropMenuPosition), 3, Gtk.Global.CurrentEventTime);
		}
		
		void OnDropMenuPosition (Gtk.Menu menu, out int x, out int y, out bool pushIn)
		{
			button.ParentWindow.GetOrigin (out x, out y);
			x += button.Allocation.X;
			y += button.Allocation.Y + button.Allocation.Height;
			pushIn = true;
		}
		
		void OnStockSelected (object s, IconEventArgs args)
		{
			Value = args.IconId;
		}
		
		void OnTextChanged (object s, EventArgs a)
		{
			if (entry.Text.Length == 0)
				Value = null;
			else
				Value = entry.Text;
		}
		
		// Called once to initialize the editor.
		public void Initialize (PropertyDescriptor prop)
		{
			if (prop.PropertyType != typeof(string))
				throw new ApplicationException ("StockItem editor does not support editing values of type " + prop.PropertyType);
		}
		
		// Called when the object to be edited changes.
		public void AttachObject (object obj)
		{
			Stetic.ObjectWrapper w = Stetic.ObjectWrapper.Lookup (obj);
			project = w.Project;
		}
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		public object Value {
			get { return icon; }
			set {
				icon = (string) value;
				if (icon != null && icon.Length > 0) {
					entry.Text = icon;
					Gdk.Pixbuf px = project.IconFactory.RenderIcon (project, icon, Gtk.IconSize.Menu);
					if (px == null)
						px = WidgetUtils.LoadIcon (icon, Gtk.IconSize.Menu);
					image.Pixbuf = px;
					imageFrame.Show ();
				} else {
					imageFrame.Hide ();
					entry.Text = "";
				}
				
				if (ValueChanged != null)
					ValueChanged (this, EventArgs.Empty);
			}
		}

		// To be fired when the edited value changes.
		public event EventHandler ValueChanged;
	}
	
}
