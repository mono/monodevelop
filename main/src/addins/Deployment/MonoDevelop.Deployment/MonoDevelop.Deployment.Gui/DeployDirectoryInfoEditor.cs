
using System;
using System.ComponentModel;
using MonoDevelop.DesignerSupport.PropertyGrid;
using Gtk;
using Gdk;

namespace MonoDevelop.Deployment
{
	internal class DeployDirectoryInfoEditor: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			if (Value != null)
				return ((DeployDirectoryInfo)Value).Description;
			else
				return string.Empty;
		}
		
		protected override IPropertyEditor CreateEditor (Rectangle cell_area, StateType state)
		{
			return new DeployDirectoryInfoEditorWidget ();
		}
	}
	
	class DeployDirectoryInfoEditorWidget : Gtk.HBox, IPropertyEditor {

		Gtk.EventBox ebox;
		Gtk.ComboBoxEntry combo;
		DeployDirectoryInfo[] values;

		public DeployDirectoryInfoEditorWidget () : base (false, 0)
		{
		}
		
		public void Initialize (EditSession session)
		{
			values = DeployService.GetDeployDirectoryInfo ();
			ebox = new Gtk.EventBox ();
			ebox.Show ();
			PackStart (ebox, true, true, 0);

			combo = Gtk.ComboBoxEntry.NewText ();
			combo.Changed += combo_Changed;
			combo.Entry.IsEditable = false;
			combo.Entry.HasFrame = false;
			combo.Entry.HeightRequest = combo.SizeRequest ().Height;
			combo.Show ();
			ebox.Add (combo);

			foreach (DeployDirectoryInfo value in values) {
				combo.AppendText (value.Description);
			}
		}

		public object Value {
			get {
				if (combo.Active != -1)
					return values [combo.Active];
				else
					return null;
			}
			set {
				if (value == null)
					combo.Active = -1;
				else {
					int i = Array.IndexOf (values, value);
					if (i != -1)
						combo.Active = i;
					else
						combo.Active = -1;
				}
			}
		}

		public event EventHandler ValueChanged;

		void combo_Changed (object o, EventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
	}
}
