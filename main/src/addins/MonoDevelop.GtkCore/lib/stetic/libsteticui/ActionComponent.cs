
using System;

namespace Stetic
{
	public class ActionComponent: Component
	{
		Gdk.Pixbuf icon;
		string label;
		
		internal static Gdk.Pixbuf DefaultActionIcon;
		
		static ActionComponent ()
		{
			DefaultActionIcon = Gdk.Pixbuf.LoadFromResource ("action.png");
		}
		
		public ActionComponent (Application owner, object backend, string name): base (owner, backend, name, owner.GetComponentType ("Gtk.Action"))
		{
		}
		
		public override string Name {
			get {
				if (name == null)
					name = ((Wrapper.Action)backend).Name;
				return name;
			}
			set {
				name = value;
				((Wrapper.Action)backend).Name = value;
			}
		}

		public string Label {
			get {
				if (label == null)
					label = ((Wrapper.Action)backend).Label;
				return label;
			}
			set {
				label = value;
				((Wrapper.Action)backend).Label = value;
			}
		}
		
		protected override void OnChanged ()
		{
			name = null;
			icon = null;
			label = null;
			base.OnChanged ();
		}
		
		public Gdk.Pixbuf Icon {
			get {
				if (icon == null) {
					byte[] data = app.Backend.GetActionIcon ((Wrapper.Action)backend);
					if (data == null)
						return DefaultActionIcon;
					else
						icon = new Gdk.Pixbuf (data);
				}
				return icon;
			}
		}
		
		public ActionGroupComponent ActionGroup {
			get { return (ActionGroupComponent) app.GetComponent (((Wrapper.Action)backend).ActionGroup, null, null); }
		}
	}
}
